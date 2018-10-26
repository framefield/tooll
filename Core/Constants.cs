// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;

namespace Framefield.Core
{
    public static class Constants
    {
        public static int Version { get { return 0x00030300; } } //major, minor, sub, subsub (for hotfix)
        public static string VersionAsString { get { return String.Format("{0}.{1}.{2}.{3}", Version >> 24, (Version >> 16) & 0xff, (Version >> 8) & 0xff, Version & 0xff); } }

        public static double Epsilon { get { return 0.001; } }
    }
}
