// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using Framefield.Core;
using Microsoft.Win32;

namespace Framefield.Tooll.Components
{
    public partial class AdditionalAssembliesWindow
    {
        public AdditionalAssembliesWindow(IEnumerable<string> additionalAssemblies, IEnumerable<string> supplierAssemblyNames)
        {
            InitializeComponent();

            foreach (var assemblyEntry in additionalAssemblies)
            {
                CreateAndAddExternalRow(assemblyEntry);
            }
            CreateAndAddExternalRow(string.Empty);

            foreach (var assemblyName in supplierAssemblyNames)
            {
                CreateAndAddInternalRow(assemblyName);
            }
            CreateAndAddInternalRow(string.Empty);
        }

        public IEnumerable<string> AdditionalAssemblies
        {
            get
            {
                return from rowEntry in XExternalAssembliesStackPanel.Children.OfType<AdditionalAssemblyEntryRow>()
                       let entryText = rowEntry.XAssemblyEntryNameEdit.Text
                       where !string.IsNullOrEmpty(entryText)
                       select entryText;
            }
        }

        public IEnumerable<Assembly> SupplierAssemblies
        {
            get
            {
                return from rowEntry in XInternalAssembliesStackPanel.Children.OfType<SupplyAssemblyEntryRow>()
                       let entry = rowEntry.XAssemblyEntryComboBox.SelectedItem as AssemblyEntry
                       where entry != null
                       select entry.Assembly;
            }
        }

        private void CreateAndAddExternalRow(string assemblyEntry)
        {
            var rowEntry = new AdditionalAssemblyEntryRow { XAssemblyEntryNameEdit = { Text = assemblyEntry } };

            rowEntry.XPathButton.Click += (sender, args) =>
                                          {
                                              var path = rowEntry.XAssemblyEntryNameEdit.Text;
                                              if (!Directory.Exists(path))
                                                  path = ".";

                                              var dlg = new OpenFileDialog
                                                            {
                                                                Filter = "Assembly|*.dll",
                                                                Title = "Select Assembly",
                                                                DereferenceLinks = false,
                                                                InitialDirectory = Path.GetFullPath(path)
                                                            };

                                              bool? result = dlg.ShowDialog();

                                              if (result != true)
                                                  return;

                                              var filepath = dlg.FileName;
                                              var currentAppPath = Path.GetFullPath(".").Replace("c:\\", "C:\\") + "\\";
                                              filepath = filepath.Replace(currentAppPath, "").Replace("\\", "/");
                                              rowEntry.XAssemblyEntryNameEdit.Text = filepath;
                                          };

            rowEntry.XAddButton.Click += (o, args) => CreateAndAddExternalRow(string.Empty);

            rowEntry.XRemoveButton.Click += (o, args) =>
                                            {
                                                if (XExternalAssembliesStackPanel.Children.Count > 1)
                                                {
                                                    XExternalAssembliesStackPanel.Children.Remove(rowEntry);
                                                }
                                            };

            XExternalAssembliesStackPanel.Children.Add(rowEntry);
        }

        private class AssemblyEntry
        {
            public Assembly Assembly { get; set; }
            public Type Type { get; set; }

            public override string ToString()
            {
                return Type.Name;
            }
        }

        private void CreateAndAddInternalRow(string assemblyEntry)
        {
            var rowEntry = new SupplyAssemblyEntryRow();

            if (assemblyEntry == string.Empty)
            {
                rowEntry.XAssemblyEntryComboBox.Items.Add(string.Empty);
                rowEntry.XAssemblyEntryComboBox.SelectedIndex = 0;
            }

            // extract all existing supplier assemblies and collect the newest one for each type
            try
            {
                var asmAndTypes = Utilities.GetAssembliesAndTypesOfCurrentDomain();
                var supplierTypes = from asmTypeTuple in asmAndTypes
                                    let asm = asmTypeTuple.Item1
                                    from type in asmTypeTuple.Item2
                                    where SupplierAssembly.IsSupplierAssembly(type)
                                    let entry = new AssemblyEntry { Assembly = asm, Type = type }
                                    group entry by entry.ToString()
                                    into typeGroups
                                    select (from groupEntry in typeGroups.ToArray()
                                            orderby File.GetCreationTime(groupEntry.Assembly.Location) descending
                                            select groupEntry).First();

                //Logger.Info("row type name: {0}", assemblyEntry);
                foreach (var type in supplierTypes)
                {
                    rowEntry.XAssemblyEntryComboBox.Items.Add(type);
                    if (assemblyEntry == type.Assembly.GetName().Name)
                    {
                        rowEntry.XAssemblyEntryComboBox.SelectedItem = type;
                    }
                    //Logger.Info("found type: " + type.Type.Name + "  " + type.Assembly.GetName().Name + " -> " + File.GetCreationTime(type.Assembly.Location));
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Error getting creation time of a supplier assembly: {0} - {1}", exception.Message, exception.InnerException);
                return;
            }

            rowEntry.XAddButton.Click += (o, args) => CreateAndAddInternalRow(string.Empty);

            rowEntry.XRemoveButton.Click += (o, args) =>
                                            {
                                                if (XInternalAssembliesStackPanel.Children.Count > 1)
                                                {
                                                    XInternalAssembliesStackPanel.Children.Remove(rowEntry);
                                                }
                                            };

            XInternalAssembliesStackPanel.Children.Add(rowEntry);
        }

        private void XOKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

    }
}
