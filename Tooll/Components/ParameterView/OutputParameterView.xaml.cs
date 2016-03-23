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
    public partial class OutputParameterView : UserControl
    {
        public OutputParameterView(Operator op, OperatorPart opPart) {
            InitializeComponent();

            _operator = op;
            _operatorPart = opPart;
            _outputIndex = opPart.Func.EvaluationIndex;

            var metaOutput = _operator.GetMetaOutput(_operatorPart);
            if (metaOutput != null)
            {
                NameTextBox.Text = metaOutput.Name;
                TypeComboBox.SelectedIndex = (int) metaOutput.OpPart.Type;

                NameTextBox.LostKeyboardFocus += (o, e) => UpdateMetaOutput();

                TypeComboBox.SelectionChanged += (o, e) => UpdateMetaOutput();
            }
        }

        private void UpdateMetaOutput()
        {
            var opPartDefinition = BasicMetaTypes.GetMetaOperatorPartOf((FunctionType)TypeComboBox.SelectedIndex);

            var metaOutput = _operator.GetMetaOutput(_operatorPart);
            metaOutput.Name = NameTextBox.Text;
            metaOutput.OpPart = opPartDefinition;

            _operator.Definition.RemoveOutput(metaOutput.ID);
            _operator.Definition.InsertOutput(_outputIndex, metaOutput);
            _operatorPart = _operator.Outputs[_outputIndex];
        }

        private readonly Operator _operator;
        private OperatorPart _operatorPart;
        private readonly int _outputIndex;
    }
}

