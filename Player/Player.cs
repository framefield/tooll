// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime;
using System.Threading;
using System.Windows.Forms;
using Framefield.Core;
using Framefield.Core.Profiling;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Multimedia;
using SharpDX.Windows;
using Un4seen.Bass;
using Utilities = Framefield.Core.Utilities;


namespace Framefield.Player
{
    internal class Player : IDisposable
    {
        public Settings ProjectSettings { get; private set; }

        public bool Initialize(ContextSettings settings)
        {
            Logger.Info("Initializing ...");

//            GCSettings.LatencyMode = GCLatencyMode.LowLatency;

            _undoRedoStack = new UndoRedoStack(false);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;


            for (int i = 0; i < _numMeasureValues; ++i)
                _averagedElapsed.Add(10000);

            ProjectSettings = new Settings("Config/ProjectSettings.json");

            _settings = settings;

            _defaultContext = OperatorPartContext.createDefault(_settings);

            var registrationEmail = ProjectSettings.TryGet("Tooll.Sound.BassNetLicense.Email", "");
            var registrationKey = ProjectSettings.TryGet("Tooll.Sound.BassNetLicense.Key", "");
            if (!String.IsNullOrEmpty(registrationEmail) && !String.IsNullOrEmpty(registrationKey))
            {
                BassNet.Registration(registrationEmail, registrationKey);
            } 
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_LATENCY, IntPtr.Zero);

            var soundFilePath = (string)ProjectSettings["Soundtrack.Path"];
            _soundStream = Bass.BASS_StreamCreateFile(soundFilePath, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
            if (_soundStream == 0)
            {
                Logger.Error("Error loading sound file {0}", soundFilePath);
                return false;
            }
            _soundLength = Bass.BASS_ChannelBytes2Seconds(_soundStream, Bass.BASS_ChannelGetLength(_soundStream, BASSMode.BASS_POS_BYTES));

            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, SharpDX.RawInput.DeviceFlags.None);
            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None);
            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericJoystick, SharpDX.RawInput.DeviceFlags.None);

            _form = new RenderForm("Framefield T2 Player");
            _form.Icon = Properties.Resources.t2;
            _form.Visible = true;
            _form.Size = new Size(_settings.DisplayMode.Width, _settings.DisplayMode.Height);

            var desc = new SwapChainDescription() {
                BufferCount = 3,
                ModeDescription = new ModeDescription(_settings.DisplayMode.Width, _settings.DisplayMode.Height,
                                                      new Rational(_settings.DisplayMode.RefreshRate, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = !_settings.FullScreen,
                OutputHandle = _form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
                Flags = SwapChainFlags.AllowModeSwitch
            };

            var featureLevels = new FeatureLevel[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_1 };
            SharpDX.Direct3D11.Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, featureLevels, desc,
                                                          out D3DDevice.Device, out D3DDevice.SwapChain);
            if (D3DDevice.Device == null) {
                Logger.Error("Failed to setup a Direct3d 11 device");
                return false;
            }
            if (D3DDevice.SwapChain == null) {
                Logger.Error("Failed to setup swap chain");
                return false;
            }

            using (var dxgiDevice = D3DDevice.Device.QueryInterface<SharpDX.DXGI.Device1>())
            {
                var adapter = dxgiDevice.Adapter;
                D3DDevice.DX10_1Device = new SharpDX.Direct3D10.Device1(adapter, SharpDX.Direct3D10.DeviceCreationFlags.BgraSupport, SharpDX.Direct3D10.FeatureLevel.Level_10_1);
            }
            D3DDevice.Direct2DFactory = new SharpDX.Direct2D1.Factory();
            D3DDevice.DirectWriteFactory = new SharpDX.DirectWrite.Factory();

            _texture = ShaderResourceView.FromFile(D3DDevice.Device, "./assets-common/image/white.png");

            _renderer = new DefaultRenderer();

            _form.KeyUp += KeyUpHandler;
            _form.KeyDown += KeyDownHandler;
            _form.Resize += ResizeHandler;

            SetupRenderBuffers();

            if (_settings.FullScreen)
                Cursor.Hide();

            D3DDevice.TouchWidth = 1920;
            D3DDevice.TouchHeight = 1080;

            _operatorLoadEndProgress = 0.3f;
            _preCacheStartProgress = _settings.PreCacheEnabled ? 0.9f : 1.0f;

            return true;
        }

        private void ResizeHandler(Object sender, EventArgs e) {
            if (D3DDevice.SwapChain == null)
                return;

            if (D3DDevice.SwapChain.IsFullScreen) {
                Cursor.Hide();
            }
            else {
                Cursor.Show();
            }
        }

        private void SetupRenderBuffers() {
            if (D3DDevice.SwapChain == null)
                return;

            Utilities.DisposeObj(ref _renderTargetView);
            Utilities.DisposeObj(ref _renderTargetDepthView);
            Utilities.DisposeObj(ref _renderDepth);

            D3DDevice.SwapChain.ResizeBuffers(3, _form.ClientSize.Width, _form.ClientSize.Height, 
                                              D3DDevice.SwapChain.Description.ModeDescription.Format, D3DDevice.SwapChain.Description.Flags);

            using (var texture = Texture2D.FromSwapChain<Texture2D>(D3DDevice.SwapChain, 0))
            {
                _renderTargetView = new RenderTargetView(D3DDevice.Device, texture);
            }

            Texture2DDescription depthdesc = new Texture2DDescription();
            depthdesc.BindFlags = BindFlags.DepthStencil;
            depthdesc.Format = Format.D32_Float_S8X24_UInt;
            depthdesc.Width = _form.ClientSize.Width;
            depthdesc.Height = _form.ClientSize.Height;
            depthdesc.MipLevels = 1;
            depthdesc.SampleDescription = new SampleDescription(1, 0);
            depthdesc.Usage = ResourceUsage.Default;
            depthdesc.OptionFlags = ResourceOptionFlags.None;
            depthdesc.CpuAccessFlags = CpuAccessFlags.None;
            depthdesc.ArraySize = 1;

            _renderDepth = new Texture2D(D3DDevice.Device, depthdesc);
            _renderTargetDepthView = new DepthStencilView(D3DDevice.Device, _renderDepth);

            D3DDevice.Device.ImmediateContext.OutputMerger.SetTargets(_renderTargetDepthView, _renderTargetView);
            _viewport = new ViewportF(0, 0, _form.ClientSize.Width, _form.ClientSize.Height, 0.0f, 1.0f);
            D3DDevice.Device.ImmediateContext.Rasterizer.SetViewport(_viewport);
        }

        private void KeyUpHandler(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Space) {
                if (Bass.BASS_ChannelIsActive(_soundStream) == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING)
                {
                    Bass.BASS_ChannelPause(_soundStream);
                    _globalTime.Stop();
                }
                else
                {
                    Bass.BASS_ChannelPlay(_soundStream, false);
                    _globalTime.Start();
                }
            }
            else if (e.KeyCode == Keys.Escape) {
                if (D3DDevice.SwapChain != null)
                    D3DDevice.SwapChain.SetFullscreenState(false, null);

                _form.Close();
            }
            else if (e.KeyCode == Keys.S) {
                // Mute sound
                float currentVolume = 0;
                Bass.BASS_ChannelGetAttribute(_soundStream, BASSAttribute.BASS_ATTRIB_VOL, ref currentVolume);
                Bass.BASS_ChannelSetAttribute(_soundStream, BASSAttribute.BASS_ATTRIB_VOL, currentVolume == 0.0f ? 1.0f : 0.0f);
            }
            // Jump positions for Square-show
            else if (e.KeyCode == Keys.D0)
            {
                float time = 0;
                Bass.BASS_ChannelSetPosition(_soundStream, time);
            }
            else if (e.KeyCode == Keys.D1)
            {
                float time = 3*60 + 10;  
                Bass.BASS_ChannelSetPosition(_soundStream, time);
            }
            else if (e.KeyCode == Keys.D2)
            {
                float time = 4 * 60 + 1; 
                Bass.BASS_ChannelSetPosition(_soundStream, time);
            }
            e.Handled = true;
        }

        private void KeyDownHandler(object sender, KeyEventArgs e) {

            if (e.KeyCode == Keys.Left) {
                var time = Bass.BASS_ChannelBytes2Seconds(_soundStream, Bass.BASS_ChannelGetPosition(_soundStream, BASSMode.BASS_POS_BYTES));
                Bass.BASS_ChannelSetPosition(_soundStream, time - 1);
            }
            if (e.KeyCode == Keys.Right) {
                var time = Bass.BASS_ChannelBytes2Seconds(_soundStream, Bass.BASS_ChannelGetPosition(_soundStream, BASSMode.BASS_POS_BYTES));
                Bass.BASS_ChannelSetPosition(_soundStream, time + 1);
            }
            if (e.KeyCode == Keys.P) {
                using (Texture2D texture = Texture2D.FromSwapChain<Texture2D>(D3DDevice.SwapChain, 0)) {
                    Directory.CreateDirectory("Temp/Screenshots");
                    Texture2D.ToFile(D3DDevice.Device.ImmediateContext, texture, ImageFileFormat.Png, String.Format(@"Temp/Screenshots/{0}.png", DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss_fff")));
                }
            }
            e.Handled = true;
        }

        private OperatorPartContext GetNewContext(float t = 0.0f)
        {
            var context = new OperatorPartContext(_defaultContext, t);
            context.D3DDevice = D3DDevice.Device;
            context.Effect = _renderer.SceneDefaultEffect;
            context.InputLayout = _renderer.SceneDefaultInputLayout;
            context.RenderTargetView = _renderTargetView;
            context.DepthStencilView = _renderTargetDepthView;
            context.DepthStencilState = _renderer.DefaultDepthStencilState;
            context.BlendState = _renderer.DefaultBlendState;
            context.BlendFactor = _renderer.DefaultBlendFactor;
            context.RasterizerState = _renderer.DefaultRasterizerState;
            context.SamplerState = _renderer.DefaultSamplerState;
            context.Viewport = _viewport;
            context.Texture0 = _texture;
            return context;
        }

        private void DrawFrame(float t, Operator op)
        {
            if (_model != null)
            {
                _model.GlobalTime = t;
            }

            D3DDevice.Device.ImmediateContext.ClearDepthStencilView(_renderTargetDepthView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            D3DDevice.Device.ImmediateContext.ClearRenderTargetView(_renderTargetView, new SharpDX.Color4(0.0f, 0.0f, 0.0f, 1.0f));
            D3DDevice.Device.ImmediateContext.InputAssembler.InputLayout = _renderer.SceneDefaultInputLayout;

            //invalidate all time accessors
            var invalidator = new OperatorPart.InvalidateInvalidatables();
            op.Outputs[0].TraverseWithFunctionUseSpecificBehavior(null, invalidator);

            var context = GetNewContext(t);

            op.Outputs[0].Eval(context);
        }

        private class ProgressOpProxy : ProgressVisualizer
        {
            private Player _parent;
            private Operator _op;
            public ProgressOpProxy(Player parent, Operator op)
            {
                _parent = parent;
                _op = op;
            }
            public void Update(float progress)
            {
                D3DDevice.BeginFrame();
                _parent.DrawFrame(progress, _op);
                D3DDevice.EndFrame();
                D3DDevice.SwapChain.Present(0, PresentFlags.None);
            }
            public void Dispose() { }
        }
        
        public void Precalc() {
            Logger.Info("Precalculating ...");
            ProgressVisualizer pv = null;

            var loaderOp = ProjectSettings.TryGet("LoaderProgressOperator", "");
            if (!String.IsNullOrEmpty(loaderOp))
            {
                Logger.Info("Loading loader progress operator ...");
                MetaManager.Instance.PrepareMetaOperators();
                pv = new ProgressOpProxy(this, MetaManager.Instance.LoadMetaOperator(new Guid(loaderOp)).CreateOperator(Guid.NewGuid()));
            }
            else {
                pv = new SimpleLoadingBar(_form, D3DDevice.Device, D3DDevice.SwapChain);
            }
            pv.Update(0.0f);

            MetaManager.InitializeCallback = progress => pv.Update(_operatorLoadEndProgress * progress);
            MetaManager.Instance.LoadMetaOperators();

            _model = new Model();

            //replace the measure operator by nop to remove render flush calls
            var script = @"using System;
                           using System.Collections.Generic;
                           namespace Framefield.Core.IDca9a3a0e_c1c7_42b6_a0e5_cdb4c61d0b18
                           {
                               public class Class_Group : OperatorPart.Function
                               {
                                   public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
                                   {
                                       foreach (var input in inputs[0].Connections) input.Eval(context);
                                       return context;
                                   }
                               }
                           }";

            var measureMetaOp = _model.MetaOpManager.GetMetaOperator(Guid.Parse("86580803-34fe-40a8-8dbf-9197dedb889c"));
            if (measureMetaOp != null)
            {
                var measureMetaOpPart = measureMetaOp.OperatorParts[0].Item2;
                measureMetaOpPart.Version = Guid.NewGuid();
                measureMetaOpPart.Script = script;
                measureMetaOpPart.Compile();
            }

            //replace lightswidgets op by nop
            var lightWidgetsMetaOp = _model.MetaOpManager.GetMetaOperator(Guid.Parse("86b0f0e1-5c9f-430e-8086-abadbf26866c"));
            if (lightWidgetsMetaOp != null)
            {
                var lightWidgetsMetaOpPart = lightWidgetsMetaOp.OperatorParts[0].Item2;
                lightWidgetsMetaOpPart.Version = Guid.NewGuid();
                lightWidgetsMetaOpPart.Script = script;
                lightWidgetsMetaOpPart.Compile();
            }

            //update everything
            var context = GetNewContext();

            var statisticsCollector = new StatisticsCollector();
            _model.HomeOperator.Outputs[0].TraverseWithFunction(statisticsCollector, null);
            int totalNumOpParts = statisticsCollector.NumTotalOpParts;
            int totalNumEvaluations = statisticsCollector.NumTotalEvaluations;

            var initialEvaluator = new InitialEvaluator(totalNumOpParts, context, pv, _operatorLoadEndProgress, _preCacheStartProgress - _operatorLoadEndProgress);
            _model.HomeOperator.Outputs[0].TraverseWithFunction(initialEvaluator, null);

            var timeClipEvaluator = new TimeClipEvaluator();
            _model.HomeOperator.Outputs[0].TraverseWithFunction(timeClipEvaluator, null);

            _model.HomeOperator.Outputs[0].MarkInvalidatables();
            OperatorPart.HasValidInvalidationMarksOnOperatorPartsForTraversing = true;

            if (_settings.PreCacheEnabled)
            {
                // draw the first, last and center frame of each TimeClip
                // in reverse order to warm up caches
                var preCacheTimes = timeClipEvaluator.GetResult();
                int i = 0;
                foreach (double t in preCacheTimes)
                {
                    Logger.Debug("pre-rendering frame at t={0}", t);
                    D3DDevice.BeginFrame();
                    DrawFrame((float)t, _model.HomeOperator);
                    D3DDevice.EndFrame();
                    i++;
                    pv.Update(_preCacheStartProgress + (1.0f - _preCacheStartProgress) * i / preCacheTimes.Count);
                }
            }

            pv.Dispose();

            Logger.Info("Statistics: number of operator parts: {0}, total evaluations: {1}, number of time accessors: {2}",
                              totalNumOpParts, totalNumEvaluations, _timeAccessorOpPartFunctions.Count);

//            ResourceManager.DisposeAll();
        }

        public void Run() {
            Logger.Info("Starting ...");
            Bass.BASS_ChannelSetPosition(_soundStream, Bass.BASS_ChannelSeconds2Bytes(_soundStream, 0.0), BASSMode.BASS_POS_BYTES);
            Bass.BASS_ChannelPlay(_soundStream, false);

            _stopwatch.Start();
            _globalTime.Start();

             RenderLoop.Run(_form, () => Update());
        }

        private void Update() {
            var time = Bass.BASS_ChannelBytes2Seconds(_soundStream, Bass.BASS_ChannelGetPosition(_soundStream, BASSMode.BASS_POS_BYTES));
            if (time >= _soundLength)
            {
                if (_settings.Looped)
                    Bass.BASS_ChannelSetPosition(_soundStream, 0);
                else if (!_exitTimer.IsRunning)
                    _exitTimer.Restart();
                else if (_exitTimer.ElapsedMilliseconds >= 2000)
                    _form.Close();
            }

//            try {
//                _undoRedoStack.ProcessReceivedCommands();
//            }
//            catch (Exception exception) {
//                Logger.Warn("Error when excecuting remote command: {0}", exception.Message);
//            }

            //double time = (double)_globalTime.ElapsedTicks / Stopwatch.Frequency;
            TimeLogger.BeginFrame(time);
            D3DDevice.BeginFrame();

            DrawFrame((float)time, _model.HomeOperator);

            D3DDevice.SwapChain.Present(_settings.VSyncEnabled ? 1 : 0, PresentFlags.None);

            Int64 elapsedTicks = _stopwatch.ElapsedTicks;
            Console.Write("time: {0:000.000}, fps: {1:000.0} ({2:000.0}/{3:000.0}), resources used: {4,2}, free: {5,2}      \r",
                           time, (double)_numMeasureValues*Stopwatch.Frequency/_averagedElapsed.Sum(e => e), (double)Stopwatch.Frequency/_averagedElapsed.Max(), (double)Stopwatch.Frequency/_averagedElapsed.Min(),
                           ResourceManager.NumUsedResources, ResourceManager.NumFreeResources);
            _averagedElapsed[_currentAveragedElapsedIndex] = elapsedTicks;
            _currentAveragedElapsedIndex++;
            _currentAveragedElapsedIndex %= _numMeasureValues;
            _stopwatch.Restart();

            D3DDevice.EndFrame();
            TimeLogger.EndFrame();
        }

        public void Dispose() 
        {
            ResourceManager.WriteResourceDescriptions();
            LogHistogram();

            Logger.Info("Unintializing ...");

            Bass.BASS_ChannelStop(_soundStream);

            Utilities.DisposeObj(ref _renderTargetView);
            Utilities.DisposeObj(ref _renderTargetDepthView);
            Utilities.DisposeObj(ref _renderDepth);
            Utilities.DisposeObj(ref _texture);

            Utilities.DisposeObj(ref _renderer);
            Utilities.DisposeObj(ref _model);

            Utilities.DisposeObj(ref D3DDevice.SwapChain);
            Utilities.DisposeObj(ref D3DDevice.DX10_1Device);
            Utilities.DisposeObj(ref D3DDevice.Direct2DFactory);
            Utilities.DisposeObj(ref D3DDevice.DirectWriteFactory);
            Utilities.DisposeObj(ref D3DDevice.Device);

            Bass.BASS_StreamFree(_soundStream);

            Utilities.DisposeObj(ref _form);

            Cursor.Show();
        }

        private void LogHistogram() {
            int maxLength = 70;
            double maxLinearFps = 0;
            foreach (float fps in TimeLogger.FPSHistogram)
                maxLinearFps = Math.Max(maxLinearFps, (double)fps);
            double maxLog10Fps = Math.Log10(maxLinearFps);

            Logger.Debug("");
            Logger.Debug("Fps Histogram:");
            Logger.Debug(" -:                  {0:00}                  {1:00}                  {2:00}",
                              20*maxLinearFps/maxLength, 40*maxLinearFps/maxLength, 60*maxLinearFps/maxLength);
            Logger.Debug(" =:                  {0:00}                  {1:00}                  {2:00}",
                              Math.Pow(10.0, 20*maxLog10Fps/maxLength), Math.Pow(10.0, 40*maxLog10Fps/maxLength), Math.Pow(10.0, 60*maxLog10Fps/maxLength));

            int idx = 0;
            foreach (float fps in TimeLogger.FPSHistogram) {
                int linearLength = (int)(fps*maxLength/maxLinearFps);
                int logLength = (int)(Math.Log10((double)fps)*maxLength/maxLog10Fps);
                string line = String.Format("{0:00}: ", idx);
                for (int i = 0; i < linearLength; ++i)
                    line += "-";
                for (int i = linearLength; i < logLength; ++i)
                    line += "=";
                Logger.Debug(line);
                ++idx;
            }
            Logger.Debug("");
        }

        private UndoRedoStack _undoRedoStack; // for remote command processing

        private ContextSettings _settings;
        private OperatorPartContext _defaultContext;
        private RenderForm _form;
        private ViewportF _viewport;
        private RenderTargetView _renderTargetView;
        private Texture2D _renderDepth;
        private ShaderResourceView _texture;
        private DepthStencilView _renderTargetDepthView;

        private Stopwatch _globalTime = new Stopwatch();

        private int _soundStream;
        private double _soundLength;
        private Model _model = null;
        private DefaultRenderer _renderer = null;

        List<OperatorPart.Function> _timeAccessorOpPartFunctions = new List<OperatorPart.Function>();

        private Stopwatch _stopwatch = new Stopwatch();
        private const int _numMeasureValues = 10;
        private List<Int64> _averagedElapsed = new List<Int64>();
        private int _currentAveragedElapsedIndex = 0;

        private Stopwatch _exitTimer = new Stopwatch();
        private float _operatorLoadEndProgress;
        private float _preCacheStartProgress;
    }

    public class StatisticsCollector : OperatorPart.IPreTraverseEvaluator
    {
        public int NumTotalOpParts { get; private set; }
        public int NumTotalEvaluations { get; private set; }

        public StatisticsCollector() {
            NumTotalOpParts = 0;
            NumTotalEvaluations = 0;
        }

        public void PreEvaluate(OperatorPart opPart) {
            NumTotalEvaluations++;
            if (!_uniqueOpParts.Contains(opPart)) {
                NumTotalOpParts++;
                _uniqueOpParts.Add(opPart);
            }
        }

        private HashSet<OperatorPart> _uniqueOpParts = new HashSet<OperatorPart>();
    }

    public class InitialEvaluator : OperatorPart.IPreTraverseEvaluator
    {
        public InitialEvaluator(int totalNumOpParts, OperatorPartContext context, ProgressVisualizer progressVisualizer, float progressStart, float progressScale)
        {
            _totalNumOpParts = totalNumOpParts;
            _context = context;
            _progressVisualizer = progressVisualizer;
            _progressStart = progressStart;
            _progressScale = progressScale;
        }

        public void PreEvaluate(OperatorPart opPart)
        {
            if (!_uniqueOpParts.Contains(opPart))
            {
                opPart.Eval(_context);
                _uniqueOpParts.Add(opPart);

                if (_uniqueOpParts.Count%100 == 0)
                {
                    float progress = Math.Min((float) _uniqueOpParts.Count/_totalNumOpParts, 1.0f);
                    _progressVisualizer.Update(_progressStart + _progressScale * progress);
                }
            }
        }

        private int _totalNumOpParts;
        private OperatorPartContext _context;
        private ProgressVisualizer _progressVisualizer;
        private HashSet<OperatorPart> _uniqueOpParts = new HashSet<OperatorPart>();
        private float _progressStart;
        private float _progressScale;
    }

    public class TimeClipEvaluator : OperatorPart.IPreTraverseEvaluator
    {
        public TimeClipEvaluator()
        {
            _times.Add(0.0);
        }

        public void PreEvaluate(OperatorPart opPart)
        {
            Framefield.Core.OperatorPartTraits.ITimeClip clip = opPart.Func as Framefield.Core.OperatorPartTraits.ITimeClip;
            if ((clip != null) && !_uniqueClips.Contains(clip)) {
                _times.Add(clip.StartTime);
                _times.Add(0.5 * (clip.StartTime + clip.EndTime));
                _times.Add(clip.EndTime - 1.0/60);
                _uniqueClips.Add(clip);
            }
        }

        public List<double> GetResult()
        {
            _times.Sort();
            // remove double timestamps and those that are too close together;
            // this algorithm is quite slow, but we're usually dealing with
            // significantly less than ~100 TimeClips, so we should be fine
            double next = 0.0;
            int i = 0;
            while (i < _times.Count)
            {
                if (_times[i] >= next)
                {
                    next = _times[i] + 0.5 / 60;
                    ++i;
                }
                else
                {
                    _times.RemoveAt(i);
                }
            }
            _times.Reverse();
            return _times;
        }

        private HashSet<Framefield.Core.OperatorPartTraits.ITimeClip> _uniqueClips = new HashSet<Framefield.Core.OperatorPartTraits.ITimeClip>();
        private List<double> _times = new List<double>();
    }
}