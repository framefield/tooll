// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using Framefield.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace CoreTests
{
    public class InputCommandsTests
    {
        protected Operator _operator;
        protected Operator _parentOperator;
        protected JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto };

        [TestInitialize]
        public void Initialize()
        {
            var metaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            _operator = metaOp.CreateOperator(Guid.NewGuid());
            var parentMeta = MetaOperatorTests.CreateGenericMultiInputMetaOperator(Guid.NewGuid());
            _parentOperator = parentMeta.CreateOperator(Guid.NewGuid());
            _parentOperator.InternalOps.Add(_operator);
            _operator.Parent = _parentOperator;
            MetaManager.Instance.AddMetaOperator(parentMeta.ID, parentMeta);
            MetaManager.Instance.AddMetaOperator(metaOp.ID, metaOp);
        }

        [TestCleanup]
        public void CleanUp()
        {
            MetaManager.Instance.RemoveMetaOperator(_operator.Definition.ID);
            MetaManager.Instance.RemoveMetaOperator(_parentOperator.Definition.ID);
            _operator.Dispose();
            _operator = null;
            _parentOperator.Dispose();
            _parentOperator = null;
        }

        protected string SerializeCommand(ICommand cmd)
        {
            var persistentCmd = new PersistentCommand() { Command = cmd };
            var jsonCommand = JsonConvert.SerializeObject(persistentCmd, Formatting.Indented, _serializerSettings);
            return jsonCommand;
        }
    }
}
