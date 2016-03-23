// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core;

namespace CoreTests
{
    [TestClass]
    public class GlobalStuff
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context) {
            MetaManager.ReadMetaOpsOnInit = false;
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup() {
        }
    }
}
