// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Framefield.Core.Commands
{
    public class CopyOperatorsCommand  : ICommand
    {
        public string Name { get { return "Copy Operators"; } }
        public bool IsUndoable { get { return false; } }

        public CopyOperatorsCommand() {}

        public CopyOperatorsCommand(IEnumerable<Guid> opIdsToCopy, MetaOperator sourceCompositionOp, MetaOperator targetCompositionOp, Point basePosition)
        {
            _opIdsToCopy = opIdsToCopy;
            _sourceCompositionOp = sourceCompositionOp;
            _targetCompositionOp = targetCompositionOp;
            _basePosition = basePosition;
        }

        // copies all operators from source composition op
        public CopyOperatorsCommand(MetaOperator sourceCompositionOp, MetaOperator targetCompositionOp, Point basePosition)
        : this(from opEntry in sourceCompositionOp.Operators select opEntry.Key, sourceCompositionOp, targetCompositionOp, basePosition)
        {
        }


        public void Undo()
        {
        }

        public void Do()
        {
            var originalToCopyMap = new Dictionary<Guid, Guid>();

            // get top left corner of all ops
            var topLeft = new Point(double.MaxValue, double.MaxValue);
            foreach (var opId in _opIdsToCopy)
            {
                if (!_sourceCompositionOp.GetVisible(opId)) 
                    continue;

                var opEntry = _sourceCompositionOp.Operators[opId];
                topLeft.X = Math.Min(topLeft.X, opEntry.Item2.Position.X);
                topLeft.Y = Math.Min(topLeft.Y, opEntry.Item2.Position.Y);
            }

            foreach (var opId in _opIdsToCopy)
            {
                var opEntry = _sourceCompositionOp.Operators[opId];
                var newOpId = _targetCompositionOp.AddOperator(opEntry.Item1, opEntry.Item2.Clone(), Guid.NewGuid());
                _targetCompositionOp.SetPosition(newOpId, _targetCompositionOp.GetPosition(newOpId) - topLeft + _basePosition);
                originalToCopyMap[opId] = newOpId;
            }

            // copy connections
            var internalConnections = (from con in _sourceCompositionOp.Connections
                                       from sourceOpID in _opIdsToCopy
                                       from targetOpID in _opIdsToCopy
                                       where con.SourceOpID == sourceOpID
                                       where con.TargetOpID == targetOpID
                                       select con).ToList();

            // create a group for each input
            var groupedConnections = (from con in internalConnections
                                      group con by (con.TargetOpID.ToString() + con.TargetOpPartID.ToString())
                                      into @group
                                      select @group).ToArray();

            // insert the connections to new op
            foreach (var conGroup in groupedConnections)
            {
                int index = 0;
                foreach (var con in conGroup)
                {
                    var conBetweenNewOps = new MetaConnection(originalToCopyMap[con.SourceOpID], con.SourceOpPartID,
                                                              originalToCopyMap[con.TargetOpID], con.TargetOpPartID);
                    _targetCompositionOp.InsertConnectionAt(conBetweenNewOps, index);
                    ++index;
                }
            }
        }

        private readonly MetaOperator _sourceCompositionOp;
        private readonly MetaOperator _targetCompositionOp;
        private readonly IEnumerable<Guid> _opIdsToCopy;
        private readonly Point _basePosition;
    }
}
