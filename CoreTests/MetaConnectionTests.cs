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
    public class MetaConnectionTests
    {
        [TestMethod]
        public void Ctor_InitWithIDAndConnectionIDs_AllAreSetCorrect() {
            var id = Guid.NewGuid();
            var sourceOpID = Guid.NewGuid();
            var sourceOpPartID = Guid.NewGuid();
            var targetOpID = Guid.NewGuid();
            var targetOpPartID = Guid.NewGuid();

            var con = new MetaConnection(id, sourceOpID, sourceOpPartID, targetOpID, targetOpPartID);

            Assert.AreEqual(id, con.ID);
            Assert.AreEqual(sourceOpID, con.SourceOpID);
            Assert.AreEqual(sourceOpPartID, con.SourceOpPartID);
            Assert.AreEqual(targetOpID, con.TargetOpID);
            Assert.AreEqual(targetOpPartID, con.TargetOpPartID);
        }

        [TestMethod]
        public void Ctor_InitWithoutIDAndOnlyWithConnectionIDs_AllAreSetCorrectAndIDIsNotEmpty() {
            var sourceOpID = Guid.NewGuid();
            var sourceOpPartID = Guid.NewGuid();
            var targetOpID = Guid.NewGuid();
            var targetOpPartID = Guid.NewGuid();

            var con = new MetaConnection(sourceOpID, sourceOpPartID, targetOpID, targetOpPartID);

            Assert.AreNotEqual(Guid.Empty, con.ID);
            Assert.AreEqual(sourceOpID, con.SourceOpID);
            Assert.AreEqual(sourceOpPartID, con.SourceOpPartID);
            Assert.AreEqual(targetOpID, con.TargetOpID);
            Assert.AreEqual(targetOpPartID, con.TargetOpPartID);
        }

    }
}
