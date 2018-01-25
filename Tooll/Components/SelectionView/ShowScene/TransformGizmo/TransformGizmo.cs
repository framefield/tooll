// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using SharpDX;
using Utilities = Framefield.Core.Utilities;

namespace Framefield.Tooll.Components.SelectionView.ShowScene.TransformGizmo
{
    public class Gizmo
    {
        internal GizmoPart[] GizmoParts;
    }

    /// <summary>
    ///     The implementation of gizmos is more complicated that you might think. This is due to
    ///     a number of abstractions we have to make:
    /// 
    ///     1. We have to rely on RawMouse-Input.
    ///     2. We don't know the Transform-Operator and its local ObjectToWorld before its being evaluated.
    ///     3. The TransformGizmo is a combination of several GizmoParts that only affect a limited number of the parameters.
    ///     4. We need encapsulate the actual manipulation into serializable commands for Undo/Redo.
    ///     5. We want to reuse as much of the implementation of the Gizmo-Parts as possible.
    ///     6. The camera-Matrix is handled by the D3DRenderSetup, where as the CameraInteraction is handled by the ShowSceneControl.
    /// 
    ///     For the reasons, we made the following decissions:
    ///     We added a new abstraction-entity "GizmoParameter" that allows us to define the relevant
    ///     parameters for a GizmoPart without precisely knowing the internals of a TransformOperator.
    ///     These Gizmo-Parameters are also used for caching is current Values from evaluation. To access
    ///     these Parameters we use the GizmoParameterIds-Enumeration.  The connection between these Parameters
    ///     and the real OperatorParts is made in InitializeParameterInputs() when we hook up the selected
    ///     transform Operator before rendering.
    ///     A Gizmo is made of GizmoParts, each controlling a subset of Parameters. In CheckForManipulationByRay() we
    ///     do a hitTest for all parts and mark the closest one as _activeGizmo. On MouseDown we start manipulation
    ///     with it. In the constructor of the gizmo, we also give each GizmoPart a unique index with is later
    ///     used inside the [TransformGizmo] operator to highlight it on hover.
    ///     The actual rendering of the Gizmo is down be evaluating an instance of the [TransformGizmo]-Operator. To
    ///     setup its location and status, we initialize its context with the correct ObjectToWorld-Matrix and a bunch
    ///     of context-Variables to do things like highlighting the active GizmoPart.
    ///     The actual hit test is done in the Gizmo's object space. For this the viewRay is transformed from worldspace
    ///     into that one.
    /// </summary>
    public class TransformGizmo : Gizmo
    {
        public TransformGizmo()
        {
            MetaOperator sceneTransformGizmoDefinition = MetaManager.Instance.GetMetaOperator(Guid.Parse("2e56b389-a983-42fe-a015-61d9f9532af4"));
            _sceneTransformGizmoOperator = sceneTransformGizmoDefinition.CreateOperator(Guid.Empty);

            GizmoParts = new GizmoPart[]
                             {
                                 new AxisGizmoPart(0, Vector3.Zero, Vector3.UnitX, new[] { _translateXParam, _translateYParam, _translateZParam }),
                                 new AxisGizmoPart(1, Vector3.Zero, Vector3.UnitY, new[] { _translateXParam, _translateYParam, _translateZParam }),
                                 new AxisGizmoPart(2, Vector3.Zero, Vector3.UnitZ, new[] { _translateXParam, _translateYParam, _translateZParam }),
                                 new PlaneGizmoPart(3, Vector3.Zero, new Vector3(1, 1, 0), new[] { _translateXParam, _translateYParam, _translateZParam }),
                                 new PlaneGizmoPart(4, Vector3.Zero, new Vector3(1, 0, 1), new[] { _translateXParam, _translateYParam, _translateZParam }),
                                 new PlaneGizmoPart(5, Vector3.Zero, new Vector3(0, 1, 1), new[] { _translateXParam, _translateYParam, _translateZParam }),
                             };
        }

        #region input groups
        private Vec3InputGroup _translateInputs;
        private Vec3InputGroup _pivotInputs;
        private Vec3InputGroup _rotateInputs;
        private Vec3InputGroup _scaleInputs;

        struct Vec3InputGroup
        {
            public OperatorPart X;
            public OperatorPart Y;
            public OperatorPart Z;
            public bool Valid { get { return X != null && Y != null && Z != null; } }

            public Vector3 EvaluateOrDefault(OperatorPartContext context, Vector3 fallBack)
            {
                return Valid ? new Vector3(X.Eval(context).Value, Y.Eval(context).Value, Z.Eval(context).Value)
                             : fallBack;
            }
        }
        private Vec3InputGroup FindVec3Inputs(Operator op, IEnumerable<string> names)
        {
            foreach (var groupName in names)
            {
                var inputGroup = FindVec3Inputs(op, groupName);
                if (inputGroup.Valid)
                    return inputGroup;
            }
            return new Vec3InputGroup();
        }

        private Vec3InputGroup FindVec3Inputs(Operator op, String name)
        {
            return new Vec3InputGroup()
            {
                X = FindInput(op, name + ".X"),
                Y = FindInput(op, name + ".Y"),
                Z = FindInput(op, name + ".Z")
            };
        }

        OperatorPart FindInput(Operator op, String name)
        {
            OperatorPart matchingInput = null;
            foreach (var input in op.Inputs.Where(input => input.Name == name))
            {
                matchingInput = input;
            }
            return matchingInput;
        }
        #endregion


        public bool SetupEvalCallbackForSelectedTransformOperators(Operator[] selectedOperators, OperatorPartContext context)
        {
            _transformGizmoTargetOp = null;
            _transformGizmoTargetValueFunction = null;

            if (selectedOperators.Length != 1)
                return false;

            var op = selectedOperators.First();
            if (op.InternalParts.Count > 0 && op.InternalParts[0].Func is ICameraProvider)
                return false;

            _translateInputs = FindVec3Inputs(op, new String[] { "Center", "Position", "Translate", "Translation", "Move" });
            _pivotInputs = FindVec3Inputs(op, "Pivot");
            _rotateInputs = FindVec3Inputs(op, "Rotate");
            _scaleInputs = FindVec3Inputs(op, "Scale");

            if (!_translateInputs.Valid)
                return false;

            _context = context;
            _transformGizmoTargetOp = op;
            _transformGizmoTargetValueFunction = op.Outputs[0].Func as Utilities.ValueFunction;
            _transformGizmoTargetValueFunction.EvaluatedEvent += GizmoValueFunc_EvaluatedEvent; // SetupContextForRenderingCamToBuffer Evaluation Callback                
            _translateXParam.Input = _translateInputs.X;
            _translateYParam.Input = _translateInputs.Y;
            _translateZParam.Input = _translateInputs.Z;
            return true;
        }

        /*
         * Returns index of GizmoPart below mouse or -1 if nothing hit
         */
        public int CheckForManipulationByRay(Ray rayInWorld)
        {
            if (State != GizmoStates.Dragged)
            {
                _activeGizmoPart = FindGizmoPartHitByRay(rayInWorld);

                if (_activeGizmoPart != null)
                {
                    State = GizmoStates.Hover;
                    return _activeGizmoPart.Index;
                }

                State = GizmoStates.Normal;
                return -1;
            }

            App.Current.UpdateRequiredAfterUserInteraction = true; // Fix me: needs to be optimized only update render views
            Ray rayInObjectBeforeDrag = ComputeRayInObject(rayInWorld, _gizmoToWorldBeforeDrag);
            _activeGizmoPart.UpdateManipulation(rayInObjectBeforeDrag, _gizmoToParent);
            return _activeGizmoPart.Index;
        }




        private GizmoPart FindGizmoPartHitByRay(Ray rayInWorld)
        {
            if (_transformGizmoTargetOp == null)
                return null;

            _rayInObject = ComputeRayInObject(rayInWorld, GizmoToWorld);

            var minDistance = (float)Double.PositiveInfinity;
            GizmoPart nearestPartHitByRay = null;

            foreach (GizmoPart gizmoPart in GizmoParts)
            {
                float hitDistance;
                bool wasHit = gizmoPart.HitTestWithRayInObject(_rayInObject, out hitDistance);
                if (wasHit && hitDistance < minDistance)
                {
                    nearestPartHitByRay = gizmoPart;
                    minDistance = hitDistance;
                }
            }
            return nearestPartHitByRay;
        }


        public void RenderTransformGizmo(OperatorPartContext context)
        {
            if (_transformGizmoTargetOp == null || !_transformGizmoTargetOp.Inputs.Any())
            {
                IsGizmoActive = false;
                return;
            }
            IsGizmoActive = true;

            var invalidator3 = new OperatorPart.InvalidateAllVariableAccessors();
            _sceneTransformGizmoOperator.Outputs[0].TraverseWithFunction(null, invalidator3);
            _sceneTransformGizmoOperator.Outputs[0].TraverseWithFunctionUseSpecificBehavior(null, invalidator3);

            GizmoToWorld = ObjectToWorldWithGizmo(context);

            var keepObjectToWorld = context.ObjectTWorld;
            context.ObjectTWorld = GizmoToWorld;

            _sceneTransformGizmoOperator.Outputs[0].Eval(context);

            context.ObjectTWorld = keepObjectToWorld;

            if (_transformGizmoTargetValueFunction != null)
                _transformGizmoTargetValueFunction.EvaluatedEvent -= GizmoValueFunc_EvaluatedEvent;
        }


        private Matrix ObjectToWorldWithGizmo(OperatorPartContext context)
        {
            Matrix transformIncludingOpTransform = Matrix.Identity;

            Vector3 orgScale;
            Quaternion orgRotate;
            Vector3 orgTranslate;
            _originalObjectToWorld.Decompose(out orgScale, out orgRotate, out orgTranslate);

            Matrix transformWithNormalizedScale = Matrix.Transformation(Vector3.Zero, Quaternion.Zero, Vector3.One, Vector3.Zero,
                                                                        orgRotate,
                                                                        orgTranslate);

            var translate = _translateInputs.EvaluateOrDefault(context, Vector3.Zero);
            var rotate = _rotateInputs.EvaluateOrDefault(context, Vector3.Zero);
            var pivot = _pivotInputs.EvaluateOrDefault(context, Vector3.Zero);

            var tmpMatrix = context.ObjectTWorld * _gizmoToParent;
            var scaleFactor = Vector4.Transform(tmpMatrix.Row4, context.WorldToCamera).Z / 20f;

            Quaternion rotationByOp = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(rotate.Y),
                                                                      MathUtil.DegreesToRadians(rotate.X),
                                                                      MathUtil.DegreesToRadians(rotate.Z));


            _gizmoToParent = Matrix.Transformation(Vector3.Zero,
                                                   Quaternion.Zero,
                                                   new Vector3(scaleFactor),
                                                   Vector3.Zero,
                                                   rotationByOp,
                                                   translate);

            _translateXParam.Value = translate.X;
            _translateYParam.Value = translate.Y;
            _translateZParam.Value = translate.Z;

            transformIncludingOpTransform = _gizmoToParent * Matrix.Translation(pivot.X, pivot.Y, pivot.Z) *
                                            transformWithNormalizedScale;

            return transformIncludingOpTransform;
        }

        private Matrix _gizmoToParent;
        public int IndexOfGizmoPartBelowMouse { get; set; }

        public void HandleLeftMouseDown()
        {
            if (State == GizmoStates.Hover)
            {
                _gizmoToWorldBeforeDrag = GizmoToWorld;
                _activeGizmoPart.StartManimpulation();
                State = GizmoStates.Dragged;
            }
        }

        public void HandleLeftMouseUp()
        {
            if (State != GizmoStates.Dragged)
                return;

            if (_activeGizmoPart != null)
                _activeGizmoPart.CompleteManipulation();
            else
            {
                Logger.Warn("ActiveGizmo-Part undefined before MouseUp");
            }
            State = GizmoStates.Normal;
        }


        private Ray ComputeRayInObject(Ray rayInWorld, Matrix gizmoToWorld)
        {
            Matrix worldToObject = gizmoToWorld;
            worldToObject.Invert();

            var rayDirectionInWorld = new Vector4(rayInWorld.Direction.X, rayInWorld.Direction.Y, rayInWorld.Direction.Z, 0);
            Vector4 rayDirectionInObject = Vector4.Transform(rayDirectionInWorld, worldToObject);

            var rayPositionInWorld = new Vector4(rayInWorld.Position.X, rayInWorld.Position.Y, rayInWorld.Position.Z, 1);
            Vector4 rayPositionInObject = Vector4.Transform(rayPositionInWorld, worldToObject);

            var rayInObject = new Ray(
                rayPositionInObject.ToVector3(),
                new Vector3(rayDirectionInObject.X, rayDirectionInObject.Y, rayDirectionInObject.Z));
            return rayInObject;
        }


        private void GizmoValueFunc_EvaluatedEvent(object sender, EventArgs e)
        {
            _originalObjectToWorld = _context.ObjectTWorld;
        }

        public enum GizmoStates
        {
            Normal,
            Hover,
            Dragged
        }

        private readonly GizmoParameter _translateXParam = new GizmoParameter { Identifier = GizmoParameterIds.TranslateX };
        private readonly GizmoParameter _translateYParam = new GizmoParameter { Identifier = GizmoParameterIds.TranslateY };
        private readonly GizmoParameter _translateZParam = new GizmoParameter { Identifier = GizmoParameterIds.TranslateZ };

        private OperatorPartContext _context;

        public GizmoStates State { get; set; }
        public bool IsGizmoActive { get; set; }
        private Ray _rayInObject;
        private GizmoPart _activeGizmoPart;
        private Matrix _originalObjectToWorld = Matrix.Identity; // before transformed by gizmo
        public Matrix GizmoToWorld { get; private set; } // includes transform by gizmo
        private Matrix _gizmoToWorldBeforeDrag;
        //private ITransformGizmoTarget _transformGizmoOutputFunc;
        private Operator _transformGizmoTargetOp;
        private Utilities.ValueFunction _transformGizmoTargetValueFunction;
        private readonly Operator _sceneTransformGizmoOperator;
    }
}