// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using Framefield.Core.Rendering;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;


namespace Framefield.Core
{
    public abstract class FXImageFunction2 : FXSourceCodeFunction
    {
        protected virtual bool NeedsDepth { get { return false; } }
        protected virtual bool ClearColorBuffer { get { return true; } }

        public OperatorPartContext PrepareAndEvalOnChange(OperatorPartContext context)
        {
            if (_effect == null)
            {
                for (int i = 0; i < NumCodes(); ++i)
                    Compile(i);

                if (_effect == null)
                    return context;

                Changed = true;
            }

            if (_renderer == null)
            {
                _renderer = new BaseRenderer();
            }

            if (_usedViewport.Width != context.Viewport.Width || _usedViewport.Height != context.Viewport.Height) 
            {
                _usedViewport = context.Viewport;
                Changed = true;
            }

            if (BuildRenderTarget())
                Changed = true;

            if (Changed)
            {
                try
                {
                    SetupEffectParams();
                    UpdateOnChange(context);
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

        protected abstract void UpdateOnChange(OperatorPartContext context);
        protected abstract void SetupEffectParams();

        protected virtual bool BuildRenderTarget()
        {
            var renderTargetResourceChanged = ResourceManager.ValidateRenderTargetResource(ref _renderTargetResource, OperatorPart, D3DDevice.Device,
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
            if (ClearColorBuffer)
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
                                     TextureMatrix = Matrix.Identity
                                 };
            subContext.Renderer.SetupEffect(subContext);
            subContext.Renderer.Render(subContext.Renderer._screenQuadMesh, subContext);
        }

        protected Resource _renderTargetResource = null;
        protected RenderTargetView _renderTargetView = null;
        protected Resource _renderDepthResource = null;
        protected DepthStencilView _renderTargetDepthView = null;
        protected ViewportF _usedViewport = new Viewport(0, 0, 512, 512);
        protected BaseRenderer _renderer;
    }


    // example usage:
//    public class Class_AddRadialGradient : FXImageFunction2
//    {
//        //>>> _inputids
//        private enum InputId
//        {
//            Code = 0,
//            ColorRadiusA = 1,
//            ColorRadiusB = 2,
//            ColorRadiusC = 3,
//            ColorAR = 4,
//            ColorAG = 5,
//            ColorAB = 6,
//            ColorAA = 7,
//            ColorBR = 8,
//            ColorBG = 9,
//            ColorBB = 10,
//            ColorBA = 11,
//            ColorCR = 12,
//            ColorCG = 13,
//            ColorCB = 14,
//            ColorCA = 15,
//            ColorDR = 16,
//            ColorDG = 17,
//            ColorDB = 18,
//            ColorDA = 19,
//            CenterX = 20,
//            CenterY = 21,
//            Image1 = 22,
//            StretchX = 23,
//            StretchY = 24
//        }
//        //<<< _inputids
//
//
//        public override void Dispose()
//        {
//            base.Dispose();
//            //>>> _cleanup2
//            Utilities.DisposeObj(ref _image1SRV);
//            //<<< _cleanup2
//        }
//
//
//        public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
//        {
//            UpdateInputs(context, inputs);
//
//            PrepareAndEvalOnChange(context);
//
//            return context;
//        }
//
//
//        protected override void SetupEffectParams()
//        {
//            //>>> _setup2
//            _effect.GetVariableByName("RenderTargetSize").AsVector().Set(new Vector2(_usedViewport.Width, _usedViewport.Height));
//            _effect.GetVariableByName("ColorRadius").AsVector().Set(_colorRadius);
//            _effect.GetVariableByName("ColorA").AsVector().Set(_colorA);
//            _effect.GetVariableByName("ColorB").AsVector().Set(_colorB);
//            _effect.GetVariableByName("ColorC").AsVector().Set(_colorC);
//            _effect.GetVariableByName("ColorD").AsVector().Set(_colorD);
//            _effect.GetVariableByName("Center").AsVector().Set(_center);
//            _effect.GetVariableByName("Image1").AsShaderResource().SetResource(_image1SRV);
//            _effect.GetVariableByName("Stretch").AsVector().Set(_stretch);
//            //<<< _setup2
//        }
//
//
//        protected override void UpdateOnChange(OperatorPartContext context)
//        {
//            ClearRenderTarget(context, new Color4(0, 0, 0, 0));
//
//            var prevBlendState = context.BlendState;
//            context.BlendState = OperatorPartContext.DefaultRenderer.DisabledBlendState;
//
//            Render(context);
//
//            context.BlendState = prevBlendState;
//        }
//
//
//        //>>> _updateInputs
//        private void UpdateInputs(OperatorPartContext context, List<OperatorPart> inputs)
//        {
//            _code = inputs[(int)InputId.Code].Eval(context).Text;
//            _colorRadius = new Vector3(inputs[(int)InputId.ColorRadiusA].Eval(context).Value, 
//                                       inputs[(int)InputId.ColorRadiusB].Eval(context).Value,
//                                       inputs[(int)InputId.ColorRadiusC].Eval(context).Value);
//            _colorA = new Color4(inputs[(int)InputId.ColorAR].Eval(context).Value, 
//                                 inputs[(int)InputId.ColorAG].Eval(context).Value, 
//                                 inputs[(int)InputId.ColorAB].Eval(context).Value, 
//                                 inputs[(int)InputId.ColorAA].Eval(context).Value);
//            _colorB = new Color4(inputs[(int) InputId.ColorBR].Eval(context).Value,
//                                 inputs[(int) InputId.ColorBG].Eval(context).Value,
//                                 inputs[(int) InputId.ColorBB].Eval(context).Value,
//                                 inputs[(int) InputId.ColorBA].Eval(context).Value);
//            _colorC = new Color4(inputs[(int) InputId.ColorCR].Eval(context).Value,
//                                 inputs[(int) InputId.ColorCG].Eval(context).Value,
//                                 inputs[(int) InputId.ColorCB].Eval(context).Value,
//                                 inputs[(int) InputId.ColorCA].Eval(context).Value);
//            _colorD = new Color4(inputs[(int) InputId.ColorDR].Eval(context).Value,
//                                 inputs[(int) InputId.ColorDG].Eval(context).Value,
//                                 inputs[(int) InputId.ColorDB].Eval(context).Value,
//                                 inputs[(int) InputId.ColorDA].Eval(context).Value);
//            _center = new Vector2(inputs[(int) InputId.CenterX].Eval(context).Value,
//                                  inputs[(int) InputId.CenterY].Eval(context).Value);
//            _image1SRV = new ShaderResourceView(context.D3DDevice, inputs[(int) InputId.Image1].Eval(context).Image);
//            _stretch = new Vector2(inputs[(int) InputId.StretchX].Eval(context).Value,
//                                   inputs[(int) InputId.StretchY].Eval(context).Value);
//        }
//
//        string _code = string.Empty;
//        Vector3 _colorRadius;
//        Color4 _colorA;
//        Color4 _colorB;
//        Color4 _colorC;
//        Color4 _colorD;
//        Vector2 _center;
//        ShaderResourceView _image1SRV;
//        Vector2 _stretch;
//        //<<< _updateInputs
//    }


}
