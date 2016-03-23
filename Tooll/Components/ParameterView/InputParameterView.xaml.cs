// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using Framefield.Core.Commands;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for ParameterView.xaml
    /// </summary>
    public partial class InputParameterView : UserControl
    {
        public InputParameterView(Operator op, OperatorPart opPart)
        {
            InitializeComponent();

            _operator = op;
            _operatorPart = opPart;

            _metaInput = _operator.GetMetaInput(_operatorPart);

            var command = new UpdateInputParameterCommand(_operator, _metaInput.ID, new UpdateInputParameterCommand.Entry(_metaInput));

            NameTextBox.Text = _metaInput.Name;
            TypeComboBox.SelectedIndex = (int)_metaInput.OpPart.Type;
            IsMultiInputComboBox.SelectedIndex = _metaInput.IsMultiInput ? 1 : 0;
            _defaultValue = _metaInput.DefaultValue.Clone();
            RelevanceComboBox.SelectedIndex = (int)_metaInput.Relevance;
            RelevanceComboBox.SelectionChanged += (o, e) => { _metaInput.Relevance = (MetaInput.RelevanceType)RelevanceComboBox.SelectedIndex; };
            RelevanceComboBox.SelectionChanged += (o, e) =>
                                                  {
                                                      var entry = new UpdateInputParameterCommand.Entry(_metaInput) { Relevance = (MetaInput.RelevanceType)RelevanceComboBox.SelectedIndex };
                                                      command = new UpdateInputParameterCommand(_operator, _metaInput.ID, entry);
                                                      App.Current.UndoRedoStack.AddAndExecute(command);
                                                  };
            SetDefaultValue();

            NameTextBox.TextChanged += NameTextBox_TextChanged;
            TypeComboBox.SelectionChanged += TypeComboBox_SelectionChanged;
            IsMultiInputComboBox.SelectionChanged += IsMultiInputComboBox_SelectionChanged;

            bool showFloatParams = (opPart.Type == FunctionType.Float);
            if (showFloatParams)
            {
                Min.Value = _metaInput.Min;
                Min.ClampToMin = false;
                Min.ClampToMax = false;
                Min.ValueChangedEvent += (newVal) => command = new UpdateInputParameterCommand(_operator, _metaInput.ID, new UpdateInputParameterCommand.Entry(_metaInput) { Min = newVal });
                Min.EditingEndedEvent += () => App.Current.UndoRedoStack.AddAndExecute(command);

                Max.Value = _metaInput.Max;
                Max.ClampToMin = false;
                Max.ClampToMax = false;
                Max.ValueChangedEvent += (newVal) => command = new UpdateInputParameterCommand(_operator, _metaInput.ID, new UpdateInputParameterCommand.Entry(_metaInput) { Max = newVal });
                Max.EditingEndedEvent += () => App.Current.UndoRedoStack.AddAndExecute(command);

                Scale.Value = _metaInput.Scale;
                Scale.ClampToMin = false;
                Scale.ClampToMax = false;
                Scale.Min = 0.001f;
                Scale.Scale = 0.01f; ;
                Scale.ValueChangedEvent += (newVal) => command = new UpdateInputParameterCommand(_operator, _metaInput.ID, new UpdateInputParameterCommand.Entry(_metaInput) { Scale = newVal });
                Scale.EditingEndedEvent += () => App.Current.UndoRedoStack.AddAndExecute(command);

                ScaleTypeComboBox.SelectedIndex = (int)_metaInput.ScaleType;
                ScaleTypeComboBox.SelectionChanged += (o, e) =>
                                                      {
                                                          command = new UpdateInputParameterCommand(_operator, _metaInput.ID, new UpdateInputParameterCommand.Entry(_metaInput) { ScaleType = (MetaInput.Scaling)ScaleTypeComboBox.SelectedIndex });
                                                          App.Current.UndoRedoStack.AddAndExecute(command);
                                                      };
                SetVisibilityForFloatParams(Visibility.Visible);

                XIsEnumComboBox.SelectedIndex = _metaInput.EnumValues.Count > 0 ? 1 : 0;
                XIsEnumComboBox.SelectionChanged += XIsEnumComboBox_SelectionChanged;
                BuildEnumEntries();
            }
            else
                SetVisibilityForFloatParams(Visibility.Hidden);

            DescriptionDoc = new TextDocument(_metaInput.Description);
            DescriptionDoc.TextChanged += HandleDescriptionChange;
            DescriptionEdit.Document = DescriptionDoc;
            DescriptionEdit.Foreground = Brushes.Gray;
            DescriptionEdit.Margin = new Thickness(4, 6, 4, 4);
            DescriptionEdit.MinHeight = 40;
            DescriptionEdit.FontSize = 13;
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMetaInput();
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetDefaultValue();

            int idx = _operator.Inputs.IndexOf(_operatorPart);
            var removeCommand = new RemoveInputCommand(_operator, _operatorPart);

            _metaInput.OpPart = BasicMetaTypes.GetMetaOperatorPartOf((FunctionType) TypeComboBox.SelectedIndex);

            // set default value
            switch ((FunctionType)TypeComboBox.SelectedIndex)
            {
                case FunctionType.Float:
                    _defaultValue = new Float(0.0f);
                    break;
                case FunctionType.Text:
                    _defaultValue = new Text(String.Empty);
                    break;
                case FunctionType.Scene:
                    _defaultValue = new Core.Scene();
                    break;
                case FunctionType.Generic:
                    _defaultValue = new Generic();
                    break;
                case FunctionType.Dynamic:
                    _defaultValue = new Dynamic();
                    break;
                case FunctionType.Mesh:
                    _defaultValue = new Core.MeshValue();
                    break;
                default:
                    Logger.Error("InputParameterView.TypeComboBox_SelectionChanged: Unknown 'FunctionType' with value {0}", TypeComboBox.SelectedIndex);
                    break;
            }
            _metaInput.DefaultValue = _defaultValue;
            var command = new AddInputCommand(_operator, _metaInput);

            ICommand[] commands = { removeCommand, command };

            App.Current.UndoRedoStack.AddAndExecute(new MacroCommand("Changed Input Type", commands));
        }

        private void IsMultiInputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMetaInput();
        }

        private void HandleDescriptionChange(object sender, EventArgs e)
        {
            _metaInput.Description = DescriptionDoc.Text;
        }

        private void XIsEnumComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var useAsEnum = comboBox.SelectedIndex == 1;
            if (useAsEnum)
            {
                _metaInput.EnumValues.Add(new MetaInput.EnumEntry("Value1", 0));
                _metaInput.EnumValues.Add(new MetaInput.EnumEntry("Value2", 0));

                BuildEnumEntries();
                Min.Value = 0;
                Max.Value = 1;
                Scale.Value = 1;
            }
            else
            {
                XEnumValuesStackPanel.Children.Clear();
                _metaInput.EnumValues.Clear();
            }
            XEnumValuesStackPanel.Visibility = useAsEnum ? Visibility.Visible : Visibility.Hidden;
        }

        private void BuildEnumEntries()
        {
            XEnumValuesStackPanel.Children.Clear();
            foreach (var enumEntry in _metaInput.EnumValues)
            {
                XEnumValuesStackPanel.Children.Add(CreateEnumRow(enumEntry));
            }
        }

        private EnumEntryDefinitionRow CreateEnumRow(MetaInput.EnumEntry enumEntry)
        {
            var rowEntry = new EnumEntryDefinitionRow(enumEntry.Name);
            rowEntry.XEnumValueEdit.Value = enumEntry.Value;

            rowEntry.XEnumEntryNameEdit.TextChanged += (o, e) =>
            {
                var args = e as TextChangedEventArgs;
                var entry = _metaInput.EnumValues.First(listEntry => { return listEntry.ID == enumEntry.ID; });
                entry.Name = rowEntry.XEnumEntryNameEdit.Text;
            };

            rowEntry.XEnumValueEdit.ValueChangedEvent += (newValue) =>
            {
                var entry = _metaInput.EnumValues.First(e => { return e.ID == enumEntry.ID; });
                entry.Value = (int)(newValue);
            };

            rowEntry.XAddButton.Click += (o, args) =>
            {
                _metaInput.EnumValues.Add(new MetaInput.EnumEntry("value", 0));
                BuildEnumEntries();
            };

            rowEntry.XRemoveButton.Click += (o, args) =>
            {
                _metaInput.EnumValues.Remove(enumEntry);
                BuildEnumEntries();
            };

            return rowEntry;
        }

        private void SetVisibilityForFloatParams(Visibility showFloatParams)
        {
            Min.Visibility = showFloatParams;
            MinLabel.Visibility = showFloatParams;
            MaxLabel.Visibility = showFloatParams;
            Max.Visibility = showFloatParams;
            ScaleLabel.Visibility = showFloatParams;
            Scale.Visibility = showFloatParams;
            ScaleTypeLabel.Visibility = showFloatParams;
            ScaleTypeComboBox.Visibility = showFloatParams;
            XIsEnumLabel.Visibility = showFloatParams;
            XIsEnumComboBox.Visibility = showFloatParams;
        }

        private void SetDefaultValue()
        {
            DefaultValue.Children.Clear();

            if (TypeComboBox.SelectedIndex == (int)FunctionType.Float)
            {
                float value = 0.0f;
                var currentFloat = _defaultValue as Float;
                if (currentFloat != null)
                    value = currentFloat.Val;
                _defaultValue = new Float(value);
                var edit = new FloatEditButton() { Value = value };
                edit.Scale = _metaInput.Scale;
                edit.Min = _metaInput.Min;
                edit.Max = _metaInput.Max;
                edit.Default = value;
                edit.ValueChangedEvent += (v) =>
                {
                    _defaultValue = new Float(v);
                    UpdateMetaInput();
                };
                DefaultValue.Children.Add(edit);
            }
            else if (TypeComboBox.SelectedIndex == (int)FunctionType.Text)
            {
                var value = String.Empty;
                var currentText = _defaultValue as Text;
                if (currentText != null)
                    value = currentText.Val;
                var edit = new TextBox();
                edit.Text = value;
                _defaultValue = new Text(value);
                edit.TextChanged += (o, e) =>
                {
                    _defaultValue = new Text(edit.Text);
                    UpdateMetaInput();
                };
                DefaultValue.Children.Add(edit);
            }
            else if (TypeComboBox.SelectedIndex == (int)FunctionType.Scene)
            {
                _defaultValue = new Core.Scene();
            }
            else if (TypeComboBox.SelectedIndex == (int)FunctionType.Generic)
            {
                _defaultValue = new Core.Generic();
            }
            else if (TypeComboBox.SelectedIndex == (int)FunctionType.Dynamic)
            {
                _defaultValue = new Core.Dynamic();
            }
            else if (TypeComboBox.SelectedIndex == (int) FunctionType.Mesh)
            {
                _defaultValue = new Core.MeshValue();
            }
        }

        private void UpdateMetaInput()
        {
            var opPartDefinition = BasicMetaTypes.GetMetaOperatorPartOf((FunctionType)TypeComboBox.SelectedIndex);

            _metaInput.Name = NameTextBox.Text;
            _metaInput.OpPart = opPartDefinition;
            _metaInput.DefaultValue = _defaultValue;
            _metaInput.IsMultiInput = IsMultiInputComboBox.SelectedIndex == 1 ? true : false;

            OperatorPart newOpPart = _metaInput.CreateInstance();

            if (_operatorPart.Type != newOpPart.Type)
            {
                _operatorPart.Type = newOpPart.Type;
                _operatorPart.Func = newOpPart.Func;
            }
            if (_operatorPart.Name != newOpPart.Name)
                _operatorPart.Name = newOpPart.Name;

            if (_operatorPart.IsMultiInput != newOpPart.IsMultiInput)
                _operatorPart.IsMultiInput = newOpPart.IsMultiInput;

            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private TextDocument DescriptionDoc { get; set; }

        private readonly Operator _operator;
        private readonly OperatorPart _operatorPart;
        private readonly MetaInput _metaInput;
        private IValue _defaultValue;
    }
}
