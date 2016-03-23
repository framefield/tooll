// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using Framefield.Core.Curve;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class MoveKeyframeCommand : ICommand
    {
        public string Name { get { return "Move Keyframe"; } }
        public bool IsUndoable { get { return true; } }

        public double NewTime
        {
            get
            {
                return _newTime;
            }
            set
            {
                if (value == _newTime)
                    return;

                var curve = Curve;
                var newerTime = value;
                var keyframeAtNewerTime = curve.HasVAt(newerTime) ? curve.GetV(newerTime).Clone() : null;

                curve.MoveV(_newTime, newerTime);

                if (_overwrittenKeyframe != null)                
                    curve.AddOrUpdateV(_newTime, _overwrittenKeyframe);
                
                _overwrittenKeyframe = keyframeAtNewerTime;
                _newTime = newerTime;
            }
        }
        
        public MoveKeyframeCommand(double initialTime, double newTime, ICurve curve)
        {
            var func = curve as OperatorPart.Function;
            OperatorPart curveOpPart = func.OperatorPart;

            _newTime = newTime;
            _initialTime = initialTime;
            _currentTime = initialTime;

            if (!curve.HasVAt(_initialTime))
            {
                Logger.Error("Tried to move non-existing keyframe from {0} to {1}", _initialTime, _newTime);
                return;
            }
            if (_newTime != _initialTime && curve.HasVAt(_newTime))
            {
                _overwrittenKeyframe = curve.GetV(_newTime).Clone();
            }
            StoreRelevantIds(curveOpPart);
        }

        private void StoreRelevantIds(OperatorPart curveOpPart)
        {
            _curveOpToAddKeyframeInstanceID = curveOpPart.Parent.ID;
            var compositionOp = curveOpPart.Parent.Parent;
            StoreCompositionOpIds(compositionOp);
        }

        private void StoreCompositionOpIds(Operator compositionOp)
        {
            _compositionOpInstanceID = compositionOp.ID;
            _compositinOpParentInstanceID = (compositionOp.Parent == null) ? Guid.Empty : compositionOp.Parent.ID;
            _compositionOpMetaID = compositionOp.Definition.ID;
        }

        public void Undo()
        {
            var curve = Curve;
            curve.MoveV(NewTime, _initialTime);
            if (_overwrittenKeyframe != null)
            {
                curve.AddOrUpdateV(NewTime, _overwrittenKeyframe);
            }                
        }

        public void Do()
        {
            var curve = Curve;
            if (_initialTime != NewTime)
            {
                curve.MoveV(_currentTime, NewTime);
            }
            
            FindParentInstance.Definition.Changed = true;
        }

        private ICurve Curve
        {
            get
            {
                var parentInstance = FindParentInstance;
                var curveInstance = (from op in parentInstance.InternalOps where op.ID == _curveOpToAddKeyframeInstanceID select op).Single();
                var curveOpPart = curveInstance.InternalParts[0];
                var curve = curveOpPart.Func as ICurve;
                return curve;
            }
        }

        private Operator FindParentInstance
        {
            get
            {
                var parentMeta = MetaManager.Instance.GetMetaOperator(_compositionOpMetaID);
                var possibleParentInstances = parentMeta.GetOperatorInstances(_compositionOpInstanceID);
                foreach (var possible in possibleParentInstances)
                {
                    var id = possible.Parent == null ? Guid.Empty : possible.Parent.ID;
                    if (id == _compositinOpParentInstanceID)
                    {
                        return possible;
                    }
                }
                Logger.Warn("AddOrUpdateKeyframeCommand: could not find parent instance of curve op, returning default value!");
                return parentMeta.GetOperatorInstance(_compositionOpInstanceID);
            }
        }

        private double _currentTime;

        [JsonProperty]
        private VDefinition _overwrittenKeyframe;
        [JsonProperty]
        private double _initialTime;
        [JsonProperty]
        private double _newTime;
        [JsonProperty]
        private Guid _compositionOpMetaID;
        [JsonProperty]
        private Guid _compositionOpInstanceID;
        [JsonProperty]
        private Guid _compositinOpParentInstanceID;
        [JsonProperty]
        private Guid _curveOpToAddKeyframeInstanceID;
    }
}
