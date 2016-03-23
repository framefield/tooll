// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests
{
    [TestClass]
    public class ScriptCompilerTests
    {
        private string m_InvalidScript = 
          @"using System;

            public class CompileTest {
                some type errors
            }";

        private const string _validScript = 
           @"using System;

            public class CompilerTestClass {
            }";


        private string m_ScriptDependingOnFramefieldCore =
          @"using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;

            namespace Framefield.Core
            {
                public class FloatOperatorsasdfasdfasdfasdf
                {

                    public static OperatorPartContext GetRandomFloat(OperatorPartContext context, List<OperatorPart> inputs) {
                        int seed = (int)inputs[0].Eval(context).Value;
                        int min = (int)inputs[1].Eval(context).Value;
                        int max = (int)inputs[2].Eval(context).Value;
                        var random = new Random(seed);
                        context.Value = random.Next(min, max);
                        return context;
                    }

                }
            }";

        [TestMethod]
        public void compileScript_ScriptDependsOnFramefieldCore_returnsValidAssembly()
        {
          var assembliesLoaded = AppDomain.CurrentDomain.GetAssemblies().ToList();
          var coreAssembly = assembliesLoaded.Find(asm => asm.GetName().Name == "Core");

          var compiler = new ScriptCompiler(true);
          var assembly = compiler.CompileScript(new[] { "System.Core.dll", coreAssembly.Location }.ToList(), m_ScriptDependingOnFramefieldCore, null);

          Assert.AreNotEqual(null, assembly);
        }

        [TestMethod]
        public void compileScript_SimpleScript_returnsCorrectType()
        {
            var compiler = new ScriptCompiler(true);
            var assembly = compiler.CompileScript(new[] { "System.Core.dll" }, _validScript, null);

            var result = assembly.GetTypes().ToList().Find(type => type.Name == "CompilerTestClass");

            Assert.AreNotEqual(null, result);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void compileScript_EmptyStript_throws()
        {
            var compiler = new ScriptCompiler(true);
            compiler.CompileScript(new string[] { }, "", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ScriptCompilerException))]
        public void compileScript_InvalidStript_throws()
        {
            var compiler = new ScriptCompiler(true);
            compiler.CompileScript(new[] { "System.Core.dll" }, m_InvalidScript, null);
        }


    }
}
