// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;

namespace Framefield.Core
{

    public interface IPointLight
    {
        Vector3 Position { get; }
        Color4 Ambient { get; }
        Color4 Diffuse { get; }
        Color4 Specular { get; }
        Vector3 Intensity { get; }
        Vector3 Attenuation { get; }
    }

    [StructLayout(LayoutKind.Explicit, Size = 80)]
    public struct PointLightBufferLayout
    {
        public PointLightBufferLayout(IPointLight pointLight)
        {
            Position = new Vector4(pointLight.Position, 1);
            Ambient = pointLight.Ambient * pointLight.Intensity.X;
            Diffuse = pointLight.Diffuse * pointLight.Intensity.Y;
            Specular = pointLight.Specular * pointLight.Intensity.Z;
            ConstantAttenuation = pointLight.Attenuation.X;
            LinearAttenuation = pointLight.Attenuation.Y;
            QuadraticAttenuation = pointLight.Attenuation.Z;
            Dummy = 0;
        }
        [FieldOffset(0)]
        public Vector4 Position;
        [FieldOffset(16)]
        public Color4 Ambient;
        [FieldOffset(32)]
        public Color4 Diffuse;
        [FieldOffset(48)]
        public Color4 Specular;
        [FieldOffset(64)]
        public float ConstantAttenuation;
        [FieldOffset(68)]
        public float LinearAttenuation;
        [FieldOffset(72)]
        public float QuadraticAttenuation;
        [FieldOffset(76)]
        public float Dummy;
    }


    [StructLayout(LayoutKind.Explicit, Size = 256)]
    public struct PointLightsConstBufferLayout
    {
        public PointLightsConstBufferLayout(HashSet<IPointLight> pointLights)
        {
            NumPointLights = pointLights.Count;
            PointLight0 = new PointLightBufferLayout();
            PointLight1 = new PointLightBufferLayout();
            PointLight2 = new PointLightBufferLayout();
            int lightIdx = 0;
            foreach (var pointLight in pointLights) {
                switch (lightIdx) {
                    case 0: PointLight0 = new PointLightBufferLayout(pointLight); break;
                    case 1: PointLight1 = new PointLightBufferLayout(pointLight); break;
                    case 2: PointLight2 = new PointLightBufferLayout(pointLight); break;
                }
                ++lightIdx;
            }
        }
        [FieldOffset(0)]
        public int NumPointLights;
        [FieldOffset(16)]
        public PointLightBufferLayout PointLight0;
        [FieldOffset(96)]
        public PointLightBufferLayout PointLight1;
        [FieldOffset(176)]
        public PointLightBufferLayout PointLight2;
    }


    public interface IPbrPointLight
    {
        Vector3 Position { get; }
        Color3 Color { get; }
        Vector3 Intensity { get; }
        float LightRadius { get; }
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct PbrPointLightBufferLayout
    {
        public PbrPointLightBufferLayout(IPbrPointLight pointLight)
        {
            Position = new Vector4(pointLight.Position, 1);
            Color = pointLight.Color.ToVector3()*pointLight.Intensity;
            LightRadius = pointLight.LightRadius;
        }
        [FieldOffset(0)]
        public Vector4 Position;
        [FieldOffset(16)]
        public Vector3 Color;
        [FieldOffset(28)]
        public float LightRadius;
    }


    public interface IPbrSphereLight
    {
        Vector3 Position { get; }
        float Radius { get; }
        Color3 Color { get; }
        Vector3 Intensity { get; }
        float LightRadius { get; }
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct PbrSphereLightBufferLayout
    {
        public PbrSphereLightBufferLayout(IPbrSphereLight sphereLight)
        {
            Position = sphereLight.Position;
            Radius = sphereLight.Radius;
            Color = sphereLight.Color.ToVector3() * sphereLight.Intensity;
            LightRadius = sphereLight.LightRadius;
        }
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(12)]
        public float Radius;
        [FieldOffset(16)]
        public Vector3 Color;
        [FieldOffset(28)]
        public float LightRadius;
    }
}
