// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Runtime.InteropServices;
using Framefield.Core.Rendering;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Framefield.Core
{

    public class DefaultRenderer : BaseRenderer
    {
        static private Effect _defaultEffect;

        public static Effect DefaultEffect
        {
            get
            {
                if (_defaultEffect == null && D3DDevice.Device != null)
                {
                    using (var bytecode = ShaderBytecode.CompileFromFile("assets-common/fx/Default.fx", "fx_5_0", ShaderFlags.None, EffectFlags.None, null, null))
                        _defaultEffect = new Effect(D3DDevice.Device, bytecode);
                }
                return _defaultEffect;
            }
        }

        public Effect SceneDefaultEffect { get { return DefaultEffect; } }

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        struct DefaultLightConstBufferLayout
        {
            public DefaultLightConstBufferLayout(Vector4 position, Color4 ambient, Color4 diffuse, Color4 specular) {
                Position = position;
                Ambient = ambient;
                Diffuse = diffuse;
                Specular = specular;
            }
            [FieldOffset(0)]
            public Vector4 Position;
            [FieldOffset(16)]
            public Color4 Ambient;
            [FieldOffset(32)]
            public Color4 Diffuse;
            [FieldOffset(48)]
            public Color4 Specular;
        }

        [StructLayout(LayoutKind.Explicit, Size = 128)]
        struct DefaultPointLightsConstBufferLayout
        {
            [FieldOffset(0)]
            public DefaultLightConstBufferLayout PointLight0;
            [FieldOffset(64)]
            public DefaultLightConstBufferLayout PointLight1;
        }

        public override void Dispose()
        {
            Utilities.DisposeObj(ref _defaultPointLightsConstBuffer);
            base.Dispose();
        }

        public override void SetupEffect(OperatorPartContext context)
        {
            base.SetupEffect(context);

            // extract point lights and material from context
            try
            {
                SetupMaterialConstBuffer(context);
                SetupFogSettingsConstBuffer(context);

                var pointLightsStruct = new DefaultPointLightsConstBufferLayout();
                var cameraToWorld = Matrix.Invert(context.WorldToCamera);
                var point0Position = Vector4.Transform(new Vector4(1000, -2000, -1000, 1), cameraToWorld);
                pointLightsStruct.PointLight0 = new DefaultLightConstBufferLayout(point0Position,
                                                                                  new Color4(0, 0, 0, 1),
                                                                                  new Color4(0.35f, 0.39f, 0.46f, 1),
                                                                                  new Color4(0, 0, 0, 1));
                var point1Position = Vector4.Transform(new Vector4(-1500, 2000, -1500, 1), cameraToWorld);
                pointLightsStruct.PointLight1 = new DefaultLightConstBufferLayout(point1Position,
                                                                                  new Color4(0, 0, 0, 1),
                                                                                  new Color4(1.0f, 0.98f, 0.81f, 1),
                                                                                  new Color4(0.2f, 0.2f, 0.2f, 1));


                using (var data = new DataStream(Marshal.SizeOf(typeof(DefaultPointLightsConstBufferLayout)), true, true))
                {
                    data.Write(pointLightsStruct);
                    data.Position = 0;

                    if (_defaultPointLightsConstBuffer == null)
                    {
                        var bufferDesc = new BufferDescription
                                             {
                                                 Usage = ResourceUsage.Default,
                                                 SizeInBytes = Marshal.SizeOf(typeof(DefaultPointLightsConstBufferLayout)),
                                                 BindFlags = BindFlags.ConstantBuffer
                                             };
                        _defaultPointLightsConstBuffer = new Buffer(context.D3DDevice, data, bufferDesc);
                    }
                    else
                    {
                        context.D3DDevice.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), _defaultPointLightsConstBuffer, 0);
                    }
                    var constBuffer = context.Effect.GetConstantBufferByName("PointLightsBuffer");
                    if (constBuffer != null && constBuffer.IsValid)
                    {
                        constBuffer.SetConstantBuffer(_defaultPointLightsConstBuffer);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error building constant buffer for default renderer: {0} - Source: {1}", e.Message, e.Source);
            }
        }

        Buffer _defaultPointLightsConstBuffer;
    }

}
