// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using Framefield.Core;
using SharpDX;

namespace Framefield.Tooll.Components.SelectionView.ShowScene.TransformGizmo
{
    internal class AxisGizmoPart : GizmoPart
    {
        private const float BOUNDING_BOX_THINKNESS = 0.175f;
        private const float AXIS_BOUNDINGBOX_LENGTH = 5f;
        private const float TOLERANCE = 0.00001f;
        private readonly Vector3 _axis;
        private BoundingBox _boundingBox;
        private Plane _plane;
        private Vector3 _pointInObjectBeforeDrag;

        /// axis 5, 0, 0
        /// bb (0, -0.1, -0.1) -> (5, 0.1, 0.1)
        public AxisGizmoPart(int index, Vector3 origin, Vector3 axis, IEnumerable<GizmoParameter> relevantParameterList)
        {
            Index = index;
            _axis = axis;

            var bbStart = new Vector3(Math.Abs(axis.X) < TOLERANCE ? -BOUNDING_BOX_THINKNESS : 0,
                                      Math.Abs(axis.Y) < TOLERANCE ? -BOUNDING_BOX_THINKNESS : 0,
                                      Math.Abs(axis.Z) < TOLERANCE ? -BOUNDING_BOX_THINKNESS : 0);

            var bbEnd = new Vector3(Math.Abs(axis.X) < TOLERANCE ? BOUNDING_BOX_THINKNESS : axis.X*AXIS_BOUNDINGBOX_LENGTH,
                                    Math.Abs(axis.Y) < TOLERANCE ? BOUNDING_BOX_THINKNESS : axis.Y*AXIS_BOUNDINGBOX_LENGTH,
                                    Math.Abs(axis.Z) < TOLERANCE ? BOUNDING_BOX_THINKNESS : axis.Z*AXIS_BOUNDINGBOX_LENGTH);

            _boundingBox = new BoundingBox(bbStart, bbEnd);


            /* Note implementing the axis transformation with a drag-plane is probably not a smart idea
             * because it will get unstable on steap angles.
             */
            _plane = new Plane();

            if ((axis - Vector3.UnitX).Length() < TOLERANCE)
            {
                _plane = new Plane(origin, origin + Vector3.UnitX, origin + Vector3.UnitY);
            }
            else if ((axis - Vector3.UnitY).Length() < TOLERANCE)
            {
                _plane = new Plane(origin, origin + Vector3.UnitX, origin + Vector3.UnitY);
            }
            else if ((axis - Vector3.UnitZ).Length() < TOLERANCE)
            {
                _plane = new Plane(origin, origin + Vector3.UnitZ, origin + Vector3.UnitY);
            }
            else
            {
                Logger.Error("Sorry, but the axis gizmo component only works along a single axis. Therefore {0} is not a valid axis", axis);
            }

            RelavantGizmoParameters = new Dictionary<GizmoParameterIds, GizmoParameter>();
            foreach (GizmoParameter parameter in relevantParameterList)
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

        public override void DragUpdate(Ray rayInObject, Matrix gizmoToParent)
        {
            Vector3 dragPositionOnPlane;

            if (!rayInObject.Intersects(ref _plane, out dragPositionOnPlane))
                Logger.Warn("No intersection with drag plane {0}", _axis);

            Vector3 offset = (dragPositionOnPlane - _pointInObjectBeforeDrag)*_axis;
            Vector4 offsetInWorld = Vector4.Transform(new Vector4(offset, 0), gizmoToParent);

            RelavantGizmoParameters[GizmoParameterIds.TranslateX].Value = offsetInWorld.X + _parameterValuesBeforeDrag[GizmoParameterIds.TranslateX];
            RelavantGizmoParameters[GizmoParameterIds.TranslateY].Value = offsetInWorld.Y + _parameterValuesBeforeDrag[GizmoParameterIds.TranslateY];
            RelavantGizmoParameters[GizmoParameterIds.TranslateZ].Value = offsetInWorld.Z + _parameterValuesBeforeDrag[GizmoParameterIds.TranslateZ];
        }
    }
}