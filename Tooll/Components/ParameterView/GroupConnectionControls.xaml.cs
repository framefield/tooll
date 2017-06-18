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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for OperatorParameterViewRow.xaml
    /// </summary>
    public partial class GroupConnectionControls : UserControl
    {
        public GroupConnectionControls(List<OperatorPart> opParts)
        {
            _operatorParts = opParts;
            if (opParts[0].Connections.Any())
            {
                var input = opParts[0].Connections[0];
                var connectedTo = input.Connections[0];
                m_SourceOperator = connectedTo.Parent;

                this.ToolTip = "Connected to " + m_SourceOperator;
            }
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectConnectedOperators();
        }

        private void SelectConnectedOperators(bool center = false)
        {
            var ops = new List<Operator>();
            foreach (var opPart in _operatorParts)
            {
                foreach (var input in opPart.Connections)
                {
                    ops.Add(input.Parent);
                }
            }
            if (ops.Count == 0)
                return;

            var CGV = App.Current.MainWindow.CompositionView.CompositionGraphView;
            CGV.SelectOperators(ops, true);
        }

        private Operator m_SourceOperator = null;
        private List<OperatorPart> _operatorParts;
    }
}