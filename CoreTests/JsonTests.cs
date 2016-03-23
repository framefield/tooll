// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Framefield.Core;
using Framefield.Core.Commands;

namespace CoreTests
{
    [TestClass]
    public class JsonTests
    {
        [TestMethod]
        public void testMetaWrite() {
            var json = new Json();
            StringBuilder sb = new StringBuilder();
            json.Writer = new JsonTextWriter(new StringWriter(sb));
            json.Writer.Formatting = Formatting.Indented;
            var meta = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            json.WriteMetaOperator(meta);
        }

        [TestMethod]
        public void testMetaRead() {
            var json = new Json();
            StringBuilder sb = new StringBuilder();
            json.Writer = new JsonTextWriter(new StringWriter(sb));
            json.Writer.Formatting = Formatting.Indented;
            var meta = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()); ;
            json.WriteMetaOperator(meta);
            var jsonData = sb.ToString();

            using (var sr = new StringReader(jsonData))
            using (var reader = new JsonTextReader(sr))
            {
                json.Reader = reader;
                var metaOp = json.ReadMetaOperator(new MetaManager());
            }
        }

        [TestMethod]
        public void testInstanceWrite() {
            var json = new Json();
            StringBuilder sb = new StringBuilder();
            json.Writer = new JsonTextWriter(new StringWriter(sb));
            json.Writer.Formatting = Formatting.Indented;
            json.WriteOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()).CreateOperator(Guid.NewGuid()));
        }


        [TestMethod]
        public void testInstanceRead() {
            var json = new Json();
            StringBuilder sb = new StringBuilder();
            json.Writer = new JsonTextWriter(new StringWriter(sb));
            json.Writer.Formatting = Formatting.Indented;
            var op = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()).CreateOperator(Guid.NewGuid());
            op.Inputs[0].Func = Utilities.CreateValueFunction(new Float(17.0f));
            json.WriteOperator(op);

            op.Inputs[0].Func = Utilities.CreateValueFunction(new Float(0.0f));

            var jsonData = sb.ToString();

            using (var sr = new StringReader(jsonData))
            using (var reader = new JsonTextReader(sr)) {
                json.Reader = reader;
                json.ReadAndSetOperatorValues(op);
            }

            var context = new OperatorPartContext();
            Assert.AreEqual(op.Inputs[0].Eval(context).Value, 17.0f);
        }

    }

}
