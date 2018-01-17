using System.Runtime.InteropServices;
using SharpDX;

namespace Framefield.Core
{
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
            Color = pointLight.Color.ToVector3() * pointLight.Intensity;
            LightRadius = pointLight.LightRadius;
        }
        [FieldOffset(0)]
        public Vector4 Position;
        [FieldOffset(16)]
        public Vector3 Color;
        [FieldOffset(28)]
        public float LightRadius;
    }
}
