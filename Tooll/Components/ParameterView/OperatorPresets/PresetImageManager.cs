using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framefield.Core;
using System.Text.RegularExpressions;
using Framefield.Core.Rendering;
using SharpDX.Direct3D11;

namespace Framefield.Tooll.Components.ParameterView.OperatorPresets
{
    /** Handles loading and providing bitmap images.*/
    class PresetImageManager
    {
        internal BitmapImage GetImageForPreset(OperatorPreset preset, bool useCache = false)
        {
            var imagePath = BuildImagePath(preset);
            //if (useCache && _cache.ContainsKey(imagePath))
            //{
            //    return _cache[imagePath];
            //}

            if (File.Exists(imagePath))
            {
                Logger.Info("Loading thumbnail: " + imagePath);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;  // Release file handle after loading
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Force reload (e.g. to load regenerate images)
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            else
            {
                _cache[imagePath] = null;
                Logger.Info("Failed to load thumbnail {0}", imagePath);
                return null;
            }
        }

        internal void RenderAndSaveThumbnail(OperatorPreset preset)
        {
            var showContentControl = App.Current.MainWindow.XRenderView.XShowContentControl;
            var renderConfig = showContentControl.RenderConfiguration;
            var renderSetup = App.Current.MainWindow.XRenderView.XShowContentControl.RenderSetup;

            var orgWidth = renderConfig.Width;
            var orgHeight = renderConfig.Height;
            renderSetup.Resize(THUMB_WIDTH, THUMB_HEIGHT);
            showContentControl.ContentRenderer.Reinitialize();

            var filePath = BuildImagePath(preset);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            try
            {
                SharpDX.Direct3D9.Texture.ToFile(
                    showContentControl.ContentRenderer.D3DImageContainer.SharedTexture,
                    filePath,
                    SharpDX.Direct3D9.ImageFileFormat.Png);

            }
            catch (SharpDX.SharpDXException e)
            {
                Logger.Info("Failed to create thumbnail. Exception thrown:" + e);
            }

            renderConfig.Width = orgWidth;
            renderConfig.Height = orgHeight;
            renderSetup.Resize(renderConfig.Width, renderConfig.Height);
            showContentControl.ContentRenderer.Reinitialize();
        }


        /*
         * This was an earlier implementation using the sequence creator. Sadly, this only works
         * for image operators and needed to be updated..
         * 
        public void RenderAndSaveThumbnail2(OperatorPreset preset)
        {
            var op = App.Current.MainWindow.XParameterView.ShownOperator; // todo: remove access to parameter view!
            if (op == null || !op.Outputs.Any())
                return;

            var output = op.Outputs.First();
            if (LivePreviewEnabled)
            {
                if (App.Current.MainWindow.XRenderView.Operator != null && App.Current.MainWindow.XRenderView.Operator.Outputs.Any())
                    output = App.Current.MainWindow.XRenderView.Operator.Outputs[0];
            }

            var currentTime = App.Current.Model.GlobalTime;
            var filePath = BuildImagePath(preset);
            var result = Regex.Match(filePath, @"(.*)/(.*)\.png");
            if (result == null)
                throw new Exception("Invalid filepath format for thumbnails:" + filePath);

            var directory = result.Groups[1].Value;
            var filename = result.Groups[2].Value;

            using (var sequenceCreator = new SequenceCreator())
            {
                sequenceCreator.Setup(output,
                                      height: THUMB_HEIGHT,
                                      width: THUMB_WIDTH,
                                      startTime: currentTime,
                                      endTime: currentTime,
                                      frameRate: 10000,
                                      fileExtension: "png",
                                      skipExistingFiles: false,
                                      directory: directory,
                                      filenameFormat: filename);
                sequenceCreator.RenderFrame();
            }
        }
        */


        public string BuildImagePath(OperatorPreset preset)
        {
            var imagePath = "assets-common/preset-thumbs/" + preset.Id + ".png";

            return imagePath;
        }

        internal const int THUMB_WIDTH = 133;
        internal const int THUMB_HEIGHT = (int)(THUMB_WIDTH * 9.0 / 16.0);

        static Dictionary<String, BitmapImage> _cache = new Dictionary<string, BitmapImage>();
    }
}
