using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framefield.Tooll.Rendering
{
    /** Manages the camera computation within D3dRenderSetup esp. switching between internel ViewerCameraPosition and selected CameraOperators */
    public class ViewerCamera
    {
        /** We need a reference to the renderConfiguration to get the currently selected op */
        public ViewerCamera(ContentRendererConfiguration config)
        {
            _renderConfig = config;
        }


        /** Provides access to the current camera for CameraInteraction */
        public Operator SelectedCameraOp
        {
            get
            {
                var camProvider = GetSelectedCamProvider();
                return camProvider != null ? _cameraOperator : null;
            }
        }


        public void ResetCamera()
        {
            CameraPosition = new Vector3(0, 0, CameraInteraction.DEFAULT_CAMERA_POSITION_Z);
            CameraTarget = new Vector3(0, 0, 0);
        }


        public Vector3 CameraPosition
        {
            get
            {
                var cameraProvider = GetSelectedCamProvider();
                return cameraProvider != null ? cameraProvider.GetLastPosition() : _viewerCameraPosition;
            }
            set
            {
                var cameraProvider = GetSelectedCamProvider();
                if (cameraProvider != null)
                {
                    cameraProvider.SetPosition(App.Current.Model.GlobalTime, value);
                }
                _viewerCameraPosition = value;
            }
        }
        private Vector3 _viewerCameraPosition = new Vector3(0, 0, CameraInteraction.DEFAULT_CAMERA_POSITION_Z);


        public Vector3 CameraTarget
        {
            get
            {
                var renderedOpAsCamera = GetSelectedCamProvider();
                return renderedOpAsCamera != null ? renderedOpAsCamera.GetLastTarget() : _viewerCameraTarget;
            }
            set
            {
                var renderedOpAsCamera = GetSelectedCamProvider();
                if (renderedOpAsCamera != null)
                {
                    renderedOpAsCamera.SetTarget(App.Current.Model.GlobalTime, value);
                }
                _viewerCameraTarget = value;
            }
        }
        private Vector3 _viewerCameraTarget = Vector3.Zero;


        public double CameraRoll
        {
            get
            {
                var camProvider = GetSelectedCamProvider();
                return camProvider != null ? camProvider.GetLastRoll() : 0;
            }
            set
            {
                var camProvider = GetSelectedCamProvider();
                if (camProvider != null)
                {
                    camProvider.SetRoll(App.Current.Model.GlobalTime, value);
                }
            }
        }


        public void GetViewDirections(out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir)
        {
            GetViewDirections(CameraTarget, CameraPosition, CameraRoll, out viewDir, out sideDir, out upDir);
        }

        public static void GetViewDirections(Vector3 camTarget, Vector3 camPos, double camRoll,
                                          out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir)
        {
            viewDir = camTarget - camPos;

            var worldUp = Vector3.UnitY;
            var roll = (float)camRoll;
            var rolledUp = Vector3.Transform(worldUp, Matrix.RotationAxis(viewDir, roll));
            rolledUp.Normalize();

            sideDir = Vector3.Cross(rolledUp.ToVector3(), viewDir);
            sideDir.Normalize();

            upDir = Vector3.Cross(viewDir, sideDir);
            upDir.Normalize();
        }


        private ICameraProvider GetSelectedCamProvider()
        {
            if (_cameraOperator != null && _cameraOperator.InternalParts.Count > 0)
            {
                return _cameraOperator.InternalParts[0].Func as ICameraProvider;
            }
            else
            {
                return null;
            }
        }

        public bool SelectedOperatorIsCamProvider
        {
            get
            {
                return
                    _cameraOperator != null
                    && _cameraOperator.InternalParts.Count > 0
                    && _cameraOperator.InternalParts[0].Func is ICameraProvider;
            }
        }

        public Matrix LastCameraProjection { get; set; }
        public Matrix LastWorldToCamera { get; set; }
        //public bool CameraHasBeenRenderedOnce { get; set; }

        private Operator _cameraOperator { get { return _renderConfig.Operator; } }
        ContentRendererConfiguration _renderConfig;

    }
}
