// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Framefield.Core
{
    using MetaOpParts = Dictionary<Guid, MetaOperatorPart>;
    using MetaOperators = Dictionary<Guid, MetaOperator>;

    public class MetaManager : IDisposable
    {
        public static readonly string OPERATOR_PATH = "Operators";
        public static readonly string OPERATOR_CACHE_PATH = @"Temp/Cache";

        //todo: this is temporary a modifiable property. if the test references are more stable this should be a constant
        public static string OPERATOR_TEST_REFERENCE_PATH { get; set; }

        public event EventHandler<System.EventArgs> ChangedEvent = (o, a) => {};

        public MetaOperators MetaOperators { get { return _metaOperators; } }
        public IEnumerable<MetaOperator> ChangedMetaOperators
        {
            get
            {
                return from metaOp in MetaOperators.Values
                       where metaOp.Changed || (metaOp.Revision == string.Empty && _useCouchDB)
                       select metaOp;
            }
        }

        private static MetaManager _instance;
        public static MetaManager Instance { 
            get { return _instance ?? (_instance = new MetaManager()); }
        }

        internal static void Dipose()
        {
            Utilities.DisposeObj(ref _instance);
        }

        private static readonly Guid HomeOperatorGuid = new Guid("{EECB57F8-B805-4522-8122-F71C9E6D54D4}");
        private static readonly Guid HomeOutputGuid = new Guid("{A9858A17-684F-4AAD-AB6F-05B96C5495B1}");
        private MetaOperator _homeOperator;
        public MetaOperator HomeOperator { get { return _homeOperator; } }

        public delegate void InitializeCallbackDelegate(float progress);
        public static InitializeCallbackDelegate InitializeCallback { get; set; }

        internal static bool ReadMetaOpsOnInit = true;

        internal MetaManager()
        {
            MetaPath = Directory.GetCurrentDirectory() + @"\" + OPERATOR_PATH + @"\";
            MetaExtension = ".mop";
            // check if couchdb should be used
//             try {
//                 if (!_couchDB.IsDBExisting(_dbName))
//                     _couchDB.CreateDatabase(_dbName);
//                 _useCouchDB = true;
//             }
//             catch (Exception e) {
//                 Logger.Info("Couldn't get or setup operators db: {0}", e.Message);
//             }
        }

        public void LoadMetaOperators()
        {
            var coreAssembly = (from asm in AppDomain.CurrentDomain.GetAssemblies()
                                where asm.GetName().Name == "Core"
                                select asm).First();

            Logger.Info("Loading operator definition parts...");
            var watch = new Stopwatch();
            watch.Start();
            try {
                Type[] t = coreAssembly.GetTypes();
            }
            catch (Exception e)
            {
                Logger.Info("e.Message");
                if (e is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = e as ReflectionTypeLoadException;
                    var loaderExceptions  = typeLoadException.LoaderExceptions;
                }
            }
            var metaOpPartTypes = (from type in coreAssembly.GetTypes()
                                   let properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                                   from p in properties
                                   where p.PropertyType == typeof (MetaOperatorPart)
                                   select new {Type = type, PropInfo = p}).ToList();

            metaOpPartTypes.ForEach(metaType =>
                                    {
                                        AddMetaOperatorPart((MetaOperatorPart) (metaType.PropInfo.GetValue(metaType.Type, null)));
                                        Logger.Debug("loaded: '{0}'", metaType.PropInfo.Name);
                                    });

            AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;

            Logger.Info("Loading operator types...");
            if (Directory.Exists(MetaPath) && ReadMetaOpsOnInit)
            {
                var metaFiles = Directory.EnumerateFiles(MetaPath, "*.mop");
                _numOps = metaFiles.Count() - 1;
                foreach (var metaFile in metaFiles)
                {
                    var id = new Guid(Path.GetFileNameWithoutExtension(metaFile));

                    if (!MetaOperators.ContainsKey(id))
                    {
                        var metaOp = LoadMetaOperator(id);
                        AddMetaOperator(id, metaOp);
                    }
                    TriggerInitializeCallback();
                }
                watch.Stop();
                Logger.Debug("Loading took {0}s", (float)watch.ElapsedMilliseconds/1000.0f);
            }
            else
            {
                Logger.Warn("No meta operators found. Is your operator database empty or corrupted?");
            }

            // try to load main op
            _homeOperator = LoadMetaOperatorFromFile(@"Config/Home.mop");
            if (_homeOperator == null)
            {
                _homeOperator = new MetaOperator(HomeOperatorGuid) { Name = "Home" };
                var output = new MetaOutput(HomeOutputGuid, "Output", BasicMetaTypes.GenericMeta);
                _homeOperator.AddOutput(output);
            }

            CheckForInconsistencies();
        }


        private Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
        {
            return CheckForMissingOperatorAssembly(args);
        }


        private static Assembly CheckForMissingOperatorAssembly(ResolveEventArgs args)
        {
            var match = MatchOperatorAssemblyFilenamePattern(args.Name);
            if (!match.Success)
                return null;

            var requestingAssemblyName = (args.RequestingAssembly != null) ? args.RequestingAssembly.FullName : string.Empty;
            Logger.Info("MetaManager.ResolveEventHandler: assembly {0} called for assembly '{1}'", requestingAssemblyName, args.Name);

            var asm = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                       where assembly.FullName == args.Name
                       select assembly).FirstOrDefault();

            if (asm == null)
            {
                Logger.Info("MetaManager.ResolveEventHandler: assembly not found in current domain. Trying to load it from file cache.");
                var assemblyFilename = match.Groups[0].Value;
                if (assemblyFilename != string.Empty)
                {
                    asm = Assembly.LoadFrom("./Temp/Cache/" + assemblyFilename + ".dll");
                }
            }

            Logger.Info("MetaManager.ResolveEventHandler: {0} requested assembly ({1}).", (asm != null ? "successfully resolved" : "failed to resolve"), args.Name);

            return asm;
        }

        internal static Match MatchOperatorAssemblyFilenamePattern(string requestedAssemblyName)
        {
            var filenameSearchPattern = new Regex(@"(.+Func_ID[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}_Version[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12})");
            return filenameSearchPattern.Match(requestedAssemblyName);
        }

        private void TriggerInitializeCallback()
        {
            if (InitializeCallback != null)
                InitializeCallback((float) MetaOperators.Count/_numOps);
        }

        public void Dispose()
        {
            _homeOperator.Dispose();
            foreach (var metaOp in _metaOperators)
            {
                metaOp.Value.Dispose();
            }
        }

        #region consistency checks

        private struct InstanceEntry
        {
            public MetaOperator CompositionOperator;
            public MetaOperator Operator;
        }

        public void CheckForInconsistencies()
        {
            // check if there are duplicated instance ids or if there are ops referenced that are not exisiting
            var collectedInstanceIds = new Dictionary<Guid, List<InstanceEntry>>();
            var missingOps = new List<Guid>();
            foreach (var opDefinition in _metaOperators)
            {
                if (opDefinition.Value == null)
                {
                    Logger.Error("Referenced undefined operator: {0}.", opDefinition.Key);
                    missingOps.Add(opDefinition.Key);
                    continue;
                }

                foreach (var opEntry in opDefinition.Value.Operators)
                {
                    var instanceId = opEntry.Key;
                    if (!collectedInstanceIds.ContainsKey(instanceId))
                    {
                        collectedInstanceIds[instanceId] = new List<InstanceEntry>();
                    }
                    collectedInstanceIds[instanceId].Add(new InstanceEntry
                                                             {
                                                                 CompositionOperator = opDefinition.Value,
                                                                 Operator = opEntry.Value.Item1
                                                             });
                }
            }

            // check for duplicated instance ids
            foreach (var c in collectedInstanceIds.Where(c => c.Value.Count > 1))
            {
                Logger.Warn("The following operators instances have the same unique ID ({0}):", c.Key);
                foreach (var entry in c.Value)
                {
                    Logger.Warn(" - operator {0} in composition op {1}", c.Value[0].Operator.Name, c.Value[0].CompositionOperator.Name);
                }
            }

            // remove from op definitions
            foreach (var opToRemove in missingOps)
            {
                _metaOperators.Remove(opToRemove);
            }

            // remove home op definition from library if exist
            if (_metaOperators.ContainsKey(_homeOperator.ID))
            {
                Logger.Warn("home operator definition {0} ({1}) also exists in operators directory. removing duplicate operator definition from library.", _homeOperator.Name, _homeOperator.ID);
                RemoveMetaOperator(_homeOperator.ID);
            }
        }

        #endregion

        public void AddMetaOperatorPart(MetaOperatorPart metaOpPart)
        {
            if (!_metaOpParts.ContainsKey(metaOpPart.ID))
            {
                _metaOpParts.Add(metaOpPart.ID, metaOpPart);
                ChangedEvent(this, EventArgs.Empty);
            }
        }

        public void AddMetaOperator(Guid id, MetaOperator metaOp)
        {
            if (_metaOperators.ContainsKey(id))
            {
                Logger.Warn("operator definition ({0}) already added. skipped adding operator definition {1}.", id, metaOp.Name);
            }
            else
            {
                _metaOperators.Add(id, metaOp);
                ChangedEvent(this, EventArgs.Empty);
            }
        }

        public MetaOperatorPart GetMetaOperatorPart(Guid id)
        {
            if (!_metaOpParts.TryGetValue(id, out var metaOpPart))
            {
                Logger.Error("Part of operator definition not found ({0}). Trying to patch this part with a new generic definition...", id.ToString());

                var type = FunctionType.Generic;
                Func<Guid, OperatorPart.Function, bool, string, OperatorPart> createFunc;
                createFunc = (lid, defaultFunction, isMultiInput, name) =>
                             {
                                 var lopPart = Utilities.CreateValueOpPart(lid, null, isMultiInput);
                                 lopPart.Type = type;
                                 lopPart.Name = name;
                                 return lopPart;
                             };
                metaOpPart = new MetaOperatorPart(id) {CreateFunc = createFunc, Type = type};
                AddMetaOperatorPart(metaOpPart);
            }

            return metaOpPart;
        }

        public void RemoveMetaOperator(Guid id)
        {
            if (_metaOperators.Remove(id))
                ChangedEvent(this, EventArgs.Empty);
        }

        internal MetaOperator GetMetaOp(string name)
        {
            return (from element in _metaOperators
                    where element.Value.Name == name
                    select element.Value).Single();
        }

        public void StoreMetaOperators()
        {
            WriteChangedMetaOperators();
            WriteDescriptionFile();
        }

        private void WriteChangedMetaOperators()
        {
            WriteOperators(ChangedMetaOperators, MetaPath, clearChangedFlags:true);
        }

        public static void WriteOperators(IEnumerable<MetaOperator> metaOpsToStore, string path, bool clearChangedFlags)
        {
            foreach (var metaOp in metaOpsToStore)
            {
//                if (_useCouchDB) {
//                    using (var writer = new StringWriter()) {
//                        WriteOpWithWriter(metaOp, writer);
//                        var idAndRev = _couchDB.StoreDocument(_dbName, metaOp.ID.ToString(), writer.ToString());
//                        metaOp.Revision = idAndRev.Item2;
//                    }
//                }

                var filename = path + metaOp.ID.ToString() + MetaExtension;
                var rev = metaOp.Revision;
                // clear revision for disc storage, to not mess up git with local rev ids
                metaOp.Revision = string.Empty;
                using (var writer = new StreamWriter(filename))
                {
                    WriteOpWithWriter(metaOp, writer);
                }
                metaOp.Revision = rev; // restore rev

                if (clearChangedFlags)
                {
                    metaOp.Changed = false;
                }
            }
        }

        public static void WriteOpWithWriter(MetaOperator metaOp, TextWriter writer) {
            var json = new Json();
            json.Writer = new JsonTextWriter(writer);
            json.Writer.Formatting = Formatting.Indented;
            json.WriteMetaOperator(metaOp);
        }

        private void WriteDescriptionFile()
        {
            using (var descriptionWriter = new StreamWriter(MetaPath + "descript.ion"))
            {
                foreach (var metaOp in MetaOperators.Values)
                {
                    var filename = metaOp.ID.ToString() + MetaExtension;
                    descriptionWriter.WriteLine(filename + " " + metaOp.Name);
                }
            }
        }

        public MetaOperator LoadMetaOperator(Guid id)
        {
            if (_useCouchDB)
            {
                try
                {
                    var jsonOp = _couchDB.GetDocument(_dbName, id.ToString(), string.Empty);
                    using (var reader = new StringReader(jsonOp))
                    {
                        return ReadMetaOpFromReader(reader);
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn("Loading operator definition for {0} failed:{1}", id.ToString(), e.Message);
                }
            }

            TriggerInitializeCallback();

            return LoadMetaOperatorFromFile(MetaPath + id.ToString() + MetaExtension);
        }

        public MetaOperator LoadMetaOperatorFromFile(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    return ReadMetaOpFromReader(reader);
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Loading operator definition for {0} failed:{1}", filePath, e.Message);
                return null;
            }
        }

        public MetaOperator ReadMetaOpFromReader(TextReader reader)
        {
            var json = new Json();
            json.Reader = new JsonTextReader(reader);
            var metaOp = json.ReadMetaOperator(this);
            Logger.Debug("Reading operator definition: {0}", metaOp.Name);
            return metaOp;
        }

        public MetaOperator GetMetaOperator(Guid id)
        {
            try
            {
                if (MetaOperators.ContainsKey(id))
                    return MetaOperators[id];

                if (_homeOperator != null && id == _homeOperator.ID)
                    return _homeOperator;

                var metaOp = LoadMetaOperator(id);
                AddMetaOperator(id, metaOp);

                return metaOp;
            }
            catch (Exception)
            {
                Logger.Warn("Operator definition not found: {0}", id.ToString());
                return null;
            }
        }


        private string MetaPath { get; set; }
        private static string MetaExtension { get; set; }
        private MetaOpParts _metaOpParts = new MetaOpParts();
        private MetaOperators _metaOperators = new MetaOperators();

        private CouchDB _couchDB = new CouchDB();
        string _dbName = "operators";
        private bool _useCouchDB = false;
        private int _numOps;
    }
}
