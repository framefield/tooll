// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core;

namespace CoreTests
{
    [TestClass]
    public class MetaManagerTests
    {
        [TestMethod]
        public void MatchOperatorAssemblyFilenamePattern_ValidInput_MatchIsSuccess()
        {
            var input = "SupplierAssemblyFunc_IDa0163af1-931f-4f37-9806-bb0a331db5cf_Version6b7f7727-1671-4a57-82a5-7dc104137c9c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var match = MetaManager.MatchOperatorAssemblyFilenamePattern(input);

            Assert.IsTrue(match.Success);
        }

        [TestMethod]
        public void MatchOperatorAssemblyFilenamePattern_InvalidInput_MatchFails()
        {
            var input = "SupplierAssembly_IDa0163af1-931f-4f37-9806-bb0a331db5cf_Version6b7f7727-1671-4a57-82a5-7dc104137c9c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var match = MetaManager.MatchOperatorAssemblyFilenamePattern(input);

            Assert.IsFalse(match.Success);
        }
    }
}
