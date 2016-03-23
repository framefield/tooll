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
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for ParameterGroup.xaml
    /// </summary>
    public partial class ParameterGroup : UserControl
    {
        public ParameterGroup()
        {
            InitializeComponent();
        }

        public ParameterGroup(OperatorPart[] inputs ) {
            Inputs= inputs;
            InitializeComponent();
        }

        public OperatorPart[] Inputs { get; set; }


        //#region dependency properties
        //public static readonly DependencyProperty ExpandedProperty = DependencyProperty.Register("Exapanded", typeof(bool), typeof(ParameterGroup), new UIPropertyMetadata(false));
        //public bool Expanded
        //{
        //    get { return (bool) GetValue(ExpandedProperty); }
        //    set { SetValue(ExpandedProperty, value); }
        //}
        //#endregion
        bool Expanded= false;

        private void XExpandButton_Click(object sender, RoutedEventArgs e)
        {                        
            if (Expanded) {
                XParameterRowsPanel.Visibility = System.Windows.Visibility.Collapsed;
                XParameterGroupPanel.Visibility = System.Windows.Visibility.Visible;
                Expanded= false;
            }
            else {
                if (!_subRowsCreated) { 
                    AddParameterGroupAsExpanedRows();
                    _subRowsCreated=true;
                }

                XParameterRowsPanel.Visibility = System.Windows.Visibility.Visible;
                XParameterGroupPanel.Visibility = System.Windows.Visibility.Collapsed;
                Expanded=true;
            }
        }



        private void AddParameterGroupAsExpanedRows() {
            foreach (var input in Inputs) {
                var subParameterRow = new OperatorParameterViewRow(new List<OperatorPart>() { input });
                subParameterRow.XParameterNameButton.Content = input.Name;
                subParameterRow.XInputControls.Children.Add(new GroupInputControl(new List<OperatorPart>() { input }));

                if (input.Type == FunctionType.Float) {
                    if (!input.IsMultiInput) {                        
                        subParameterRow.XParameterValue.Children.Add(new FloatParameterControl(input));
                    }
                }
                else if (input.Type == FunctionType.Text) {
                    var paramEdit = new TextParameterValue(input);
                    subParameterRow.XParameterValue.Children.Add(paramEdit);
                }
                else if (input.Type == FunctionType.Scene) {
                    var paramEdit = new SceneParameterValue(input);
                    subParameterRow.XParameterValue.Children.Add(paramEdit);
                }
                XParameterRowsPanel.Children.Add(subParameterRow);
            }
        }

        private bool _subRowsCreated = false;

    }
}
