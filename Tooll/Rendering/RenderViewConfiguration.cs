using Framefield.Core;
using Framefield.Tooll.Components.SelectionView.ShowScene.TransformGizmo;
using System;

namespace Framefield.Tooll.Rendering
{
    /** Defines how content of undefined type (Image, Scene or Mesh) should be rendered. */
    public class RenderViewConfiguration
    {
        public Operator Operator;
        public CameraSetup CameraSetup; // points to SetupForOperator or View

        public bool ShowGridAndGizmos;
        public int PreferredCubeMapSideIndex = -1;
        public int ShownOutputIndex;
        public TransformGizmo TransformGizmo;

        public bool RenderWithGammaCorrection;
        public double TimeScrubOffset;


        public int Width
        {
            get { return _width; }
            set { _width = Math.Max(1, value); }
        }
        private int _width = 1;

        public int Height
        {
            get { return _height; }
            set { _height = Math.Max(1, value); }
        }
        private int _height = 1;

        public RenderViewConfiguration Clone()
        {
            return new RenderViewConfiguration()
            {
                Width = Width,
                Height = Height,
                ShownOutputIndex = ShownOutputIndex,
                Operator = Operator,
                CameraSetup = CameraSetup,
                RenderWithGammaCorrection = RenderWithGammaCorrection,
            };
        }
    }
}
