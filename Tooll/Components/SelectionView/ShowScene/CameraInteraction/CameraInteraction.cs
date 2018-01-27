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
using Framefield.Tooll.Rendering;

namespace Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction
{
    /// <summary>
    /// Handles mouse and keyboard interaction of Scene Selection-Views.
    /// 
    /// The steps for a mouse-klick to a camera-interaction could be like this...
    /// 
    /// - WPF triggers MouseDown-event, which...
    /// - gets handled by MouseDown-Handler in ShowContentControl, which...
    /// - calls HandleMouseDown
    /// - set _XXXmouseButtonPressed members
    /// 
    /// - on each frame WPF emits RenderCompositionTarget...
    /// - which gets handled by ShowContentControl.App_CompositionTargertRenderingHandler
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
        public CameraInteraction(RenderViewConfiguration renderConfig, ShowContentControl showContentControl)
        {
            MaxMoveVelocity = (float)App.Current.ProjectSettings.GetOrSetDefault("Tooll.SelectionView.Camera.MaxVelocity", MAX_MOVE_VELOCITY_DEFAULT);
            _cameraAcceleration = (float)App.Current.ProjectSettings.GetOrSetDefault("Tooll.SelectionView.Camera.Acceleration", CAMERA_ACCELERATION_DEFAULT);
            _frictionKeyboardManipulation = (float)App.Current.ProjectSettings.GetOrSetDefault("Tooll.SelectionView.Camera.Friction", 0.3f);
            _showContentControl = showContentControl;
            _renderConfig = renderConfig;

            _spaceMouse = new SpaceMouse(this, renderConfig);
            GizmoPartHitIndex = -1;
        }



        public void Discard()
        {
            _spaceMouse.Discard();
        }


        private Operator _lastRenderedOperator;

        public bool UpdateAndCheckIfRedrawRequired()
        {
            if (_showContentControl.RenderSetup == null)
                return false;

            // Stop all transitions when switching between camera-ops
            if (_renderConfig.Operator != _lastRenderedOperator)
            {
                _lastRenderedOperator = _renderConfig.Operator;
                _cameraPositionGoal = _renderConfig.CameraSetup.Position;
                _cameraTargetGoal = _renderConfig.CameraSetup.Target;
                MoveVelocity = Vector3.Zero;
                _isTransitionActive = false;
            }

            // This is an extremely unfortunate solution to check if the camera has been manipulated from the
            // outside (e.g. by another view, parameters or animation)
            if (!_isTransitionActive && (PositionDistance.Length() > STOP_DISTANCE_THRESHOLD || TargetDistance.Length() > STOP_DISTANCE_THRESHOLD))
            {
                _cameraPositionGoal = _renderConfig.CameraSetup.Position;
                _cameraTargetGoal = _renderConfig.CameraSetup.Target;
                return true;
            }

            var redrawRequired = false;
            UpdateRawMouseData();

            if (_interactionLocked)
                return false;

            _interactionLocked = true;

            // Manipulation...
            redrawRequired |= ManipulateGizmos();

            if (_renderConfig.TransformGizmo.State != TransformGizmo.TransformGizmo.GizmoStates.Dragged)
            {
                ManipulateCameraByMouse();
            }

            _manipulatedByKeyboard = ManipulateCameraByKeyboard();

            _spaceMouse.ManipulateCamera();

            // Transition...
            redrawRequired |= ComputeCameraMovement();

            _interactionLocked = false;

            return redrawRequired;
        }

        /* 
         * Returns false if camera didn't move
         */
        private bool ComputeCameraMovement()
        {
            var frameDurationFactor = (float)(App.Current.TimeSinceLastFrame) / FRAME_DURATION_AT_60_FPS;

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
                    _orbitDelta *= new Vector2(ORBIT_HORIZONTAL_FRICTION, ORBIT_VERTICAL_FRICTION) / frameDurationFactor;
                }

                if (MoveVelocity.Length() > MaxMoveVelocity)
                {
                    MoveVelocity *= MaxMoveVelocity / MoveVelocity.Length();
                }
                else if (!_manipulatedByKeyboard)
                {
                    MoveVelocity *= (1 - _frictionKeyboardManipulation) / frameDurationFactor;
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
                                                 : _renderConfig.CameraSetup.Position - _renderConfig.CameraSetup.Target;

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

            _renderConfig.CameraSetup.Position = _cameraPositionGoal = _cameraTargetGoal + viewDirection;
            _manipulatedByMouseWheel = true;
        }


        public void HandleMouseDown(MouseButton changedButton)
        {
            switch (changedButton)
            {
                case MouseButton.Left:
                    _renderConfig.TransformGizmo.HandleLeftMouseDown();
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
                    _renderConfig.TransformGizmo.HandleLeftMouseUp();
                    _leftMouseButtonPressed = false;
                    break;

                case MouseButton.Middle:
                    _middleMouseButtonPressed = false;
                    break;

                case MouseButton.Right:
                    _rightMouseButtonPressed = false;

                    var dragDistances = (_rightMousePressedAt - _mousePos).Length;
                    wasRightClick = !AnyMouseButtonPressed() && dragDistances < SystemParameters.MinimumHorizontalDragDistance;
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

            var rayInWorld = ComputeMouseViewRayInWorld(_showContentControl.RenderSetup);
            var hitIndex = _renderConfig.TransformGizmo.CheckForManipulationByRay(rayInWorld);

            if (hitIndex == GizmoPartHitIndex)
                return false;

            GizmoPartHitIndex = hitIndex;
            _renderConfig.TransformGizmo.IndexOfGizmoPartBelowMouse = hitIndex; // Ugly hack so scene can set context-variable for hover
            return true;
        }


        private Ray ComputeMouseViewRayInWorld(D3DRenderSetup renderSetup)
        {
            var windowPos = _showContentControl.PointFromScreen(_mousePos);

            var x = (float)(2.0 * windowPos.X / _showContentControl.ActualWidth - 1);
            var y = (float)-(2.0 * windowPos.Y / _showContentControl.ActualHeight - 1);

            var rayNds = new Vector3(x, y, 1.0f); // Normalized Device coordinates
            var rayClip = new Vector4(rayNds.X, rayNds.Y, -1, 1);

            var inverseProjection = (_renderConfig.CameraSetup.LastCameraProjection);
            inverseProjection.Invert();

            var rayEye = Vector4.Transform(rayClip, inverseProjection);
            rayEye.Z = 1;
            rayEye.W = 0;

            var inverseViewMatrix = (_renderConfig.CameraSetup.LastWorldToCamera);
            inverseViewMatrix.Invert();

            var rayDirectionInWorld = Vector4.Transform(rayEye, inverseViewMatrix);
            Vector3 raySourceInWorld = _renderConfig.CameraSetup.Position;
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
            var factorX = (float)(_mouseDragDelta.X / _renderConfig.Height * ROTATE_MOUSE_SENSIVITY * Math.PI / 180.0);
            var factorY = (float)(_mouseDragDelta.Y / _renderConfig.Height * ROTATE_MOUSE_SENSIVITY * Math.PI / 180.0);

            Matrix rotAroundX = Matrix.RotationAxis(_renderConfig.CameraSetup.SideDir, factorY);
            Matrix rotAroundY = Matrix.RotationAxis(_renderConfig.CameraSetup.UpDir, factorX);
            Matrix rot = Matrix.Multiply(rotAroundX, rotAroundY);

            var viewDir2 = new Vector4(_cameraTargetGoal - _cameraPositionGoal, 1);
            var viewDirRotated = Vector4.Transform(viewDir2, rot);

            var newTarget = _cameraPositionGoal + new Vector3(viewDirRotated.X, viewDirRotated.Y, viewDirRotated.Z);
            _lookingAroundDelta = newTarget - _cameraTargetGoal;
        }


        private void DragOrbit()
        {
            _orbitDelta = new Vector2((float)(_mouseDragDelta.X / _renderConfig.Height * ORBIT_SENSIVITY * Math.PI / 180.0),
                                      (float)(_mouseDragDelta.Y / _renderConfig.Height * ORBIT_SENSIVITY * Math.PI / 180.0));
        }


        private void OrbitByAngle(Vector2 rotationSpeed)
        {
            var currentTarget = _renderConfig.CameraSetup.Target;
            var viewDir = _renderConfig.CameraSetup.ViewDir;
            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;

            Matrix rotAroundX = Matrix.RotationAxis(_renderConfig.CameraSetup.SideDir, rotationSpeed.Y);
            Matrix rotAroundY = Matrix.RotationAxis(_renderConfig.CameraSetup.UpDir, rotationSpeed.X);
            Matrix rot = Matrix.Multiply(rotAroundX, rotAroundY);
            Vector4 newViewDir = Vector3.Transform(viewDir, rot);
            newViewDir.Normalize();

            // Set new position and freeze cam-target transitions
            _renderConfig.CameraSetup.Position = _cameraPositionGoal = _renderConfig.CameraSetup.Target - newViewDir.ToVector3() * viewDirLength;
            _cameraTargetGoal = currentTarget;
        }

        private void Pan()
        {
            var factorX = (float)(-_mouseDragDelta.X / _renderConfig.Height);
            var factorY = (float)(_mouseDragDelta.Y / _renderConfig.Height);

            var length = (_cameraTargetGoal - _cameraPositionGoal).Length();
            var sideDir = _renderConfig.CameraSetup.SideDir;
            var upDir = _renderConfig.CameraSetup.UpDir;
            sideDir *= factorX * length;
            upDir *= factorY * length;

            _cameraPositionGoal += sideDir + upDir;
            _cameraTargetGoal += sideDir + upDir;
        }
        #endregion


        #region Keyboard Interaction


        void StopTransitionOfPositionTarget()
        {
            _cameraTargetGoal = _renderConfig.CameraSetup.Target;
            _cameraPositionGoal = _renderConfig.CameraSetup.Position;
        }

        private bool ManipulateCameraByKeyboard()
        {
            var frameDurationFactor = (float)(App.Current.TimeSinceLastFrame);

            var sideDir = _renderConfig.CameraSetup.SideDir;
            var upDir = _renderConfig.CameraSetup.UpDir;
            var viewDir = _renderConfig.CameraSetup.ViewDir;

            var viewDirLength = viewDir.Length();
            var initialVelocity = MoveVelocity.Length() < STOP_DISTANCE_THRESHOLD ? INITIAL_MOVE_VELOCITY : 0;

            var increaseAccelerationWithZoom = (_cameraPositionGoal - _cameraTargetGoal).Length() / 10f;
            var accelFactor = _cameraAcceleration * increaseAccelerationWithZoom;

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
                        _cameraPositionGoal = new Vector3(0, 0, CameraSetup.DEFAULT_CAMERA_POSITION_Z);
                        _cameraTargetGoal = new Vector3(0, 0, 0f);
                        interactionKeysPressedCount++;
                        break;
                    // Center Camera
                    case Key.C:
                        var delta = _renderConfig.TransformGizmo.IsGizmoActive
                            ? _renderConfig.TransformGizmo.GizmoToWorld.TranslationVector - _cameraTargetGoal
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

            if (_showContentControl.IsFocused && mousePressed)
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
            get { return _cameraTargetGoal - _renderConfig.CameraSetup.Target; }
            set { _renderConfig.CameraSetup.Target = _cameraTargetGoal - value; }
        }

        private Vector3 _cameraPositionGoal = new Vector3(0, 0, CameraSetup.DEFAULT_CAMERA_POSITION_Z);
        Vector3 PositionDistance
        {
            get { return _cameraPositionGoal - _renderConfig.CameraSetup.Position; }
            set { _renderConfig.CameraSetup.Position = _cameraPositionGoal - value; }
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

        private readonly ShowContentControl _showContentControl;

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
        private bool _interactionLocked;

        private Vector _mouseDragDelta;
        private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();
        private readonly float _frictionKeyboardManipulation; // set from Project-Settings in Constructor!
        private readonly float _cameraAcceleration; // set from Project-Settings in Constructor!
        private readonly SpaceMouse _spaceMouse;

        private RenderViewConfiguration _renderConfig;
    }
}