// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;
using Framefield.Core;
using SharpDX;

namespace Framefield.Tooll.Components.SelectionView.ShowScene.TransformGizmo
{
    internal class PlaneGizmoPart : GizmoPart
    {
        public PlaneGizmoPart(int index, Vector3 origin, Vector3 diagonal, IEnumerable<GizmoParameter> relevantParameterList)
        {
            Index = index;
            _diagonal = diagonal;
            _boundingBox = new BoundingBox(origin, origin + diagonal);
            _plane = new Plane(origin + new Vector3(_diagonal.X, 0, 0),
                               origin + new Vector3(0, _diagonal.Y, 0),
                               origin + new Vector3(0, 0, _diagonal.Z));

            RelavantGizmoParameters = new Dictionary<GizmoParameterIds, GizmoParameter>();
            foreach (var parameter in relevantParameterList)
            {
                RelavantGizmoParameters[parameter.Identifier] = parameter;
            }
        }

        public override bool HitTestWithRayInObject(Ray rayInObject, out float hitDistance)
        {
            bool result = rayInObject.Intersects(ref _boundingBox, out hitDistance);
            if (result)
                _pointInObjectBeforeDrag = rayInObject.Position + rayInObject.Direction*hitDistance;

            return result;
        }

        private Vector3 _pointInObjectBeforeDrag;

        public override void DragUpdate(Ray rayInObject, Matrix gizmoToParent)
        {
            Vector3 dragPositionOnPlane;

            if (!rayInObject.Intersects(ref _plane, out dragPositionOnPlane))
                Logger.Warn("No intersection with drag plane {0}", _diagonal);

            Vector3 offset = (dragPositionOnPlane - _pointInObjectBeforeDrag)*_diagonal;
            Vector4 offsetInWorld = Vector4.Transform(new Vector4(offset, 0), gizmoToParent);

            RelavantGizmoParameters[GizmoParameterIds.TranslateX].Value = offsetInWorld.X + _parameterValuesBeforeDrag[GizmoParameterIds.TranslateX];
            RelavantGizmoParameters[GizmoParameterIds.TranslateY].Value = offsetInWorld.Y + _parameterValuesBeforeDrag[GizmoParameterIds.TranslateY];
            RelavantGizmoParameters[GizmoParameterIds.TranslateZ].Value = offsetInWorld.Z + _parameterValuesBeforeDrag[GizmoParameterIds.TranslateZ];
        }

        private readonly Vector3 _diagonal;
        private Plane _plane;
        private BoundingBox _boundingBox;
    }
}
