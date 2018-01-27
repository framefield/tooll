// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using Framefield.Core.Inputs;
using Framefield.Tooll.Rendering;
using SharpDX;

namespace Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction
{
    public class SpaceMouse
    {
        private CameraInteraction _cameraInteraction;
        private Vector3 _spaceMouseTranslateVector;
        private Vector3 _spaceMouseRotateVector;
        private int _spaceMouseEventCount;

        public SpaceMouse(CameraInteraction cameraInteraction, RenderViewConfiguration renderConfig)
        {
            App.Current.MainWindow.SpaceMouseHandlerWpf.Active3DxMouse.MotionEvent += SpaceMouseMotionHandler;
            App.Current.MainWindow.SpaceMouseHandlerWpf.Active3DxMouse.ButtonEvent += SpaceMouseButtonHandler;
            _cameraInteraction = cameraInteraction;
            _renderConfig = renderConfig;

        }

        RenderViewConfiguration _renderConfig;

        public void Discard()
        {
            App.Current.MainWindow.SpaceMouseHandlerWpf.Active3DxMouse.MotionEvent -= SpaceMouseMotionHandler;
            App.Current.MainWindow.SpaceMouseHandlerWpf.Active3DxMouse.ButtonEvent -= SpaceMouseButtonHandler;
        }

        internal void ManipulateCamera()
        {
            if (_spaceMouseEventCount == 0)
                return;

            _renderConfig.CameraSetup.GetViewDirections(out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir);

            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;

            float translationVelocity = _spaceMouseTranslateVector.Length() / 2000.0f;
            var direction = _spaceMouseTranslateVector;
            direction.Normalize();

            if (translationVelocity < _cameraInteraction.MaxMoveVelocity)
                direction *= translationVelocity;
            else
                direction *= _cameraInteraction.MaxMoveVelocity;

            var oldPosition = _renderConfig.CameraSetup.Position;

            var moveDir = direction.X * sideDir - direction.Y * viewDir - direction.Z * upDir;

            var rotAroundX = Matrix.RotationAxis(sideDir, -_spaceMouseRotateVector.X / 8000.0f);
            var rotAroundY = Matrix.RotationAxis(upDir, -_spaceMouseRotateVector.Y / 8000.0f);
            var rot = Matrix.Multiply(rotAroundX, rotAroundY);
            var newViewDir = Vector3.Transform(viewDir, rot);
            newViewDir.Normalize();

            _renderConfig.CameraSetup.Position = oldPosition + moveDir;
            _renderConfig.CameraSetup.Target = oldPosition + moveDir + newViewDir.ToVector3() * viewDirLength;

            _spaceMouseEventCount = 0;
            _spaceMouseTranslateVector = new Vector3(0, 0, 0);
            _spaceMouseRotateVector = new Vector3(0, 0, 0);

        }

        private void SpaceMouseButtonHandler(object sender, Core.Inputs.SpaceMouse.ButtonEventArgs e)
        {
            _cameraInteraction.MoveVelocity = new SharpDX.Vector3(0, 0, 0);
            _renderConfig.CameraSetup.ResetCamera();
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void SpaceMouseMotionHandler(object sender, Core.Inputs.SpaceMouse.MotionEventArgs e)
        {
            if (e.TranslationVector != null)
            {
                _spaceMouseTranslateVector.X += e.TranslationVector.X;
                _spaceMouseTranslateVector.Y += e.TranslationVector.Y;
                _spaceMouseTranslateVector.Z += e.TranslationVector.Z;
            }
            if (e.RotationVector != null)
            {
                // Swap axes from HID orientation to a right handed coordinate system that matches WPF model space
                _spaceMouseRotateVector.X += e.RotationVector.X;
                _spaceMouseRotateVector.Y += -e.RotationVector.Z;
                _spaceMouseRotateVector.Z += e.RotationVector.Y;
            }
            _spaceMouseEventCount++;
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }
    }
}