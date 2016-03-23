// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using AvalonDock;
using Framefield.Core;
using System.Windows;
using System.IO;

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
                        expandedItems.Add(e);
                }
            }
            return expandedItems;
        }

        /**
         * Recursively create subtree and add OperatorTypeButtons
         */
        private void PopulateTree(ItemsControl item, OperatorTypeTree typeTree, String prefix, HashSet<string> expandedItems)
        {
            typeTree.Operators.Sort(CompareOpsByName);
            foreach (var op in typeTree.Operators)
            {
                var b = new OperatorTypeButton(op, false);
                _operatorTypeButtons.Add(b);
                item.Items.Add(b);
            }
            typeTree.Children.Sort(CompareOpsTypeTreeByName);
            foreach (var subtree in typeTree.Children)
            {
                var newSubTree = new TreeViewItem
                                     {
                                         IsExpanded = expandedItems.Contains(prefix + subtree.Name), 
                                         Header = subtree.Name
                                     };
                item.Items.Add(newSubTree);
                PopulateTree(newSubTree, subtree, prefix + subtree.Name, expandedItems);
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

        private readonly OperatorTypeTree _typeTree = new OperatorTypeTree("root");
        private readonly List<OperatorTypeButton> _operatorTypeButtons = new List<OperatorTypeButton>();
    }
}
