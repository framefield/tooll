// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Framefield.Tooll.Components.SearchForOpWindow.DragManagers
{
    class OutputDragManager : DragManager
    {
        public OutputDragManager(ReplaceOperatorWindow window) : base(window)
        {
        }

        protected override void MoveDraggedItemToNewListIfPossible(object sender)
        {
            var item = sender as ListViewItem;
            var viewModel = item.DataContext as OpPartViewModel;
            var index = Window.XNewOutputs.Items.IndexOf(viewModel);
            if (IsPossibleDropTarget(index))
            {
                if (Window.NewOutputs[index].OpPart.ID != Guid.Empty)
                        Window.UnassignedOutputs.Add(Window.NewOutputs[index]);

                Window.NewOutputs[index] = DraggingItem;
                Window.UnassignedOutputs.Remove(DraggingItem);
            }
            item.BorderBrush = Brushes.Transparent;
        }

        protected override void ColorItemBorderDependingOnPossibleDropTarget(object sender)
        {
            var item = sender as ListViewItem;
            var viewModel = item.DataContext as OpPartViewModel;
            var index = Window.XNewOutputs.Items.IndexOf(viewModel);

            item.BorderBrush = IsPossibleDropTarget(index) ? Brushes.LawnGreen : Brushes.Red;
        }

        protected override bool IsPossibleDropTarget(int index)
        {
            if (index < 0)
                return false;

            var listToCheck = Window.OldOutputs;
            return listToCheck[index].OpPart.Type == DraggingItem.OpPart.Type;
        }
    }
}
