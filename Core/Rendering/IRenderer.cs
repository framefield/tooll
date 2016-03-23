// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using SharpDX.Direct3D11;

namespace Framefield.Core
{
    public interface IRenderer
    {
        InputLayout SceneDefaultInputLayout { get; }
        InputLayout ScreenQuadInputLayout { get; }

        Mesh _screenQuadMesh { get; }

        void SetupEffect(OperatorPartContext context);
        void Render(Mesh mesh, OperatorPartContext context);
        void Render(Mesh mesh, OperatorPartContext context, int techniqueIdx);
        void RenderToScreen(Texture2D image, OperatorPartContext context);
    }

}
