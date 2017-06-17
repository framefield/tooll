// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;

namespace Framefield.Core
{
    public interface IMaterial
    {
        Color4 Ambient { get; }
        Color4 Diffuse { get; }
        Color4 Specular { get; }
        Color4 Emission { get; }
        float Shininess { get; }
    }

    public class DefaultMaterial : IMaterial
    {
        public Color4 Ambient { get { return new Color4(0.2f, 0.2f, 0.2f, 1.0f); } }
        public Color4 Diffuse { get { return new Color4(0.8f, 0.8f, 0.8f, 1.0f); } }
        public Color4 Specular { get { return new Color4(1, 1, 1, 1); } }
        public Color4 Emission { get { return new Color4(0, 0, 0, 1); } }
        public float Shininess { get { return 10.0f; } }
    }

    [StructLayout(LayoutKind.Explicit, Size = 80)]
    public struct MaterialConstBufferLayout
    {
        public MaterialConstBufferLayout(IMaterial material)
        {
            MaterialAmbient = material.Ambient;
            MaterialDiffuse = material.Diffuse;
            MaterialSpecular = material.Specular;
            MaterialEmission = material.Emission;
            MaterialShininess = material.Shininess;
        }
        [FieldOffset(0)]
        public Color4 MaterialAmbient;
        [FieldOffset(16)]
        public Color4 MaterialDiffuse;
        [FieldOffset(32)]
        public Color4 MaterialSpecular;
        [FieldOffset(48)]
        public Color4 MaterialEmission;
        [FieldOffset(64)]
        public float MaterialShininess;
    }

    public interface IPbrMaterial
    {
        Texture2D Albedo { get; }
        Texture2D Roughness { get; }
        Texture2D Metal { get; }
        Texture2D NormalMap { get; }
        Texture2D AO { get; }
        Texture2D Emissive { get; }
    }

    public struct DefaultPbrMaterial : IPbrMaterial
    {
        public Texture2D Albedo { get { return null; } }
        public Texture2D Roughness { get { return null; } }
        public Texture2D Metal { get { return null; } }
        public Texture2D NormalMap { get { return null; } }
        public Texture2D AO { get { return null; } }
        public Texture2D Emissive { get { return null; } }
    }

    public interface IPbrImageBasedLightingSetup
    {
        Texture2D PrefilteredSpecularCubeMap { get; }
        Texture2D BrdfLookupTexture { get; }
        Matrix DiffuseR { get; }
        Matrix DiffuseG { get; }
        Matrix DiffuseB { get; }
    }

    public struct DefaultPbrImageBasedLightingSetup : IPbrImageBasedLightingSetup
    {
        public Texture2D PrefilteredSpecularCubeMap => null;
        public Texture2D BrdfLookupTexture => null;
        public Matrix DiffuseR => Matrix.Identity;
        public Matrix DiffuseG => Matrix.Identity;
        public Matrix DiffuseB => Matrix.Identity;
    }
}
