// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Framefield.Core;
using Framefield.Core.Testing;
using Framefield.Shared;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Multimedia;
using Un4seen.Bass;
using Device = SharpDX.Direct3D11.Device;
using Utilities = Framefield.Core.Utilities;

namespace Framefield.OperatorTester
{
    class OperatorTester
    {
        [STAThread]
        static void Main()
        {
            LibPathManager.SetDllSearchPath();
            AppDomain.CurrentDomain.AssemblyResolve += LibPathManager.CustomResolve;

            RunTests();
        }

        private static void RunTests()
        {
            Logger.Initialize(Dispatcher.CurrentDispatcher);

            var operatorTester = new OperatorTester();
            try
            {
                Directory.CreateDirectory("logs");
                var consoleWriter = new ConsoleWriter();
                consoleWriter.Filter = LogEntry.EntryLevel.INF | LogEntry.EntryLevel.WRN | LogEntry.EntryLevel.ERR;
                Logger.AddWriter(consoleWriter);

                var logWriter = new FileWriter(String.Format("logs/{0}.log", DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss_fff")));
                logWriter.Filter = LogEntry.EntryLevel.ALL;
                Logger.AddWriter(logWriter);

                Logger.Info("Version: {0}.{1} ({2}, {3})", Constants.VersionAsString, BuildProperties.Build, BuildProperties.Branch, BuildProperties.CommitShort);

                try
                {
                    string filterPattern = String.Empty;
                    if (Environment.GetCommandLineArgs().Count() > 1)
                        filterPattern = Environment.GetCommandLineArgs()[1];

                    try
                    {
                        new Regex(filterPattern);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("error: filter pattern is not a valid regular expression: {0}", ex.Message);
                        Console.WriteLine("usage: {0} [<filter-pattern> [<test-reference-path>]]", new FileInfo(Environment.GetCommandLineArgs()[0]).Name);
                        Logger.Dispose();
                        return;
                    }

                    string referencePath = "assets-ff/test-references/";
                    if (Environment.GetCommandLineArgs().Count() > 2)
                        referencePath = Environment.GetCommandLineArgs()[2];

                    if (referencePath.Count() > 0 && !Directory.Exists(referencePath))
                    {
                        Console.WriteLine("error: given test reference path is das not exist");
                        Console.WriteLine("usage: {0} [<filter-pattern> [<test-reference-path>]]", new FileInfo(Environment.GetCommandLineArgs()[0]).Name);
                        Logger.Dispose();
                        return;
                    }


                    Logger.Info("Initializing ...");
                    Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_LATENCY, IntPtr.Zero);

                    var featureLevels = new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_1 };
                    D3DDevice.Device = new Device(DriverType.Hardware, DeviceCreationFlags.None | DeviceCreationFlags.BgraSupport, featureLevels);
                    using (var dxgiDevice = D3DDevice.Device.QueryInterface<SharpDX.DXGI.Device1>())
                    {
                        var adapter = dxgiDevice.Adapter;
                        D3DDevice.DX10_1Device = new SharpDX.Direct3D10.Device1(adapter, SharpDX.Direct3D10.DeviceCreationFlags.BgraSupport, SharpDX.Direct3D10.FeatureLevel.Level_10_1);
                    }
                    D3DDevice.Direct2DFactory = new SharpDX.Direct2D1.Factory();
                    D3DDevice.DirectWriteFactory = new SharpDX.DirectWrite.Factory();

                    SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, SharpDX.RawInput.DeviceFlags.None);
                    SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None);
                    SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericJoystick, SharpDX.RawInput.DeviceFlags.None);

                    MetaManager.Instance.LoadMetaOperators();
                    Logger.Info("Using test filter pattern \"{0}\"", filterPattern);
                    MetaManager.OPERATOR_TEST_REFERENCE_PATH = referencePath;
                    Logger.Info("Using test reference path \"{0}\"", MetaManager.OPERATOR_TEST_REFERENCE_PATH);

                    Logger.Info("Testing ...");
                    var allTestsMetaOpID = Guid.Parse("7dccfa46-2551-4efb-a802-5ed3be914c54");
                    var result = TestUtilities.EvaluateTests(allTestsMetaOpID, filterPattern);
                    Environment.ExitCode = result.Item1 ? 0 : 1;
                    Logger.Info("Result:");
                    Logger.Info("\n" + result.Item2);

                    Logger.Info("Finalizing ...");
                    ResourceManager.DisposeAll();
                    Utilities.DisposeObj(ref D3DDevice.DX10_1Device);
                    Utilities.DisposeObj(ref D3DDevice.Direct2DFactory);
                    Utilities.DisposeObj(ref D3DDevice.DirectWriteFactory);
                    Utilities.DisposeObj(ref D3DDevice.Device);
                }
                catch (Exception ex)
                {
                    ResourceManager.DisposeAll();
                    Utilities.DisposeObj(ref D3DDevice.Device);

                    String reportString = "Message:".PadRight(15) + ex.Message + "\n\n";
                    reportString += "Source:".PadRight(15) + ex.Source + "\n";
                    reportString += "InnerException:".PadRight(15) + ex.InnerException + "\n\n";
                    reportString += "Stacktrace:\n--------------" + "\n";
                    reportString += CrashReporter.GetFormattedStackTrace(ex) + "\n";
                    Logger.Error(reportString);
                }

                Logger.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\nFATAL: Exception thrown where it never should happen: " + ex.Message);
            }
        }
    }

}
