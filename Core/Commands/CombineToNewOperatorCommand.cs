// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{

    public class CombineToNewOperatorCommand : ICommand
    {
        public string Name { get { return "Combine to New Operator"; } }
        public bool IsUndoable { get { return true; } }

        public CombineToNewOperatorCommand() { }

        public CombineToNewOperatorCommand(Operator compositionOp, IEnumerable<Operator> opsToCombine, String name, String @namespace, String description, Point position)
        {
            _compositionOpMetaID = compositionOp.Definition.ID;
            _combinedOpMetaID = Guid.NewGuid();
            _combinedOpInstanceID = Guid.NewGuid();
            _opsToCombineInstanceIDs = (from op in opsToCombine select op.ID).ToList();
            _name = name;
            _namespace = @namespace;
            _description = description;
            _position = position;
        }

        public void Undo()
        {
            var compositionMetaOp = MetaManager.Instance.GetMetaOperator(_compositionOpMetaID);
            compositionMetaOp.UngroupOperator(_combinedOpInstanceID, MetaManager.Instance.GetMetaOperator(Guid.Parse("7424a910-d647-4049-9fda-78189bcaa964")));
            MetaManager.Instance.RemoveMetaOperator(_combinedOpMetaID);
        }

        public void Do()
        {
            var compositionMetaOp = MetaManager.Instance.GetMetaOperator(_compositionOpMetaID);
            var combinedMetaOp = compositionMetaOp.CombineToNewOp(_combinedOpMetaID, _combinedOpInstanceID,  _opsToCombineInstanceIDs, _name, _namespace, _description,  _position.X, _position.Y);
            MetaManager.Instance.AddMetaOperator(_combinedOpMetaID, combinedMetaOp);
        }

        [JsonProperty]
        Guid _compositionOpMetaID;
        [JsonProperty]
        Guid _combinedOpMetaID;
        [JsonProperty]
        Guid _combinedOpInstanceID;
        [JsonProperty]
        List<Guid> _opsToCombineInstanceIDs;
        [JsonProperty]
        string _name;
        [JsonProperty]
        string _namespace;
        [JsonProperty]
        string _description;
        [JsonProperty]
        Point _position;
    }

}
