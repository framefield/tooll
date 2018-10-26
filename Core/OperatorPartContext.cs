// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using Framefield.Core.Rendering;

namespace Framefield.Core
{

    public class OperatorPartContext
    {
        public static readonly string POINT_LIGHT_CONTAINER_ID = "Core.Context.PointLightContainer";
        public static readonly string MATERIAL_ID = "Core.EvaluationContext.Material";
        public static readonly string PBR_MATERIAL_ID = "Core.EvaluationContext.PbrMaterial";
        public static readonly string PBR_IMAGE_BASED_LIGHTING_ID = "Core.EvaluationContext.PbrImageBasedLighting";
        public static readonly string PBR_SPHERE_LIGHT_CONTAINER_ID = "Core.Context.PbrSphereLightContainer";
        public static readonly string FOG_SETTINGS_ID = "Core.EvaluationContext.FogSettings";
        public static readonly string TESTS_EVALUATOR_ID = "Core.Testing.TestsEvaluator";
        public static readonly string UI_EVENT_ID = "Core.UI.Event";
        public static readonly string DEBUG_VARIABLE_NAME = "ShowDebugOverlays";
        public static readonly string PLOT_FLOAT_VALUE = "Value.ForPlot";
        public static readonly string PREFERRED_CUBEMAP_SIDE_INDEX = "Tooll.UI.CubeMapSideIndex";

        public float Time { get; set; }
        public float GlobalTime { get; private set; }
        public Dictionary<string, float> Variables { get { return _variables; } }
        public Dictionary<string, object> Objects { get { return _objects; } }
        public float Value { get; set; }
        public string Text { get; set; }
        public Mesh Mesh { get; set; }
        public Texture2D Image { get; set; }
        public Texture2D DepthImage { get; set; }
        public Texture3D Volume { get; set; }
        public Matrix ObjectTWorld { get; set; }
        public Matrix WorldToCamera { get; set; }
        public Matrix CameraProjection { get; set; }
        public Matrix TextureMatrix { get; set; }
        public Effect Effect { get; set; }
        public Device D3DDevice { get; set; }
        public SharpDX.DXGI.Format ImageBufferFormat { get; set; }

        public RenderTargetView RenderTargetView { get; set; }
        public RenderTargetView[] RenderTargetViews { get; set; }
        public DepthStencilView DepthStencilView { get; set; }
        public ViewportF Viewport { get; set; }
        public ShaderResourceView Texture0 { get; set; }
        public ShaderResourceView SkySphereSRV { get; set; }
        public InputLayout InputLayout { get; set; }
        public IRenderer Renderer { get; set; }
        public BlendState BlendState { get; set; }
        public Color4 BlendFactor { get; set; }
        public DepthStencilState DepthStencilState { get; set; }
        public RasterizerState RasterizerState { get; set; }
        public SamplerState SamplerState { get; set; }
        public dynamic Dynamic { get; set; }

        public static OperatorPartContext createDefault(ContextSettings settings)
        {
            var context = new OperatorPartContext(0.0f);
            context.D3DDevice = Core.D3DDevice.Device;
            context.Viewport = new ViewportF(0, 0, settings.DisplayMode.Width, settings.DisplayMode.Height);
            context.Variables.Add("Screensize.Width", settings.DisplayMode.Width);
            context.Variables.Add("Screensize.Height", settings.DisplayMode.Height);
            context.Variables.Add("AspectRatio", (float)settings.AspectRatio);
            context.Variables.Add("Samples", (float)settings.Sampling);
            context.Variables.Add("FullScreen", settings.FullScreen ? 1.0f : 0.0f);
            context.Variables.Add("LoopMode", settings.Looped ? 1.0f : 0.0f);
            return context;
        }

        public OperatorPartContext(float globalTime = 0.0f)
        {
            D3DDevice = Core.D3DDevice.Device;
            GlobalTime = globalTime;
            Time = globalTime;
            Value = 0.0f;
            Text = "";
            Mesh = null;
            Image = null;
            DepthImage = null;
            ObjectTWorld = Matrix.Identity;
            WorldToCamera = Matrix.LookAtLH(new Vector3(0, 0, -2.415f), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            CameraProjection = Matrix.PerspectiveFovLH(3.1415f / 4.0f, 1.3333f, 0.1f, 100);
            TextureMatrix = Matrix.Identity;
            Renderer = DefaultRenderer;
            InputLayout = Renderer.SceneDefaultInputLayout;
            BlendState = null;
            BlendFactor = new Color4(1, 1, 1, 1);
            DepthStencilState = null;
            RasterizerState = null;
            SamplerState = null;
            Dynamic = null;
            ImageBufferFormat = SharpDX.DXGI.Format.R16G16B16A16_Float;       // SharpDX.DXGI.Format.R8G8B8A8_UNorm;
            Viewport = new Viewport(0, 0, 512, 512);

            _objects[POINT_LIGHT_CONTAINER_ID] = new HashSet<IPointLight>();
            _objects[MATERIAL_ID] = new DefaultMaterial();
            _objects[PBR_MATERIAL_ID] = new DefaultPbrMaterial();
            _objects[PBR_IMAGE_BASED_LIGHTING_ID] = new DefaultPbrImageBasedLightingSetup();
            _objects[PBR_SPHERE_LIGHT_CONTAINER_ID] = new List<IPbrSphereLight>();
            _objects[FOG_SETTINGS_ID] = new DefaultFogSettings();
        }

        public OperatorPartContext(OperatorPartContext other)
        {
            GlobalTime = other.GlobalTime;
            Time = other.Time;
            _variables = new Dictionary<string, float>(other._variables);
            _objects = new Dictionary<string, object>(other._objects);
            Value = other.Value;
            Text = string.Copy(other.Text);
            Mesh = other.Mesh;
            Image = other.Image;
            DepthImage = other.DepthImage;
            Volume = other.Volume;
            ObjectTWorld = other.ObjectTWorld;
            WorldToCamera = other.WorldToCamera;
            CameraProjection = other.CameraProjection;
            TextureMatrix = other.TextureMatrix;
            Effect = other.Effect;
            D3DDevice = other.D3DDevice;
            RenderTargetView = other.RenderTargetView;
            RenderTargetViews = other.RenderTargetViews;
            DepthStencilView = other.DepthStencilView;
            Viewport = other.Viewport;
            Texture0 = other.Texture0;
            SkySphereSRV = other.SkySphereSRV;
            Renderer = other.Renderer;
            InputLayout = other.InputLayout;
            BlendState = other.BlendState;
            BlendFactor = other.BlendFactor;
            DepthStencilState = other.DepthStencilState;
            RasterizerState = other.RasterizerState;
            SamplerState = other.SamplerState;
            Dynamic = other.Dynamic;
            ImageBufferFormat = other.ImageBufferFormat;
        }

        public OperatorPartContext(OperatorPartContext other, float globalTime)
            : this(other)
        {
            GlobalTime = globalTime;
            Time = globalTime;
        }

        private readonly Dictionary<string, float> _variables = new Dictionary<string, float>();
        private readonly Dictionary<string, object> _objects = new Dictionary<string, object>();

        public static DefaultRenderer DefaultRenderer { get { return _defaultRenderer; } }
        private static readonly DefaultRenderer _defaultRenderer = new DefaultRenderer();
    }

}
