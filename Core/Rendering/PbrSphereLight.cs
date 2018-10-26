using System.Runtime.InteropServices;
using SharpDX;

namespace Framefield.Core.Rendering
{
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
