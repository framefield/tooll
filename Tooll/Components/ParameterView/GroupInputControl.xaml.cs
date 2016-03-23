// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Tooll.Components.Dialogs;
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
using ICommand = Framefield.Core.ICommand;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for OperatorParameterViewRow.xaml
    /// </summary>
    public partial class GroupInputControl : UserControl
    {
        public GroupInputControl(List<OperatorPart> opParts)
        {
            m_OperatorParts = opParts;
            InitializeComponent();

            m_MixedControl = new GroupMixedAnimationConnectionControls(m_OperatorParts) { Visibility = Visibility.Hidden };
            m_AnimationControl = new GroupAnimationControls(m_OperatorParts) { Visibility = Visibility.Hidden };
            m_ConnectionControl = new GroupConnectionControls(m_OperatorParts) { Visibility = Visibility.Hidden };
            Controls.Children.Add(m_MixedControl);
            Controls.Children.Add(m_AnimationControl);
            Controls.Children.Add(m_ConnectionControl);
            ConnectEventHandler();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ConnectEventHandler();
            UpdateControl();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach (var opPart in m_OperatorParts)
                opPart.ManipulatedEvent -= UpdateHandler;
        }

        private void ConnectEventHandler()
        {
            foreach (var opPart in m_OperatorParts)
                opPart.ManipulatedEvent += UpdateHandler;
        }

        private void UpdateHandler(object o, EventArgs e)
        {
            UpdateControl();
        }

        private void UpdateControl()
        {
            bool atLeastOneElementIsAnimated = false;
            bool atLeastOneElementIsConnected = false;
            bool allElementsAreNotModified = true;
            foreach (var opPart in m_OperatorParts)
            {
                var animationOpPart = Animation.GetRegardingAnimationOpPart(opPart);
                atLeastOneElementIsAnimated |= animationOpPart != null;
                atLeastOneElementIsConnected |= animationOpPart == null && opPart.Connections.Count > 0;
                allElementsAreNotModified &= animationOpPart == null && opPart.Connections.Count == 0;
            }

            if (atLeastOneElementIsAnimated)
            {
                m_MixedControl.Visibility = Visibility.Hidden;
                m_AnimationControl.Visibility = Visibility.Visible;
                m_ConnectionControl.Visibility = Visibility.Hidden;
            }
            else if (atLeastOneElementIsConnected)
            {
                m_MixedControl.Visibility = Visibility.Hidden;
                m_AnimationControl.Visibility = Visibility.Hidden;
                m_ConnectionControl.Visibility = Visibility.Visible;
            }
            else if (allElementsAreNotModified)
            {
                m_MixedControl.Visibility = Visibility.Hidden;
                m_AnimationControl.Visibility = Visibility.Hidden;
                m_ConnectionControl.Visibility = Visibility.Hidden;
            }
            else
            {
                m_MixedControl.Visibility = Visibility.Visible;
                m_AnimationControl.Visibility = Visibility.Hidden;
                m_ConnectionControl.Visibility = Visibility.Hidden;
            }
        }

        private void SetKeyframe_Clicked(object sender, RoutedEventArgs e)
        {
            App.Current.UndoRedoStack.AddAndExecute(new SetupAnimationCommand(m_OperatorParts, App.Current.Model.GlobalTime));
        }

        private void HandlePublishAsInput(object sender, RoutedEventArgs e)
        {
            if (m_OperatorParts.Count == 0)
                return;

            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            List<ISelectable> selectedElements = cgv.SelectedElements;

            var baseName = m_OperatorParts[0].Parent.GetMetaInput(m_OperatorParts[0]).Name.Split(new[] { '.' })[0];
            var parameters = (from opPart in m_OperatorParts
                              let splittedName = opPart.Parent.GetMetaInput(opPart).Name.Split(new[] { '.' })
                              select new { OpPart = opPart, SubName = splittedName.Count() > 1 ? splittedName.Last() : String.Empty }).ToList();

            var popup = new TextInputWindow();
            popup.XText.Text = "Input parameter name?";
            popup.XTextBox.Text = baseName;
            popup.XTextBox.SelectAll();
            popup.XTextBox.Focus();
            popup.ShowDialog();
            if (popup.DialogResult == false)
                return;

            var commandList = new List<ICommand>();
            foreach (var p in parameters)
            {
                var name = popup.XTextBox.Text;
                if (p.SubName.Any())
                    name += "." + p.SubName;

                var publishCommand = new PublishAsInputCommand(p.OpPart, name);
                publishCommand.Do();
                commandList.Add(publishCommand);
            }
            App.Current.UndoRedoStack.Add(new MacroCommand("Publish as Inputs", commandList));

            if (m_OperatorParts.Count > 1)
                cgv.SelectedElements = selectedElements;
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            XContextMenu.IsOpen = true;
        }

        private List<OperatorPart> m_OperatorParts;
        private GroupMixedAnimationConnectionControls m_MixedControl;
        private GroupAnimationControls m_AnimationControl;
        private GroupConnectionControls m_ConnectionControl;
    }
}
