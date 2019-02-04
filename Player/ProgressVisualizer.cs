// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framefield.Core;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Windows;
using Utilities = Framefield.Core.Utilities;

namespace Framefield.Player
{
    public interface ProgressVisualizer : IDisposable
    {
        void Update(float progress);
    }

    public class SimpleLoadingBar : ProgressVisualizer
    {
        public SimpleLoadingBar(RenderForm form, SharpDX.Direct3D11.Device device, SwapChain swapChain) {
            _form = form;
            _device = device;
            _swapChain = swapChain;

            Texture2D backBuffer = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0);
            _renderView = new RenderTargetView(_device, backBuffer);

            var shaderCode = @"
struct VS_IN
{
	float4 pos : POSITION;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	output.pos = input.pos;
	output.col = input.col;
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	return input.col;
}

technique10 Render
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}";
            using (var bytecode = ShaderBytecode.Compile(shaderCode, "fx_5_0", ShaderFlags.None, EffectFlags.None, null, null))
                _effect = new Effect(D3DDevice.Device, bytecode);
            _technique = _effect.GetTechniqueByIndex(0);
            _pass = _technique.GetPassByIndex(0);
            _layout = new InputLayout(_device, _pass.Description.Signature, new[] {
                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0) 
            });
        }

        public void Dispose() {
            Utilities.DisposeObj(ref _renderView);
        }

        public void Update(float progress) {
            //todo: the current device state should be stored here and restored after _swapChain.Present()

            _device.ImmediateContext.ClearState();
            _device.ImmediateContext.OutputMerger.SetTargets(_renderView);
            _device.ImmediateContext.Rasterizer.SetViewport(new ViewportF(0, 0, _form.ClientSize.Width, _form.ClientSize.Height, 0.0f, 1.0f));

            Vector4 color;
            if (progress < 0.8f)
                color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            else
            {
                float i = 5.0f - progress/0.2f;
                color = new Vector4(i, i, i, 1.0f);
            }

            int streamSize = 6*2*16;
            var stream = new DataStream(streamSize, true, true);
            stream.WriteRange(new[] {
                new Vector4(-0.8f, 0.05f, 0.5f, 1.0f), color,
                new Vector4(-0.8f + 1.6f*progress, 0.05f, 0.5f, 1.0f), color,
                new Vector4(-0.8f + 1.6f*progress, -0.05f, 0.5f, 1.0f), color,

                new Vector4(-0.8f, 0.05f, 0.5f, 1.0f), color,
                new Vector4(-0.8f + 1.6f*progress, -0.05f, 0.5f, 1.0f), color,
                new Vector4(-0.8f, -0.05f, 0.5f, 1.0f), color
            });
            stream.Position = 0;

            var vertices = new SharpDX.Direct3D11.Buffer(_device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = streamSize,
                Usage = ResourceUsage.Default
            });
            stream.Dispose();

            _device.ImmediateContext.ClearRenderTargetView(_renderView, SharpDX.Color.Black);

            _device.ImmediateContext.InputAssembler.InputLayout = _layout;
            _device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, 32, 0));

            for (int i = 0; i < _technique.Description.PassCount; ++i)
            {
                _pass.Apply(_device.ImmediateContext);
                _device.ImmediateContext.Draw(6, 0);
            }

            _swapChain.Present(0, PresentFlags.None);
            vertices.Dispose();

        }

        private RenderForm _form;
        private SharpDX.Direct3D11.Device _device = null;
        private SwapChain _swapChain = null;
        private RenderTargetView _renderView = null;
        private Effect _effect = null;
        private EffectTechnique _technique = null;
        private EffectPass _pass = null;
        private InputLayout _layout = null;
    }
}
