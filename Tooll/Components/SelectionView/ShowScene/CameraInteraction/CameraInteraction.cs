// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Framefield.Core;
using SharpDX;
using Key = System.Windows.Input.Key;
using Keyboard = System.Windows.Input.Keyboard;
using Point = System.Windows.Point;


namespace Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction
{
    /// <summary>
    /// Handles mouse and keyboard interaction of Scene Selection-Views.
    /// 
    /// The steps for a mouse-klick to a camera-interaction could be like this...
    /// 
    /// - WPF triggers MouseDown-event, which...
    /// - gets handled by MouseDown-Handler in ShowSceneControl, which...
    /// - calls HandleMouseDown
    /// - set _XXXmouseButtonPressed members
    /// 
    /// - on each frame WPF emits RenderCompositionTarget...
    /// - which gets handled by ShowSceneControl.App_CompositionTargertRenderingHandler
    /// - which calls UpdateAndCheckIfRedrawRequired()
    /// - checks if we need to render because...
    ///   
    ///   - Camera has been changed from outside, or
    ///   - asks the RawMouseInput for the latest MousePosition, and if...
    ///   - mouse is interactive with a TransformGizmo, or...
    ///   - Camera is been manipulated by Keyboard, Mouse (e.g. _XXXMouseButtonPressed), SpaceMouse, or...
    ///   - Camera is been updated by a transition (flicking, etc.)
    /// 
    /// For the transitions we slowly blend the scene's CameraPosition and CameraTarget into  
    /// _cameraTargetGoal and _cameraPositionGoal. If the distance between camera and its "goal" is
    /// below a threshold, the transition stops and we no longer need to update (e.g. rerender) the view.
    /// 
    /// If a camera-operator is selected it parameters get automatically manipulated, because we direcly 
    /// set the scene's CameraPosition and CameraTarget-parameters, which does this check for us.
    /// 
    /// </summary>
    public class CameraInteraction
    {

        #region Construction and Clean-Up

        public CameraInteraction(ShowSceneControl showSceneControl)
        {
            MaxMoveVelocity = (float) App.Current.ProjectSettings.GetOrSetDefault("Tooll.SelectionView.Camera.MaxVelocity", MAX_MOVE_VELOCITY_DEFAULT);
            _cameraAcceleration = (float)App.Current.ProjectSettings.GetOrSetDefault("Tooll.SelectionView.Camera.Acceleration", CAMERA_ACCELERATION_DEFAULT);
            _frictionKeyboardManipulation = (float) App.Current.ProjectSettings.GetOrSetDefault("Tooll.SelectionView.Camera.Friction", 0.3f);
            _showSceneControl = showSceneControl;

            _spaceMouse = new SpaceMouse(this, showSceneControl.RenderSetup);
            GizmoPartHitIndex = -1;

        }

        // Clean up Event handlers
        public void Discard()
        {
            _spaceMouse.Discard();
        }

        #endregion


        public bool UpdateAndCheckIfRedrawRequired()
        {
            if (_showSceneControl.RenderSetup == null)
                return false;

            // Stop all transitions when switching between camera-ops
            if (_showSceneControl.RenderSetup.CurrentCameraOp != _cameraOperator)
            {
                _cameraOperator = _showSceneControl.RenderSetup.CurrentCameraOp;
                _cameraPositionGoal = _showSceneControl.RenderSetup.CameraPosition;
                _cameraTargetGoal = _showSceneControl.RenderSetup.CameraTarget;
                MoveVelocity = Vector3.Zero;
                _isTransitionActive = false;
            }

            // This is an extremely unfortunate solution to check if the camera has been manipulated from the
            // outside (e.g. by another view, parameters or animation)
            if (!_isTransitionActive && (PositionDistance.Length() > STOP_DISTANCE_THRESHOLD || TargetDistance.Length() > STOP_DISTANCE_THRESHOLD)) {
                _cameraPositionGoal = _showSceneControl.RenderSetup.CameraPosition;
                _cameraTargetGoal = _showSceneControl.RenderSetup.CameraTarget;
                return true;               
            }

            var redrawRequired = false;
            UpdateRawMouseData();

            if (_lockInteraction)
                return false;

            _lockInteraction = true;
           
            // Manipulation...
            redrawRequired |= ManipulateGizmos();

            if (_showSceneControl.RenderSetup.TransformGizmo.State != TransformGizmo.TransformGizmo.GizmoStates.Dragged)
            {
                ManipulateCameraByMouse();
            }

            _manipulatedByKeyboard = ManipulateCameraByKeyboard();

            _spaceMouse.ManipulateCamera();

            // Transition...
            redrawRequired |= ComputeCameraMovement();

            _lockInteraction = false;

            return redrawRequired;
        }

        /* 
         * Returns false if camera didn't move
         */
        private bool ComputeCameraMovement()
        {
            var frameDurationFactor = (float) (App.Current.TimeSinceLastFrame)/FRAME_DURATION_AT_60_FPS;

            if (PositionDistance.Length() > STOP_DISTANCE_THRESHOLD
                || TargetDistance.Length() > STOP_DISTANCE_THRESHOLD
                || MoveVelocity.Length() > STOP_DISTANCE_THRESHOLD
                || _lookingAroundDelta.Length() > STOP_DISTANCE_THRESHOLD
                || _manipulatedByMouseWheel
                || _orbitDelta.Length() > 0.001f
                || _manipulatedByKeyboard)
            {
                if (_orbitDelta.Length() > 0.001f)
                {
                    OrbitByAngle(_orbitDelta);
                    _orbitDelta *= new Vector2(ORBIT_HORIZONTAL_FRICTION, ORBIT_VERTICAL_FRICTION)/frameDurationFactor;
                }

                if (MoveVelocity.Length() > MaxMoveVelocity)
                {
                    MoveVelocity *= MaxMoveVelocity/MoveVelocity.Length();
                }
                else if (!_manipulatedByKeyboard)
                {
                    MoveVelocity *= (1 - _frictionKeyboardManipulation)/frameDurationFactor;
                }

                _cameraPositionGoal += MoveVelocity;
                _cameraTargetGoal += MoveVelocity + _lookingAroundDelta;
                _lookingAroundDelta = Vector3.Zero;                

                PositionDistance *= CAMERA_MOVE_FRICTION / frameDurationFactor;
                TargetDistance *= CAMERA_MOVE_FRICTION / frameDurationFactor;

                _isTransitionActive = true;
                _manipulatedByMouseWheel = false;
                return true;
            }
            else
            {
                StopTransitionOfPositionTarget();
                _isTransitionActive = false;
                return false;
            }
        }

        #region WPF event handles for mouse-keys, -wheel and keyboard

        /**
         * WPF will discard some mouse-wheel events on slow framerates which leads to a laggy
         * interaction in complex scenes. For that reason, we include the framerate into the zoom-speed
         * and -- sadly -- avoid transitions for for zooming.
         */
        public void HandleMouseWheel(float delta)
        {
            var transitionActive = PositionDistance.Length() > STOP_DISTANCE_THRESHOLD || TargetDistance.Length() > STOP_DISTANCE_THRESHOLD;

            var viewDirection = transitionActive ? _cameraPositionGoal - _cameraTargetGoal
                                                 : _showSceneControl.RenderSetup.CameraPosition - _showSceneControl.RenderSetup.CameraTarget;

            var frameDurationFactor = (float)(App.Current.TimeSinceLastFrame) / FRAME_DURATION_AT_60_FPS;

            var zoomFactorForCurrentFramerate = 1 + (ZOOM_SPEED * frameDurationFactor);

            if (delta < 0)
            {
                viewDirection *= zoomFactorForCurrentFramerate;
            }
            else
            {
                viewDirection /= zoomFactorForCurrentFramerate;
            }
            
            _showSceneControl.RenderSetup.CameraPosition= _cameraPositionGoal = _cameraTargetGoal + viewDirection;
            _manipulatedByMouseWheel = true;
        }


        public void HandleMouseDown(MouseButton changedButton)
        {
            switch (changedButton)
            {
                case MouseButton.Left:
                    _showSceneControl.RenderSetup.TransformGizmo.HandleLeftMouseDown();
                    _leftMouseButtonPressed = true;
                    break;
                case MouseButton.Middle:
                    _middleMouseButtonPressed = true;
                    break;
                case MouseButton.Right:
                    _rightMouseButtonPressed = true;
                    _rightMousePressedAt = _mousePos;
                    break;
            }
        }

        private Point _rightMousePressedAt;

        /// <summary>
        /// Uses the original mouse event to update local parameters for dragging the camera.
        /// </summary>
        /// <param name="changedButton"></param>
        /// <returns>true if right click detected</returns>
        public bool HandleMouseUp(MouseButton changedButton)
        {
            var wasRightClick = false;
            switch (changedButton)
            {
                case MouseButton.Left:
                    _showSceneControl.RenderSetup.TransformGizmo.HandleLeftMouseUp();
                    _leftMouseButtonPressed = false;
                    break;

                case MouseButton.Middle:
                    _middleMouseButtonPressed = false;
                    break;

                case MouseButton.Right:
                    _rightMouseButtonPressed = false;

                    var dragDistances = (_rightMousePressedAt - _mousePos).Length;
                    wasRightClick = !AnyMouseButtonPressed() &&  dragDistances < SystemParameters.MinimumHorizontalDragDistance;
                    break;
            }
            return wasRightClick;
        }


        public void HandleKeyDown(KeyEventArgs e)
        {
            var key = e.Key;

            // Ignore non-interaction keys and anything if control is pressed
            if (!INTERACTION_KEYS.Contains(key) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                return;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                key = e.SystemKey; // If Alt ist pressed, key must be stored as systemkey

            if (key != Key.LeftAlt)
                _pressedKeys.Add(key);
        }


        public bool HandleKeyUp(KeyEventArgs e)
        {
            if (!INTERACTION_KEYS.Contains(e.Key) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                return true;

            _pressedKeys.Remove(e.Key);
            var key = e.Key;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                key = e.SystemKey;

            _pressedKeys.Remove(key);
            return false;
        }

        public void HandleFocusLost()
        {
            _pressedKeys.Clear();
        }

        #endregion


        #region Gizmo-Manimupulation


        private bool ManipulateGizmos()
        {
            if (!(_mouseMoveDelta.Length > MOUSE_MOVE_THRESHOLD))
                return false;

            if (_showSceneControl.RenderSetup.LastContext == null)
                return false;

            var rayInWorld = ComputeMouseViewRayInWorld(_showSceneControl.RenderSetup);
            var hitIndex = _showSceneControl.RenderSetup.TransformGizmo.CheckForManipulationByRay(rayInWorld);

            if (hitIndex == GizmoPartHitIndex)
                return false;
            
            GizmoPartHitIndex = hitIndex;
            _showSceneControl.RenderSetup.IndexOfGizmoPartBelowMouse = hitIndex; // Ugly hack so scene can set context-variable for hover
            return true;
        }


        private Ray ComputeMouseViewRayInWorld(D3DRenderSetup renderSetup)
        {
            var windowPos = _showSceneControl.PointFromScreen(_mousePos);

            var x = (float)(2.0 * windowPos.X / _showSceneControl.ActualWidth - 1);
            var y = (float)-(2.0 * windowPos.Y / _showSceneControl.ActualHeight - 1);

            var rayNds = new Vector3(x, y, 1.0f); // Normalized Device coordinates
            var rayClip = new Vector4(rayNds.X, rayNds.Y, -1, 1);

            var inverseProjection = (renderSetup.LastContext.CameraProjection);
            inverseProjection.Invert();

            var rayEye = Vector4.Transform(rayClip, inverseProjection);
            rayEye.Z = 1;
            rayEye.W = 0;

            var inverseViewMatrix = (renderSetup.LastContext.WorldToCamera);
            inverseViewMatrix.Invert();

            var rayDirectionInWorld = Vector4.Transform(rayEye, inverseViewMatrix);
            Vector3 raySourceInWorld = renderSetup.CameraPosition;
            var rayInWorld = new Ray(raySourceInWorld, new Vector3(rayDirectionInWorld.X, rayDirectionInWorld.Y, rayDirectionInWorld.Z));
            return rayInWorld;
        }
        #endregion


        #region Mouse interaction

        private void ManipulateCameraByMouse()
        {
            var altPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
            var ctrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            if (_leftMouseButtonPressed)
            {
                if (altPressed)
                {
                    Pan();
                }
                else if (ctrlPressed)
                {
                    LookAround();
                }
                else
                {
                    DragOrbit();
                }                    
            }
            else if (_rightMouseButtonPressed)
            {                
                Pan();
            }
            else if (_middleMouseButtonPressed)
            {
                LookAround();
            }

            _mouseDragDelta = new Vector();
        }


        private void LookAround()
        {
            Vector3 viewDir, sideDir, upDir;
            _showSceneControl.RenderSetup.CalcDirections(out viewDir, out sideDir, out upDir);

            var factorX = (float) (_mouseDragDelta.X/_showSceneControl.ActualHeight*ROTATE_MOUSE_SENSIVITY*Math.PI/180.0);
            var factorY = (float) (_mouseDragDelta.Y/_showSceneControl.ActualHeight*ROTATE_MOUSE_SENSIVITY*Math.PI/180.0);

            Matrix rotAroundX = Matrix.RotationAxis(sideDir, factorY);
            Matrix rotAroundY = Matrix.RotationAxis(upDir, factorX);
            Matrix rot = Matrix.Multiply(rotAroundX, rotAroundY);

            var viewDir2 = new Vector4(_cameraTargetGoal - _cameraPositionGoal, 1);
            var viewDirRotated = Vector4.Transform(viewDir2, rot);

            var newTarget = _cameraPositionGoal + new Vector3(viewDirRotated.X, viewDirRotated.Y , viewDirRotated.Z);
            _lookingAroundDelta = newTarget - _cameraTargetGoal;
        }


        private void DragOrbit()
        {
            _orbitDelta = new Vector2((float)(_mouseDragDelta.X / _showSceneControl.ActualHeight * ORBIT_SENSIVITY * Math.PI / 180.0),
                                      (float)(_mouseDragDelta.Y / _showSceneControl.ActualHeight * ORBIT_SENSIVITY * Math.PI / 180.0));
        }


        private void OrbitByAngle(Vector2 rotationSpeed)
        {
            Vector3 viewDir, sideDir, upDir;
            var currentTarget = _showSceneControl.RenderSetup.CameraTarget;

            D3DRenderSetup.CalcDirections(currentTarget, _cameraPositionGoal, 0, out viewDir, out sideDir, out upDir);

            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;

            Matrix rotAroundX = Matrix.RotationAxis(sideDir, rotationSpeed.Y);
            Matrix rotAroundY = Matrix.RotationAxis(upDir, rotationSpeed.X);
            Matrix rot = Matrix.Multiply(rotAroundX, rotAroundY);
            Vector4 newViewDir = Vector3.Transform(viewDir, rot);
            newViewDir.Normalize();

            // Set new position and freeze cam-target transitions
            _showSceneControl.RenderSetup.CameraPosition= _cameraPositionGoal = _showSceneControl.RenderSetup.CameraTarget - newViewDir.ToVector3()*viewDirLength;
            _cameraTargetGoal = currentTarget;
        }

        private void Pan()
        {
            Vector3 viewDir, sideDir, upDir;
            _showSceneControl.RenderSetup.CalcDirections(out viewDir, out sideDir, out upDir);

            var factorX = (float) (-_mouseDragDelta.X/_showSceneControl.ActualHeight);
            var factorY = (float) (_mouseDragDelta.Y/_showSceneControl.ActualHeight);

            var length = (_cameraTargetGoal - _cameraPositionGoal).Length();
            sideDir *= factorX * length;
            upDir *= factorY * length;

            _cameraPositionGoal += sideDir + upDir;
            _cameraTargetGoal += sideDir + upDir;
        }
        #endregion


        #region Keyboard Interaction


        void StopTransitionOfPositionTarget()
        {
            _cameraTargetGoal = _showSceneControl.RenderSetup.CameraTarget;
            _cameraPositionGoal = _showSceneControl.RenderSetup.CameraPosition;
        }

        private bool ManipulateCameraByKeyboard()
        {
            var frameDurationFactor = (float) (App.Current.TimeSinceLastFrame);

            Vector3 viewDir, sideDir, upDir;
            _showSceneControl.RenderSetup.CalcDirections(out viewDir, out sideDir, out upDir);

            var viewDirLength = viewDir.Length();
            var initialVelocity = MoveVelocity.Length() < STOP_DISTANCE_THRESHOLD ? INITIAL_MOVE_VELOCITY : 0;

            var increaseAccelerationWithZoom = (_cameraPositionGoal - _cameraTargetGoal).Length()/10f;
            var accelFactor = _cameraAcceleration*increaseAccelerationWithZoom;

            var interactionKeysPressedCount = 0;
            foreach (var key in _pressedKeys)
            {
                switch (key)
                {
                    case Key.A:
                    case Key.Left:
                        MoveVelocity -= sideDir * (accelFactor + initialVelocity) * frameDurationFactor;
                        interactionKeysPressedCount++;
                        break;
                    case Key.D:
                    case Key.Right:
                        MoveVelocity += sideDir * (accelFactor + initialVelocity) * frameDurationFactor;
                        interactionKeysPressedCount++;
                        break;
                    case Key.W:
                    case Key.Up:
                        MoveVelocity += viewDir * (accelFactor + initialVelocity) / viewDirLength * frameDurationFactor;
                        interactionKeysPressedCount++;
                        break;
                    case Key.S:
                    case Key.Down:
                        MoveVelocity -= viewDir * (accelFactor + initialVelocity) / viewDirLength * frameDurationFactor;
                        interactionKeysPressedCount++;
                        break;
                    case Key.E:
                        MoveVelocity += upDir * (accelFactor + initialVelocity) * frameDurationFactor;
                        interactionKeysPressedCount++;
                        break;
                    case Key.X:
                        MoveVelocity -= upDir * (accelFactor + initialVelocity) * frameDurationFactor;
                        interactionKeysPressedCount++;
                        break;
                    case Key.F:                        
                        MoveVelocity = Vector3.Zero;
                        _cameraPositionGoal = new Vector3(0, 0, DEFAULT_CAMERA_POSITION_Z);
                        _cameraTargetGoal = new Vector3(0, 0, 0f);
                        interactionKeysPressedCount++;
                        break;
                    // Center Camera
                    case Key.C:
                        var delta = _showSceneControl.RenderSetup.TransformGizmo.IsGizmoActive
                            ? _showSceneControl.RenderSetup.TransformGizmo.GizmoToWorld.TranslationVector - _cameraTargetGoal
                            : -_cameraTargetGoal;

                        _cameraTargetGoal += delta;
                        _cameraPositionGoal += delta;
                        interactionKeysPressedCount++;
                        break;
                }
            }
            return interactionKeysPressedCount > 0;            
        }

        #endregion

        public const float DEFAULT_CAMERA_POSITION_Z = -2.415f; // matches a 2-unit height rectangle in origin at 45 degree FOV

        #region RAW-MouseData

        /**
         * Because MouseMove event is not triggered if CompositionTarget.Render
         * gets too complex, this method uses raw input from user32 as a workaround
         * to provide interactive mouse rotation.
         * 
         * Because MouseDown/MouseUp events are also not triggered, we have to
         * rely on the virtual key for the left mouse button to check if a dragging
         * is currently happening.
         * 
         * Read more at https://streber.framefield.com/5381
         */

        private void UpdateRawMouseData()
        {
            Win32RawInput.POINT screenSpacePoint;
            Win32RawInput.GetCursorPos(out screenSpacePoint);

            // note that screenSpacePoint is in screen-space pixel coordinates, 
            // not the same WPF Units you get from the MouseMove event. 
            // You may want to convert to WPF units when using GetCursorPos.
            var currentMousePosition = new Point(screenSpacePoint.X,
                                                 screenSpacePoint.Y);
            const int LEFT_BUTTON_VIRTUAL_KEY_CODE = 0x01;
            const int RIGHT_BUTTON_VIRTUAL_KEY_CODE = 0x02;
            const int MIDDLE_BUTTON_VIRTUAL_KEY_CODE = 0x04;

            /**
             * sadly, we can't use this flags for anything more that determing with "something" was dragged with
             * "some" mouseButton. We can't assign this flags to the members controlled by the MouseDown/Up handlers
             * because this would interfer with the focus-state can the window's mouse capute and thus cause camera
             * manipulation even if the mouse is no longer inside the window. I know, that this is a mindfuck.
             */
            var leftMousePressed = Convert.ToInt32(Win32RawInput.GetAsyncKeyState(LEFT_BUTTON_VIRTUAL_KEY_CODE)) != 0;
            var rightMousePressed = Convert.ToInt32(Win32RawInput.GetAsyncKeyState(RIGHT_BUTTON_VIRTUAL_KEY_CODE)) != 0;
            var middleMousePressed = Convert.ToInt32(Win32RawInput.GetAsyncKeyState(MIDDLE_BUTTON_VIRTUAL_KEY_CODE)) != 0;
            var mousePressed = leftMousePressed || rightMousePressed || middleMousePressed;

            if (_showSceneControl.IsFocused && mousePressed)
            {
                if (_mouseWasReleased)
                {
                    _mouseDragDelta = new Vector();
                    _mouseWasReleased = false;
                }
                else
                {
                    _mouseDragDelta = currentMousePosition - _mousePos;
                }
            }
            else
            {
                _mouseDragDelta = new Vector();
                _mouseWasReleased = true;
            }
            _mouseMoveDelta = currentMousePosition - _mousePos;
            _mousePos = currentMousePosition;
        }

        #endregion

        public bool AnyMouseButtonPressed()
        {
            return _leftMouseButtonPressed || _middleMouseButtonPressed || _rightMouseButtonPressed;
        }

        public int GizmoPartHitIndex { get; private set; }

        private Vector3 _cameraTargetGoal = Vector3.Zero;
        Vector3 TargetDistance
        {
            get { return _cameraTargetGoal - _showSceneControl.RenderSetup.CameraTarget; }
            set { _showSceneControl.RenderSetup.CameraTarget = _cameraTargetGoal - value; }
        }

        private Vector3 _cameraPositionGoal = new Vector3(0, 0, CameraInteraction.DEFAULT_CAMERA_POSITION_Z); 
        Vector3 PositionDistance
        {
            get { return _cameraPositionGoal - _showSceneControl.RenderSetup.CameraPosition; }
            set { _showSceneControl.RenderSetup.CameraPosition = _cameraPositionGoal - value; }
        }
       
        private Vector3 _lookingAroundDelta = Vector3.Zero;
        private Vector2 _orbitDelta;
        private const float ROTATE_MOUSE_SENSIVITY = 300;
        private const float ORBIT_SENSIVITY = 200;
        private const float ORBIT_HORIZONTAL_FRICTION = 0.92f;
        private const float ORBIT_VERTICAL_FRICTION = 0.86f;    // A bit less to avoid sliding into gimbal lock

        private const float MOUSE_MOVE_THRESHOLD = 0.01f;
        private const float INITIAL_MOVE_VELOCITY = 0.0f;
        const float CAMERA_MOVE_FRICTION = 0.80f;

        private const float ZOOM_SPEED = 0.2f;
        private const float STOP_DISTANCE_THRESHOLD = 0.001f;
        private const float FRAME_DURATION_AT_60_FPS = 0.016f;

        private const float MAX_MOVE_VELOCITY_DEFAULT = 2f;
        private const float CAMERA_ACCELERATION_DEFAULT = 1f;

        private Operator _cameraOperator;
        private readonly ShowSceneControl _showSceneControl;

        private bool _isTransitionActive;
        private bool _manipulatedByMouseWheel;
        private bool _manipulatedByKeyboard;

        private bool _leftMouseButtonPressed;
        private bool _middleMouseButtonPressed;
        private bool _rightMouseButtonPressed;


        /**
         * This list is an ugly repetion of the keys checked in ManiplulateCameryByKeyboard.
         * We need it to keep up a an array of pressed keys. This is prone to errors and should be refactored
         */
        private static readonly List<Key> INTERACTION_KEYS = new List<Key>
                                                                 {
                                                                     Key.A,
                                                                     Key.S,
                                                                     Key.D,
                                                                     Key.E,
                                                                     Key.X,
                                                                     Key.F,
                                                                     Key.C,
                                                                     Key.W,
                                                                 };

        internal Vector3 MoveVelocity = Vector3.Zero;   // required by space-mouse
        internal readonly float MaxMoveVelocity; // set from Project-Settings in Constructor!

        private Point _mousePos;
        private Vector _mouseMoveDelta;
        private bool _mouseWasReleased = true;
        private bool _lockInteraction;
        //private bool _redrawRequired;

        private Vector _mouseDragDelta;
        private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();
        private readonly float _frictionKeyboardManipulation; // set from Project-Settings in Constructor!
        private readonly float _cameraAcceleration; // set from Project-Settings in Constructor!
        private readonly SpaceMouse _spaceMouse;
    }
}