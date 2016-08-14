// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Framefield.Core;
using Framefield.Core.Commands;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for StringParameterValue.xaml
    /// </summary>
    public partial class TextParameterValue : UserControl, IParameterControl
    {
        public OperatorPart ValueHolder { get; private set; }

        TextButtonEdit XTextEdit;

        public TextParameterValue(OperatorPart valueHolder) {

            InitializeComponent();
            ValueHolder = valueHolder;

            ValueHolder.ChangedEvent += ValueHolder_ChangedEvent;

            var context = new OperatorPartContext();

            MouseUp += HandleMouseUp;

            // Insert Path Picker button
            if (valueHolder.Name.EndsWith("Path")) {
                var pickFileButton = new Button();
                pickFileButton.Focusable = true;
                pickFileButton.Content = "...";
                XGrid.Children.Add(pickFileButton);
                Grid.SetColumn(pickFileButton, 0);

                pickFileButton.Click += new RoutedEventHandler(PickFileButton_Click);
            }
            else {
                XFirstColumDefinition.Width = new GridLength(0);                
            }


            XTextEdit= new TextButtonEdit();
            XGrid.Children.Add(XTextEdit);
            Grid.SetColumn(XTextEdit, 1);
            if (valueHolder.Name.EndsWith("Code")) {
                XTextEdit.Text = "Code stuff";
            }
            else {
                XTextEdit.Text = valueHolder.Eval(context).Text;
                XTextEdit.XTextEdit.TextChanged += TextChangedHandler;
                XTextEdit.EditingStartedEvent += XTextEdit_EditingStarted;
                XTextEdit.EditingCompletedEvent += XTextEdit_EditingCompleted;
            }

            if (valueHolder.Name.EndsWith("Text"))
            {
                XTextEdit.EnableLineBreaks();
            }

            var parent = valueHolder.Parent;
            var parentMeta = parent.Definition;
            int index = parent.Inputs.IndexOf(valueHolder);

            if (index >= 0) {
                var metaInput = parentMeta.Inputs[index];
                var valueType = metaInput.DefaultValue as Value<string>;
                if (valueType != null) {
                    string defaultValue = valueType.Val;
                    XTextEdit.Default = defaultValue;
                }
            }
            UpdateGUI();
        }

        public void ResetToDefaultHandler(object sender, EventArgs e)
        {
            TryExecutatingResetSingleParameter();
        }


        private void TryExecutatingResetSingleParameter()
        {
            if (IsConnected)
            {
                MessageBox.Show("To reset a connected paramter, you first have to disconnect it.");
                return;
            }

            var removeAnimationCommand = new RemoveAnimationCommand(ValueHolder, 0);
            var setValueToDefaultCmd = new ResetInputToDefault(ValueHolder);
            Framefield.Core.ICommand[] commands = { removeAnimationCommand, setValueToDefaultCmd };

            App.Current.UndoRedoStack.AddAndExecute(new MacroCommand("Reset Paramter", commands));
            App.Current.UpdateRequiredAfterUserInteraction = true;
            XTextEdit.Text = XTextEdit.Default; // we also need to reset the text-edit trigger default-color
            UpdateGUI();
        }



        void PickFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
                return; 

            var defaultPath = ".\\assets";
            var tmpContext = new OperatorPartContext(); 
            var startPath = ValueHolder.Eval(tmpContext).Text;
            var dialogTitle = "Select file";

            var pickedFilePath = UIHelper.PickFileWithDialog(defaultPath, startPath, dialogTitle);

            if (pickedFilePath != "")
            {
                if (_updateValueCommand == null)
                    XTextEdit_EditingStarted();
                XTextEdit.XTextEdit.Text = pickedFilePath;
                XTextEdit_EditingCompleted();
            }
        }



        private void HandleMouseUp(object sender, MouseButtonEventArgs e) 
        {
            var userControl = sender as Control;
            if (userControl == null)
                return;

            var contextMenu = new ContextMenu();

            var setDefaultMenuItem = new MenuItem();
            setDefaultMenuItem.Header = "Set as default";
            setDefaultMenuItem.Click += (o, a) => {
                var setInputAsAndResetToDefaultCmd = new SetInputAsAndResetToDefaultCommand(ValueHolder);
                App.Current.UndoRedoStack.AddAndExecute(setInputAsAndResetToDefaultCmd);
                App.Current.UpdateRequiredAfterUserInteraction = true; // ToDo: This line should be moved to the source of interaction. Here, we only deal with result.
            };
            contextMenu.Items.Add(setDefaultMenuItem);

            userControl.ContextMenu = contextMenu;
        }


        private void TextChangedHandler(Object sender, TextChangedEventArgs e)
        {
            if (_updateValueCommand == null)
                return;

            _updateValueCommand.Value = new Text(XTextEdit.XTextEdit.Text);
            _updateValueCommand.Do();
            App.Current.UpdateRequiredAfterUserInteraction = true;
            UpdateGUI();
        }

        private void XTextEdit_EditingStarted()
        {
            _updateValueCommand = new UpdateOperatorPartValueFunctionCommand(ValueHolder, new Text(XTextEdit.XTextEdit.Text));
        }

        private void XTextEdit_EditingCompleted()
        {
            if (_updateValueCommand == null)
                return;

            App.Current.UndoRedoStack.Add(_updateValueCommand);
            _updateValueCommand = null;
        }

        private void ValueHolder_ChangedEvent(object sender, OperatorPart.ChangedEventArgs e)
        {
            if (_updateValueCommand == null)
            {
                // We're not editing, so update value visualization accordingly to change

                string textString = "";
                if (IsConnected)
                {
                    textString =  ValueHolder.Eval(new Core.OperatorPartContext()).Text;
                }
                else
                {
                    var textValue = (ValueHolder.Func as Utilities.ValueFunction).Value as Text;
                    if (textValue != null)
                    {
                        textString = textValue.Val;
                    }                    
                }
                XTextEdit.XButton.Content = textString;
                XTextEdit.XTextEdit.Text = textString;
                UpdateGUI();
            }
        }


        #region update user interface
        //-----------------------------------------------------------------------------------
        // Helper Functions to update UI

        private void UpdateGUI()
        {
            if (ValueHolder.Connections.Count > 0)
            {
                XTextEdit.XButton.Foreground = Brushes.DodgerBlue;
                XTextEdit.IsEnabled = false;
            }
            else
            {
                XTextEdit.XButton.Foreground = Brushes.White;
                XTextEdit.IsEnabled = true;
            }
        }
        #endregion




        private bool IsConnected
        {
            get { return ValueHolder != null && ValueHolder.Connections.Count > 0; }
        }

        private UpdateOperatorPartValueFunctionCommand _updateValueCommand;
    }
}

