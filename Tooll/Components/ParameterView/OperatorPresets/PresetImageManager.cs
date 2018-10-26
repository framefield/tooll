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
using Framefield.Tooll.Rendering;
using System.Windows.Interop;
using System.Windows.Media;
using Framefield.Tooll.Utils;

namespace Framefield.Tooll.Components.ParameterView.OperatorPresets
{
    /** Handles loading and providing bitmap images.*/
    class PresetImageManager
    {
        public PresetImageManager()
        {
            if (!Directory.Exists(DirectoryManager.USER_PRESET_THUMBNAILS))
            {
                Logger.Debug("Creating folder " + DirectoryManager.USER_PRESET_THUMBNAILS);
                Directory.CreateDirectory(DirectoryManager.USER_PRESET_THUMBNAILS);
            }
        }


        internal ImageSource GetImageForPreset(OperatorPreset preset, bool useLive = true)
        {
            var imagePath = BuildImagePath(preset);
            if (useLive && _renderSetupsByPreset.ContainsKey(preset))
            {
                return _renderSetupsByPreset[preset].D3DImageContainer;
            }

            if (File.Exists(imagePath))
            {
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



        internal void StartPreviewCache()
        {
            _renderSetupsByPreset.Clear();
        }


        internal void ReleasePreviousImages()
        {
            foreach (var renderS in _renderSetupsByPreset.Values)
            {
                _renderSetupPool.Enqueue(renderS);
            }
            _renderSetupsByPreset.Clear();
        }



        private D3DRenderSetup CreateOrReuseRenderSetup(RenderViewConfiguration referenceConfig = null)
        {
            if (_renderSetupPool.Count > 0)
            {
                var setup = _renderSetupPool.Dequeue();
                setup.RenderConfig.CameraSetup = referenceConfig.CameraSetup;
                setup.RenderConfig.Operator = referenceConfig.Operator;
                setup.RenderConfig.RenderWithGammaCorrection = referenceConfig.RenderWithGammaCorrection;
                return setup;
            }

            Logger.Info("Creating new renderSetup of Preset-thumb");

            var clonedRenderConfig = referenceConfig.Clone();
            clonedRenderConfig.Width = THUMB_WIDTH;
            clonedRenderConfig.Height = THUMB_HEIGHT;
            var renderSetup = new D3DRenderSetup(clonedRenderConfig);
            return renderSetup;
        }



        private Queue<D3DRenderSetup> _renderSetupPool = new Queue<D3DRenderSetup>();


        private Dictionary<OperatorPreset, D3DRenderSetup> _renderSetupsByPreset = new Dictionary<OperatorPreset, D3DRenderSetup>();

        /** Used for live preview */
        internal D3DImageSharpDX RenderImageForPreset(OperatorPreset preset, RenderViewConfiguration referenceConfig = null)
        {
            if (referenceConfig == null)
                referenceConfig = App.Current.MainWindow.XRenderView.XShowContentControl.RenderConfiguration;

            var renderSetup = CreateOrReuseRenderSetup(referenceConfig);

            _renderSetupsByPreset[preset] = renderSetup;

            return renderSetup.RenderToD3dImage();
        }


        /** Renders and saves a thumbnail to disk */
        internal void RenderAndSaveThumbnail(OperatorPreset preset)
        {
            var showContentControl = App.Current.MainWindow.XRenderView.XShowContentControl;
            var renderConfig = showContentControl.RenderConfiguration;
            var renderSetup = showContentControl.RenderSetup;

            var orgWidth = renderConfig.Width;
            var orgHeight = renderConfig.Height;
            var orgGizmos = renderConfig.ShowGridAndGizmos;
            renderSetup.Resize(THUMB_WIDTH, THUMB_HEIGHT);
            renderConfig.ShowGridAndGizmos = false;

            showContentControl.RenderSetup.Reinitialize();

            var filePath = BuildImagePath(preset);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Logger.Info("Failed to delete thumbnail. Exception thrown:" + e);
                }
            }


            try
            {
                SharpDX.Direct3D9.Texture.ToFile(
                    renderSetup.D3DImageContainer.SharedTexture,
                    filePath,
                    SharpDX.Direct3D9.ImageFileFormat.Png);
            }
            catch (SharpDX.SharpDXException e)
            {
                Logger.Info("Failed to create thumbnail. Exception thrown:" + e);
            }

            renderConfig.Width = orgWidth;
            renderConfig.Height = orgHeight;
            renderConfig.ShowGridAndGizmos = orgGizmos;
            renderSetup.Resize(renderConfig.Width, renderConfig.Height);
            showContentControl.RenderSetup.Reinitialize();
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
            var filename = preset.Id + ".png";
            return preset.IsInstancePreset
                ? DirectoryManager.USER_PRESET_THUMBNAILS + filename
                : DirectoryManager.GLOBAL_PRESET_THUMBNAILS + filename;
        }

        internal const int THUMB_WIDTH = 133;
        internal const int THUMB_HEIGHT = (int)(THUMB_WIDTH * 9.0 / 16.0);

        static Dictionary<String, BitmapImage> _cache = new Dictionary<string, BitmapImage>();
    }
}
