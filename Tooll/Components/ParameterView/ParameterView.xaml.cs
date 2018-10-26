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
using SharpDX;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for ParameterView.xaml
    /// </summary>
    public partial class ParameterView : UserControl
    {
        public ParameterView()
        {
            InitializeComponent();
        }

        public Operator ShownOperator { get; set; }

        public bool PreventUIUpdate { get; set; }

        public void UpdateViewToCurrentSelection(object sender, SelectionHandler.FirstSelectedChangedEventArgs e)
        {
            if (PreventUIUpdate)
                return;

            if (e.Element is CompositionGraphView cgv)
            {
                ShownOperator = cgv.CompositionOperator;
                Content = new OperatorParameterView(ShownOperator);
                return;
            }

            if (e.Element is OperatorWidget opWidget)
            {
                ShownOperator = opWidget.Operator;
                Content = new OperatorParameterView(ShownOperator);
                return;
            }
            ShownOperator = null;

            if (e.Element is InputWidget inputWidget)
            {
                MainWindow mainWindow = App.Current.MainWindow;
                var op = mainWindow.CompositionView.CompositionGraphView.CompositionOperator;
                Content = new InputParameterView(op, inputWidget.OperatorPart);
                return;
            }

            if (e.Element is OutputWidget outputWidget)
            {
                MainWindow mainWindow = App.Current.MainWindow;
                var op = mainWindow.CompositionView.CompositionGraphView.CompositionOperator;
                Content = new OutputParameterView(op, outputWidget.OperatorPart);
                return;
            }

            Content = null;
        }

        public void FocusNameField()
        {
            if (Content is OperatorParameterView opv)
            {
                opv.XNameTextBox.Focus();
                opv.XNameTextBox.EnableTextEdit();
            }
        }
    }
}