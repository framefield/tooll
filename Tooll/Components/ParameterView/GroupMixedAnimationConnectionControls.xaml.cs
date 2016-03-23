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
    public partial class GroupMixedAnimationConnectionControls : UserControl
    {
        public GroupMixedAnimationConnectionControls(List<OperatorPart> opParts) {
            InitializeComponent();
        }
    }
}
