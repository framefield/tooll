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

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for UndoRedoView.xaml
    /// </summary>
    public partial class UndoRedoView : UserControl
    {
        public UndoRedoView()
        {
            DataContext = this;
            InitializeComponent();

            var undoListBinding = new Binding();
            undoListBinding.Source = App.Current.UndoRedoStack;
            undoListBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            undoListBinding.Path = new PropertyPath("UndoList");
            XUndoListBox.SetBinding(ItemsControl.ItemsSourceProperty, undoListBinding);

            var redoListBinding = new Binding();
            redoListBinding.Source = App.Current.UndoRedoStack;
            redoListBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            redoListBinding.Path = new PropertyPath("RedoList");
            XRedoListBox.SetBinding(ItemsControl.ItemsSourceProperty, redoListBinding);

        }
    }
}
