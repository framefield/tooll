// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for QuickCreateWindow.xaml
    /// </summary>
    public partial class QuickCreateWindow
    {
        public MetaOperator SelectedMetaOp { get; private set; }

        public Core.FunctionType SelectedType { get; set; }

        private readonly Operator _compositionOperator;
        private readonly Dictionary<Guid, int> _numberOfMetaOperatorUsage = new Dictionary<Guid, int>();

        private bool IsCompositionOperatorAProjectOperator
        {
            get
            {
                var op = _compositionOperator;
                while (op != App.Current.Model.HomeOperator)
                {
                    if (op.Definition.Namespace.StartsWith(@"projects."))
                        return true;

                    op = op.Parent;
                }
                return false;
            }
        }

        public Point RelavantPositionOnWorkspace { get; set; }

        public QuickCreateWindow(Operator compositionOperator)
        {
            SelectedType = FunctionType.Generic;
            InitializeComponent();
            SelectedMetaOp = null;
            _compositionOperator = compositionOperator;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitDictionaryWithMetaOperatorUsageCount();
            XSearchTextBox.Focus();
            SelectedMetaOp = null;
            if (SelectedType != FunctionType.Generic)
            {
                UpdateSearchResults();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (XSearchTextBox.Text != "")
            {
                UpdateSearchResults();
            }
            UpdateSearchResultVisibility();
        }

        private void UpdateSearchResultVisibility()
        {
            if (XSearchTextBox.Text == "")
            {
                XSearchResults.Visibility = Visibility.Collapsed;
                XSearchPlaceholder.Visibility = Visibility.Visible;
                XIngredientsFinder.Visibility = Visibility.Visible;
            }
            else
            {
                XSearchResults.Visibility = Visibility.Visible;
                XSearchPlaceholder.Visibility = Visibility.Collapsed;
                XIngredientsFinder.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSearchResults()
        {
            var pattern = XSearchTextBox.Text.Select((t, i) => XSearchTextBox.Text.Substring(i, 1))
                          .Where(subString => Regex.Match(subString, "[A-Z0-9_-]", RegexOptions.IgnoreCase) != Match.Empty)
                          .Aggregate(".*", (current, subString) => current + (subString + ".*"));

            var currentProjectName = GetCurrentProjectName();

            var filteredOpEntries = (from metaOpEntry in App.Current.Model.MetaOpManager.MetaOperators
                                     where (Regex.Match(metaOpEntry.Value.Name, pattern, RegexOptions.IgnoreCase) != Match.Empty ||
                                            metaOpEntry.Value.Description.IndexOf(XSearchTextBox.Text, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                                            metaOpEntry.Value.Namespace.IndexOf(XSearchTextBox.Text, StringComparison.InvariantCultureIgnoreCase) != -1
                                     )
                                     let rating = ComputeRelevancy(metaOpEntry.Value, XSearchTextBox.Text, currentProjectName)
                                     orderby rating
                                     select new { Op = metaOpEntry.Value, Rating = rating }).Reverse().Take(100);

            XResultList.Items.Clear();
            foreach (var o in filteredOpEntries)
            {
                var button = new OperatorTypeButton(o.Op);
                button.Opacity = IsOperatorRelevantToProject(o.Op) ? 1 : 0.4;
                XResultList.Items.Add(button);
                button.Width = XResultList.ActualWidth;
            }

            XResultList.SelectedIndex = 0;
            SelectedMetaOp = null;
        }

        private static string GetCurrentProjectName()
        {
            string currentProjectName = null;
            var cgvOperator = GetCurrentCompositionOperator();
            var nameSpace = cgvOperator.Definition.Namespace;

            currentProjectName = GetProjectFromNamespace(nameSpace);
            return currentProjectName;
        }

        private static Operator GetCurrentCompositionOperator()
        {
            var CGV = App.Current.MainWindow.CompositionView.CompositionGraphView;
            var cgvOperator = CGV.CompositionOperator;
            return cgvOperator;
        }

        private static string GetProjectFromNamespace(string nameSpace)
        {
            if (nameSpace.StartsWith("projects."))
            {
                var components = nameSpace.Split('.');
                if (components.Count() > 1)
                {
                    return components[1];
                }
            }
            return null;
        }

        private bool IsOperatorRelevantToProject(MetaOperator operatorDefinition)
        {
            if (operatorDefinition.Namespace.StartsWith("lib."))
            {
                if (operatorDefinition.Namespace.Contains("._test"))
                    return false;

                return true;
            }

            if (operatorDefinition.Namespace.StartsWith("examples."))
                return true;

            var CGV = App.Current.MainWindow.CompositionView.CompositionGraphView;

            // Projects-operators in Home
            if (operatorDefinition.Namespace.StartsWith("projects.") &&
                operatorDefinition.Namespace.Split('.').Count() == 2 &&
                GetCurrentCompositionOperator().Definition.Name == "Home")
            {
                return true;
            }

            // Same project ?
            var projectNamespace = GetProjectFromNamespace(operatorDefinition.Namespace);
            if (projectNamespace != null && projectNamespace == GetCurrentProjectName())
                return true;

            // Current user
            // TODO:implement

            return false;
        }

        private double ComputeRelevancy(MetaOperator op, string query, string currentProjectName)
        {
            double relevancy = 1;

            if (op.Name.Equals(query, StringComparison.InvariantCultureIgnoreCase))
            {
                relevancy *= 5;
            }

            if (op.Name.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
            {
                relevancy *= 4.5;
            }
            else
            {
                if (op.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    relevancy *= 3;
                }
            }

            // Bump up if query occurs in description
            if (query.Length > 2)
            {
                if (op.Description.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    relevancy *= 1.2;
                }
            }

            if (op.Name == "Time")      // disfavor shadow ops
                relevancy *= 0.3;

            if (op.Name == "Curve")      // disfavor shadow ops
                relevancy *= 0.05;

            if (op.InstanceCount > 0)
            {
                relevancy *= -0.5 / op.InstanceCount + 2.0;
            }

            relevancy *= 2 - 1.0 / (0.3 * _numberOfMetaOperatorUsage[op.ID] + 0.7);

            //relevancy *= (1 + (1.0 / (op.Name.Length + op.Namespace.Length)) * 0.05);

            // boost operators that match currently selected operator
            if (SelectedType != FunctionType.Generic
                && op.Inputs.Count > 0
                && op.Inputs.First().OpPart.Type == this.SelectedType)
            {
                relevancy *= 4;
            }

            if (Regex.Match(op.Namespace, @"^lib\..*", RegexOptions.IgnoreCase) != Match.Empty)
            {
                relevancy *= 1.6;
            }

            if (IsCompositionOperatorInNamespaceOf(op))
            {
                relevancy *= 1.9;
            }
            else if (!IsCompositionOperatorAProjectOperator && op.Namespace.StartsWith(@"projects.") && op.Namespace.Split('.').Length == 2)
            {
                relevancy *= 1.9;
            }

            // Bump up operators from same namespace as current project
            var projectName = GetProjectFromNamespace(op.Namespace);
            if (projectName != null && projectName == currentProjectName)
                relevancy *= 5;

            return relevancy;
        }

        private bool IsCompositionOperatorInNamespaceOf(MetaOperator op)
        {
            return op.Namespace.StartsWith(_compositionOperator.Definition.Namespace);
        }

        private void InitDictionaryWithMetaOperatorUsageCount()
        {
            foreach (var metaOp in App.Current.Model.MetaOpManager.MetaOperators)
            {
                _numberOfMetaOperatorUsage.Add(metaOp.Key, 1);
            }
            CountUsageOfMetaOps();
        }

        private void CountUsageOfMetaOps()
        {
            foreach (var metaOp in App.Current.Model.MetaOpManager.MetaOperators)
            {
                foreach (var internalIds in metaOp.Value.InternalOperatorsMetaOpId)
                {
                    if (_numberOfMetaOperatorUsage.ContainsKey(internalIds))
                    {
                        _numberOfMetaOperatorUsage[internalIds]++;
                    }
                }
            }
        }

        private bool _showingPreview = false;
        private List<ISelectable> _selectionBeforePreview;
        private MetaOperator _exampleMetaOp;

        public void ShowOpDescription(MetaOperator metaOperator)
        {
            if (metaOperator == null)
            {
                XName.Text = "";
                XNamespace.Text = "";
                XDescription.Text = "";
                XShowExampleButton.Visibility = Visibility.Hidden;
                return;
            }

            _exampleMetaOp = Utils.OpUtils.FindExampleOperator(metaOperator);
            XShowExampleButton.Visibility = _exampleMetaOp != null
                                          ? Visibility.Visible : Visibility.Hidden;

            XName.Text = metaOperator.Name;
            XNamespace.Text = metaOperator.Namespace;
            XDescription.Text = metaOperator.Description;

            // Show preview
            if (XPreviewCheckbox.IsChecked.Value)
            {
                _showingPreview = true;
                App.Current.MainWindow.XParameterView.PreventUIUpdate = true;
                var compoGraphView = App.Current.MainWindow.CompositionView.CompositionGraphView;
                _selectionBeforePreview = compoGraphView.SelectedElements;
                compoGraphView.AddOperatorAtPosition(metaOperator, RelavantPositionOnWorkspace);
            }
        }

        public void EndShowOpPreview(MetaOperator metaOperator)
        {
            // Undo preview
            if (_showingPreview)
            {
                App.Current.MainWindow.XParameterView.PreventUIUpdate = true;
                App.Current.UndoRedoStack.Undo();

                var compoGraphView = App.Current.MainWindow.CompositionView.CompositionGraphView;
                compoGraphView.SelectedElements = _selectionBeforePreview;
                App.Current.MainWindow.XParameterView.PreventUIUpdate = false;
                _showingPreview = false;
            }
        }

        public void AddOperatorToWorkspace(MetaOperator metaOperator)
        {
            var compoGraphView = App.Current.MainWindow.CompositionView.CompositionGraphView;

            if (_showingPreview)
            {
                // we don't have to do anything, but prevent the preview comment to be undone
                //_showingPreview = false;
            }
            else
            {
                compoGraphView.AddOperatorAtPosition(metaOperator, RelavantPositionOnWorkspace);
            }

            // Don't close window when ALT is pressed
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                _showingPreview = false;
                return;
            }

            // Make sure the Parameter view gets refreshed
            App.Current.MainWindow.XParameterView.PreventUIUpdate = false;
            if (_showingPreview)
            {
                _showingPreview = false;
                var newElement = compoGraphView.SelectionHandler.SelectedElements.ToList();
                compoGraphView.SelectionHandler.Clear();    // To trigger an update we have to clear the list first
                compoGraphView.SelectionHandler.SetElements(newElement);
            }

            Close();
            compoGraphView.Focus();
        }

        private void OnKeyUpHandler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (XResultList.SelectedIndex > 0)
                        XResultList.SelectedIndex--;
                    e.Handled = true;
                    SelectedMetaOp = null;
                    break;

                case Key.Down:
                    if (XResultList.SelectedIndex < XResultList.Items.Count)
                        XResultList.SelectedIndex++;
                    e.Handled = true;
                    SelectedMetaOp = null;
                    break;

                case Key.Return:
                    {
                        var button = XResultList.SelectedItem as OperatorTypeButton;
                        if (button != null)
                        {
                            SelectedMetaOp = button.MetaOp;
                        }
                        e.Handled = true;
                        Close();
                        break;
                    }
                case Key.Escape:
                    e.Handled = true;
                    Close();
                    break;
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EndShowOpPreview(null);

            if (sender is ListView listView)
            {
                listView.ScrollIntoView(listView.SelectedItem);
            }

            if (XResultList.SelectedItem is OperatorTypeButton button)
            {
                var selectedMetaOp = button.MetaOp;
                ShowOpDescription(button.MetaOp);
            }
        }

        private void XShowExampleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_exampleMetaOp == null)
            {
                Logger.Info("Boooh No example found");
                return;
            }

            SelectedMetaOp = _exampleMetaOp;
            Close();
        }
    }
}