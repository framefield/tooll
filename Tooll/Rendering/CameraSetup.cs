using Framefield.Core.OperatorPartTraits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framefield.Core;
using SharpDX;

namespace Framefield.Tooll.Rendering
{
    /** Holds all necessary attributes to describe a camera for rendering 
     * and provides helper functionality of computations. 
     * Can represent a CameraOperator the internal camera view of a RenderUserControl
     */
    public class CameraSetup : ICameraProvider
    {
        public event EventHandler<EventArgs> AttributeChangedEvent;

        public void ResetCamera()
        {
            Position = new Vector3(0, 0, CameraSetup.DEFAULT_CAMERA_POSITION_Z);
            Target = new Vector3(0, 0, 0);
        }


        public Vector3 Position
        {
            get { return _viewerCameraPosition; }
            set
            {
                _viewerCameraPosition = value;
                AttributeChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }


        public Vector3 Target
        {
            get { return _viewerCameraTarget; }
            set
            {
                _viewerCameraTarget = value;
                AttributeChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        public double Roll
        {
            get { return _roll; }
            set
            {
                _roll = value;
                AttributeChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }


        public Vector3 SideDir
        {
            get
            {
                GetViewDirections(Target, Position, Roll, out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir);
                return sideDir;
            }
        }

        public Vector3 ViewDir
        {
            get
            {
                GetViewDirections(Target, Position, Roll, out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir);
                return viewDir;
            }
        }

        public Vector3 UpDir
        {
            get
            {
                GetViewDirections(Target, Position, Roll, out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir);
                return upDir;
            }
        }



        public void GetViewDirections(out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir)
        {
            GetViewDirections(Target, Position, Roll, out viewDir, out sideDir, out upDir);
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



        #region Implement ICameraProvider

        public Vector3 GetLastPosition() { return _viewerCameraPosition; }
        public void SetPosition(double time, Vector3 pos) { _viewerCameraPosition = pos; }

        public Vector3 GetLastTarget() { return _viewerCameraTarget; }
        public void SetTarget(double time, Vector3 target) { _viewerCameraTarget = target; }

        public double GetLastRoll() { return _roll; }
        public void SetRoll(double time, double roll) { _roll = roll; }

        public double CalculateFOV(OperatorPartContext context)
        {
            throw new NotImplementedException();
        }

        public double GetLastFOV()
        {
            throw new NotImplementedException();
        }

        public Matrix CalculateWorldToCamera(OperatorPartContext context)
        {
            throw new NotImplementedException();
        }

        public Matrix GetLastWorldToCamera()
        {
            throw new NotImplementedException();
        }

        public Matrix CalculateCameraToView(OperatorPartContext context)
        {
            throw new NotImplementedException();
        }

        public Matrix GetLastCameraToView()
        {
            throw new NotImplementedException();
        }
        #endregion

        // This only for caching and initialized by D3DRenderSetup
        public Matrix LastCameraProjection { get; set; }
        public Matrix LastWorldToCamera { get; set; }


        private Vector3 _viewerCameraPosition = new Vector3(0, 0, CameraSetup.DEFAULT_CAMERA_POSITION_Z);
        private Vector3 _viewerCameraTarget = Vector3.Zero;
        private double _roll = 0;
        public const float DEFAULT_CAMERA_POSITION_Z = -2.415f; // matches a 2-unit height rectangle in origin at 45 degree FOV
    }

}
