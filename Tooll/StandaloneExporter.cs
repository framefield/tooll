// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Framefield.Core;

namespace Framefield.Tooll
{
    public static class StandaloneExporter
    {
        public static void Export(Operator operatorToExport, String baseExportPath)
        {
            var firstOutputOfOperatorToExport = operatorToExport.Outputs.FirstOrDefault();
            if (firstOutputOfOperatorToExport == null)
            {
                Logger.Warn("This Operators doens't have any outputs. Nothing to export.");
                return;
            }

            Logger.Info("Exporting started...");
            try
            {
                // create output dir
                if (Directory.Exists(baseExportPath))
                    Directory.Delete(baseExportPath, true);
                Directory.CreateDirectory(baseExportPath);

                // traverse and collect
                var collectedMetaOperators = new HashSet<MetaOperator>();
                firstOutputOfOperatorToExport.CollectAllMetaOperators(collectedMetaOperators);
                Logger.Info("  Exporting {0} operators...", collectedMetaOperators.Count);

                var operatorExportPath = baseExportPath + "Operators" + @"\";
                Directory.CreateDirectory(operatorExportPath);
                MetaManager.WriteOperators(collectedMetaOperators, operatorExportPath, clearChangedFlags:false);


                var configExportPath = baseExportPath + "config" + @"\";
                Directory.CreateDirectory(configExportPath);
                // make the selected op the home op (move from operators dir to config dir and rename it to Home.mop)
                File.Move(operatorExportPath + operatorToExport.Definition.ID + ".mop", configExportPath + "Home.mop");

                App.Current.ProjectSettings.SaveAs(configExportPath + "ProjectSettings.json");


                var filePathsToCopy = new List<String>();

                var collectedOperators = new HashSet<Operator>();
                firstOutputOfOperatorToExport.CollectAllOperators(collectedOperators);
                foreach (var foundOp in collectedOperators)
                {
                    foreach (var input in foundOp.Inputs)
                    {
                        if (input.Name.EndsWith("Path") && input.Connections.Count == 0)
                        {
                            var context = new OperatorPartContext();
                            string path = input.Eval(context).Text;
                            if (!File.Exists(path))
                            {
                                Logger.Warn("  {0} links to the non-existing file {1}.", foundOp.Definition.Name, path);
                            }
                            else if (Path.IsPathRooted(path))
                            {
                                Logger.Warn("  {0} loads {1} with an absolute filepath which can't be copied to the export directory.", foundOp.Definition.Name, path);
                            }
                            else
                            {
                                filePathsToCopy.Add(path);
                            }
                        }
                    }
                }

                var soundFilePath = (string)App.Current.ProjectSettings["Soundtrack.Path"];
                if (Path.IsPathRooted(soundFilePath))
                {
                    Logger.Warn("  The sound file {0} is an absolute filepath which can't be copied to the export directory.", soundFilePath);
                }
                else
                {
                    filePathsToCopy.Add(soundFilePath);
                }
                filePathsToCopy.Add("assets-common/image/white.png");
                filePathsToCopy.AddRange(Directory.GetFiles("assets-common/fx/", "*"));
                filePathsToCopy.AddRange(Directory.GetFiles("assets-common/bmfont/", "*"));


                var collectedDlls = new HashSet<String>();
                foreach (var metaOp in collectedMetaOperators)
                {
                    if (metaOp.IsBasic)
                    {
                        var operatorPartDefinition = metaOp.OperatorParts.First().Item2;
                        foreach (var asmFile in operatorPartDefinition.AdditionalAssemblies)
                        {
                            filePathsToCopy.Add(asmFile);

                            var info = new FileInfo(asmFile);
                            Assembly asm = Assembly.LoadFile(info.FullName);
                            CollectAllReferencedDlls(asm, collectedDlls);
                        }
                    }
                }

                filePathsToCopy.Add("Player.exe");
                filePathsToCopy.Add("Player.exe.config");

                /* Tom: I temporarily disabled gathering the dependencies because it triggered
                 * a crash unless Framefield.fbx.dll could not be found in tooll's root directory.
                 * Since the complete lib-directory is already copied above, it's not necessary,
                 * however, we should clean this up eventually.
                 */ 
                /*
                var playerFileInfo = new FileInfo("Player.exe");
                Assembly playerAssembly = Assembly.LoadFile(playerFileInfo.FullName);
                CollectAllReferencedDlls(playerAssembly, collectedDlls);
                foreach (var collectedDll in collectedDlls)
                {
                    if (collectedDll.StartsWith(playerFileInfo.DirectoryName))
                    {
                        var relativePath = collectedDll.Replace(playerFileInfo.DirectoryName + @"\", "");
                        filePathsToCopy.Add(relativePath);
                    }
                }
                 */
                  
                filePathsToCopy.AddRange(Directory.GetFiles("libs/x64", "sharpdx*.dll"));
                filePathsToCopy.AddRange(Directory.GetFiles("libs/x64", "d3d*.dll"));
                filePathsToCopy.Add("libs/x64/bass.dll");
                filePathsToCopy.Add("libs/x64/libfbxsdk.dll");

                filePathsToCopy.Add("Core.dll");
                filePathsToCopy.AddRange(Directory.GetFiles("libs/", "*"));

                foreach (var filePath in filePathsToCopy)
                {
                    string targetFilePath = baseExportPath + filePath;
                    string targetPath = Path.GetDirectoryName(targetFilePath);
                    if (!Directory.Exists(targetPath))
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    if (!File.Exists(targetFilePath))
                    {
                        File.Copy(Directory.GetCurrentDirectory() + @"\" + filePath, targetFilePath);
                    }
                }

                Logger.Info("Exporting finished");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to export: {0}", ex.Message);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        private static extern int GetModuleFileNameA(IntPtr hModule, StringBuilder path, int size);

        private static void CollectAllReferencedDlls(Assembly assembly, HashSet<String> collectedDlls)
        {
            foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies())
            {
                var loadedAssembly = Assembly.Load(assemblyName);
                if (!collectedDlls.Contains(loadedAssembly.Location))
                {
                    collectedDlls.Add(loadedAssembly.Location);
                    CollectAllReferencedDlls(loadedAssembly, collectedDlls);

                    foreach (Type type in loadedAssembly.GetTypes())
                    {
                        foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            DllImportAttribute attrib = (DllImportAttribute)Attribute.GetCustomAttribute(method, typeof(DllImportAttribute));
                            if (attrib != null && !collectedDlls.Contains(attrib.Value))
                            {
                                //var beforeModules = new HashSet<String>();
                                //ProcessModuleCollection myProcessModuleCollection = Process.GetCurrentProcess().Modules;
                                //for (int i = 0; i < myProcessModuleCollection.Count; ++i)
                                //{
                                //    beforeModules.Add(myProcessModuleCollection[i].FileName);
                                //}

                                IntPtr loadedModuleHandle = LoadLibrary(attrib.Value);
                                if (loadedModuleHandle != null)
                                {
                                    StringBuilder path = new StringBuilder(1024);
                                    GetModuleFileNameA(loadedModuleHandle, path, path.Capacity);
                                    if (path.ToString().EndsWith("dll", StringComparison.CurrentCultureIgnoreCase))
                                        collectedDlls.Add(path.ToString());

                                    //var afterModules = new HashSet<String>();
                                    //myProcessModuleCollection = Process.GetCurrentProcess().Modules;
                                    //for (int i = 0; i < myProcessModuleCollection.Count; ++i)
                                    //{
                                    //    afterModules.Add(myProcessModuleCollection[i].FileName);
                                    //}

                                    FreeLibrary(loadedModuleHandle);

                                    //HashSet<string> complemenetSet = new HashSet<string>(afterModules.Except(beforeModules));
                                    //collectedDlls.UnionWith(complemenetSet);
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
