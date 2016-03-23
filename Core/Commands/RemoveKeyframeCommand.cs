// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core.Curve;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Framefield.Core.Commands
{
    public class RemoveKeyframeCommand : ICommand
    {
        public string Name { get { return "Remove Keyframe"; } }
        public bool IsUndoable { get { return true; } }

        public double[] KeyframeTimes
        {
            get { return _valuesForKeyframeRestoration.Select(value => value.KeyframeTime).ToArray(); }
        }

        public VDefinition[] KeyframeValues
        {
            get { return _valuesForKeyframeRestoration.Select(value => value.KeyframeValue).ToArray(); }
        }

        [JsonConstructor]
        private RemoveKeyframeCommand() { }

        public RemoveKeyframeCommand(IEnumerable<Tuple<double, ICurve>> timeCurveTuples, double globalTime)
        {
            _valuesForKeyframeRestoration = new List<ValuesForKeyframeRestoration>();

            foreach (var timeWithCurve in timeCurveTuples)
            {
                var curve = timeWithCurve.Item2;
                var time = timeWithCurve.Item1;
                var func = curve as OperatorPart.Function;
                var curveOpPart = func.OperatorPart;
                _valuesForKeyframeRestoration.Add(new ValuesForKeyframeRestoration
                                                  {
                                                      KeyframeTime = time,
                                                      KeyframeValue = curve.GetV(time),
                                                      CurveOpToAddKeyframeInstanceID = curveOpPart.Parent.ID,
                                                      CurveOpToAddKeyframeMetaID = curveOpPart.Parent.Definition.ID
                                                  });
                FindAndSetOpPartAndLastValueOfCurve(globalTime, curveOpPart);
            }
        }

        public RemoveKeyframeCommand(Tuple<double, ICurve> timeCurveTuple, double globalTime)
            : this(new[] { timeCurveTuple }, globalTime)
        {
        }

        public void Do()
        {
            _commands.Clear();
            for (var i = 0; i < _valuesForKeyframeRestoration.Count; i++)
            {
                var curveOpPart = GetCurveOpPartAtIndex(i);
                var curve = curveOpPart.Func as ICurve;
                curve.RemoveV(_valuesForKeyframeRestoration[i].KeyframeTime);
                if (!curve.GetPoints().Any())
                {
                    var opPart = GetOperatorPart(_opPartsAndLastValues[curveOpPart.Parent.ID].Item1);
                    var removeAnimationCommand = new RemoveAnimationCommand(opPart, _opPartsAndLastValues[curveOpPart.Parent.ID].Item2);
                    _commands.Add(removeAnimationCommand);
                    removeAnimationCommand.Do();
                }
            }
        }


        public void Undo()
        {
            foreach (var command in _commands)
            {
                command.Undo();
            }

            for (var i = 0; i < _valuesForKeyframeRestoration.Count; i++)
            {
                var curveOpPart = GetCurveOpPartAtIndex(i);
                var curve = curveOpPart.Func as ICurve;
                curve.AddOrUpdateV(_valuesForKeyframeRestoration[i].KeyframeTime, _valuesForKeyframeRestoration[i].KeyframeValue);
            }

        }

        private OperatorPart GetCurveOpPartAtIndex(int i)
        {
            var curveMeta = MetaManager.Instance.GetMetaOperator(_valuesForKeyframeRestoration[i].CurveOpToAddKeyframeMetaID);
            var curveInstance = curveMeta.GetOperatorInstance(_valuesForKeyframeRestoration[i].CurveOpToAddKeyframeInstanceID);
            var curveOpPart = curveInstance.InternalParts[0];

            return curveOpPart;
        }

        private static OperatorPart GetOperatorPart(IdsToFindOpPart ids)
        {
            var parentMeta = MetaManager.Instance.GetMetaOperator(ids.ParentMetaId);
            var parentInstance = parentMeta.GetOperatorInstance(ids.ParentId);
            var opPart = parentInstance.Inputs.SingleOrDefault(input => input.ID == ids.OpPartId);

            return opPart;
        }

        private void FindAndSetOpPartAndLastValueOfCurve(double globalTime, OperatorPart curveOpPart)
        {
            var opPart = Utils.GetOperatorPartBelongingToCurve(curveOpPart.Func as ICurve);
            if (opPart == null)
                opPart = curveOpPart;

            var lastValue = Utils.GetCurrentValueAtTime(opPart, globalTime);
            _opPartsAndLastValues[curveOpPart.Parent.ID] = new Tuple<IdsToFindOpPart, float>(new IdsToFindOpPart
                                                                                             {
                                                                                                 OpPartId = opPart.ID,
                                                                                                 ParentId = opPart.Parent.ID,
                                                                                                 ParentMetaId = opPart.Parent.Definition.ID
                                                                                             }, lastValue);
        }

        [JsonProperty]
        private readonly List<ValuesForKeyframeRestoration> _valuesForKeyframeRestoration;

        [JsonProperty]
        private readonly Dictionary<Guid, Tuple<IdsToFindOpPart, float>> _opPartsAndLastValues = new Dictionary<Guid, Tuple<IdsToFindOpPart, float>>();

        private readonly List<ICommand> _commands = new List<ICommand>();

        private struct ValuesForKeyframeRestoration
        {
            public VDefinition KeyframeValue;
            public double KeyframeTime;
            public Guid CurveOpToAddKeyframeMetaID;
            public Guid CurveOpToAddKeyframeInstanceID;
        }

        private struct IdsToFindOpPart
        {
            public Guid OpPartId;
            public Guid ParentId;
            public Guid ParentMetaId;
        }
    }
}
