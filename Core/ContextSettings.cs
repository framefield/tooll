// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using SharpDX.Direct3D9;

namespace Framefield.Core
{
    public class ContextSettings
    {
        public DisplayMode DisplayMode { get; set; }
        public int Sampling { get; set; }
        public double AspectRatio { get; set; }
        public bool FullScreen { get; set; }
        public bool Looped { get; set; }
        public bool VSyncEnabled { get; set; }
        public bool PreCacheEnabled { get; set; }

        public ContextSettings()
        {
            DisplayMode = new DisplayMode() { Width = 1920, Height = 1080, RefreshRate = 60, Format = Format.A8R8G8B8 };
            Sampling = 0;
            AspectRatio = DisplayMode.AspectRatio;
            FullScreen = true;
            Looped = false;
            VSyncEnabled = true;
            PreCacheEnabled = true;
        }
    }
}
