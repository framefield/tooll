// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Runtime.InteropServices;
using SharpDX;

namespace Framefield.Core
{

    public enum FogMode
    {
        LINEAR = 0,
        EXPONENTIAL = 1,
        EXPONENTIAL_2 = 2
    }

    public interface IFogSettings
    {
        Color4 Color { get; }
        float Start { get; }
        float End { get; }
        FogMode Mode { get; }
        float Density { get; }
        float Exponent { get; }
    }

    public class DefaultFogSettings : IFogSettings
    {
        public Color4 Color { get { return new Color4(0.1f, 0.1f, 0.1f, 0); } }
        public float Start { get { return 0.0f; } }
        public float End { get { return 1000.0f; } }
        public FogMode Mode { get { return FogMode.LINEAR; } }
        public float Density { get { return 1.0f; } }
        public float Exponent { get { return 1.0f; } }
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct FogSettingsConstBufferLayout
    {
        public FogSettingsConstBufferLayout(IFogSettings fogSettings)
        {
            Color = fogSettings.Color;
            Start = fogSettings.Start;
            End = fogSettings.End;
            Scale = 1.0f/(End - Start);
            Exponent = fogSettings.Exponent;
        }
        [FieldOffset(0)]
        public Color4 Color;
        [FieldOffset(16)]
        public float Start;
        [FieldOffset(20)]
        public float End;
        [FieldOffset(24)]
        public float Scale;
        [FieldOffset(28)]
        public float Exponent;
    }


}
