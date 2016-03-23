// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using Framefield.Core.Rendering;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;


namespace Framefield.Core
{
    public abstract class FXImageFunction : FXSourceCodeFunction
    {
        protected virtual bool NeedsDepth { get { return true; } }
        protected virtual Format ColorBufferFormat { get { return Format.R8G8B8A8_UNorm; } }
        protected virtual BlendState BlendState { get { return _renderer.DefaultBlendState; } }

        protected virtual Format GetColorBufferFormat(OperatorPartContext context)
        {
            return context.ImageBufferFormat;
        }

        protected virtual ViewportF GetViewport(OperatorPartContext context)
        {
            return context.Viewport;
        } 


        public delegate void EvalFunctionOnChange();
        public OperatorPartContext PrepareAndEvalOnChange(OperatorPartContext context, EvalFunctionOnChange functionOnChange)
        {
            //pirx: workaround to compile the shader for the first time. this should be done some where outside.
            if (_firstEval)
            {
                for (int i = 0; i < NumCodes(); ++i)
                    Compile(i);
                _firstEval = false;
                Changed = true;
            }

            if (_effect == null)
            {
                return context;
            }

            if (_renderer == null)
            {
                 _renderer = new BaseRenderer();
            }

            var viewport = GetViewport(context);

            if (_usedViewport.Width != viewport.Width || _usedViewport.Height != viewport.Height) 
            {
                _usedViewport = viewport;
                Changed = true;
            }

            if (BuildRenderTarget(GetColorBufferFormat(context)))
                Changed = true;

            if (Changed)
            {
                try
                {
                    functionOnChange();
                }
                catch (Exception exception)
                {
                    Logger.Error("Render error: {0}/{1}: {2}", OperatorPart.Parent.Parent, OperatorPart.Parent, exception.Message);
                }
                Changed = false;
            }

            context.Image = _renderTargetResource.Texture;
            if (_renderDepthResource != null)
                context.DepthImage = _renderDepthResource.Texture;

            return context;
        }


        protected virtual bool BuildRenderTarget(Format colorBufferFormat)
        {
            var renderTargetResourceChanged = ResourceManager.ValidateRenderTargetResource(ref _renderTargetResource, OperatorPart, D3DDevice.Device, colorBufferFormat,
                                                                                           (int)_usedViewport.Width, (int)_usedViewport.Height);
            if (renderTargetResourceChanged)
            {
                Utilities.DisposeObj(ref _renderTargetView);
                _renderTargetView = new RenderTargetView(D3DDevice.Device, _renderTargetResource.Texture);
            }

            var depthStencilResourceChanged = false;
            if (NeedsDepth)
            {
                depthStencilResourceChanged = ResourceManager.ValidateDepthStencilResource(ref _renderDepthResource, OperatorPart, D3DDevice.Device,
                                                                                           (int)_usedViewport.Width, (int)_usedViewport.Height);
                if (depthStencilResourceChanged)
                {
                    Utilities.DisposeObj(ref _renderTargetDepthView);

                    var depthViewDesc = new DepthStencilViewDescription
                                            {
                                                Format = Format.D32_Float, 
                                                Dimension = DepthStencilViewDimension.Texture2D
                                            };

                    _renderTargetDepthView = new DepthStencilView(D3DDevice.Device, _renderDepthResource.Texture, depthViewDesc);
                }
            }

            return renderTargetResourceChanged || depthStencilResourceChanged;
        }

        public override void Dispose()
        {
            ResourceManager.Dispose(_renderTargetResource);
            Utilities.DisposeObj(ref _renderTargetView);
            ResourceManager.Dispose(_renderDepthResource);
            Utilities.DisposeObj(ref _renderTargetDepthView);
            Utilities.DisposeObj(ref _renderer);
            base.Dispose();
        }

        protected void ClearRenderTarget(OperatorPartContext context, Color4 color)
        {
            if (_renderTargetDepthView != null)
                context.D3DDevice.ImmediateContext.ClearDepthStencilView(_renderTargetDepthView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0xff);
            context.D3DDevice.ImmediateContext.ClearRenderTargetView(_renderTargetView, color);
        }

        protected void Render(OperatorPartContext context)
        {
            var subContext = new OperatorPartContext(context)
                                 {
                                     DepthStencilView = _renderTargetDepthView,
                                     RenderTargetView = _renderTargetView,
                                     Effect = _effect,
                                     Renderer = _renderer,
                                     InputLayout = context.Renderer.ScreenQuadInputLayout,
                                     CameraProjection = Matrix.OrthoLH(1, 1, -100, 100),
                                     WorldToCamera = Matrix.Identity,
                                     ObjectTWorld = Matrix.Identity,
                                     TextureMatrix = Matrix.Identity,
                                     BlendState = _blendState// OperatorPartContext.DefaultRenderer.DefaultBlendState, 
                                 };
            subContext.Renderer.SetupEffect(subContext);
            subContext.Renderer.Render(subContext.Renderer._screenQuadMesh, subContext);
        }

        protected Resource _renderTargetResource = null;
        protected RenderTargetView _renderTargetView = null;
        protected Resource _renderDepthResource = null;
        protected DepthStencilView _renderTargetDepthView = null;
        protected ViewportF _usedViewport = new ViewportF(0, 0, 512, 512);
        protected bool _firstEval = true;
        protected BaseRenderer _renderer;
        protected BlendState _blendState = OperatorPartContext.DefaultRenderer.DefaultBlendState;
    }
}
