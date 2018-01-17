// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

//#define USE_SOCKETS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NGit.Storage.File;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Multimedia;

using Device = SharpDX.Direct3D11.Device;
using Un4seen.Bass;
using Logger = Framefield.Core.Logger;
using Framefield.Core;
using Framefield.Core.Profiling;
using Framefield.Shared;
using System.Windows.Forms.VisualStyles;

namespace Framefield.Tooll
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public bool UpdateRequiredAfterUserInteraction { get; set; }
        public event EventHandler<EventArgs> UpdateAfterUserInteractionEvent;
        public event EventHandler<EventArgs> CompositionTargertRenderingEvent;

        public new static App Current { get { return Application.Current as App; } }

        public new MainWindow MainWindow { get { return base.MainWindow as MainWindow; } set { base.MainWindow = value; } }

        public Model Model { get; private set; }
        public UndoRedoStack UndoRedoStack { get; private set; }
        public Settings ProjectSettings { get; private set; }
        public Settings UserSettings { get; private set; }

        public Components.Console.ConsoleViewWriter ConsoleViewWriter { get; private set; }
        public double TimeSinceLastFrame { get; set; }
        public OperatorGitRepository OperatorRepository { get; private set; }

        private AutoBackup AutoBackup { get; set; }


        private void SetupApplication()
        {
            //count to length of the Dispatcher-queue to later prevent frozen UI and slaggy performance
            Dispatcher.Hooks.OperationPosted += (o, e) => Interlocked.Increment(ref _queueLength);
            Dispatcher.Hooks.OperationStarted += (o, e) => Interlocked.Decrement(ref _queueLength);
            Dispatcher.Hooks.OperationAborted += (o, e) => Interlocked.Decrement(ref _queueLength);



            Logger.Initialize(Current.Dispatcher);

            Console.SetOut(new StreamToLogEntryWriter(LogEntry.EntryLevel.INF));
            Console.SetError(new StreamToLogEntryWriter(LogEntry.EntryLevel.ERR));

            ConsoleViewWriter = new Components.Console.ConsoleViewWriter();
            Logger.AddWriter(ConsoleViewWriter);

            if (File.Exists(@"../.dropbox"))
                throw new ArgumentException(String.Format("Tooll may not be started within this dropbox directory.\nPlease make a local copy to your hard drive, e.g. To your desktop."));



            // Load Configu
            if (!Directory.Exists("Config"))
            {
                Logger.Debug("Creating missing directory 'Config'...");
                Directory.CreateDirectory("Config");
            }

            ProjectSettings = new Settings("Config/ProjectSettings.json");
            UserSettings = new Settings("Config/UserSettings.json");

            if (UserSettings.GetOrSetDefault("Tooll.AutoBackupEnabled", true))
            {
                TryToRecoverFromBackupAfterCrash();
            }

            // Start Logging
            Directory.CreateDirectory(@"Log");
            var logWriter = new FileWriter(String.Format(@"Log/{0}.log", DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss_fff")))
            {
                Filter = LogEntry.EntryLevel.ALL
            };
            Logger.AddWriter(logWriter);
            Logger.Info(STARTUP_IDENTIFIER_PRECEDING_TIMESTAMP + DateTime.Now);
            Logger.Info("Version: {0}", Core.Utilities.GetCompleteVersionString());

            
            SetupOperatorGitRepository();

            D3DDevice.Device = new Device(DriverType.Hardware, DeviceCreationFlags.Debug |  DeviceCreationFlags.BgraSupport);
            if (D3DDevice.Device.CreationFlags.HasFlag(DeviceCreationFlags.Debug))
            {
                D3DDevice.DebugDevice = new DeviceDebug(D3DDevice.Device);
            }
            var featureLevel = D3DDevice.Device.FeatureLevel;
            Logger.Info("Found DirectX Device with feature level {0}", featureLevel);

            Logger.Info(".net version {0}", System.Environment.Version);

            using (var dxgiDevice = D3DDevice.Device.QueryInterface<SharpDX.DXGI.Device1>())
            {
                var adapter = dxgiDevice.Adapter;
                D3DDevice.DX10_1Device = new SharpDX.Direct3D10.Device1(adapter, SharpDX.Direct3D10.DeviceCreationFlags.BgraSupport,
                                                                        SharpDX.Direct3D10.FeatureLevel.Level_10_1);
            }
            D3DDevice.Direct2DFactory = new SharpDX.Direct2D1.Factory();
            D3DDevice.DirectWriteFactory = new SharpDX.DirectWrite.Factory();

            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, SharpDX.RawInput.DeviceFlags.None, IntPtr.Zero);
            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None, IntPtr.Zero);
            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericJoystick, SharpDX.RawInput.DeviceFlags.None, IntPtr.Zero);
            ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var progressDialog = new LoadOperatorDefinitionsProgressDialog();
            progressDialog.ShowDialog();
            ShutdownMode = ShutdownMode.OnMainWindowClose;


            Model = new Model();

            var isSender = String.Compare(ProjectSettings.GetOrSetDefault("Tooll.IsHost", "true"), "true", StringComparison.OrdinalIgnoreCase) == 0;
            UndoRedoStack = new UndoRedoStack(isSender);

            if (!UserSettings.Contains("View.WorkspaceMatrices"))
            {
                UserSettings["View.WorkspaceMatrices"] = new Dictionary<Guid, Matrix>();
            }
            else
            {
                var matrices = (JObject) UserSettings["View.WorkspaceMatrices"];
                UserSettings["View.WorkspaceMatrices"] = JsonConvert.DeserializeObject<Dictionary<Guid, Matrix>>(matrices.ToString());
            }

            MetaManager.OPERATOR_TEST_REFERENCE_PATH = ProjectSettings.GetOrSetDefault("Tooll.OperatorTestReferencePath", "assets-ff/test-references/");

            OperatorPresetManager = new OperatorPresetManager();

            AutoBackup = new AutoBackup();
            AutoBackup.SecondsBetweenSaves = (int)UserSettings.GetOrSetDefault("Tooll.AutoBackupPeriodInSeconds", 60*5);
            AutoBackup.Enabled = UserSettings.GetOrSetDefault("Tooll.AutoBackupEnabled", true);

            if(AutoBackup.Enabled)
                AutoBackup.ReduceNumberOfBackups();


            TimeLogger.Enabled = ProjectSettings.GetOrSetDefault("Tooll.ProfilingEnabled", false);

            UpdateRequiredAfterUserInteraction = false;

            CompositionTarget.Rendering += CompositionTarget_RenderingHandler;
            Model.GlobalTimeChangedEvent += Model_GlobalTimeChangedHandler;
            Exit += App_Exit;

            // Load demo soundtrack and setup bass sound system
            SetupSoundSystemWithSoundtrack();
        }


        private void TryToRecoverFromBackupAfterCrash()
        {
            if (!Directory.Exists(TOOLL_LOG_DIRECTORY)) 
                return;

            // Check log-file for insuccessful shutdown
            var directory = new DirectoryInfo(TOOLL_LOG_DIRECTORY);
            var lastFile = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            if (lastFile == null)
                return;

            string completeText;
            try
            {
                completeText = File.ReadAllText(lastFile.FullName);
            }
            catch(Exception e) {
                throw new ShutDownException("Couldn't open last log-file. Is tooll.exe still running? Please check Task-Manager:" + e);
            }
            
            if (!completeText.Contains(STARTUP_IDENTIFIER_PRECEDING_TIMESTAMP) || completeText.Contains(SHUT_DOWN_IDENTIFIER))                 
                return;

            // Get last startup time from Log
            var result = Regex.Match(completeText, STARTUP_IDENTIFIER_PRECEDING_TIMESTAMP + @"\s*(.*?)\n");
            if (!result.Success)
                return;

            var timestamp = result.Groups[1].Value;
            DateTime? lastStartupTime=null;
            try
            {
                lastStartupTime = DateTime.Parse(timestamp);
            }
            catch
            {
                return;
            }
            
            // Check if backups
            var lastBackupTime = AutoBackup.GetTimeOfLastBackup();
            double secondsSinceLastBackup = 0;
            if (lastBackupTime > lastStartupTime)
            {
                var duration = (DateTime.Now - lastBackupTime);
                secondsSinceLastBackup = duration.Value.TotalSeconds;
            }

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var startAfterCrashDialog = new Components.Dialogs.StartAfterCrashDialog(secondsSinceLastBackup);
            startAfterCrashDialog.ShowDialog();
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Restore backup
            var restoreBackup = startAfterCrashDialog.DialogResult;
            if (restoreBackup.Value == true)
            {
                Tooll.AutoBackup.RestoreLast();
            }
        }


        private void SetupOperatorGitRepository()
        {
            const string NewtonSoftDllFilename = "Newtonsoft.Json.dll";
            if (File.Exists(NewtonSoftDllFilename))
            {
                throw new ShutDownException("Please remove " + NewtonSoftDllFilename + " from Tooll's directory because it will break compilation of Operators.",
                                            "Incorrect Settings");
            }

            var operatorRepository = new OperatorGitRepository(MetaManager.OPERATOR_PATH);
            const string operatorRepositoryUrl = "Git.OperatorRepositoryURL";
            const string operatorRepositoryBranch = "Git.OperatorRepositoryBranch";
            if (ProjectSettings.Contains(operatorRepositoryUrl))
            {
                if (!ProjectSettings.Contains("Git.OperatorRepositoryBranch"))
                {
                    // https://streber.framefield.com/1636#5__operatorrepositoryremoteurl_definiert_aber_gitbranch_undefiniert_oder_fehlerhaft
                    throw new ShutDownException("Your project settings misses a definition for GitBranch.\n\nPlease fix this before restarting Tooll.",
                                                "Incorrect Settings");
                }

                var repositoryUrl = ProjectSettings[operatorRepositoryUrl] as string;
                var branchToUse = ProjectSettings[operatorRepositoryBranch] as string;
                OperatorRepository = operatorRepository;
                OperatorRepository.RemotePath = repositoryUrl;
                if (!Directory.Exists(MetaManager.OPERATOR_PATH))
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    // https://streber.framefield.com/1636#1__operatorrepositoryremoteurl_definiert_aber_operators_fehlt
                    // fetch operators from url
                    var progressDialog = new CloneRepositoryProgressDialog(OperatorRepository)
                                             {
                                                 LocalPath = OperatorRepository.LocalPath,
                                                 RemotePath = OperatorRepository.RemotePath
                                             };
                    progressDialog.ShowDialog();

                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                else if (!operatorRepository.IsValid)
                {
                    // https://streber.framefield.com/1636#2__operatorrepositoryremoteurl_definiert_aber_operators__git_fehlt
                    throw new ShutDownException(String.Format("git-repository is set to '{0}' in project settings, but 'Operators/.git' is missing or broken.\n\nPlease fix this or remove the Operators directory. Tooll will then fetch the Operators from the server.", repositoryUrl),
                                                "Missing Operator");
                }

                if (operatorRepository.LocalRepo.GetBranch() != branchToUse)
                {
                    // https://streber.framefield.com/1636#4__operatorrepositoryremoteurl_existiert_aber_operators__git__gt_branch____projectsettings_gitbranch
                    throw new ShutDownException(String.Format("Error: Your 'Operators/.git' branch '{0}' doesn't match the project settings '{1}'.\n\nPlease fix this before restarting Tooll.", operatorRepository.LocalRepo.GetBranch(), branchToUse),
                                                "Incorrect Settings");
                }

                var developerGit = new NGit.Api.Git(new FileRepository("./.git"));
                if (developerGit.GetRepository().GetAllRefs().Any())
                {
                    // valid developer git repository available, so check if branch of developer git repos matches operators repos branch
                    var developerGitBranch = developerGit.GetRepository().GetBranch();
                    if (developerGitBranch != branchToUse)
                    {
                        throw new ShutDownException(String.Format("Error: You starting Tooll as developer but your 'Operators/.git' branch '{0}' doesn't match the Tooll/.git branch '{1}'.\n\nPlease fix this before restarting Tooll.", branchToUse, developerGitBranch),
                                                    "Incorrect Settings");
                    }
                }

                OperatorRepository.Branch = branchToUse;
            }
            else
            {
                if (!Directory.Exists(MetaManager.OPERATOR_PATH) || !Directory.GetFiles(MetaManager.OPERATOR_PATH, "*.mop").Any())
                {
                    // https://streber.framefield.com/1636#6__operatorsrepositoryremoteurl_nicht_definiert_aber_operators__existiert_nicht
                    throw new ShutDownException("Your Operator directory is missing or empty.\n\nYou can define Git.OperatorsRepositoryURL and Git.OperatorRepositoryBranch in your projects settings to fetch a fresh copy.",
                                                "Incorrect Settings");
                }

                if (operatorRepository.IsValid)
                {
                    // https://streber.framefield.com/1636#3__operatorrepositoryremoteurl_nicht_definiert_aber_operators__git_existiert
                    throw new ShutDownException("Although you didn't specify a git repository in your project settings, the directory 'Operators/.git' exists.\n\nPlease fix this.",
                                                "Incorrect Settings");
                }
            }
        }


        private void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == 0x00FF) //WM_INPUT
                SharpDX.RawInput.Device.HandleMessage((IntPtr) msg.lParam);
        }

        void Model_GlobalTimeChangedHandler(object sender, EventArgs e) {
            UpdateRequiredAfterUserInteraction = true;
        }

        private void App_Exit(object sender, EventArgs e)
        {
            CompositionTarget.Rendering -= CompositionTarget_RenderingHandler;
            Model.GlobalTimeChangedEvent -= Model_GlobalTimeChangedHandler;

            AutoBackup.Dispose();
            SavePresets();
            
            OperatorPartContext.DefaultRenderer.Dispose();
            DefaultRenderer.DefaultEffect.Dispose();
            Model.Dispose();
            ResourceManager.DisposeAll();
            D3DDevice.Device.Dispose();
            D3DDevice.DX10_1Device.Dispose();
            D3DDevice.Direct2DFactory.Dispose();
            D3DDevice.DirectWriteFactory.Dispose();
            
            Logger.Info(SHUT_DOWN_IDENTIFIER);
            Logger.Dispose();

            if (D3DDevice.DebugDevice != null)
            {
                D3DDevice.DebugDevice.ReportLiveDeviceObjects(ReportingLevel.Detail);
                Utilities.DisposeObj(ref D3DDevice.DebugDevice);
            }
        }


        /***
         *  Flush Dispatcher-Queue
         *  read more at http://kentb.blogspot.de/2008_04_01_archive.html 
         *  and http://www.codeproject.com/Articles/152137/DispatcherFrame-Look-in-Depth
         */
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
               new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        public object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }  

        

        #region preset manager and serialization

        public OperatorPresetManager OperatorPresetManager { get; private set; }

        private void SavePresets()
        {
            OperatorPresetManager.SavePresets();
        }

        #endregion

        /**
         *  Primary WPF-handler called before updateing the user interface
         */ 
        void CompositionTarget_RenderingHandler(object sender, EventArgs e) 
        {
            UpdateTimeSinceLastFrame();

            if (CompositionTargertRenderingEvent != null)
                CompositionTargertRenderingEvent(this, EventArgs.Empty);

#if USE_SOCKETS
            try
            {
                UpdateRequiredAfterUserInteraction |= UndoRedoStack.ProcessReceivedCommands();
            }
            catch (Exception exception)
            {
                Logger.Warn("Error when excecuting a remote command:\n'{0}'", exception.Message);
            }
#endif

            if (UpdateRequiredAfterUserInteraction ||
                (MainWindow != null && Math.Abs(MainWindow.CompositionView.PlaySpeed) > 0.001))
            {
                UpdateGlobalTime();

                if (UpdateAfterUserInteractionEvent != null)
                    UpdateAfterUserInteractionEvent(this, new EventArgs());
                UpdateRequiredAfterUserInteraction = false;

                // Flush the dispatcher queue if too long (this is likely to result in frame drops but will ensure responsiveness of controls)
                if (_queueLength > 1)
                {
                    DoEvents();
                }
            }
        }


        /**
         * Set default Culture to invariant to prevent "."->"," formatting on German computers
         */
        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var uri = Current.StartupUri;   // Keep MainWindow properties until setup-dialogs is completed
            var mw = Current.MainWindow;

            SetupApplication();

            Current.StartupUri = uri;
            Current.MainWindow = mw;

            base.OnStartup(e);
        }


        public new int Run()
        {
            try
            {
                LibPathManager.SetDllSearchPath();
                AppDomain.CurrentDomain.AssemblyResolve += LibPathManager.CustomResolve;
                return base.Run();
            }
            catch (ShutDownSilentException)
            {
                // do nothing, simply exit without any dialog
            }
            catch (ShutDownException shutdownException)
            {
                UIHelper.ShowErrorMessageBox(shutdownException.Message, shutdownException.Title);
            }
#if !DEBUG
            catch (Exception ex)
            {
                Components.CrashReportWindow.ShowCrashReportForException(ex);
            }
#endif
            return 0;
        }

        #region Playback helpers
        private void UpdateGlobalTime()
        {
            double newTime = Model.GlobalTime + MainWindow.CompositionView.PlaySpeed * TimeSinceLastFrame;

            // During playback override time from soundtrack
            if (Bass.BASS_ChannelIsActive(m_SoundStream) != BASSActive.BASS_ACTIVE_STOPPED &&
                IsPlayingForward() &&
                IsTimeWithinSongRange(newTime))
            {
                newTime = Bass.BASS_ChannelBytes2Seconds(m_SoundStream,
                                                         Bass.BASS_ChannelGetPosition(m_SoundStream, BASSMode.BASS_POS_BYTES));
            }

            if (App.Current.MainWindow.CompositionView.XTimeView.LoopMode && IsPlayingForward())
            {
                if (newTime > MainWindow.CompositionView.XTimeView.EndTime)
                {
                    newTime = MainWindow.CompositionView.XTimeView.StartTime;
                    SetStreamToTime(newTime);
                }
            }

            if (IsTimeWithinSongRange(newTime))
            {
                if (!IsPlayingForward())
                {
                    SetStreamToTime(newTime);
                }
                else if (Bass.BASS_ChannelIsActive(m_SoundStream) != BASSActive.BASS_ACTIVE_PLAYING)
                {
                    Bass.BASS_ChannelPlay(m_SoundStream, false);
                }
            }
            else
            {
                Bass.BASS_ChannelPause(m_SoundStream);
            }

            if (Model.GlobalTime != newTime)    // Prevent triggering Dependency property change
            {
                Model.GlobalTime = newTime;
            }
        }

        private bool IsPlayingForward()
        {
            return MainWindow.CompositionView.PlaySpeed > 0.001;
        }

        private bool IsTimeWithinSongRange(double time)
        {
            return time >= 0.0f && time < m_SoundLength;
        }

        private void UpdateTimeSinceLastFrame()
        {
            var delta = (double)_stopwatch.ElapsedTicks / Stopwatch.Frequency;
            if (delta < 0.016)
            {                
                //Logger.Warn("Clamping invalid time frame update {0}", delta);
                delta = 0.016;
            }
            TimeSinceLastFrame = delta;

            _stopwatch.Restart();
        }
        #endregion

        #region Soundtrack playback through BASS

        private int m_SoundStream;
        private double m_SoundLength;
        private float m_OriginalFrequency = 0.0f;

        private int SoundStream
        {
            get { return m_SoundStream; }
            set
            {
                Bass.BASS_ChannelStop(m_SoundStream);
                Bass.BASS_StreamFree(m_SoundStream);
                m_SoundStream = value;
                m_SoundLength = Bass.BASS_ChannelBytes2Seconds(m_SoundStream, Bass.BASS_ChannelGetLength(m_SoundStream, BASSMode.BASS_POS_BYTES));

                BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(m_SoundStream);

                if (info == null)
                    MessageBox.Show("No soundsystem found. Please make sure, your soundcard is connected to a speaker or headset.");

                if (info.chans != 2)
                    Logger.Error("Soundtrack: Only stereo sound streams supported!");

                Bass.BASS_ChannelSetPosition(m_SoundStream, Bass.BASS_ChannelSeconds2Bytes(m_SoundStream, App.Current.Model.GlobalTime), BASSMode.BASS_POS_BYTES);
                Bass.BASS_ChannelGetAttribute(m_SoundStream, BASSAttribute.BASS_ATTRIB_FREQ, ref m_OriginalFrequency);
            }
        }

        internal void PlayStream(double speed)
        {
            if (speed > 0.001)
            {
                Bass.BASS_ChannelSetAttribute(m_SoundStream, BASSAttribute.BASS_ATTRIB_FREQ, m_OriginalFrequency*(float) speed);
                if (Bass.BASS_ChannelIsActive(m_SoundStream) != BASSActive.BASS_ACTIVE_PLAYING)
                    Bass.BASS_ChannelPlay(m_SoundStream, false);
            }
            else
            {
                StopStream();
            }
        }

        internal void StopStream()
        {
            Bass.BASS_ChannelPause(m_SoundStream);
        }

        internal void SetStreamToTime(double time)
        {
            Bass.BASS_ChannelSetPosition(m_SoundStream, Bass.BASS_ChannelSeconds2Bytes(m_SoundStream, time), BASSMode.BASS_POS_BYTES);
        }

        internal void ToggleMutePlayback()
        {
            float currentVolume = 0;
            Bass.BASS_ChannelGetAttribute(m_SoundStream, BASSAttribute.BASS_ATTRIB_VOL, ref currentVolume);
            Bass.BASS_ChannelSetAttribute(m_SoundStream, BASSAttribute.BASS_ATTRIB_VOL, currentVolume == 0.0f ? 1.0f : 0.0f);
        }

        private bool IsPlayingNormal()
        {
            return MainWindow.CompositionView.PlaySpeed > 0.001;
        }
        
        private void SetupSoundSystemWithSoundtrack()
        {

            var registrationEmail = ProjectSettings.TryGet("Tooll.Sound.BassNetLicense.Email", "");
            var registrationKey = ProjectSettings.TryGet("Tooll.Sound.BassNetLicense.Key", "");
            if (!String.IsNullOrEmpty(registrationEmail) && !String.IsNullOrEmpty(registrationKey))
            {
                BassNet.Registration(registrationEmail, registrationKey);                
            }

            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_LATENCY, IntPtr.Zero);

            while (!ProjectSettings.Contains("Soundtrack.Path") ||
                   !File.Exists((string) ProjectSettings["Soundtrack.Path"]))
            {
                ProjectSettings["Soundtrack.Path"] = UIHelper.PickFileWithDialog(".", ProjectSettings.TryGet("Soundtrack.Path", "."), "Select Soundtrack");
            }

            if (!ProjectSettings.Contains("Soundtrack.ImagePath"))
            {
                ProjectSettings["Soundtrack.ImagePath"] = UIHelper.PickFileWithDialog(".", ProjectSettings.TryGet("Soundtrack.Path", "."), "Select optional frequency image for soundtrack");
            }

            var soundFilePath = (string) ProjectSettings["Soundtrack.Path"];
            SetProjectSoundFile(soundFilePath);
        }


        public void SetProjectSoundFile(string soundFilePath)
        {
            ProjectSettings["Soundtrack.Path"] = soundFilePath;
            Bass.BASS_StreamFree(SoundStream);
            SoundStream = Bass.BASS_StreamCreateFile(soundFilePath, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
        }

        #endregion

        private Stopwatch _stopwatch = new Stopwatch();
        private int _queueLength = 0;
        private static string SHUT_DOWN_IDENTIFIER = "Shutdown of Tooll has been completed successfully.";
        private static string STARTUP_IDENTIFIER_PRECEDING_TIMESTAMP = "Tooll is starting up at ";

        private const string TOOLL_LOG_DIRECTORY = "Log";
    }
}
