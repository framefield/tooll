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
        public GroupConnectionControls(List<OperatorPart> opParts) {
            m_OperatorParts = opParts;

            //this.ToolTip = "Connected to " + opParts[0].Connections[0].ToString();  // ToDo: Must be set to the source of the connection
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            foreach (var el in App.Current.MainWindow.CompositionView.CompositionGraphView.XOperatorCanvas.Children) {
                var opwi = el as OperatorWidget;
                if (opwi != null) {
                    if (opwi.Operator == m_SourceOperator) {
                        App.Current.MainWindow.CompositionView.CompositionGraphView.SelectionHandler.SetElement(opwi);
                        return;
                    }
                }
            }
        }

        private Operator m_SourceOperator = null;
        private List<OperatorPart> m_OperatorParts;
    }
}
