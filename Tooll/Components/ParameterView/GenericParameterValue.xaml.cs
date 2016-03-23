// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Windows.Controls;
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for StringParameterValue.xaml
    /// </summary>
    public partial class GenericParameterValue : UserControl, IParameterControl
    {
        public GenericParameterValue(OperatorPart valueHolder)
        {
            InitializeComponent();
            ValueHolder = valueHolder;
        }

        public OperatorPart ValueHolder { get; private set; }
    }
}