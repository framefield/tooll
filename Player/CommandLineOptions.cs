// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Framefield.Core;
using SharpDX.Direct3D9;

namespace Framefield.Player
{
    public class CommandLineOptions
    {
        public CommandLineOptions(ContextSettings settings, IEnumerable<string> args)
        {
            var options = from possibleOption in args
                          where possibleOption.StartsWith("--")
                          select possibleOption.Substring(2).ToLower().Replace('-', '_').Replace("_", "");

            foreach (var option in options)
            {
                switch (option)
                {
                    case "hidedialog":
                    case "nodialog":
                        HideDialog = true;
                        break;
                    case "timelogging":
                        TimeLoggingEnabled = true;
                        break;
                    case "precalc":
                    case "precalconly":
                    case "exit":
                    case "exitafterprecalc":
                    case "quit":
                    case "quitafterprecalc":
                        PrecalcOnly = true;
                        break;
                    case "fs":
                    case "fullscreen":
                        settings.FullScreen = true;
                        break;
                    case "win":
                    case "window":
                    case "windowed":
                    case "nofs":
                    case "nofullscreen":
                        settings.FullScreen = false;
                        break;
                    case "loop":
                    case "looped":
                        settings.Looped = true;
                        break;
                    case "noloop":
                        settings.Looped = false;
                        break;
                    case "vsync":
                        settings.VSyncEnabled = true;
                        break;
                    case "novsync":
                        settings.VSyncEnabled = false;
                        break;
                    case "precache":
                        settings.PreCacheEnabled = true;
                        break;
                    case "noprecache":
                        settings.PreCacheEnabled = false;
                        break;
                    case "noaa":
                    case "nomsaa":
                        settings.Sampling = 0;
                        break;
                    default:
                        // antialiasing: --2x, --4x, --8x, ...
                        // resolution: --1920x1080 (implies 60 Hz refresh rate)
                        // resolution and refresh rate: --1920x1080x50
                        if (option.Contains("x"))
                        {
                            var parts = option.Split(new char[] { 'x' }, 3, StringSplitOptions.RemoveEmptyEntries);
                            int width = 0, height = 0, rate = 60;
                            if (!int.TryParse(parts[0], out width)) break;
                            if (parts.Length < 2)
                            {
                                settings.Sampling = width;
                                break;
                            }
                            if (!int.TryParse(parts[1], out height)) break;
                            if (parts.Length > 2)
                            {
                                if (!int.TryParse(parts[2], out rate)) break;
                            }
                            settings.DisplayMode = new DisplayMode() { Width = width, Height = height, RefreshRate = rate, Format = Format.A8R8G8B8 };
                        }
                        else
                        // aspect ratio: --16/9, --4:3, ...
                        {
                            var parts = option.Replace(':', '/').Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            int num = 0, den = 0;
                            if (parts.Length != 2
                            || !int.TryParse(parts[0], out num)
                            || !int.TryParse(parts[1], out den)
                            || num < 0 || den < 1) break;
                            settings.AspectRatio = (double)num / (double)den;
                        }
                        break;
                }
            }
        }

        public bool HideDialog { get; private set; }
        public bool TimeLoggingEnabled { get; private set; }
        public bool PrecalcOnly { get; private set; }
    }
}
