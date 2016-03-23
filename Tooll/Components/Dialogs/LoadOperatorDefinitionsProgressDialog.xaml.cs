// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Threading.Tasks;
using System.Windows;
using Framefield.Core;

namespace Framefield.Tooll
{
    public partial class LoadOperatorDefinitionsProgressDialog
    {
        public LoadOperatorDefinitionsProgressDialog()
        {
            InitializeComponent();
            Loaded += LoadOperatorDefinitionsProgressDialog_LoadedAsync;
        }

        async void LoadOperatorDefinitionsProgressDialog_LoadedAsync(object sender, RoutedEventArgs e)
        {
            IProgress<float> progressIndicator = new Progress<float>(ReportProgress);
            MetaManager.InitializeCallback = progressIndicator.Report;
            await Task.Run(() => MetaManager.Instance.LoadMetaOperators());
            Close();
        }

        void ReportProgress(float progress)
        {
            XProgressText.Text = (progress*100.0f).ToString("0.") + "%";
            XProgressBar.Value = progress*100;
        }
    }
}
