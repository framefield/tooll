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
using Framefield.Core.Commands;


namespace Framefield.Tooll
{
    public partial class AnnotationControl : UserControl
    {
        public AnnotationControl() {
            InitializeComponent();
        }

        
        private void OnLoaded(object sender, RoutedEventArgs e) 
        {
            _visualParent = this.VisualParent as UIElement;
            var vm = DataContext as AnnotationViewModel;
            _op = vm.OperatorWidget.Operator;
        }


        #region XAML event handler

        private List<Operator> operatorsToMove  ;
        private List<UpdateOperatorPropertiesCommand.Entry>  startEntries;
        private List<Point> startPositions;

        private void XHeaderThumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _mousePositionAtDragStart = Mouse.GetPosition(_visualParent);
            var CGV = App.Current.MainWindow.CompositionView.XCompositionGraphView;

            // Collect operators inside annotation
            operatorsToMove = new List<Operator>();
            startEntries = new List<UpdateOperatorPropertiesCommand.Entry>();
            startPositions = new List<Point>();

            operatorsToMove.Add(_op);
            startEntries.Add( new UpdateOperatorPropertiesCommand.Entry(_op));
            startPositions.Add(_op.Position);

            if(!Keyboard.Modifiers.HasFlag(ModifierKeys.Alt ))
            {
                foreach (var o in CGV.CompositionOperator.InternalOps)
                {
                    if (o.Position.X >= _op.Position.X
                        && o.Position.X + o.Width <= _op.Position.X + _op.Width
                        && o.Position.Y >= _op.Position.Y
                        && o.Position.Y+ CompositionGraphView.GRID_SIZE <= _op.Position.Y + Height
                    ) {
                        operatorsToMove.Add(o);
                        startEntries.Add(new UpdateOperatorPropertiesCommand.Entry(o));
                        startPositions.Add(o.Position);
                    }
                }
            }
            _updatePropertiesCmd = new UpdateOperatorPropertiesCommand(operatorsToMove, startEntries);
        }


        private void XHeaderThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var offset = Mouse.GetPosition(_visualParent) - _mousePositionAtDragStart;

            for (int i = 0; i < operatorsToMove.Count; i++)
            {                
                _updatePropertiesCmd.ChangeEntries[i].Position = new Point(offset.X + startPositions[i].X, offset.Y + startPositions[i].Y);
            }
            _updatePropertiesCmd.Do();
            e.Handled = true;
        }


        private void XHeaderThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var currentDragPosition = Mouse.GetPosition(_visualParent);
            var delta = _mousePositionAtDragStart - currentDragPosition;

            if (delta.Length > 4)
            {
                App.Current.UndoRedoStack.AddAndExecute(_updatePropertiesCmd);
            }
            else
            {
                // Select Element
                var vm = DataContext as AnnotationViewModel;
                var CGV = App.Current.MainWindow.CompositionView.XCompositionGraphView;
                CGV.SelectionHandler.SetElement(vm.OperatorWidget);

                _updatePropertiesCmd.Undo();
            }
        }



        // Resize
        private double _heightOnDragStart;
        private double _widthOnDragStart;

        private void XSizeThumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _mousePositionAtDragStart = Mouse.GetPosition(_visualParent);
            _heightOnDragStart = Height;
            _widthOnDragStart = Width;
            
            var startEntry = new UpdateOperatorPropertiesCommand.Entry(_op);
            _updatePropertiesCmd = new UpdateOperatorPropertiesCommand(_op, startEntry);
        }


        private void XSizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var offset = Mouse.GetPosition(_visualParent) - _mousePositionAtDragStart;
            _updatePropertiesCmd.ChangeEntries[0].Width = Math.Max(25, _widthOnDragStart + offset.X);
            _updatePropertiesCmd.Do();

            var vm = DataContext as AnnotationViewModel;
            vm.Height = Math.Max(15, _heightOnDragStart + offset.Y);        // FIXME: This manipulation can't be undone
            e.Handled = true;
        }


        private void XSizeThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var currentDragPosition = Mouse.GetPosition(_visualParent);
            var delta = _mousePositionAtDragStart - currentDragPosition;

            if (delta.Length > 4)
            {
                App.Current.UndoRedoStack.AddAndExecute(_updatePropertiesCmd);
            }
            else
            {
                _updatePropertiesCmd.Undo();
            }
        }

        #endregion

        private Point _mousePositionAtDragStart;
        private UpdateOperatorPropertiesCommand _updatePropertiesCmd;        
        private UIElement _visualParent;
        private Operator _op;

    }
}
