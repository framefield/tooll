// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AvalonDock;
using Framefield.Core.Commands;
using ICSharpCode;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using System.Diagnostics;

using Framefield.Tooll.Utils;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for OperatorParameterView.xaml
    ///
    /// Understanding the nesting structure:
    ///
    /// OperatorParameterView
    ///   .XParameterStackPanel.Children= List of...
    ///     OperatorParameterViewRow
    ///       .XInputControls.Children = GroupInputControl
    ///       .XParameterValue.Children= List of...
    ///         - FloatParameterControl : FloatEditButton
    ///         - RGBAParameterControl
    ///            .XGrid.Children = List of...
    ///             - FloatParameterControl
    ///         - VectorNParameterValue
    /// </summary>
    public partial class OperatorParameterView : UserControl
    {
        private static List<FloatParameterControl> _floatParameterControlPool = new List<FloatParameterControl>();

        private StackPanel XParameterStackPanel;
        private UpdateOperatorPropertiesCommand _updateOperatorPropertiesCommand;
        private UpdateOperatorPropertiesCommand.Entry _opStateEntry;

        public OperatorParameterView(Operator op)
        {
            var watch = new Stopwatch();
            watch.Start();

            InitializeComponent();
            _operator = op;

            _exampleMetaOp = OpUtils.FindExampleOperator(_operator.Definition);
            XExampleButton.IsEnabled = _exampleMetaOp != null;
            XExampleButton.Foreground = _exampleMetaOp != null ? Brushes.White : Brushes.Black;

            var binding = new Binding("Namespace");
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = op.Definition;
            binding.Path = new PropertyPath("Namespace");
            NamespaceTextBox.SetBinding(TextBox.TextProperty, binding);

            binding = new Binding("Type");
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = op.Definition;
            binding.Path = new PropertyPath("Name");
            TypeTextBox.SetBinding(TextBox.TextProperty, binding);

            binding = new Binding("OperatorName") { Mode = BindingMode.OneWay };
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = op;
            binding.Path = new PropertyPath("Name");
            XNameTextBox.XTextEdit.SetBinding(TextBox.TextProperty, binding);
            XNameTextBox.EditingStartedEvent += () =>
                                                {
                                                    _opStateEntry = new UpdateOperatorPropertiesCommand.Entry(op);
                                                    _updateOperatorPropertiesCommand = new UpdateOperatorPropertiesCommand(op, _opStateEntry);
                                                };
            XNameTextBox.EditingUpdatedEvent += (o, ev) =>
                                                {
                                                    _updateOperatorPropertiesCommand.ChangeEntries[0].Name = XNameTextBox.textEditButton.XTextEdit.Text;
                                                    _updateOperatorPropertiesCommand.Do();
                                                };
            XNameTextBox.EditingCompletedEvent += () =>
                                                  {
                                                      App.Current.UndoRedoStack.Add(_updateOperatorPropertiesCommand);
                                                      _updateOperatorPropertiesCommand = new UpdateOperatorPropertiesCommand(op, _opStateEntry);
                                                  };
            XParameterStackPanel = new StackPanel();
            XParameterStackPanel.Children.Clear();

            // find groups
            var inputGroups = from input in op.Inputs
                              group input by input.Name.Split(new[] { '.' })[0] into g
                              select new { Name = g.Key, Inputs = g.ToArray() };

            foreach (var group in inputGroups)
            {
                var singleParameterRow = new OperatorParameterViewRow(group.Inputs.ToList());
                singleParameterRow.XParameterNameButton.Content = group.Name;

                singleParameterRow.XInputControls.Children.Add(new GroupInputControl(group.Inputs.ToList())); // This is slooww (half of operator selection time)

                // Single Parameter
                if (group.Inputs.Length == 1)
                {
                    XParameterStackPanel.Children.Add(singleParameterRow);

                    var input = group.Inputs[0];
                    if (input.Type == FunctionType.Float)
                    {
                        if (!input.IsMultiInput)
                        {
                            var metaInput = input.Parent.GetMetaInput(input);
                            var enumValues = metaInput.EnumValues;

                            // ENUM - Parameter
                            if (enumValues.Count > 0)
                            {
                                var newEnumControl = new EnumParameterValue(input);
                                singleParameterRow.ResetToDefaultEvent += newEnumControl.ResetToDefaultHandler;
                                singleParameterRow.XParameterValue.Children.Add(newEnumControl);
                            }

                            // Single Float Parameter
                            else
                            {
                                FloatParameterControl newFloatControl = new FloatParameterControl(input);
                                singleParameterRow.XParameterValue.Children.Add(newFloatControl);
                                singleParameterRow.ResetToDefaultEvent += newFloatControl.ParameterRow_ResetSingleParameterHandler;
                                singleParameterRow.StartManipulationEvent += newFloatControl.ParameterRow_StartManipulationHandler;
                                singleParameterRow.UpdateManipulationEvent += newFloatControl.ParameterRow_UpdateManipulaitonHandler;
                                singleParameterRow.EndManipulationEvent += newFloatControl.ParameterRow_EndManipulationHandler;
                            }
                        }
                    }
                    else if (input.Type == FunctionType.Text)
                    {
                        var paramEdit = new TextParameterValue(input);
                        if (input.Name.EndsWith("Text"))
                        {
                            singleParameterRow.XAdditionalContent.Child = paramEdit;
                            singleParameterRow.ResetToDefaultEvent += paramEdit.ResetToDefaultHandler;
                        }
                        else
                        {
                            singleParameterRow.XParameterValue.Children.Add(paramEdit);
                        }
                    }
                    else if (input.Type == FunctionType.Scene)
                    {
                        var paramEdit = new SceneParameterValue(input);
                        singleParameterRow.XParameterValue.Children.Add(paramEdit);
                    }
                    else if (input.Type == FunctionType.Image)
                    {
                        var paramEdit = new ImageParameterValue(input);
                        singleParameterRow.XParameterValue.Children.Add(paramEdit);
                    }
                    else if (input.Type == FunctionType.Dynamic)
                    {
                        var paramEdit = new DynamicParameterValue(input);
                        singleParameterRow.XParameterValue.Children.Add(paramEdit);
                    }
                    else if (input.Type == FunctionType.Generic)
                    {
                        var paramEdit = new GenericParameterValue(input);
                        singleParameterRow.XParameterValue.Children.Add(paramEdit);
                    }
                    else if (input.Type == FunctionType.Mesh)
                    {
                        var paramEdit = new MeshParameterValue(input);
                        singleParameterRow.XParameterValue.Children.Add(paramEdit);
                    }
                }

                // Float Parameter Groups
                else
                {
                    if (group.Inputs.Length == 4
                        && group.Inputs[0].Name.EndsWith(".R")
                        && group.Inputs[1].Name.EndsWith(".G")
                        && group.Inputs[2].Name.EndsWith(".B")
                        && group.Inputs[3].Name.EndsWith(".A"))
                    {
                        var rgbaControl = new RgbaParameterValue(group.Inputs);
                        singleParameterRow.ResetToDefaultEvent += rgbaControl.ResetToDefaultHandler;
                        singleParameterRow.StartManipulationEvent += rgbaControl.StartManipulationHandler;
                        singleParameterRow.UpdateManipulationEvent += rgbaControl.UpdateManipulationHandler;
                        singleParameterRow.EndManipulationEvent += rgbaControl.EndManipulationHandler;
                        singleParameterRow.XParameterValue.Children.Add(rgbaControl);
                    }
                    else
                    {
                        var vectorControl = new VectorNParameterValue(group.Inputs);

                        singleParameterRow.StartManipulationEvent += vectorControl.StartManipulationHandler;
                        singleParameterRow.UpdateManipulationEvent += vectorControl.UpdateManipulationHandler;
                        singleParameterRow.EndManipulationEvent += vectorControl.EndManipulationHandler;
                        singleParameterRow.ResetToDefaultEvent += vectorControl.ResetToDefaultHandler;
                        singleParameterRow.XParameterValue.Children.Add(vectorControl);
                    }

                    var parameterGroupComponent = new ParameterGroup(group.Inputs);
                    parameterGroupComponent.XParameterGroupPanel.Children.Add(singleParameterRow);
                    XParameterStackPanel.Children.Add(parameterGroupComponent);
                }
            }

            var descriptionBox = new ICSharpCode.AvalonEdit.Editing.TextArea();
            descriptionBox.ToolTip = "Operator description. Click to edit";
            descriptionBox.Foreground = Brushes.Gray;
            descriptionBox.Margin = new Thickness(4, 6, 4, 4);
            descriptionBox.MinHeight = 40;
            descriptionBox.FontSize = 13;

            _descriptionDoc = new TextDocument(op.Definition.Description);
            _descriptionDoc.TextChanged += HandleDescriptionChange;
            descriptionBox.Document = _descriptionDoc;
            XParameterStackPanel.Children.Add(descriptionBox);

            XParameterScrollViewer.Content = XParameterStackPanel;
            App.Current.OperatorPresetManager.FindAndShowPresetsForSelectedOp();

            watch.Stop();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("OperatorParameterView.Unload()");
            foreach (var opPart in _operator.Inputs)
            {
                opPart.ManipulatedEvent += opPart_ModifiedEventHandler;
            }
            XNameTextBox.DropFocusAfterEdit = true;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach (var opPart in _operator.Inputs)
            {
                opPart.ManipulatedEvent -= opPart_ModifiedEventHandler;
            }
        }

        private void opPart_ModifiedEventHandler(object sender, EventArgs e)
        {
            UpdateParameterViewRowHighlights();
        }

        private void HandleDescriptionChange(object sender, EventArgs e)
        {
            _operator.Definition.Description = _descriptionDoc.Text;
        }

        public void UpdateParameterViewRowHighlights()
        {
            foreach (var o in XParameterStackPanel.Children)
            {
                if (o is OperatorParameterViewRow row)
                {
                    row.UpdateAnimationFocusHighlight();
                }

                if (o is ParameterGroup group)
                {
                    foreach (var subO in group.XParameterGroupPanel.Children)
                    {
                        var subRow = subO as OperatorParameterViewRow;
                        if (subRow != null)
                        {
                            subRow.UpdateAnimationFocusHighlight();
                        }
                    }
                    foreach (var subO in group.XParameterRowsPanel.Children)
                    {
                        var subRow = subO as OperatorParameterViewRow;
                        if (subRow != null)
                        {
                            subRow.UpdateAnimationFocusHighlight();
                        }
                    }
                }
            }
            CustomCommands.FitCurveValueRangeCommand.Execute(null, this);
        }

        #region XAML events

        private void TypeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.XLibraryView.UpdateOperatorTree();
        }

        private void NamespaceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.XLibraryView.UpdateOperatorTree();
        }

        #endregion XAML events

        private void Output_Clicked(object sender, RoutedEventArgs e)
        {
            var CGV = App.Current.MainWindow.CompositionView.CompositionGraphView;
            CGV.SelectConnectedOutputWidget();
        }

        private TextDocument _descriptionDoc { get; set; }
        private Operator _operator { get; set; }

        private void ShowExample_Clicked(object sender, RoutedEventArgs e)
        {
            if (_exampleMetaOp == null)
            {
                Logger.Info("Boooh No example found");
                return;
            }

            var CGV = App.Current.MainWindow.CompositionView.CompositionGraphView;
            CGV.AddOperatorAtPosition(_exampleMetaOp, _operator.Position + new Vector(100, 100));
        }

        private MetaOperator _exampleMetaOp;
    }
}