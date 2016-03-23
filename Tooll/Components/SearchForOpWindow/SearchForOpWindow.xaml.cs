// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Framefield.Core;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Framefield.Tooll.Components.SearchForOpWindow
{
    /// <summary>
    ///     Interaction logic for SearchForOpWindow.xaml
    /// </summary>
    public partial class SearchForOpWindow
    {
        private readonly Operator _compOp = App.Current.MainWindow.CompositionView.CompositionGraphView.CompositionOperator;
        private List<Operator> _subtree;
        private Operator[] _filteredOpEntries = { };
        private ListSortDirection _sortDirection;
        private enum _filteredBy { Name, Namespace };

        public SearchForOpWindow()
        {
            InitializeComponent();
            _subtree = Utils.GetLowerOps(_compOp);
            if (_compOp == App.Current.Model.HomeOperator)
                XCheckSearchInSubtree.Visibility = Visibility.Collapsed;
            else
                XCheckSearchInSubtree.Content += " " + _compOp.Definition.Name;

            XNamespaceHeader.Width = Width * 0.3;
            XPathHeader.Width = Width * 0.4;
            XOpNameHeader.Width = Width * 0.3;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            XSearchTextBox.Focus();
            UpdateSearchResults();
        }

        private void OnSearchTextChanged(object sender, RoutedEventArgs e)
        {
            XSearchTextBox.Background = Brushes.White;
            XWatermark.Visibility = Visibility.Hidden;

            if (XSearchTextBox.Text == string.Empty)
            {
                XWatermark.Visibility = Visibility.Visible;
            }
            else if (XCheckRegex.IsChecked == true && !VerifyRegEx())
            {
                Title = "SearchForOpWindow (invalid entry)";
                XSearchTextBox.Background = Brushes.IndianRed;
                XResultList.Items.Clear();
                return;
            }

            UpdateSearchResults();
        }

        private bool VerifyRegEx()
        {
            string testPattern = XSearchTextBox.Text;
            bool isValid = true;
            if (!string.IsNullOrEmpty(testPattern))
            {
                try
                {
                    Regex.Match("", testPattern);
                }
                catch (ArgumentException)
                {
                    isValid = false;
                }
            }
            else
            {
                isValid = false;
            }
            return isValid;
        }

        private void UpdateSearchResults()
        {
            if (XCheckNamespace.IsChecked == true)
            {
                _filteredOpEntries = FilteredOpEntries(_filteredBy.Namespace);
            }
            else
            {
                _filteredOpEntries = FilteredOpEntries(_filteredBy.Name);
            }
            if (_filteredOpEntries != null)
            {
                XResultList.Items.Clear();
                foreach (Operator op in _filteredOpEntries)
                {
                    var bright = op.Definition.Outputs.Count > 0 ? new SolidColorBrush(UIHelper.BrightColorFromType(op.Definition.Outputs[0].OpPart.Type)) : new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
                    var dark = op.Definition.Outputs.Count > 0 ? new SolidColorBrush(UIHelper.ColorFromType(op.Definition.Outputs[0].OpPart.Type)) : new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
                    var item = new ResultItem
                               {
                                   OpName = op.Name != string.Empty ? op.Name : op.Definition.Name,
                                   Path = Utils.GetOpPath(op),
                                   Namespace = op.Definition.Namespace,
                                   OpTypeColor = bright,
                                   BGColor = dark
                               };

                    XResultList.Items.Add(item);
                }
            }
            Title = "SearchForOpWindow (found " + _filteredOpEntries.Count() + " matching results)";
        }

        private double CalculateRelevancy(Operator op, List<Operator> subtree)
        {
            double rel = 1;
            if (op.Name.StartsWith(XSearchTextBox.Text, StringComparison.InvariantCultureIgnoreCase))
                rel *= 4;
            if (op.Definition.Name.StartsWith(XSearchTextBox.Text, StringComparison.InvariantCultureIgnoreCase))
                rel *= 2;
            if (subtree.Contains(op))
                rel *= 5;
            rel /= Utils.GetHierachy(op).Count;
            return rel;
        }

        private Operator[] FilteredOpEntries(_filteredBy searchBy)
        {
            string pattern = XCheckRegex.IsChecked == true ? XSearchTextBox.Text
                                                           : XSearchTextBox.Text.Select((t, i) => XSearchTextBox.Text.Substring(i, 1))
                                                                           .Where(subString => Regex.Match(subString, "[A-Z0-9_-]", RegexOptions.IgnoreCase) != Match.Empty)
                                                                           .Aggregate(".*", (current, subString) => current + (subString + ".*"));
            Operator source = XCheckSearchInSubtree.IsChecked == false ? App.Current.Model.HomeOperator : _compOp;
            return (from op in Utils.GetLowerOps(source)
                    where (Regex.Match(searchBy == _filteredBy.Namespace ? op.Definition.Namespace : op.Definition.Name, pattern, RegexOptions.IgnoreCase) != Match.Empty)
                    let rating = CalculateRelevancy(op, _subtree)
                    orderby rating
                    select op).Reverse().Take(50).ToArray();
        }

        private void XResultList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (XResultList.SelectedIndex >= 0)
            {
                var opToSearch = _filteredOpEntries[XResultList.SelectedIndex];
                Utils.JumpTo(opToSearch);
            }
        }

        private void XCheckNamespace_OnClick(object sender, RoutedEventArgs e)
        {
            OnSearchTextChanged(sender, e);
        }

        private void OnKeyUpHandler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Return:
                    if (XResultList.SelectedIndex < 0)
                        XResultList.SelectedIndex = 0;
                    e.Handled = true;
                    Close();
                    break;
                case Key.Escape:
                    e.Handled = true;
                    Close();
                    break;
                case Key.Delete:
                    App.Current.MainWindow.RemoveOperators(OpsToDelete);
                    _subtree = Utils.GetLowerOps(_compOp);
                    UpdateSearchResults();
                    e.Handled = true;
                    break;
            }
        }

        private void XSearchTextBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (XResultList.SelectedIndex > 0)
                        XResultList.SelectedIndex--;
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (XResultList.SelectedIndex < XResultList.Items.Count)
                        XResultList.SelectedIndex++;
                    e.Handled = true;
                    break;
            }
        }

        private IEnumerable<Operator> OpsToDelete
        {
            get
            {
                return (from object item in XResultList.SelectedItems
                        select _filteredOpEntries.ElementAt(XResultList.SelectedItems.IndexOf(item))
                        into op
                        where op != null
                        select op);
            }
        }

        private void OnHeaderClicked(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked != null)
            {
                _sortDirection = _sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                XResultList.Items.SortDescriptions.Clear();
                XResultList.Items.SortDescriptions.Add(new SortDescription(headerClicked.ContentStringFormat, _sortDirection));
                e.Handled = true;
            }
        }

        private void SearchForOpWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            XNamespaceHeader.Width = Width * 0.3;
            XPathHeader.Width = Width * 0.4;
            XOpNameHeader.Width = Width * 0.3;
        }

        public class ResultItem
        {
            public string OpName { get; set; }
            public string Path
            {
                get { return _path + OpName; } 
                set { _path = value; }
            }
            public string Namespace { get; set; }
            public SolidColorBrush OpTypeColor { get; set; }
            public SolidColorBrush BGColor { get; set; }

            private string _path;
        }
    }
}
