// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Framefield.Core
{
    public class MetaOperatorPart
    {
        public delegate OperatorPart.Function FunctionCreatorDelegate();
        #region Events
        public event EventHandler<EventArgs> ScriptChangedEvent;
        #endregion

        #region Properties
        public Guid ID { get; private set; }
        public Func<Guid, OperatorPart.Function, bool, string, OperatorPart> CreateFunc { get; set; } // ID * defaultFunction * isMultiInput * name
        public bool IsMultiInput { get; set; }
        public bool ScriptChanged { get; set; }
        public MetaOperator Parent { get; set; }

        public string Script
        {
            get { return _script; }
            set
            {
                if (Script == value)
                    return;

                UpdateVersion();

                _script = value;
            }
        }

        private void UpdateVersion()
        {
            if (!IsOpStoredInCore)
            {
                Version = Guid.NewGuid(); // set new version only if no core op
                Logger.Debug("Created new operator version: {0}", Version.ToString());
            }
        }

        private bool IsOpStoredInCore
        {
            get
            {
                var coreAssembly = GetAssemblyFromCurrentDomain("Core");
                var functorType = (from t in coreAssembly.GetTypes()
                                   where t.Namespace == ScriptNamespace
                                   where t.IsSubclassOf(typeof(OperatorPart.Function))
                                   select t).FirstOrDefault();
                return functorType != null;
            }
        }

        public IEnumerable<string> AdditionalAssemblies
        {
            get { return _additionalAssemblies; }
            set
            {
                _additionalAssemblies.Clear();
                _additionalAssemblies.AddRange(value);
                UpdateVersion();
            }
        }

        public FunctionCreatorDelegate FunctionCreator { get; private set; }
        public string Name { get; internal set; } 
        public FunctionType Type { get; set; }
        public Guid Version { get; set; }
        #endregion

        #region C'tors

        public MetaOperatorPart(Guid id)
        {
            ID = id;
            Version = Guid.NewGuid();
            FunctionCreator = () => Utilities.CreateValueFunction(new Float(0));
            ScriptAssembly = null;
            ScriptChanged = false;
        }
        #endregion

        public MetaOperatorPart Clone(string name) 
        {
            var opPart = new MetaOperatorPart(Guid.NewGuid())
                             {
                                 Version = Version,
                                 CreateFunc = CreateFunc,
                                 IsMultiInput = IsMultiInput,
                                 Type = Type,
                                 AdditionalAssemblies = AdditionalAssemblies
                             };

            opPart.Name = name;

            if (Script != null)
            {
                var nameWithoutEnding = name.EndsWith("Func") ? name.Substring(0, name.Length - 4) : name;
                opPart.Script = ScriptCodeDefinition.UpdateClassNameAndNamespaceInScript(opPart.ScriptNamespace, nameWithoutEnding, Script);
                opPart.Compile();
            }

            return opPart;
        }

        public OperatorPart CreateOpPart(Guid id)
        {
            var opPartFunction = FunctionCreator();
            if (opPartFunction == null)
                return CreateFunc(id, Utilities.CreateValueFunction(new Float(0.0f)), false, "");
            else
                return new OperatorPart(id, opPartFunction) { IsMultiInput = IsMultiInput, Name = Name, Type = Type };
        }

        public Assembly ScriptAssembly { get; private set; }
        public string ScriptNamespace { get { return "Framefield.Core.ID" + ID.ToString().Replace('-', '_'); } }
        public string SourceName { get { return Name + "_ID" + ID.ToString() + "_Version" + Version.ToString(); } }

        public CompilerErrorCollection Compile()
        {
            var coreAssembly = GetAssemblyFromCurrentDomain("Core");

            try
            {
                var assemblyToUse = GenerateAssembly(coreAssembly, SourceName, forceInMemoryGeneration: IsOpStoredInCore);
                var functorType = (from t in assemblyToUse.GetTypes()
                    where t.IsSubclassOf(typeof (OperatorPart.Function))
                    select t).First();
                FunctionCreator = () => assemblyToUse.CreateInstance(functorType.FullName) as OperatorPart.Function;
                ScriptAssembly = assemblyToUse;
                Logger.Info("Updating " + Name + " succeeded");
            }
            catch (ScriptCompilerException e)
            {
                Logger.Error("Failed to update operator {0}. The operator has been disabled.\n{1}", Name, e.ToString());
                FunctionCreator = () => Utilities.CreateValueFunction(new Float(0));
                ScriptAssembly = null;
                return e.CompilerResults.Errors;
            }
            catch (TargetInvocationException e)
            {
                Logger.Error("Failed to invoke a component on an instance of the compiled operator {0}. The operator has been disabled.\n{1}", Name, e.ToString());
                FunctionCreator = () => Utilities.CreateValueFunction(new Float(0));
                ScriptAssembly = null;
                return new CompilerErrorCollection();                
            }

            ScriptChanged = true;
            if (ScriptChangedEvent != null)
                ScriptChangedEvent(this, EventArgs.Empty);
            return new CompilerErrorCollection();
        }

        public void HandleDependencyOperator_ScriptChange(object sender, EventArgs e)
        {
            UpdateVersion();
            Compile();
        }

        private Assembly GenerateAssembly(Assembly coreAssembly, string sourceName, bool forceInMemoryGeneration)
        {
            var compiler = new ScriptCompiler(forceInMemoryGeneration);
            var sharpDxAssembly = GetAssemblyFromCurrentDomain("SharpDX");
            var jsonAssembly = GetAssemblyFromCurrentDomain("Newtonsoft.Json");
            var fixedAssemblies = new[] { "System.Core.dll", "System.Drawing.dll", coreAssembly.Location, sharpDxAssembly.Location, jsonAssembly.Location };

            var supplierAssemblies = new List<string>();
            if (Parent != null)
            {
                // extract dependency ops (supplier assemblies)
                var asmAndTypes = Utilities.GetAssembliesAndTypesOfCurrentDomain();
                foreach (var opEntry in Parent.Operators)
                {
                    var supplierOpPartDefinition = opEntry.Value.Item1.OperatorParts[0].Item2;
                    var supplierAssembly = (from asmTypeTuple in asmAndTypes
                                            let asm = asmTypeTuple.Item1
                                            from type in asmTypeTuple.Item2
                                            where SupplierAssembly.IsSupplierAssembly(type)
                                            where asm.FullName.Contains("Func_ID" + supplierOpPartDefinition.ID + "_Version")
                                            orderby File.GetCreationTime(asm.Location) descending // newest version first
                                            select asm).FirstOrDefault();

                    if (supplierAssembly != null)
                    {
                        supplierAssemblies.Add(supplierAssembly.Location);
                    }
                    else
                    {
                        Logger.Error("MetaOperatorPart.GenerateAssembly: could not find supplierAssembly '{0}'.", supplierOpPartDefinition.Name);
                    }
                }
            }

            var dependencyAssemblies = fixedAssemblies.Concat(AdditionalAssemblies).Concat(supplierAssemblies);
            return compiler.CompileScript(dependencyAssemblies, Script, sourceName);
        }

        private static Assembly GetAssemblyFromCurrentDomain(string assemblyName)
        {
            return (from asm in AppDomain.CurrentDomain.GetAssemblies()
                    where asm.GetName().Name == assemblyName
                    select asm).First();
        }

        /// <summary>
        /// Returns the type of the operator part's state class (which is
        /// defined by implementing IOperatorPartState). If return value
        /// is null then no state class is available/used.
        /// </summary>
        internal Type StateType
        {
            get
            {
                if (ScriptAssembly == null)
                    return null;

                return (from t in ScriptAssembly.GetTypes()
                        where t.Namespace == ScriptNamespace
                        where t.GetInterfaces().Contains(typeof(IOperatorPartState))
                        where t.GetConstructor(System.Type.EmptyTypes) != null
                        select t).FirstOrDefault();
            }
        }


        internal void InitScript(string newScript, Guid version)
        {
            var coreAssembly = GetAssemblyFromCurrentDomain("Core");

            var functorType = (from t in coreAssembly.GetTypes()
                               where t.Namespace == ScriptNamespace
                               where t.IsSubclassOf(typeof(OperatorPart.Function))
                               select t).FirstOrDefault();

            var assemblyToUse = coreAssembly;

            try
            {
                if (functorType != null)
                {
                    Logger.Info("Implementation of operator {0} is used from core assembly.", functorType.Name);
                    var cachePath = MetaManager.OPERATOR_CACHE_PATH + "/";
                    var path = cachePath + SourceName + ".cs";
                    if (File.Exists(path))
                    {
                        using (var sourceReader = new StreamReader(path))
                        {
                            _script = sourceReader.ReadToEnd();
                        }
                        var dllName = cachePath + SourceName + ".dll";
                        if (File.Exists(dllName))
                            File.Delete(dllName);

                        var pdbName = cachePath + SourceName + ".pdb";
                        if (File.Exists(pdbName))
                            File.Delete(pdbName);
                        ScriptChanged = true;
                    }
                }
                else
                {
                    _script = newScript;

                    assemblyToUse = GenerateAssembly(coreAssembly, SourceName, forceInMemoryGeneration: false);

                    functorType = (from t in assemblyToUse.GetTypes()
                                   where t.IsSubclassOf(typeof(OperatorPart.Function))
                                   select t).First();
                }
                FunctionCreator = () =>
                                  {
                                      try
                                      {
                                          return assemblyToUse.CreateInstance(functorType.FullName) as OperatorPart.Function;
                                      }
                                      catch (Exception ex)
                                      {
                                          Logger.Error("{0}: Failed to create operator function. replacement created instead. \n\t{1}\n\t{2}", functorType.Name, ex.Message, ex.InnerException);
                                      }
                                      return Utilities.CreateValueFunction(new Float(0));
                                  };
                ScriptAssembly = assemblyToUse;
                Logger.Debug("Updating " + Name + " succeeded");
            }
            catch (ScriptCompilerException e)
            {
                Logger.Error("Failed to update operator {0}. The operator has been disabled.\n{1}", Name, e.ToString());
                FunctionCreator = () => { return Utilities.CreateValueFunction(new Float(0)); };
                ScriptAssembly = null;
                return;
            }
            if (ScriptChangedEvent != null)
                ScriptChangedEvent(this, EventArgs.Empty);
        }

        string _script;
        readonly List<string> _additionalAssemblies = new List<string>();
    }

}
