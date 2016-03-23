// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Framefield.Core;
using Framefield.Core.Commands;

namespace CoreTests
{
    using MetaOpEntry = Tuple<MetaOperator, MetaOperator.InstanceProperties>;
    using MetaOpEntryContainer = Dictionary<Guid, Tuple<MetaOperator, MetaOperator.InstanceProperties>>;

    [TestClass]
    public class CouchDBTests
    {
        static CouchDB _couchDB = new CouchDB() { ServerUrl = "http://localhost:5984" };
        static string _dbName = "test_db";

        [ClassInitialize]
        public static void Initialize(TestContext context) {
            _couchDB.CreateDatabase(_dbName);
        }

        [ClassCleanup]
        public static void Cleanup() {
            _couchDB.DeleteDatabase(_dbName);
        }

        [TestMethod]
        public void CreateDatabase_ReturnListContainsCreatedDB() {
            var dbName = "creation_test_db";
            _couchDB.CreateDatabase(dbName);
            var databases = _couchDB.GetDatabases();
            databases.Single(s => s == dbName); // throws if not found

            // cleanup
            _couchDB.DeleteDatabase(dbName);
        }

        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException))]
        public void DeleteDatabase_ReturnListContainsCreatedDB() {
            var dbName = "deletion_test_db";
            _couchDB.CreateDatabase(dbName);
            _couchDB.DeleteDatabase(dbName);

            var databases = _couchDB.GetDatabases();
            databases.Single(s => s == dbName); // throws if not found
        }

        [TestMethod]
        public void IsDBExisting_TryNotExistingDB_ReturnsFalse() {
            var dbName = Guid.NewGuid().ToString();

            var isExisting = _couchDB.IsDBExisting(dbName);

            Assert.AreEqual(false, isExisting);
        }

        [TestMethod]
        public void IsDBExisting_TryTestDB_ReturnsTrue() {
            var isExisting = _couchDB.IsDBExisting(_dbName);

            Assert.AreEqual(true, isExisting);
        }

        [TestMethod]
        public void TestGetAllDatabases_ReturnList() {
            var databases = _couchDB.GetDatabases();
            Assert.AreEqual(true, new List<string>(databases).Count > 0);
        }

        [TestMethod]
        public void testDocumentCreation_replyContainsIdAndRev() {
            var id = Guid.NewGuid().ToString();
            var doc = new JObject(new JProperty("test", "1234"));
            var idAndRev = _couchDB.StoreDocument(_dbName, id, doc.ToString());

            Assert.AreEqual(id, idAndRev.Item1);
            Assert.IsFalse(string.IsNullOrEmpty(idAndRev.Item2));
        }

        [TestMethod]
        public void testDocumentUpdate_replyContainsIdAndNewRev() {
            var id = Guid.NewGuid().ToString();
            var doc = new JObject(new JProperty("test", "1234"));
            var idAndRev = _couchDB.StoreDocument(_dbName, id, doc.ToString());
            var updatedDoc = new JObject(new JProperty("test123", "jfkjdjf1234"),
                                         new JProperty("_rev", idAndRev.Item2.ToString()));

            var updatedIdAndRev = _couchDB.StoreDocument(_dbName, id, updatedDoc.ToString());
            Assert.AreEqual(id, updatedIdAndRev.Item1);
            Assert.AreNotEqual(idAndRev.Item2, updatedIdAndRev.Item2);
        }

        [TestMethod]
        public void testDocumentDeletion_deletionReplyContainsOk() {
            var id = Guid.NewGuid().ToString();
            var doc = new JObject(new JProperty("test", "1234"));
            var idAndRev = _couchDB.StoreDocument(_dbName, id, doc.ToString());

            var delResponse = _couchDB.DeleteDocument(_dbName, id, idAndRev.Item2);
            var jsonResponse = JObject.Parse(delResponse);

            Assert.AreEqual(true , jsonResponse.Value<bool>("ok"));
        }

        [TestMethod]
        public void testTempViewCreation() {

        }

        public static MetaOperator CreateCombinedMetaOperator(Guid metaId) {
            var inputGuid = Guid.NewGuid();
            var outputGuid = Guid.NewGuid();
            var opInstanceGuid = Guid.NewGuid();
            var op = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            var inputs = new List<MetaInput>() { new MetaInput(inputGuid, "Input", BasicMetaTypes.FloatMeta, new Float(0.0f), false) };

            return new MetaOperator(metaId) {
                Name = "Float",
                Inputs = inputs,
                Outputs = new[] { new MetaOutput(outputGuid, "Output", BasicMetaTypes.FloatMeta) }.ToList(),
                Operators = new MetaOpEntryContainer() { { opInstanceGuid, new MetaOpEntry(op, new MetaOperator.InstanceProperties(op.Inputs)) } },
                Connections = new[] { new MetaConnection(Guid.Empty,     inputGuid,        opInstanceGuid, op.Inputs[0].ID), 
                                      new MetaConnection(opInstanceGuid, op.Outputs[0].ID, Guid.Empty,     outputGuid) }.ToList(),
            };
        }

        public class TypeNameSerializationBinder : SerializationBinder
        {
            public string TypeFormat { get; private set; }

            public TypeNameSerializationBinder(string typeFormat)
            {
                TypeFormat = typeFormat;
            }

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.Name;
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                string resolvedTypeName = string.Format(TypeFormat, typeName);

                return Type.GetType(resolvedTypeName, true);
            }
        }


        [TestMethod]
        public void testCmdSerialization() {
            var parentMetaID = Guid.NewGuid();
            var parentMetaOp = CreateCombinedMetaOperator(parentMetaID);
            MetaManager.Instance.AddMetaOperator(parentMetaID, parentMetaOp);
            var opToDelete = parentMetaOp.Operators.First().Value.Item1;
            MetaManager.Instance.AddMetaOperator(opToDelete.ID, opToDelete);
            var parentOp = parentMetaOp.CreateOperator(Guid.NewGuid());
            var cmd = new DeleteOperatorsCommand(parentOp, new List<Operator>() { parentOp.InternalOps[0] });
            cmd.Do();

            var serializedCmd = JsonConvert.SerializeObject(cmd, Formatting.Indented, new JsonSerializerSettings
                                                                                      {
                                                                                          TypeNameHandling = TypeNameHandling.Objects,
                                                                                          TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                                                                                      });
            var cmdType = cmd.GetType().AssemblyQualifiedName;

            var cmdID = Guid.NewGuid().ToString();
            var jsonCommand = new JObject(
                                  new JProperty("Type", "Command"),
                                  new JProperty("CommandType", cmdType),
                                  new JProperty("Command", serializedCmd));

            // store in db
            _couchDB.StoreDocument(_dbName, cmdID, jsonCommand.ToString());

            // read from db
            var response = _couchDB.GetDocument(_dbName, cmdID, string.Empty);

            var obj = JObject.Parse(response);
            var typeName = obj["CommandType"].Value<string>();
            var commandType = Type.GetType(typeName);
            var commandJson = obj["Command"].Value<string>();
            var command = (ICommand) JsonConvert.DeserializeObject(commandJson, commandType, new JsonSerializerSettings
                                                                                             {
                                                                                                 TypeNameHandling = TypeNameHandling.Objects
                                                                                             });
            Assert.AreEqual("Delete Operators", command.Name);
            Assert.AreEqual(0, parentMetaOp.Operators.Count);
            command.Undo();
            Assert.AreEqual(1, parentMetaOp.Operators.Count);
        }

    }
}
