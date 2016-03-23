// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.IO;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core;

namespace CoreTests
{
    [TestClass]
    public class ResourceTests
    {
        private String f_filePath = String.Empty;

        [TestInitialize]
        public void initialize()
        {
            f_filePath = @"temp_imageFile.png";
            Bitmap image = new Bitmap(10, 10);
            image.Save(f_filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        [TestCleanup]
        public void cleanup()
        {
            File.Delete(f_filePath);
        }

        [TestMethod]
        public void TryGet_ReadRawResource_ReturnsValidResource()
        {
            RawResource rawResource = ResourceManager.ReadRaw(f_filePath);

            Assert.IsNotNull(rawResource);
        }

        [TestMethod]
        public void TryGet_ReadSameRawResources_Returns2EqualRawResource()
        {
            RawResource rawResource1 = ResourceManager.ReadRaw(f_filePath);
            RawResource rawResource2 = ResourceManager.ReadRaw(f_filePath);

            Assert.AreEqual(rawResource1.GetHashCode(), rawResource2.GetHashCode()); //because the resource was cached
        }

        [TestMethod]
        public void TryGet_ReadSameRawResourcesViaAbsolutAndRelativFilename_Returns2EqualRawResource()
        {
            FileInfo fi = new FileInfo(f_filePath);
            RawResource rawResource1 = ResourceManager.ReadRaw(f_filePath);
            RawResource rawResource2 = ResourceManager.ReadRaw(fi.FullName);

            Assert.AreEqual(rawResource1.GetHashCode(), rawResource2.GetHashCode()); //because the resource was cached
        }

        [TestMethod]
        public void TryGet_ReadSameFileAsDifferentResources_FirstReadResourceIsValidSecondFails()
        {

            RawResource rawResource = ResourceManager.ReadRaw(f_filePath);
            
            //this fails, because the file is already cached but as a raw resource. currently there exists no conversion from raw to image resource.
            //therefor the 'casting' fails.
            ImageResource imageResource = ResourceManager.ReadImage(f_filePath);

            Assert.IsNotNull(rawResource);
            Assert.IsNull(imageResource);
        }

    }
}
