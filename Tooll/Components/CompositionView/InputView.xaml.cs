// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Framefield.Core;
using Framefield.Core.Commands;
using Image = Framefield.Core.Image;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for InputView.xaml
    /// </summary>
    public partial class InputView : UserControl
    {
        public static RoutedCommand ChangeKeyControlCommand = new RoutedCommand();
        public Canvas Panel { get { return XPanel; } set { XPanel = value; } }

        public InputView() {
            InitializeComponent();
        }

        public MatrixTransform ViewTransform { get { return XViewTransform; } set { XViewTransform = value; } }

        public bool ProcessClickEventOnConnections(Point p) {
            return false;
        }

        private void OnMouseRightDown(object sender, MouseButtonEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) {
                m_MatrixOnDragStart = this.ViewTransform.Value;
                UIElement el = sender as UIElement;
                if (el != null) {
                    el.CaptureMouse();
                    m_DragStartPosition = e.GetPosition(this);
                    m_IsRightMouseDragging = true;
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e) {
            if (m_IsRightMouseDragging) {
                double deltaX = -(m_DragStartPosition.X - e.GetPosition(this).X);
                double deltaY = 0;//-(m_DragStartPosition.Y - e.GetPosition(this).Y);
                Matrix m = m_MatrixOnDragStart;
                m.Translate(deltaX, deltaY);
                ViewTransform.Matrix = m;
                CompositionGraphView cgv = UIHelper.FindParent<CompositionGraphView>(this);
                if (cgv != null)
                    cgv.UpdateConnectionsFromInputs();
            }
        }

        private void OnMouseRightUp(object sender, MouseButtonEventArgs e) {
            m_IsRightMouseDragging = false;
            UIElement thumb = sender as System.Windows.Controls.Primitives.Thumb;
            if (thumb != null) {
                thumb.ReleaseMouseCapture();
                if (Math.Abs(m_DragStartPosition.X - e.GetPosition(this).X) + Math.Abs(m_DragStartPosition.Y - e.GetPosition(this).Y) > 3)
                    XThumb.ContextMenu = null;
                else
                    XThumb.ContextMenu = Resources["ContextMenu"] as System.Windows.Controls.ContextMenu;
            }
        }

        private void OnMouseDownOnCanvas(object sender, MouseButtonEventArgs e) {
        }


        private void OnAddGenericInput(object sender, RoutedEventArgs e)
        {
            AddInputToComposition(new MetaInput(Guid.NewGuid(), "Generic", BasicMetaTypes.GenericMeta, new Generic(), false));
        }

        private void OnAddDynamicInput(object sender, RoutedEventArgs e)
        {
            AddInputToComposition(new MetaInput(Guid.NewGuid(),  "Dynamic", BasicMetaTypes.DynamicMeta, new Dynamic(), false));
        }

        private void OnAddFloatInput(object sender, RoutedEventArgs e)
        {
            var inputDefinition = new MetaInput(Guid.NewGuid(), "Value", BasicMetaTypes.FloatMeta, new Float(0.0f), false);
            inputDefinition.Min = -10000.0f;
            inputDefinition.Max = 10000.0f;
            inputDefinition.Scale = 0.1f;
            AddInputToComposition(inputDefinition);
        }

        private void OnAddTextInput(object sender, RoutedEventArgs e)
        {
            AddInputToComposition(new MetaInput(Guid.NewGuid(), "Text", BasicMetaTypes.TextMeta, new Text(string.Empty), false));
        }

        private void OnAddSceneInput(object sender, RoutedEventArgs e)
        {
            AddInputToComposition(new MetaInput(Guid.NewGuid(), "Scene", BasicMetaTypes.SceneMeta, new Core.Scene(), false));
        }

        private void OnAddImageInput(object sender, RoutedEventArgs e)
        {
            AddInputToComposition(new MetaInput(Guid.NewGuid(), "Image", BasicMetaTypes.ImageMeta, new Image(), false));
        }

        private void OnAddMeshInput(object sender, RoutedEventArgs e)
        {
            AddInputToComposition(new MetaInput(Guid.NewGuid(), "Mesh", BasicMetaTypes.MeshMeta, new MeshValue(), false));
        }

        private void OnAddColorInput(object sender, RoutedEventArgs e) {
            var defaultName = "Color";
            var extensions = new String[] { "R", "G", "B", "A" };
            var min = 0.0f;
            var max = 1.0f;
            var scale = 0.01f;

            CreateFloatInputGroup(defaultName, extensions, min, max, scale);
        }

        private void CreateFloatInputGroup(string defaultName, string[] extensions, float min, float max, float scale) {
            var inputWindow = new Components.Dialogs.TextInputWindow();
            inputWindow.XTextBox.Text = defaultName;
            inputWindow.XTextBox.SelectAll();
            inputWindow.XTextBox.Focus();
            inputWindow.XText.Text = "Input Group";
            inputWindow.XOKButton.Content = "Create";
            inputWindow.Title = "Create input group";
            inputWindow.ShowDialog();

            if (inputWindow.DialogResult == true && inputWindow.XTextBox.Text != String.Empty)
            {
                var opTitle = inputWindow.XTextBox.Text;

                var opPart = BasicMetaTypes.FloatMeta;
                var inputsToAdd = new MetaInput[extensions.Length];
                for(int i = 0; i < extensions.Length; i++) {
                    var metaInput = new MetaInput(Guid.NewGuid(), opTitle + "." + extensions[i], opPart, new Float(1.0f), false);
                    metaInput.Min = min;
                    metaInput.Max = max;
                    metaInput.Scale = scale;
                    metaInput.Name = opTitle + "." + extensions[i];
                    inputsToAdd[i] = metaInput;
                }
                AddInputToComposition(inputsToAdd);
            }
        }

        private void OnAddVec3Input(object sender, RoutedEventArgs e) {
            var defaultName = "Name";
            var extensions = new String[] { "X", "Y", "Z" };
            var min = -10000.0f;
            var max = 10000.0f;
            var scale = 0.1f;

            CreateFloatInputGroup(defaultName, extensions, min, max, scale);
        }


        private void OnAddVec2Input(object sender, RoutedEventArgs e) {
            var defaultName = "Name";
            var extensions = new String[] { "X", "Y" };
            var min = -10000.0f;
            var max = 10000.0f;
            var scale = 0.1f;

            CreateFloatInputGroup(defaultName, extensions, min, max, scale);
        }

        private void OnAddSize2Input(object sender, RoutedEventArgs e) {
            var defaultName = "Size";
            var extensions = new String[] { "Width", "Height" };
            var min = 0.0f;
            var max = 10000.0f;
            var scale = 0.1f;

            CreateFloatInputGroup(defaultName, extensions, min, max, scale);
        }

        private void OnAddSize3Input(object sender, RoutedEventArgs e) {
            var defaultName = "Size";
            var extensions = new String[] { "Width", "Height", "Depth" };
            var min = 0.0f;
            var max = 10000.0f;
            var scale = 0.1f;

            CreateFloatInputGroup(defaultName, extensions, min, max, scale);
        }

        private void AddInputToComposition(MetaInput inputToAdd) {
            var compositionView = UIHelper.FindParent<CompositionView>(this);
            var compOp = compositionView.CompositionGraphView.CompositionOperator;

            var command = new AddInputCommand(compOp, inputToAdd);
            App.Current.UndoRedoStack.AddAndExecute(command);
        }

        private void AddInputToComposition(MetaInput[] inputsToAdd)
        {
            var compositionView = UIHelper.FindParent<CompositionView>(this);
            var compOp = compositionView.CompositionGraphView.CompositionOperator;

            var commands = new AddInputCommand[inputsToAdd.Length];
            for (var i = 0; i < inputsToAdd.Length; i++)
            {
                var command = new AddInputCommand(compOp, inputsToAdd[i]);
                commands[i] = command;
            }
            var macroCommand = new MacroCommand("Add input paramter group", commands);
            App.Current.UndoRedoStack.AddAndExecute(macroCommand);
        }

        private bool m_IsRightMouseDragging = false;
        private Point m_DragStartPosition;
        private Matrix m_MatrixOnDragStart;

        private void OnAddTriggerInput(object sender, RoutedEventArgs e) {
            var opPart = BasicMetaTypes.FloatMeta;
            var metaInput = new MetaInput(Guid.NewGuid(), "Trigger", opPart, new Float(0.0f), false);
            metaInput.Min = 0;
            metaInput.Max = 1;
            AddInputToComposition(metaInput);
        }

        private void XThumb_DragCompleted(object sender, DragCompletedEventArgs e) {
            var CGV = App.Current.MainWindow.CompositionView.XCompositionGraphView;
            CGV.SelectionHandler.SetElement(CGV);
        }
    }
}
