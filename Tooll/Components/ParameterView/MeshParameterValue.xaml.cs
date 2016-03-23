// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Windows.Controls;
using Framefield.Core;

namespace Framefield.Tooll
{
    public partial class MeshParameterValue : UserControl, IParameterControl
    {
        public MeshParameterValue(OperatorPart valueHolder)
        {
            InitializeComponent();
            ValueHolder = valueHolder;
        }

        public OperatorPart ValueHolder { get; private set; }
    }
}