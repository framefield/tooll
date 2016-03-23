// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Framefield.Core.Commands
{
    public class UpdateOperatorPropertiesCommand : ICommand
    {
        public string Name { get { return _name; } set { _name = value; } }
        public bool IsUndoable { get { return true; } }

        public List<Entry> ChangeEntries { get { return _changeEntries; } }

        public class Entry
        {
            public Entry()
            {
            }

            public Entry(Operator instance)
            {
                Name = instance.Name;
                Position = instance.Position;
                Width = instance.Width;
                Visibility = instance.Visible;
                Disabled = instance.Disabled;
            }

            public string Name;
            public Point Position;
            public double Width;
            public bool Visibility;
            public bool Disabled;
        }


        public UpdateOperatorPropertiesCommand() { }

        public UpdateOperatorPropertiesCommand(Operator instance, Entry changes)
            : this(new[] { instance }, new[] { changes })
        {
        }

        public UpdateOperatorPropertiesCommand(IEnumerable<Operator> operators, IEnumerable<Entry> changes)
        {
            foreach (var op in operators)
            {
                _operatorsMetaIDs.Add(op.Definition.ID);
                _operatorsInstanceIDs.Add(op.ID);
                _previousEntries.Add(new Entry(op));
            }
            _changeEntries = changes.ToList();
        }

        public UpdateOperatorPropertiesCommand(Guid operatorMetaID, Guid operatorInstanceID, Entry changes)
        {
            _operatorsMetaIDs.Add(operatorMetaID);
            _operatorsInstanceIDs.Add(operatorInstanceID);
            _previousEntries.Add(changes);
            _changeEntries = new List<Entry> { changes };
        }

        public void Do()
        {
            for (int idx = 0; idx < _operatorsMetaIDs.Count; ++idx)
            {
                var metaOp = MetaManager.Instance.GetMetaOperator(_operatorsMetaIDs[idx]);
                var opInstances = metaOp.GetOperatorInstances(_operatorsInstanceIDs[idx]);
                var entry = ChangeEntries[idx];
                foreach (var instance in opInstances)
                {
                    ApplyEntryToOperatorInstance(instance, entry);
                }
            }
        }

        public void Undo()
        {
            for (int idx = 0; idx < _operatorsMetaIDs.Count; ++idx)
            {
                var metaOp = MetaManager.Instance.GetMetaOperator(_operatorsMetaIDs[idx]);
                var opInstance = metaOp.GetOperatorInstance(_operatorsInstanceIDs[idx]);
                var entry = _previousEntries[idx];
                ApplyEntryToOperatorInstance(opInstance, entry);
            }
        }

        private static void ApplyEntryToOperatorInstance(Operator opInstance, Entry entry)
        {
            opInstance.Name = entry.Name;
            opInstance.Position = entry.Position;
            opInstance.Width = entry.Width;
            opInstance.Visible = entry.Visibility;

            if(opInstance.Disabled != entry.Disabled)
                opInstance.Disabled = entry.Disabled;
        }

        [JsonProperty]
        private List<Guid> _operatorsMetaIDs = new List<Guid>();
        [JsonProperty]
        private List<Guid> _operatorsInstanceIDs = new List<Guid>();
        [JsonProperty]
        private List<Entry> _previousEntries = new List<Entry>();
        [JsonProperty]
        private List<Entry> _changeEntries = new List<Entry>();

        private string _name = "Update Operator";
    }
}
