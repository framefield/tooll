// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Framefield.Tooll.Components.SearchForOpWindow.DragManagers
{
    public abstract class DragManager
    {
        private Point _startPosition;
        private ListView _dragSource;
        protected ReplaceOperatorWindow Window;
        protected OpPartViewModel DraggingItem;

        protected DragManager(ReplaceOperatorWindow window)
        {
            Window = window;
        }

        public void StartDragging(object sender, MouseButtonEventArgs e)
        {
            _startPosition = e.GetPosition(null);
            _dragSource = sender as ListView;
        }

        public void Dragging(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || DraggingItem == null)
            {
                DraggingItem = _dragSource.SelectedItem as OpPartViewModel;
                return;
            }

            var actualPosition = e.GetPosition(null);
            var moved = _startPosition - actualPosition;
            if (SystemParameters.MinimumHorizontalDragDistance > Math.Abs(moved.X) && SystemParameters.MinimumVerticalDragDistance > Math.Abs(moved.Y))
                return;

            var dataFormat = DataFormats.FileDrop;
            var dataObject = new DataObject(dataFormat, DraggingItem);
            DragDrop.DoDragDrop(_dragSource, dataObject, DragDropEffects.Move);
        }

        public void DragEnter(object sender, DragEventArgs e)
        {
            ColorItemBorderDependingOnPossibleDropTarget(sender);
        }

        public void Drop(object sender, DragEventArgs e)
        {
            MoveDraggedItemToNewListIfPossible(sender);
            DraggingItem = null;
        }

        protected abstract void ColorItemBorderDependingOnPossibleDropTarget(object sender);
        protected abstract void MoveDraggedItemToNewListIfPossible(object sender);
        protected abstract bool IsPossibleDropTarget(int index);
    }
}
