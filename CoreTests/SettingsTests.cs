// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core;

namespace CoreTests
{
    [TestClass]
    public class SettingsTests
    {

        [TestMethod]
        public void TryGet_NonExistingKeyWithDefault_ReturnsDefault()
        {
            var settings = new Settings(Path.GetTempFileName().Replace(".tmp", ".json"));

            var @default = "default";
            var result = settings.TryGet("test", @default);

            Assert.AreEqual(@default, result);
        }

        [TestMethod]
        public void TryGetBool_NonExistingKeyWithDefaultFalse_ReturnsFalse()
        {
            var settings = new Settings(Path.GetTempFileName().Replace(".tmp", ".json"));

            var @default = false;
            var result = settings.TryGet("test", @default);

            Assert.AreEqual(@default, result);
        }

        [TestMethod]
        public void TryGetBool_NonExistingKeyWithDefaultTrue_ReturnsTrue()
        {
            var settings = new Settings(Path.GetTempFileName().Replace(".tmp", ".json"));

            var @default = true;
            var result = settings.TryGet("test", @default);

            Assert.AreEqual(@default, result);
        }

        [TestMethod]
        public void GetOrSetDefault_NonExistingKeyWithDefaultFalse_False()
        {
            var settings = new Settings(Path.GetTempFileName().Replace(".tmp", ".json"));

            var result = settings.GetOrSetDefault("test", false);

            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void GetOrSetDefault_NonExistingKeyWithDefaultTrue_True()
        {
            var settings = new Settings(Path.GetTempFileName().Replace(".tmp", ".json"));

            var result = settings.GetOrSetDefault("test", true);

            Assert.AreEqual(true, result);
        }
    }
}
