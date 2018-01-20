using Framefield.Core;


namespace Framefield.Tooll.Rendering
{
    /** Defines how content of undefined type (Image, Scene or Mesh) should be rendered. */
    public class ContentRendererConfiguration
    {
        public int Width;
        public int Height;
        public bool ShowGridAndGizmos;
        public int PreferredCubeMapSideIndex;
        public int ShownOutputIndex;
        public Operator Operator;
        public bool RenderWithGammaCorrection;
    }
}
