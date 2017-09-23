// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using SharpDX;

namespace Framefield.Core
{

    public static class Mathf
    {
        public static bool IsEqual(float lhs, float rhs, float epsilon = 0.001f)
        {
            return Math.Abs(lhs - rhs) < epsilon;
        }

        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            return (val.CompareTo(min) < 0) ? min
                                            : (val.CompareTo(max) > 0) ? max
                                                                       : val;
        }

        public static float Clamp01(float val)
        {
            return (val <= 0f) ? 0f
                             : (val >= 1f) ? 1f
                                           : val;
        }

        public static float Max(float A, float B)
        {
            return (A >= B) ? A : B;
        }

        public static float Min(float A, float B)
        {
            return (A <= B) ? A : B;
        }

        public static float SmoothStep(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        public static float getAngleDifference(float a1, float a2)
        {
            var d = (float)Math.Abs(a1 - a2);
            return d > 180f ? 360f - d : d;
        }

        // t within [0, 1]
        public static float Lerp(float a, float b, float t)
        {
            return a * (1.0f - t) + b * t;
        }

        public static float RadToDegree(float rad)
        {
            return rad * 180f / (float)Math.PI;
        }

        public static float DegreeToRad(float degree)
        {
            return degree * (float)Math.PI / 180f;
        }
    }
}
