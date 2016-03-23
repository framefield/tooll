// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using Framefield.Core.Curve;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class AddOrUpdateKeyframeCommand : ICommand
    {
        public string Name { get { return "Add Keyframe"; } }
        public bool IsUndoable { get { return true; } }

        public double KeyframeTime { get; set; }
        public VDefinition KeyframeValue { get; set; }

        public AddOrUpdateKeyframeCommand() { }
        
        public AddOrUpdateKeyframeCommand(double time, VDefinition keyValue, ICurve curve)
        {
            var func = curve as OperatorPart.Function;
            OperatorPart curveOpPart = func.OperatorPart;

            if (curve.HasVAt(time))
            {
                _previousKeyframeValue = curve.GetV(time);
            }

            KeyframeTime = time;
            _initialKeyframeTime = time;
            _previousKeyframeTime = time;
            KeyframeValue = keyValue;
            StoreRelevantIds(curveOpPart);
        }

        public AddOrUpdateKeyframeCommand(double time, double value, OperatorPart opPartToAddKeyframe)
        {
            var newKey = new VDefinition();
            OperatorPart curveOpPart = Animation.GetRegardingAnimationOpPart(opPartToAddKeyframe);
            if (curveOpPart != null)
            {
                // already existing animation, so check for previous keyframe
                var curve = curveOpPart.Func as ICurve;
                if (curve.HasVAt(time))
                {
                    _previousKeyframeValue = curve.GetV(time);
                }

                double? prevU = curve.GetPreviousU(time);
                if (prevU != null)
                    newKey = curve.GetV(prevU.Value);
            }

            newKey.Value = value;

            KeyframeTime = time;
            _initialKeyframeTime = time;
            _previousKeyframeTime = time;
            KeyframeValue = newKey;
            StoreRelevantIds(curveOpPart);
        }

        public AddOrUpdateKeyframeCommand(double time, double value, Operator compositionOp, Guid curveInstanceId, Guid curveOpPartId)
        {
            var newKey = new VDefinition() { Value = value };
            KeyframeTime = time;
            _previousKeyframeTime = time;
            KeyframeValue = newKey;
            _curveOpToAddKeyframeInstanceID = curveInstanceId;
            StoreCompositionOpIds(compositionOp);
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
            if (_previousKeyframeValue != null)
            {
                curve.MoveV(KeyframeTime, _initialKeyframeTime);
                _previousKeyframeTime = _initialKeyframeTime;
                curve.AddOrUpdateV(_initialKeyframeTime, _previousKeyframeValue);
            }
            else
            {
                // was new key, so remove it
                curve.RemoveV(KeyframeTime);
            }
        }

        public void Do()
        {
            var curve = Curve;
            if (_previousKeyframeTime != KeyframeTime)
            {
                curve.MoveV(_previousKeyframeTime, KeyframeTime);
                _previousKeyframeTime = KeyframeTime;
            }
            curve.AddOrUpdateV(KeyframeTime, KeyframeValue);
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

        private double _previousKeyframeTime;

        [JsonProperty]
        private VDefinition _previousKeyframeValue;
        [JsonProperty]
        private double _initialKeyframeTime;
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
