// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using NGit;
using NGit.Api;
using NGit.Storage.File;
using NGit.Transport;
using NGit.Util;
using NSch;
using Logger = Framefield.Core.Logger;

namespace Framefield.Tooll
{
    public class OperatorGitRepository
    {
        public readonly String LocalPath = string.Empty;
        private string _branch;
        public String RemotePath { get; set; }

        public bool IsValid { get { return Git != null && Git.GetRepository().GetAllRefs().Any(); } }

        public String Branch
        {
            get { return _branch; }
            set
            {
                _branch = value;

                var branches = Git.BranchList()
                                  .SetListMode(ListBranchCommand.ListMode.ALL)
                                  .Call();

                var searchedRemoteBranch = (from branch in branches
                                            let wholeBranchName = branch.GetName()
                                            where wholeBranchName.StartsWith("refs/remotes/origin/")
                                            let branchName = wholeBranchName.Remove(0, "refs/remotes/origin/".Length)
                                            where branchName == _branch
                                            select branchName).SingleOrDefault();

                if (searchedRemoteBranch == null)
                {
                    try
                    {
                        // remote ref spec doesn't exist yet, so create it
                        var refSpec = new RefSpec().SetSourceDestination(_branch, _branch);
                        Git.Push()
                           .SetRemote("origin")
                           .SetRefSpecs(new List<RefSpec>() {refSpec})
                           .Call();
                    }
                    catch (Exception exception)
                    {
                        Core.Logger.Info("Error creating remote branch for local branch '{0}': {1}", _branch, exception.Message);
                    }
                }

                UpdateConfig();
            }
        }

        public Repository LocalRepo { get; private set; }
        public Git Git { get; private set; }

        public OperatorGitRepository(string localPath)
        {
            LocalPath = localPath;
            LocalRepo = new FileRepository(LocalPath + "/.git");
            Git = new Git(LocalRepo);
            SshSessionFactory.SetInstance(new MyJschConfigSessionFactory());
        }

        public void UpdateConfig()
        {
            var config = Git.GetRepository().GetConfig();
            config.SetBoolean("core", null, "autocrlf", false);
            config.SetString("branch", _branch, "merge", "refs/heads/" + _branch);
            config.SetString("branch", _branch, "remote", "origin");
            config.SetBoolean("branch", Branch, "rebase", true);
            if (App.Current.UserSettings.Contains("User.Name"))
            {
                config.SetString("user", null, "name", (string) App.Current.UserSettings["User.Name"]);
            }
            if (App.Current.UserSettings.Contains("User.Email"))
            {
                config.SetString("user", null, "email", (string) App.Current.UserSettings["User.Email"]);
            }
            config.Save();
        }

        private class MyJschConfigSessionFactory : JschConfigSessionFactory
        {
            protected override void Configure(OpenSshConfig.Host hc, Session session)
            {
            }

            protected override JSch GetJSch(OpenSshConfig.Host hc, FS fs)
            {
                JSch.SetConfig("StrictHostKeyChecking", "no"); // automatic acceptance for known host if 'known_hosts' is missing
                var jsch = base.GetJSch(hc, fs);
                var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var sshPath = homePath + @"\.ssh";
                jsch.AddIdentity(sshPath + @"\id_rsa");
                jsch.SetKnownHosts(sshPath + @"\known_hosts");
                return jsch;
            }
        }
    }
}