using Framefield.Core;
using System;

namespace Framefield.Tooll.Rendering
{
    /** Defines how content of undefined type (Image, Scene or Mesh) should be rendered. */
    public class ContentRendererConfiguration
    {
        public bool ShowGridAndGizmos;
        public int PreferredCubeMapSideIndex = -1;
        public int ShownOutputIndex;
        public Operator Operator;
        public bool RenderWithGammaCorrection;

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
    }
}
