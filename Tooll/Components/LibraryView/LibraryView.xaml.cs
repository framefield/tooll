// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using AvalonDock;
using Framefield.Core;
using System.Windows;
using System.IO;
using System.Text;
using Framefield.Tooll.Components.Dialogs;
using Framefield.Core.Commands;

namespace Framefield.Tooll
{
    public partial class LibraryView : DocumentContent
    {
        public LibraryView()
        {
            InitializeComponent();
            Title = "Library";

            App.Current.Model.MetaOpManager.ChangedEvent += (o, a) => UpdateOperatorTree();
            UpdateOperatorTree();
        }

        public void UpdateOperatorTree()
        {
            var expandedItems = CollectExpandedItems("", XTreeView);
            XTreeView.Items.Clear();
            _typeTree.Clear();

            foreach (var metaOpEntry in App.Current.Model.MetaOpManager.MetaOperators)
            {
                if (metaOpEntry.Value == null)
                {
                    string messageBoxText = String.Format("Requested undefined MetaOperator {0}.\nDo you want to delete Config/Home.mop? You should also delete Temp/Cache before restarting.", metaOpEntry.Key);
                    var caption = "Inconsistent Operator";
                    var button = MessageBoxButton.YesNo;
                    var icon = MessageBoxImage.Warning;
                    var result = MessageBox.Show(messageBoxText, caption, button, icon);
                    if (result == MessageBoxResult.Yes)
                    {
                        File.Delete(@"Config/Home.mop");
                        //Directory.Delete(@".\Operators\Cache");
                    }

                    App.Current.Shutdown(110);
                    Environment.Exit(110);
                }

                _typeTree.SortInOperator(metaOpEntry.Value);
            }
            _operatorTypeButtons.Clear();
            PopulateTree(XTreeView, _typeTree, "", expandedItems);
        }


        HashSet<string> CollectExpandedItems(String prefix, ItemsControl item)
        {
            var expandedItems = new HashSet<string>();
            var treeViewItem = item as TreeViewItem;
            String fullname = prefix;
            if (treeViewItem != null)
            {
                fullname = prefix + (String)treeViewItem.Header;
                if (treeViewItem.IsExpanded)
                    expandedItems.Add(fullname);
            }

            foreach (var child in item.Items)
            {
                if (child is ItemsControl)
                {
                    foreach (var e in CollectExpandedItems(fullname, (ItemsControl)child))
                    {
                        expandedItems.Add(e);
                    }
                }
            }
            return expandedItems;
        }

        private ContextMenu CreateContextMenu()
        {
            var newContextMenu = new ContextMenu();

            var menuItem = new MenuItem()
            {
                Header = "Rename namespace",
            };
            menuItem.Click += RenameNamespaceHandler;
            newContextMenu.Items.Add(menuItem);
            return newContextMenu;
        }

        private void RenameNamespaceHandler(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is OperatorTypeTree tree)
            {
                var names = new List<String>();
                foreach (var p in tree.Parents)
                {
                    names.Add(p.Name);
                }
                names.Add(tree.Name);

                var joinedNames = String.Join(".", names);

                var popup = new TextInputWindow();
                popup.XText.Text = "Rename operator namespace?";
                popup.XTextBox.Text = joinedNames;
                popup.XTextBox.SelectAll();
                popup.XTextBox.Focus();
                popup.ShowDialog();

                if (popup.DialogResult == false)
                    return;

                var newNameSpace = popup.XTextBox.Text;
                if (newNameSpace.EndsWith("."))
                {
                    MessageBox.Show("Namespace should have tailing '.' character.", "Sorry");
                    return;
                }

                if (newNameSpace.Contains(" "))
                {
                    MessageBox.Show("Namespace should not contain spaces.", "Sorry");
                    return;
                }

                var commands = new List<ICommand>();

                foreach (var opDefinition in App.Current.Model.MetaOpManager.MetaOperators.Values)
                {
                    if (!opDefinition.Namespace.StartsWith(joinedNames))
                        continue;

                    var newNamespace = opDefinition.Namespace.Replace(joinedNames, newNameSpace);
                    commands.Add(new RenameOperatorNamespaceCommand(opDefinition, newNamespace));
                }

                if (commands.Count > 0)
                {
                    Logger.Info("Updated namespace of {0} Operators", commands.Count);
                    var macroCommand = new MacroCommand("Rename namespace", commands);
                    App.Current.UndoRedoStack.AddAndExecute(macroCommand);
                    App.Current.MainWindow.XLibraryView.UpdateOperatorTree();
                }
                else
                {
                    Logger.Info("No matching operators found");
                }
            }
        }


        /**
         * Recursively create subtree and add OperatorTypeButtons
         */
        private void PopulateTree(ItemsControl item, OperatorTypeTree typeTree, String prefix, HashSet<string> expandedItems)
        {
            typeTree.Children.Sort(CompareOpsTypeTreeByName);

            foreach (var subtree in typeTree.Children)
            {
                var newSubTreeItem = new TreeViewItem()
                {
                    IsExpanded = expandedItems.Contains(prefix + subtree.Name),
                    Header = subtree.Name,
                    DataContext = subtree,
                };

                newSubTreeItem.ContextMenu = CreateContextMenu();
                item.Items.Add(newSubTreeItem);
                PopulateTree(newSubTreeItem, subtree, prefix + subtree.Name, expandedItems);
            }

            typeTree.Operators.Sort(CompareOpsByName);

            foreach (var op in typeTree.Operators)
            {
                var b = new OperatorTypeButton(op, false);
                _operatorTypeButtons.Add(b);
                item.Items.Add(b);
            }
        }


        void HighlightMatchingOperators(FunctionType opType)
        {
            foreach (var otb in _operatorTypeButtons)
            {
                otb.Opacity = IsMatchingOpType(opType, otb) ? 1.0 : 0.3;
            }
        }

        private static bool IsMatchingOpType(FunctionType opType, OperatorTypeButton otb)
        {
            return (otb.MetaOp.Inputs.Count > 0) &&
                   ((otb.MetaOp.Inputs[0].OpPart.Type == opType) ||
                    (otb.MetaOp.Inputs[0].OpPart.Type == FunctionType.Generic));
        }

        void HighlightNothing()
        {
            foreach (var otb in _operatorTypeButtons)
            {
                otb.Opacity = 1.0;
            }
        }

        public void UpdateViewToCurrentSelectionHandler(object sender, SelectionHandler.FirstSelectedChangedEventArgs e)
        {
            var opWidget = e.Element as OperatorWidget;
            if (opWidget != null)
            {
                if (opWidget.Operator.Outputs.Count > 0)
                {
                    HighlightMatchingOperators(opWidget.Operator.Outputs[0].Type);
                }
                return;
            }
            HighlightNothing();
        }


        private static int CompareOpsByName(MetaOperator lhs, MetaOperator rhs)
        {
            return lhs.Name.CompareTo(rhs.Name);
        }

        private static int CompareOpsTypeTreeByName(OperatorTypeTree lhs, OperatorTypeTree rhs)
        {
            return lhs.Name.CompareTo(rhs.Name);
        }

        private readonly OperatorTypeTree _typeTree = new OperatorTypeTree("root", new List<OperatorTypeTree>());
        private readonly List<OperatorTypeButton> _operatorTypeButtons = new List<OperatorTypeButton>();
    }
}
