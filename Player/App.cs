// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.IO;
using System.Windows.Threading;
using Framefield.Core;
using Framefield.Core.Profiling;
using Framefield.Shared;


namespace Framefield.Player
{
    class App
    {
        [STAThread]
        private static void Main(string[] args)
        {
            LibPathManager.SetDllSearchPath();
            AppDomain.CurrentDomain.AssemblyResolve += LibPathManager.CustomResolve;

            RunPlayer(args);
        }

        private static void RunPlayer(string[] args)
        {
            Logger.Initialize(Dispatcher.CurrentDispatcher);
            var commandLineOptions = new CommandLineOptions(args);

            try
            {
                using (var player = new Player())
                {
                    OperatorPart.EnableEventPropagationByDefault = false;
                    var consoleWriter = new ConsoleWriter();
                    consoleWriter.Filter = LogEntry.EntryLevel.INF | LogEntry.EntryLevel.WRN | LogEntry.EntryLevel.ERR;
                    Logger.AddWriter(consoleWriter);

                    Directory.CreateDirectory("logs");
                    var logWriter = new FileWriter(String.Format("logs/{0}.log", DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss_fff")));
                    logWriter.Filter = LogEntry.EntryLevel.ALL;
                    Logger.AddWriter(logWriter);

                    Logger.Info("Version: {0}.{1} ({2}, {3})", Constants.VersionAsString, BuildProperties.Build, BuildProperties.Branch, BuildProperties.CommitShort);

                    try
                    {
                        var startUpDlg = new StartUpDialog();
                        if (!commandLineOptions.HideDialog)
                        {
                            startUpDlg.ShowDialog();
                            if (!startUpDlg.Accepted)
                                return;
                        }

                        if (player.Initialize(startUpDlg.Settings))
                        {
                            TimeLogger.Enabled = commandLineOptions.TimeLoggingEnabled;
                            player.Precalc();
                            player.Run();
                        }
                    }
                    catch (Exception ex)
                    {
                        var reportString = "Message:".PadRight(15) + ex.Message + "\n\n";
                        reportString += "Source:".PadRight(15) + ex.Source + "\n";
                        reportString += "InnerException:".PadRight(15) + ex.InnerException + "\n\n";
                        reportString += "Stacktrace:\n--------------" + "\n";
                        reportString += CrashReporter.GetFormattedStackTrace(ex) + "\n";
                        Logger.Error(reportString);

                        Console.Write("Press any key to continue . . .");
                        Console.ReadKey(true);
                    }
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
