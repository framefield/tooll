// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class ReplaceOperatorCommand : ICommand
    {
        public List<Guid> NewInputs
        {
            get { return _newInputs; }
            set { _newInputs = value; }
        }

        public List<Guid> NewOutputs
        {
            get { return _newOutputs; }
            set { _newOutputs = value; }
        } 

        public string Name { get { return "Replace Operator"; } }
        public bool IsUndoable { get { return true; } }

        public ReplaceOperatorCommand(MetaOperator parentMetaOp, MetaOperator oldOperator, MetaOperator newOperator, Guid internalOldID, Guid internalNewID = new Guid())
        {
            _parentMetaOpID = parentMetaOp.ID;
            _oldOperatorID = oldOperator.ID;
            _newOperatorID = newOperator.ID;
            _internalOldID = internalOldID;
            _internalNewID = internalNewID != Guid.Empty ? internalNewID : Guid.NewGuid();
            _outgoingConnections = parentMetaOp.Connections.Where(connection => connection.SourceOpID == _internalOldID).ToArray();
            _ingoingConnections = parentMetaOp.Connections.Where(connection => connection.TargetOpID == internalOldID).ToArray();
            FillNewInputs();
            FillNewOutputs();
        }

        private void FillNewOutputs()
        {
            foreach (var oldOutput in OldOperator.Outputs)
            {
                var matchingNameOutput = NewOperator.Outputs.Find(output => output.Name == oldOutput.Name);
                if (IsNewValidOutput(matchingNameOutput, oldOutput))
                {
                    _newOutputs.Add(matchingNameOutput.ID);
                }
                else
                {
                    var validOutput = NewOperator.Outputs.Find(output => IsNewValidOutput(output, oldOutput));
                    if (validOutput != null)
                    {
                        _newOutputs.Add(validOutput.ID);
                    }
                    else
                    {
                        _newOutputs.Add(Guid.Empty);
                    }
                }
            }
        }

        private static bool IsNewValidOutput(MetaOutput matchingNameOutput, MetaOutput oldOutput)
        {
            return matchingNameOutput != null &&
                   matchingNameOutput.OpPart.Type == oldOutput.OpPart.Type;
        }

        private void FillNewInputs()
        {
            foreach (var oldInput in OldOperator.Inputs)
            {
                var matchingNameInput = NewOperator.Inputs.Find(input => input.Name == oldInput.Name);
                if (IsNewValidInput(matchingNameInput, oldInput))
                {
                    _newInputs.Add(matchingNameInput.ID);
                }
                else
                {
                    var validInput = NewOperator.Inputs.Find(input => IsNewValidInput(input, oldInput));
                    if (validInput != null)
                    {
                        _newInputs.Add(validInput.ID);
                    }
                    else
                    {
                        _newInputs.Add(Guid.Empty);
                    }
                }
            }
        }

        private bool IsNewValidInput(MetaInput matchingNameInput, MetaInput oldInput)
        {
            return matchingNameInput != null &&
                   matchingNameInput.OpPart.IsMultiInput == oldInput.OpPart.IsMultiInput &&
                   matchingNameInput.OpPart.Type == oldInput.OpPart.Type;
        }

        public void Undo()
        {
            ReplaceOperator(OldOperator, _internalOldID, _internalNewID);
            var oldInputIDs = OldOperator.Inputs.Select(input => input.ID).ToList();
            var oldOutputIDs = OldOperator.Outputs.Select(output => output.ID).ToList();
            InsertNewConnections(_internalOldID, oldInputIDs, oldOutputIDs);
        }

        public void Do()
        {
            ReplaceOperator(NewOperator, _internalNewID, _internalOldID);
            InsertNewConnections(_internalNewID, _newInputs, _newOutputs);
        }

        private void ReplaceOperator(MetaOperator operatorToAdd, Guid idToAdd, Guid idToRemove)
        {
            var position = ParentOperator.Operators[idToRemove].Item2.Position;
            ParentOperator.RemoveOperator(idToRemove);
            ParentOperator.AddOperator(operatorToAdd, idToAdd);
            ParentOperator.Operators[idToAdd].Item2.Position = position;
        }

        private void InsertNewConnections(Guid internalOpID, IList<Guid> inputs, IList<Guid> outputs)
        {
            foreach (var connection in _outgoingConnections)
            {
                var outputIndex = OldOperator.Outputs.IndexOf(OldOperator.Outputs.Find(output => output.ID == connection.SourceOpPartID));
                var sourceOpPartID = outputIndex >= 0 ? outputs[outputIndex] : Guid.Empty;
                if (sourceOpPartID != Guid.Empty)
                    ParentOperator.InsertConnectionAt(new MetaConnection(internalOpID, sourceOpPartID, connection.TargetOpID, connection.TargetOpPartID));
            }
            foreach (var connection in _ingoingConnections)
            {
                var inputIndex = OldOperator.Inputs.IndexOf(OldOperator.Inputs.Find(input => input.ID == connection.TargetOpPartID));
                var targetOpPartID = inputIndex >= 0 ? inputs[inputIndex] : Guid.Empty;
                if (targetOpPartID != Guid.Empty)
                    ParentOperator.InsertConnectionAt(new MetaConnection(connection.SourceOpID, connection.SourceOpPartID, internalOpID, targetOpPartID));
            }
        }

        private MetaOperator OldOperator
        {
            get { return MetaManager.Instance.GetMetaOperator(_oldOperatorID); }
        }

        private MetaOperator NewOperator
        {
            get { return MetaManager.Instance.GetMetaOperator(_newOperatorID); }
        }

        private MetaOperator ParentOperator
        {
            get { return MetaManager.Instance.GetMetaOperator(_parentMetaOpID); }
        }

        [JsonProperty]
        private readonly Guid _parentMetaOpID;
        [JsonProperty]
        private readonly Guid _oldOperatorID;
        [JsonProperty]
        private readonly Guid _newOperatorID;
        [JsonProperty]
        private readonly Guid _internalOldID;
        [JsonProperty]
        private Guid _internalNewID;
        [JsonProperty]
        private List<Guid> _newInputs = new List<Guid>();
        [JsonProperty]
        private List<Guid> _newOutputs = new List<Guid>();
        [JsonProperty]
        private MetaConnection[] _outgoingConnections;
        [JsonProperty]
        private MetaConnection[] _ingoingConnections;
    }
}
