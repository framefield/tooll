// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Threading.Tasks;
using System.Windows;
using NGit;
using NGit.Api;
using Sharpen;

namespace Framefield.Tooll
{
    public partial class CloneRepositoryProgressDialog
    {
        public string LocalPath { get; set; }
        public string RemotePath { get; set; }
        private OperatorGitRepository _repos;

        public CloneRepositoryProgressDialog(OperatorGitRepository repos)
        {
            InitializeComponent();
            _repos = repos;
            Loaded += CloneRepositoryProgressDialog_LoadedAsync;
        }

        async void CloneRepositoryProgressDialog_LoadedAsync(object sender, RoutedEventArgs e)
        {
            var progressIndicator = new Progress<ProgressState>(ReportProgress);
            await CloneReposAsync(_repos, progressIndicator);
            _repos.UpdateConfig();
            Close();
        }

        void ReportProgress(ProgressState progressState)
        {
            XProgressText.Text = string.Format("{0} ({1}/{2}) - {3}% done.", progressState.TaskName, progressState.Cmp, progressState.TotalWork, progressState.Percent);
            XProgressBar.Value = progressState.Percent;
        }

        struct ProgressState
        {
            public string TaskName;
            public int Cmp;
            public int TotalWork;
            public int Percent;
        }


        private async Task CloneReposAsync(OperatorGitRepository repos, IProgress<ProgressState> progress)
        {
            await Task.Run(() =>
                           {
                               try
                               {
                                   Git.CloneRepository()
                                      .SetProgressMonitor(new ProgressMonitor(progress))
                                      .SetURI(repos.RemotePath)
                                      .SetDirectory(new FilePath(repos.LocalPath))
                                      .Call();

                               }
                               catch (Exception)
                               {
                                   var errorText = string.Format("Unable to download operators from: {0}\n\nYou may find more details in your log-file.",
                                                                 repos.RemotePath + " (" + repos.Branch + ")");
                                   // throw new ShutDownException(errorText, "Connection Failure");
                               }
                           });
        }

        private class ProgressMonitor : TextProgressMonitor
        {
            private IProgress<ProgressState> _progress;

            public ProgressMonitor(IProgress<ProgressState> progress)
            {
                _progress = progress;
            }

            protected override void OnUpdate(string taskName, int cmp, int totalWork, int pcnt)
            {
                base.OnUpdate(taskName, cmp, totalWork, pcnt);
                _progress.Report(new ProgressState { TaskName = taskName, Cmp = cmp, TotalWork = totalWork, Percent = pcnt });
            }

            protected override void OnUpdate(string taskName, int workCurr)
            {
                base.OnUpdate(taskName, workCurr);
                _progress.Report(new ProgressState { TaskName = taskName, TotalWork = workCurr });
            }

            protected override void OnEndTask(string taskName, int cmp, int totalWork, int pcnt)
            {
                base.OnEndTask(taskName, cmp, totalWork, pcnt);
                _progress.Report(new ProgressState { TaskName = taskName, Cmp = cmp, TotalWork = totalWork, Percent = pcnt });
            }

            protected override void OnEndTask(string taskName, int workCurr)
            {
                base.OnEndTask(taskName, workCurr);
                _progress.Report(new ProgressState { TaskName = taskName, TotalWork = workCurr });
            }
        }
    }
}
