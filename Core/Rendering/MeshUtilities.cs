// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

namespace Framefield.Core.Rendering
{
    public static class MeshUtilities
    {
        public static void CalcTBNSpace(Vector3 p0, Vector2 uv0, Vector3 p1, Vector2 uv1, Vector3 p2, Vector2 uv2, Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
        {
            var q1 = p1 - p0;
            var q2 = p2 - p0;
            var st1 = uv1 - uv0;
            var st2 = uv2 - uv0;
            var s1 = st1.X;
            var t1 = st1.Y;
            var s2 = st2.X;
            var t2 = st2.Y;

            var t = new Vector3(q1.X*t2 - q2.X*t1, q1.Y*t2 - q2.Y*t1, q1.Z*t2 - q2.Z*t1)*1.0f/(s1*t2 - s2*t1);
            var bt = new Vector3(-q1.X*s2 + q2.X*s1, -q1.Y*s2 + q2.Y*s1, -q1.Z*s2 + q2.Z*s1)*1.0f/(s1*t2 - s2*t1);

            bitangent = Vector3.Cross(normal, t);
            bitangent.Normalize();
            tangent = Vector3.Cross(bitangent, normal);
            tangent.Normalize();
        }
    }

    // still in development!
    internal class NormalSmoother
    {
        public void AddPositionValues(Vector3 pos, long streamPosition, Vector3 normal)
        {
            var hash = pos.GetHashCode();
            if (!_positionValues.ContainsKey(hash))
            {
                _positionValues[pos.GetHashCode()] = new Entry();
                _positions.Add(pos);
            }
            var entry = _positionValues[hash];
            entry.Normals.Add(normal);
            entry.StreamPositions.Add(streamPosition);
        }

        public void CalcNormals(DataStream stream)
        {
            foreach (var pos in _positions)
            {
                var entry = _positionValues[pos.GetHashCode()];
                var averageNormal = new Vector3();
                foreach (var normal in entry.Normals)
                {
                    averageNormal += normal;
                }
                averageNormal /= (float) entry.Normals.Count;
                averageNormal.Normalize();
                foreach (var streamPos in entry.StreamPositions)
                {
                    stream.Position = streamPos;
                    stream.Write(averageNormal);
                }
            }
        }

        private class Entry
        {
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<long> StreamPositions = new List<long>();
        }

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly Dictionary<Int32, Entry> _positionValues = new Dictionary<Int32, Entry>();
    }
}
