// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core.Curve;

namespace Framefield.Core
{
    public static class Animation
    {
        public static void SetOperatorPartValue(OperatorPart opPart, double time, float value)
        {
            var animationOpPart = GetRegardingAnimationOpPart(opPart);
            if (animationOpPart != null)
            {
                var animationCurve = animationOpPart.Func as ICurve;
                Utils.AddKeyframeAtTime(animationCurve, time, value);
            }
            else
            {
                opPart.Func = Utilities.CreateValueFunction(new Float(value));
            }
        }

        public static OperatorPart GetRegardingAnimationOpPart(OperatorPart input)
        {
            var animationOpPart = FindOpPartWithFunctorTypeInSubTree<ICurve>(input, 3);
            var timeAccOpPart = FindOpPartWithFunctorTypeInSubTree<OperatorPartTraits.ITimeAccessor>(animationOpPart, 3);
            if (animationOpPart != null && timeAccOpPart != null)
                return animationOpPart;

            return null;
        }

        public static OperatorPart FindOpPartWithFunctorTypeInSubTree<T>(OperatorPart opPart, int maxDepth) where T : class
        {
            if (opPart == null || maxDepth < 0)
                return null;

            var castedOpPart = opPart.Func as T;
            if (castedOpPart != null)
                return opPart;

            foreach (var input in opPart.Connections)
            {
                var foundElement = FindOpPartWithFunctorTypeInSubTree<T>(input, maxDepth - 1);
                if (foundElement != null)
                    return foundElement;
            }

            return null;
        }
    }
}
