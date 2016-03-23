// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Tooll.Components.SearchForOpWindow.DragManagers;
using Framefield.Tooll.Components.SearchForOpWindow.ResultFinders;
using ICommand = Framefield.Core.ICommand;

namespace Framefield.Tooll.Components.SearchForOpWindow
{
    using OpPartUsage = OpPartViewModel.OpPartUsage;

    public partial class ReplaceOperatorWindow : Window
    {
        private Operator _compositionOperator;
        private MetaOperator _metaOperatorToBeReplacedWith;
        
        private readonly ObservableCollection<ReplaceOperatorViewModel> _results;
        private int _oldOperatorIndex = -1;
        private Operator OldOperator { get { return _oldOperatorIndex >= 0 && _oldOperatorIndex < _results.Count ? _results[_oldOperatorIndex].Operator : null; } }
        private readonly ObservableCollection<AutoCompleteEntry> _possibleSearchResults;
        private readonly ObservableCollection<AutoCompleteEntry> _possibleReplaceResults;

        private readonly ObservableCollection<OpPartViewModel> _oldInputs;
        private readonly ObservableCollection<OpPartViewModel> _oldOutputs;
        private readonly ObservableCollection<OpPartViewModel> _newInputs;
        private readonly ObservableCollection<OpPartViewModel> _newOutputs;
        private readonly ObservableCollection<OpPartViewModel> _unassignedInputs;
        private readonly ObservableCollection<OpPartViewModel> _unassignedOutputs;
 
        private DragManager _dragManager;
        private readonly InputDragManager _inputDragManager;
        private readonly OutputDragManager _outputDragManager;

        private List<ICommand> _commandList  = new List<ICommand>(); 

        private OpPartViewModel EmptyOpPart { get { return new OpPartViewModel(new OperatorPart(Guid.Empty, new Utilities.DefaultValueFunction())); } }

        private enum SearchOption {Home = 0, Current = 1, Meta = 2, Path = 3}
        private SearchOption CheckedOption
        {
            get
            {
                var checkedRadioButton = XsearchOptionGrid.Children.OfType<RadioButton>().FirstOrDefault(radioButton => radioButton.IsChecked == true);
                if (checkedRadioButton == XCurrentButton)
                    return SearchOption.Current;
                if (checkedRadioButton == XEveryWhereButton)
                    return SearchOption.Meta;
                if (checkedRadioButton == XHomeButton)
                    return SearchOption.Home;
                
                return SearchOption.Path;
            }
        }

        private ResultFinder actualFinder
        {
            get
            {
                switch (CheckedOption)
                {
                        case SearchOption.Current:
                            return new CurrentFinder(this);
                        case SearchOption.Home: 
                            return new HomeFinder(this);
                        case SearchOption.Meta:
                            return new MetaFinder(this);
                        default:
                            return new PathFinder(this, _compositionOperator);
                }
            }
        }

        public ObservableCollection<ReplaceOperatorViewModel> Results { get { return _results; } }
        public ObservableCollection<AutoCompleteEntry> PossibleSearchResults { get { return _possibleSearchResults; } }
        public ObservableCollection<AutoCompleteEntry> PossibleReplaceResults { get { return _possibleReplaceResults; } }

        public ObservableCollection<OpPartViewModel> OldInputs { get { return _oldInputs; } }
        public ObservableCollection<OpPartViewModel> OldOutputs { get { return _oldOutputs; } }
        public ObservableCollection<OpPartViewModel> NewInputs { get { return _newInputs; } }
        public ObservableCollection<OpPartViewModel> NewOutputs { get { return _newOutputs; } }
        public ObservableCollection<OpPartViewModel> UnassignedInputs { get { return _unassignedInputs; } } 
        public ObservableCollection<OpPartViewModel> UnassignedOutputs { get { return _unassignedOutputs; } } 

        public ReplaceOperatorWindow()
        {
            _inputDragManager = new InputDragManager(this);
            _outputDragManager = new OutputDragManager(this);
            _results = new ObservableCollection<ReplaceOperatorViewModel>();
            _possibleSearchResults = new ObservableCollection<AutoCompleteEntry>();
            _possibleReplaceResults = new ObservableCollection<AutoCompleteEntry>();
            _oldInputs = new ObservableCollection<OpPartViewModel>();
            _oldOutputs = new ObservableCollection<OpPartViewModel>();
            _newInputs = new ObservableCollection<OpPartViewModel>();
            _newOutputs = new ObservableCollection<OpPartViewModel>();
            _unassignedInputs = new ObservableCollection<OpPartViewModel>();
            _unassignedOutputs = new ObservableCollection<OpPartViewModel>();
            _compositionOperator = App.Current.MainWindow.CompositionView.CompositionGraphView.CompositionOperator;

            InitializeComponent();

            XResultList.DataContext = Results;
            XReplacePopupList.DataContext = PossibleReplaceResults;
            XSearchPopupList.DataContext = PossibleSearchResults;
            XNewInputs.DataContext = NewInputs;
            XNewOutputs.DataContext = NewOutputs;
            XOldInputs.DataContext = OldInputs;
            XOldOutputs.DataContext = OldOutputs;
            XUnassignedInputs.DataContext = UnassignedInputs;
            XUnassignedOutputs.DataContext = UnassignedOutputs;
        }
        
        #region Listener

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePossibleResults(XSearchTextBox);
        }

        private void OnReplaceTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePossibleResults(XReplaceWithTextBox);
        }

        private void ResultOnSelctionChanged(object sender, SelectionChangedEventArgs e)
        {
            _oldInputs.Clear();
            _oldOutputs.Clear();
            if (_results.Any())
            {
                var selectedIndex = XResultList.SelectedIndex;
                _oldOperatorIndex = selectedIndex;
                if (OldOperator == null)
                    return;

                _compositionOperator = OldOperator.Parent ?? App.Current.Model.HomeOperator;
                FillOldOpParts();
                if (e.RemovedItems.Count > 0 && e.RemovedItems[0] != null)
                {
                    var selectedBefore = e.RemovedItems[0] as ReplaceOperatorViewModel;
                    if (selectedBefore.Operator.Definition != OldOperator.Definition)
                        FillAssignments();
                }
                Utils.JumpTo(OldOperator);
                XOldInputLabel.Content = "Inputs of " + _results[_oldOperatorIndex].NamespaceAndName;
            }
        }

        private void OnSearchKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down:
                    ChangeSelectedIndex(XSearchPopupList, 1);
                    break;
                case Key.Up:
                    ChangeSelectedIndex(XSearchPopupList, -1); 
                    break;
                case Key.Return :
                    UpdateSearchResults();
                    break;
            }
        }

        private void OnReplaceTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Down :
                    ChangeSelectedIndex(XReplacePopupList, 1);
                    break;
                case Key.Up :
                    ChangeSelectedIndex(XReplacePopupList, -1);
                    break;
                case Key.Return :
                    FillAssignments();
                    Keyboard.Focus(XOldInputs);
                    break;
            }
        }

        private void InputsOnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var listToSynchronize = sender == XNewInputs ? XOldInputs : XNewInputs;
            SynchronizeScrollViewOf(listToSynchronize, e);
        }

        private void OutputsOnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var listToSynchronize = sender == XNewOutputs ? XOldOutputs : XNewOutputs;
            SynchronizeScrollViewOf(listToSynchronize, e);
        }

        private void NewOpPartItemOnRightClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListViewItem;
            var viewModel = item.DataContext as OpPartViewModel;
            if (viewModel.OpPart.ID == Guid.Empty)
                return;

            var index = viewModel.Usage == OpPartUsage.Input ? XNewInputs.Items.IndexOf(viewModel) : XNewOutputs.Items.IndexOf(viewModel);
            if (viewModel.Usage == OpPartUsage.Input)
            {
                _unassignedInputs.Add(_newInputs[index]);
                _newInputs[index] = EmptyOpPart;
            }
            else
            {
                _unassignedOutputs.Add(_newOutputs[index]);
                _newOutputs[index] = EmptyOpPart;
            }
        }

        private void ReplacePopupOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = e.AddedItems.Count > 0 ? e.AddedItems[0] as AutoCompleteEntry : null;
            if (selectedItem != null)
            {
                _metaOperatorToBeReplacedWith = selectedItem.MetaOperator;
            }
        }

        private void ReplacePopupOnClick(object sender, MouseButtonEventArgs e)
        {
            FillAssignments();
        }

        private void SearchPopupOnClick(object sender, MouseButtonEventArgs e)
        {
            UpdateSearchResults();
        }

        private void PathOptionChecked(object sender, RoutedEventArgs e)
        {
            switch (CheckedOption)
            {
                case SearchOption.Home:
                    XPathTextBox.Text = @"~/";
                    break;
                case SearchOption.Current:
                    XPathTextBox.Text = @"~/" + Utils.GetOpPath(_compositionOperator) + _compositionOperator.Definition.Name;
                    break;
            }
        }

        private void ReplaceButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (_oldOperatorIndex >= _results.Count || _oldOperatorIndex < 0 || _metaOperatorToBeReplacedWith == null)
                return;

            _commandList.Clear();
            ReplaceOperatorAndUpdateResultModelAt(_oldOperatorIndex);
            App.Current.UndoRedoStack.Add(new MacroCommand("Replace Operator Command", _commandList));

            ChangeSelectedIndex(XResultList, 1);
        }

        private void ReplaceAllButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (_metaOperatorToBeReplacedWith == null)
                return;

            _commandList.Clear();
            for (var i = 0; i < _results.Count; i++)
            {
                ReplaceOperatorAndUpdateResultModelAt(i);
            }
            if (_commandList.Any())
                App.Current.UndoRedoStack.Add(new MacroCommand("Replace All Ops", _commandList));
        }

        private void CancelOnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region D'n'D stuff

        private void InputOnStartDrag(object sender, MouseButtonEventArgs e)
        {
            _dragManager = _inputDragManager;
            _dragManager.StartDragging(sender, e);
        }

        private void OutputOnStartDrag(object sender, MouseButtonEventArgs e)
        {
            _dragManager = _outputDragManager;
            _dragManager.StartDragging(sender, e);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragManager == null)
                return;
            
            _dragManager.Dragging(sender, e);
        }

        private void NewOpPartItemOnDragEnter(object sender, DragEventArgs e)
        {
            _dragManager.DragEnter(sender, e);
        }

        private void NewOpPartItemOnDragLeave(object sender, DragEventArgs e)
        {
            var item = sender as ListViewItem;
            item.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void NewOpPartItemOnDrop(object sender, DragEventArgs e)
        {
            _dragManager.Drop(sender, e);
            _dragManager = null;
        }

        #endregion

        #region private methods

        private void UpdateSearchResults()
        {
            _results.Clear();
            actualFinder.FindResults();
            if (_results.Any())
            {
                XResultList.SelectedIndex = 0;
                _possibleSearchResults.Clear();
            }
            if (_metaOperatorToBeReplacedWith != null)
                AssignMatchingNewOpParts();
        }

        private void UpdatePossibleResults(TextBox textBox)
        {
            var resultList = textBox == XSearchTextBox ? _possibleSearchResults : _possibleReplaceResults;
            resultList.Clear();
            foreach (var metaOp in MetaManager.Instance.MetaOperators.Values)
            {
                if(Utils.IsSearchTextMatchingToMetaOp(metaOp, textBox.Text))
                    resultList.Add(new AutoCompleteEntry(metaOp));
            }
            
        }

        private void FillOldOpParts()
        {
            foreach (var input in OldOperator.Inputs)
            {
                var isConnected = OldOperator.Parent.Connections.Any(connection => connection.TargetOpPart == input);
                _oldInputs.Add(new OpPartViewModel(input) { IsConnected = isConnected });
            }
            foreach (var output in OldOperator.Outputs)
            {
                var isConnected = OldOperator.Parent.Connections.Any(connection => connection.SourceOpPart == output);
                _oldOutputs.Add(new OpPartViewModel(output) { IsConnected = isConnected });
            }
        }

        private void AssignMatchingNewOpParts()
        {
            AssignMatchingNewInputs();
            AssignMatchingNewOutputs();
        }

        private void AssignMatchingNewInputs()
        {
            _newInputs.Clear();
            foreach (var oldInputModel in _oldInputs)
            {
                var matchingUnassigned = _unassignedInputs.FirstOrDefault(unassigned => unassigned.Name == oldInputModel.Name && unassigned.OpPart.Type == oldInputModel.OpPart.Type);
                if (matchingUnassigned != null)
                {
                    _unassignedInputs.Remove(matchingUnassigned);
                    _newInputs.Add(matchingUnassigned);
                }
                else
                {
                    _newInputs.Add(EmptyOpPart);
                }
            }
        }

        private void AssignMatchingNewOutputs()
        {
            _newOutputs.Clear();
            foreach (var oldOutputModel in _oldOutputs)
            {
                var matchingUnassigned = _unassignedOutputs.FirstOrDefault(unassigned => unassigned.Name == oldOutputModel.Name && unassigned.OpPart.Type == oldOutputModel.OpPart.Type);
                if (matchingUnassigned != null)
                {
                    _unassignedOutputs.Remove(matchingUnassigned);
                    _newOutputs.Add(matchingUnassigned);
                }
                else
                {
                    _newOutputs.Add(EmptyOpPart);
                }
            }
        }

        private void ChangeSelectedIndex(ListView listView, int change)
        {
            if (change > 0)
            {
                if (listView.SelectedIndex < listView.Items.Count - 1)
                    listView.SelectedIndex += change;

                else
                    listView.SelectedIndex = change - 1;
            }
            else if (change < 0)
            {
                if (listView.SelectedIndex <= 0)
                    listView.SelectedIndex = listView.Items.Count + change;

                else
                    listView.SelectedIndex += change;
            }
        }

        private void FillAssignments()
        {
            if (_metaOperatorToBeReplacedWith == null)
                return;

            _unassignedInputs.Clear();
            foreach (var metaInput in _metaOperatorToBeReplacedWith.Inputs)
            {
                _unassignedInputs.Add(new OpPartViewModel(metaInput.CreateInstance()) { Usage = OpPartUsage.Input });
            }
            _unassignedOutputs.Clear();
            foreach (var metaOutput in _metaOperatorToBeReplacedWith.Outputs)
            {
                _unassignedOutputs.Add(new OpPartViewModel(metaOutput.CreateInstance()) { Usage = OpPartUsage.Output });
            }
            AssignMatchingNewOpParts();
            XReplaceWithTextBox.Text = _metaOperatorToBeReplacedWith.Namespace + _metaOperatorToBeReplacedWith.Name;
            _possibleReplaceResults.Clear();
        }

        private void SynchronizeScrollViewOf(ListView listToSynchronize, ScrollChangedEventArgs e)
        {
            var border = VisualTreeHelper.GetChild(listToSynchronize, 0) as Decorator;
            if (border != null)
            {
                var scrollViewer = border.Child as ScrollViewer;
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
                }
            }
        }

        private void ReplaceOperatorAndUpdateResultModelAt(int index)
        {
            if (_results[index].IsReplaced)
                return;
            
            var oldOp = _results[index].Operator;
            var parent = oldOp.Parent;
            var newID = Guid.NewGuid();
            var command = new ReplaceOperatorCommand(parent.Definition, oldOp.Definition, _metaOperatorToBeReplacedWith, oldOp.ID, newID)
            {
                NewInputs = _newInputs.Select(input => input.OpPart.ID).ToList(),
                NewOutputs = _newOutputs.Select(output => output.OpPart.ID).ToList()
            };
            command.Do();

            var newOp = parent.InternalOps.Find(internalOp => internalOp.ID == newID);
            if (newOp != null)
            {
                UpdateResultsWhereOpIsSameInstanceLikeReplacedOp(oldOp, parent, newOp);
            }
            _commandList.Add(command);
        }

        private void UpdateResultsWhereOpIsSameInstanceLikeReplacedOp(Operator oldOp, Operator parent, Operator newOp)
        {
            for (var i = 0; i < _results.Count; i++)
            {
                var op = _results[i].Operator;
                if (op.ID == oldOp.ID && op.Parent.Definition.ID == parent.Definition.ID)
                    UpdateReplacedResultViewModelFor(newOp, i);
            }
        }

        private void UpdateReplacedResultViewModelFor(Operator newOp, int replacedIndex)
        {
            var replacedModel = _results[replacedIndex];
            _results[replacedIndex] = new ReplaceOperatorViewModel(newOp)
            {
                Name = replacedModel.Name,
                Namespace = replacedModel.Namespace,
                InstanceName = replacedModel.InstanceName,
                IsReplaced = true
            };
        }

        #endregion
    }
}
