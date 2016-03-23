// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Framefield.Core;

namespace Framefield.Tooll.Components
{
    /// <summary>
    /// Interaction logic for CrashReportWindow.xaml
    /// </summary>
    public partial class CrashReportWindow : Window
    {
        public CrashReportWindow() {
            InitializeComponent();
        }

        void ExitButtonHandler(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        public static void ShowCrashReportForException(Exception ex)
        {
            var reportString = "Message:".PadRight(15) + ex.Message + "\n\n";
            reportString += "Source:".PadRight(15) + ex.Source + "\n";
            reportString += "InnerException:".PadRight(15) + ex.InnerException + "\n\n";
            reportString += "Stacktrace:\n--------------" + "\n";
            reportString += CrashReporter.GetFormattedStackTrace(ex) + "\n";
            Logger.Error(reportString);

            CrashReporter.WriteCrashReport(ex);
            var crashReporter = new Components.CrashReportWindow();
            crashReporter.XCrashReportTextBox.Text = String.Format("Version: {0}.{1} ({2}, {3})\n", 
                                                                    Constants.VersionAsString, 
                                                                    BuildProperties.Build, 
                                                                    BuildProperties.Branch, 
                                                                    BuildProperties.CommitShort);
            crashReporter.XCrashReportTextBox.Text += CrashReporter.ComposeCrashReport(ex);
            crashReporter.ShowDialog();
        }

    }
}
