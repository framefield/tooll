// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows.Media;
using Framefield.Core;


namespace Framefield.Tooll.Utils
{
    public struct HSV
    {
        public float H;
        public float S;
        public float V;


        public HSV(float h, float s, float v)
        {
            H = h;
            S = s;
            V = v;
        }

        public HSV FromRGB(RGB rgb)
        {
            return HSV.FromRGB(rgb.R, rgb.G, rgb.B);
        }

        public static HSV FromRGB(float r, float g, float b)
        {
            float delta, h,s, v;
            float tmp = (r < g) ? r : g;
            float min = (tmp < b) ? tmp : b;

            tmp = (r > g) ? r : g;
            float max = (tmp > b) ? tmp : b;

            v = max;				
            delta = max - min;
            if (max == min)
            {
                return new HSV( 0, 0, max);
            }
            else if (max != 0)
            {
                s = delta/max;
            }
            else
            {
                // r = g = b = 0		    // s = 0, v is undefined
                s = 0;
                h = 0;
                return new HSV(h, s, v);
            }
            if (r == max)
                h = (g - b) / delta;		// between yellow & magenta
            else if (g == max)
                h = 2 + (b - r) / delta;	// between cyan & yellow
            else
                h = 4 + (r - g) / delta;	// between magenta & cyan
            h *= 60;				        // degrees
            if (h < 0)
                h += 360;
            return new HSV(h, s, v);
        }            
    }

    public struct HSL
    {
        public float H;
        public float S;
        public float L;


        public HSL(float h, float s, float v)
        {
            H = h;
            S = s;
            L = v;
        }
        public static HSL FromRGB(RGB rgb)
        {
            return HSL.FromRGB(rgb.R, rgb.G, rgb.B);
        }

        public static HSL FromRGB(float r, float g, float b)
        {
            float tmp = (r < g) ? r : g;
            float min = (tmp < b) ? tmp : b;

            tmp = (r > g) ? r : g;
            float max = (tmp > b) ? tmp : b;

            float delta = max - min;
            float lum = (min + max) / 2.0f;
            float sat = 0;
            if (lum > 0.0f && lum < 1.0f)
            {
                sat = delta / ((lum < 0.5f) ? (2.0f * lum) : (2.0f - 2.0f * lum));
            }

            float hue = 0.0f;
            if (delta > 0.0f)
            {
                if (max == r && max != g)
                    hue += (g - b) / delta;
                if (max == g && max != b)
                    hue += (2.0f + (b - r) / delta);
                if (max == b && max != r)
                    hue += (4.0f + (r - g) / delta);
                hue *= 60.0f;
            }

            return new HSL(hue, sat, lum);
        }
    }

    public struct RGB
    {
        public float R;
        public float G;
        public float B;

        public RGB(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }        

        public static RGB FromHSV(HSV hsv)
        {
            return RGB.FromHSV(hsv.H, hsv.S, hsv.V);
        }

        public static RGB FromHSV(float h, float s, float v)
        {
            float satR, satG, satB;
            if (h < 120.0f)
            {
                satR = (120.0f - h) / 60.0f;
                satG = h / 60.0f;
                satB = 0.0f;
            }
            else if (h < 240.0f)
            {
                satR = 0.0f;
                satG = (240.0f - h) / 60.0f;
                satB = (h - 120.0f) / 60.0f;
            }
            else
            {
                satR = (h - 240.0f) / 60.0f;
                satG = 0.0f;
                satB = (360.0f - h) / 60.0f;
            }
            satR = (satR < 1.0f) ? satR : 1.0f;
            satG = (satG < 1.0f) ? satG : 1.0f;
            satB = (satB < 1.0f) ? satB : 1.0f;

            return new RGB( v*(s*satR + (1.0f - s)),
                            v*(s*satG + (1.0f - s)),
                            v*(s*satB + (1.0f - s)));
        }


        public static RGB FromHSL(HSL hsl)
        {
            return RGB.FromHSL(hsl.H, hsl.S, hsl.L);
        }

        public static RGB FromHSL(float h, float s, float l)
        {
            float r, g, b, m, c, x;

            h /= 60;
            if (h < 0) h = 6 - (-h%6);
            h %= 6;

            s = Math.Max(0, Math.Min(1, s));
            l = Math.Max(0, Math.Min(1, l));

            c = (1 - Math.Abs((2*l) - 1))*s;
            x = c*(1 - Math.Abs((h%2) - 1));

            if (h < 1)
            {
                r = c;
                g = x;
                b = 0;
            }
            else if (h < 2)
            {
                r = x;
                g = c;
                b = 0;
            }
            else if (h < 3)
            {
                r = 0;
                g = c;
                b = x;
            }
            else if (h < 4)
            {
                r = 0;
                g = x;
                b = c;
            }
            else if (h < 5)
            {
                r = x;
                g = 0;
                b = c;
            }
            else
            {
                r = c;
                g = 0;
                b = x;
            }

            m = l - c/2;

            return new RGB(r+m, g+m, b+m);
        }
    }
}
