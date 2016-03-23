// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace Framefield.Core
{

    public class ScriptCompilerException : Exception
    {
        public ScriptCompilerException(string script, CompilerResults results) : base("Could not compile script")
        {
            CompilerResults = results;
            _scriptCode = script;
        }

        public ScriptCompilerException(string message, Exception inner) : base(message, inner)
        {
        }

        public override string ToString()
        {
            var lines = _scriptCode.Split(new string[] { "\n" }, StringSplitOptions.None).ToList();

            string compilerError = string.Empty;
            foreach (CompilerError error in CompilerResults.Errors)
            {
                compilerError += string.Format("error {0}: {1}", error.ErrorNumber, error.ErrorText) + Environment.NewLine;
                if (error.Line > 0)
                    compilerError += string.Format("{0,4}  in \"{1}\"", error.Line, lines[error.Line - 1]) + Environment.NewLine;
                compilerError += "         " + (new String(' ', error.Column)) + "^" + Environment.NewLine;
            }

            return compilerError;
        }

        public CompilerResults CompilerResults { get; set; }
        private string _scriptCode;
    }

    public class ScriptCompiler
    {
        public ScriptCompiler(bool forceInMemoryGeneration)
        {
            _csharpCompilerParameters = new CompilerParameters();
#if DEBUG
            _csharpCompilerParameters.CompilerOptions = String.Empty;
            _csharpCompilerParameters.IncludeDebugInformation = true;
#else
            _csharpCompilerParameters.CompilerOptions = "/optimize";
            _csharpCompilerParameters.IncludeDebugInformation = false;
#endif
            _csharpCompilerParameters.GenerateExecutable = false;
            ForceInMemoryGeneration = forceInMemoryGeneration;
        }

        public Assembly CompileScript(IEnumerable<string> referenceAssemblies, string script, string outputName)
        {
            if (string.IsNullOrEmpty(script))
                throw new Exception("empty script");

            var cachePath = MetaManager.OPERATOR_CACHE_PATH + @"\"; // path separator must be backslash, otherwise compiling won't work!
            if (!Directory.Exists(cachePath))
                Directory.CreateDirectory(cachePath);

            var path = cachePath + outputName;
            var dllPath = path + ".dll";
            var asmAlreadyExists = File.Exists(dllPath);

            if (asmAlreadyExists && (outputName != null))
            {
                var fullPath = new FileInfo(dllPath).FullName;
                return Assembly.LoadFile(fullPath);
            }

            var sourcePath = path + ".cs";
            using (var sourceFile = new StreamWriter(sourcePath))
            {
                sourceFile.Write(script);
            }

            _csharpCompilerParameters.GenerateInMemory = ForceInMemoryGeneration || (outputName == null) || asmAlreadyExists;

            if (!_csharpCompilerParameters.GenerateInMemory)
                _csharpCompilerParameters.OutputAssembly = dllPath;

            _csharpCompilerParameters.ReferencedAssemblies.Clear();
            _csharpCompilerParameters.ReferencedAssemblies.Add("Libs/Newtonsoft.Json.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("Libs/SharpDX.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("Libs/SharpDX.D3DCompiler.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("Libs/SharpDX.Direct2D1.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("Libs/SharpDX.Direct3D10.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("Libs/SharpDX.Direct3D11.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("Libs/SharpDX.Direct3D11.Effects.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("Libs/SharpDX.DXGI.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("System.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("System.Web.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("System.Web.Extensions.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            _csharpCompilerParameters.ReferencedAssemblies.Add("System.Xml.dll");
            foreach (var referenceAssembly in referenceAssemblies)
                _csharpCompilerParameters.ReferencedAssemblies.Add(referenceAssembly);

            var results = (outputName != null) ? _csharpCompiler.CompileAssemblyFromFile(_csharpCompilerParameters, new[] { sourcePath })
                                               : _csharpCompiler.CompileAssemblyFromSource(_csharpCompilerParameters, script);

            if (results.Errors.Count > 0)
            {
                throw new ScriptCompilerException(script, results);
            }

            return results.CompiledAssembly;
        }

        private static CodeDomProvider _csharpCompiler = CodeDomProvider.CreateProvider("CSharp");
        private CompilerParameters _csharpCompilerParameters = null;
        private bool ForceInMemoryGeneration { get; set; }
    }
}
