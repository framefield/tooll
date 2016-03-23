// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using Framefield.Core.Inputs;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Framefield.Core.Rendering;



namespace Framefield.Core.Rendering
{
    public class SequenceCreator : IDisposable
    {
        public void Setup(OperatorPart outputOpPart, double startTime = 0, double endTime = 184, double frameRate = 30, double width = 1920, double height = 1080,
                          string fileExtension = "png", bool skipExistingFiles = false, string directory = "output", string filenameFormat = "[T]", SharpDX.DXGI.Format imageFormat = Format.R8G8B8A8_UNorm)
        {
            try
            {
                Dispose();

                _outputOpPart = outputOpPart;
                _directory = directory;
                _fileNameFormat = filenameFormat;
                _startTime = startTime;
                _endTime = endTime;
                _frameRate = frameRate;
                _width = (int) width;
                _height = (int) height;
                _samples = 2;
                _fileExtension = fileExtension.ToLower();
                _skipExistingFiles = skipExistingFiles;

                _defaultContext = new OperatorPartContext(0.0f);
                _defaultContext.Variables.Add("Screensize.Width", _width);
                _defaultContext.Variables.Add("Screensize.Height", _height);
                _defaultContext.Variables.Add("AspectRatio", (float) _width/_height);
                _defaultContext.Variables.Add("Samples", _samples);
                _defaultContext.Variables.Add("FullScreen", 0.0f);
                _defaultContext.Variables.Add("LoopMode", 0.0f);
                _defaultContext.ImageBufferFormat = imageFormat;

                _frameTime = 1.0/_frameRate;

                Directory.CreateDirectory(_directory);

                _renderer = new DefaultRenderer();

                _texture = ShaderResourceView.FromFile(D3DDevice.Device, "./assets-common/image/white.png");


                _renderTargetResource = null;
                ResourceManager.ValidateRenderTargetResource(ref _renderTargetResource, _outputOpPart, D3DDevice.Device, _width, _height, imageFormat);
                _renderTargetView = new RenderTargetView(D3DDevice.Device, _renderTargetResource.Texture);

                _renderDepthResource = null;
                ResourceManager.ValidateDepthStencilResource(ref _renderDepthResource, _outputOpPart, D3DDevice.Device, _width, _height);
                var depthViewDesc = new DepthStencilViewDescription();
                depthViewDesc.Format = Format.D32_Float;
                depthViewDesc.Dimension = DepthStencilViewDimension.Texture2D;

                _renderTargetDepthView = new DepthStencilView(D3DDevice.Device, _renderDepthResource.Texture, depthViewDesc);

                _gpuSyncer = new BlockingGpuSyncer(D3DDevice.Device);

                D3DDevice.Device.ImmediateContext.OutputMerger.SetTargets(_renderTargetDepthView, _renderTargetView);
                _viewport = new ViewportF(0, 0, _width, _height, 0.0f, 1.0f);
                D3DDevice.Device.ImmediateContext.Rasterizer.SetViewport(_viewport);


                var timeAccessorCollector = new OperatorPart.CollectOpPartFunctionsOfType<OperatorPartTraits.ITimeAccessor>();
                _outputOpPart.TraverseWithFunction(timeAccessorCollector, null);
                _timeAccessorOpPartFunctions = new List<OperatorPart.Function>();
                foreach (var opPartFunction in timeAccessorCollector.CollectedOpPartFunctions)
                {
                    _timeAccessorOpPartFunctions.Add(opPartFunction as OperatorPart.Function);
                }
                _currentTime = _startTime;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to setup image-sequence {0}", e.Message);
            }
        }

        public float RenderFrame()
        {
            try
            {
                if (!_skipExistingFiles || !File.Exists(buildFileName(_currentTime)))
                {
                    D3DDevice.Device.ImmediateContext.ClearDepthStencilView(_renderTargetDepthView,
                                                    DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
                    D3DDevice.Device.ImmediateContext.ClearRenderTargetView(_renderTargetView, new SharpDX.Color4(0, 0, 0, 0));
                    D3DDevice.Device.ImmediateContext.InputAssembler.InputLayout = _renderer.SceneDefaultInputLayout;

                    foreach (var opPartFunction in _timeAccessorOpPartFunctions)
                        opPartFunction.OperatorPart.EmitChangedEvent();

                    var context = new OperatorPartContext(_defaultContext, (float)_currentTime)
                    {
                        D3DDevice = D3DDevice.Device,
                        Effect = _renderer.SceneDefaultEffect,
                        InputLayout = _renderer.SceneDefaultInputLayout,
                        RenderTargetView = _renderTargetView,
                        DepthStencilView = _renderTargetDepthView,
                        DepthStencilState = _renderer.DefaultDepthStencilState,
                        BlendState = _renderer.DefaultBlendState,
                        BlendFactor = _renderer.DefaultBlendFactor,
                        RasterizerState = _renderer.DefaultRasterizerState,
                        SamplerState = _renderer.DefaultSamplerState,
                        Viewport = _viewport,
                        Texture0 = _texture                        
                    };                    

                    if (_outputOpPart.Parent.FunctionType == FunctionType.Scene)
                    {

                        _outputOpPart.Eval(context);
                        _gpuSyncer.Sync(D3DDevice.Device.ImmediateContext);
                    }
                    else if (_outputOpPart.Type == FunctionType.Image)
                    {
                        var image = _outputOpPart.Eval(new OperatorPartContext(context)).Image;
                        if (image != null)
                        {
                            _renderer.SetupBaseEffectParameters(context);
                            _renderer.RenderToScreen(image, context);
                        }
                        _gpuSyncer.Sync(D3DDevice.Device.ImmediateContext);                    
                    }

                    var format = ImageFileFormat.Png;
                    if (_fileExtension == "bmp")
                    {
                        format = ImageFileFormat.Bmp;
                    }
                    else if (_fileExtension == "jpg")
                    {
                        format = ImageFileFormat.Jpg;
                    }
                    else if (_fileExtension == "dds")
                    {
                        format = ImageFileFormat.Dds;
                    }
                    Texture2D.ToFile(D3DDevice.Device.ImmediateContext, _renderTargetResource.Texture, format, buildFileName(_currentTime));
                }
                _currentTime += _frameTime;
                double progress = (_currentTime - _startTime)/(_endTime - _startTime);
                return (float) progress;
            }
            catch (Exception ex)
            {
                Logger.Error("Error rendering image sequence at {0}: {1}", _currentTime, ex);
            }
            return 0;
        }

        private String buildFileName(double time)
        {
            double roundedTime = Math.Round(time, 3);
            int hours = (int) (roundedTime/3600);
            int minutes = (int) ((roundedTime/60)%60);
            int seconds = (int) (roundedTime%60);
            int milliseconds = (int) ((roundedTime*1000)%1000);
            string formattedFilename = _fileNameFormat.Replace("[T]", String.Format("{0:00}_{1:00}_{2:00}_{3:000}",
                                                                                    hours,
                                                                                    minutes,
                                                                                    seconds,
                                                                                    milliseconds));

            // Use [FFFF] frame format
            Match matchF = Regex.Match(_fileNameFormat, @"^(.*)\[(F+)\](.*)$");

            if (matchF.Success)
            {
                formattedFilename = matchF.Groups[1].Value
                                  + String.Format("{0:" + new String('0', matchF.Groups[2].Value.Length) + "}", Math.Floor(time * _frameRate))
                                  + matchF.Groups[3].Value;
            }

            if (formattedFilename.IndexOf("[") != -1)
            {
                Logger.Error(String.Format("Filename includes invalid format token: {0}", formattedFilename));
            }

            return String.Format("{0}/{1}.{2}",
                                 _directory,
                                 formattedFilename,
                                 _fileExtension);
        }

        public virtual void Dispose()
        {
            Utilities.DisposeObj(ref _renderTargetView);
            Utilities.DisposeObj(ref _renderTargetResource);
            Utilities.DisposeObj(ref _renderTargetDepthView);
            Utilities.DisposeObj(ref _renderDepthResource);
            Utilities.DisposeObj(ref _texture);
            Utilities.DisposeObj(ref _renderer);
            Utilities.DisposeObj(ref _gpuSyncer);
        }

        private OperatorPart _outputOpPart;

        private string _directory;
        private double _startTime;
        private double _endTime;
        private double _frameRate;
        private int _width;
        private int _height;
        private int _samples;
        private OperatorPartContext _defaultContext;
        private double _frameTime;
        private double _currentTime;
        private string _fileExtension = "png";
        private string _fileNameFormat = "[T]";
        private bool _skipExistingFiles;

        private DefaultRenderer _renderer;
        private ShaderResourceView _texture;
        private Resource _renderTargetResource;
        private RenderTargetView _renderTargetView;
        private Resource _renderDepthResource;
        private DepthStencilView _renderTargetDepthView;
        private ViewportF _viewport;
        private List<OperatorPart.Function> _timeAccessorOpPartFunctions;
        private BlockingGpuSyncer _gpuSyncer;
    }

}
