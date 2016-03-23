// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Framefield.Core.Rendering
{
    public class OffScreenRenderer : IDisposable
    {
        //note: only valid after successful call of RenderFrame()
        public Texture2D ColorImage { get { return _renderTargetResource.Texture; } }
        public Texture2D DepthImage { get { return _renderTargetDepthResource.Texture; } }

        public void Setup(OperatorPart outputOp, double width, double height)
        {
            if (_outputOp == outputOp && _width == (int)width && _height == (int)height)
                return;

            try
            {
                Dispose();

                _outputOp = outputOp;
                _width = (int)width;
                _height = (int)height;
                _samples = 2;

                _renderer = new DefaultRenderer();

                _texture = ShaderResourceView.FromFile(D3DDevice.Device, "./assets-common/image/white.png");

                _renderTargetResource = null;
                ResourceManager.ValidateRenderTargetResource(ref _renderTargetResource, _outputOp, D3DDevice.Device, _width, _height);
                _renderTargetView = new RenderTargetView(D3DDevice.Device, _renderTargetResource.Texture);

                _renderTargetDepthResource = null;
                ResourceManager.ValidateDepthStencilResource(ref _renderTargetDepthResource, _outputOp, D3DDevice.Device, _width, _height);
                var depthViewDesc = new DepthStencilViewDescription();
                depthViewDesc.Format = Format.D32_Float;
                depthViewDesc.Dimension = DepthStencilViewDimension.Texture2D;

                _renderTargetDepthView = new DepthStencilView(D3DDevice.Device, _renderTargetDepthResource.Texture, depthViewDesc);

                _gpuSyncer = new BlockingGpuSyncer(D3DDevice.Device);

                D3DDevice.Device.ImmediateContext.OutputMerger.SetTargets(_renderTargetDepthView, _renderTargetView);
                _viewport = new ViewportF(0, 0, _width, _height, 0.0f, 1.0f);
                D3DDevice.Device.ImmediateContext.Rasterizer.SetViewport(_viewport);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to setup imagefile-sequence: {0}", e.Message);
            }
        }

        public bool RenderFrame(OperatorPartContext context)
        {
            try
            {
                var subContext = new OperatorPartContext(context);
                context.Variables["Screensize.Width"] = _width;
                context.Variables["Screensize.Height"] = _height;
                context.Variables["AspectRatio"] = (float)_width/_height;
                context.Variables["Samples"] = _samples;
                context.Variables["FullScreen"] = 0.0f;
                context.Variables["LoopMode"] = 0.0f;

                subContext.D3DDevice = D3DDevice.Device;
                subContext.RenderTargetView = _renderTargetView;
                subContext.DepthStencilState = _renderer.DefaultDepthStencilState;
                subContext.BlendState = _renderer.DefaultBlendState;
                subContext.BlendFactor = _renderer.DefaultBlendFactor;
                subContext.RasterizerState = _renderer.DefaultRasterizerState;
                subContext.SamplerState = _renderer.DefaultSamplerState;
                subContext.Viewport = _viewport;
                subContext.Texture0 = _texture;

                switch (_outputOp.Type)
                {
                    case FunctionType.Scene:
                        subContext.Effect = _renderer.SceneDefaultEffect;
                        subContext.InputLayout = _renderer.SceneDefaultInputLayout;
                        subContext.DepthStencilView = _renderTargetDepthView;

                        Setup(subContext, _renderer);

                        _outputOp.Eval(subContext);
                        break;
                    case FunctionType.Image:
                        subContext.Effect = _renderer.ScreenRenderEffect;
                        subContext.InputLayout = _renderer.ScreenQuadInputLayout;
                        subContext.DepthStencilView = null;

                        Setup(subContext, _renderer);

                        var image = _outputOp.Eval(new OperatorPartContext(subContext)).Image;
                        if (image != null)
                        {
                            _renderer.SetupBaseEffectParameters(subContext);
                            _renderer.RenderToScreen(image, subContext);
                        }
                        break;
                }

                _gpuSyncer.Sync(D3DDevice.Device.ImmediateContext);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Rendering image sequence failed at {0}: {1}", context.Time, ex.Message);
            }
            return false;
        }

        static public void Setup(OperatorPartContext context, DefaultRenderer renderer)
        {
            context.D3DDevice.ImmediateContext.OutputMerger.SetTargets(context.DepthStencilView, context.RenderTargetView);
            context.D3DDevice.ImmediateContext.Rasterizer.SetViewport(context.Viewport);

            if (context.DepthStencilView != null)
            {
                context.D3DDevice.ImmediateContext.ClearDepthStencilView(context.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            }
            context.D3DDevice.ImmediateContext.ClearRenderTargetView(context.RenderTargetView, new SharpDX.Color4(0.0f, 0.0f, 0.0f, 0.0f));
            context.D3DDevice.ImmediateContext.InputAssembler.InputLayout = context.InputLayout;
        }

        public virtual void Dispose()
        {
            Utilities.DisposeObj(ref _renderTargetDepthView);
            Utilities.DisposeObj(ref _renderTargetDepthResource);
            Utilities.DisposeObj(ref _renderTargetView);
            Utilities.DisposeObj(ref _renderTargetResource);
            Utilities.DisposeObj(ref _texture);
            Utilities.DisposeObj(ref _renderer);
            Utilities.DisposeObj(ref _gpuSyncer);
        }


        OperatorPart _outputOp;

        int _width = -1;
        int _height = -1;
        int _samples;

        DefaultRenderer _renderer;
        ShaderResourceView _texture;
        Resource _renderTargetResource;
        RenderTargetView _renderTargetView;
        Resource _renderTargetDepthResource;
        DepthStencilView _renderTargetDepthView;
        ViewportF _viewport;
        private BlockingGpuSyncer _gpuSyncer;
    }
}
