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
    /**
    *   This is a cleaned up version of the fence selection interactor.
     *  
     *  This class works as a mediator between...
     *  - SelectionHandler
     *  - Control that handles different controls (like the graph view)
     *  - 
     *  
     * The FenceSelector should be added into a canvas to the Xaml tree.
     * During selection, it visualizes and updates a selection fence. On
     * each change, it iterates over the list of selected items and checks
     * wether they overlap with the fence.
     * 
     * -> tricky: make sure that 
     * checks if any of the selectables are inside the 
     *  
     */
    public partial class  FenceSelection : UserControl
    {
        public FenceSelection(SelectionHandler sh, Canvas itemsContainer) {
            InitializeComponent();
            Visibility = System.Windows.Visibility.Collapsed;
            m_SelectionHandler = sh;
            m_ItemsContainer = itemsContainer;
        }

        public void HandleDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            m_DragStartPosition = new Point(e.HorizontalOffset, e.VerticalOffset);
            m_FenceTopLeft = m_DragStartPosition;

            Visibility = System.Windows.Visibility.Visible;

            Canvas.SetLeft(this, m_FenceTopLeft.X);
            Canvas.SetTop(this, m_FenceTopLeft.Y);
            Height = 0;
            Width = 0;

            if (Keyboard.Modifiers == ModifierKeys.Shift) {
                m_SelectMode= SelectMode.Add;
            }

            else if (Keyboard.Modifiers == ModifierKeys.Control) {
                m_SelectMode = SelectMode.Remove;
            }
            else {
                m_SelectMode = SelectMode.Replace;
            }
        }

        public void HandleDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) {
            if (!m_SelectionStarted) {
                if (e.HorizontalChange != 0 || e.VerticalChange != 0) {
                    m_SelectionStarted = true;
                    if (m_SelectMode == SelectMode.Replace) {
                        if (m_SelectionHandler != null)
                            m_SelectionHandler.Clear();
                    }
                }
            }
            else {
                Point newPosition = new Point(m_DragStartPosition.X + e.HorizontalChange,
                                              m_DragStartPosition.Y + e.VerticalChange);


                if (e.VerticalChange < 0) {

                    Canvas.SetTop(this, newPosition.Y);
                    Height = -e.VerticalChange;
                }
                else {
                    Canvas.SetTop(this, m_FenceTopLeft.Y);
                    Height = e.VerticalChange;
                }

                if (e.HorizontalChange < 0) {
                    Canvas.SetLeft(this, newPosition.X);
                    Width = -e.HorizontalChange;
                }
                else {
                    Canvas.SetLeft(this, m_FenceTopLeft.X);
                    Width = e.HorizontalChange;
                }

                var visualParent = this.VisualParent as UIElement;

                if (m_SelectionHandler != null) {
                    List<ISelectable> elementsToSelect = new List<ISelectable>();
                    foreach (var child in m_ItemsContainer.Children) {
                        var selectableWidget = child as ISelectable;
                        if (selectableWidget != null) {
                            var uiElement = selectableWidget as UserControl;
                            if (uiElement != null) {
                                var uiTopLeftT = uiElement.TranslatePoint( new Point(), visualParent);

                                double elementWidth = Double.IsNaN(selectableWidget.Width) ? 0 : selectableWidget.Width;
                                double elementHeight = Double.IsNaN(selectableWidget.Height) ? 0 : selectableWidget.Height;

                                var uiButtomLeftT = uiElement.TranslatePoint(new Point(elementWidth, elementHeight), visualParent);
                                var fenceTopLeftT = this.TranslatePoint(new Point(), visualParent);

                                var fenceBottomRight = new Point(Width, Height);
                                var fenceBottomRightT = this.TranslatePoint(fenceBottomRight, visualParent);

                                var fenceRectT = new Rect(fenceTopLeftT, fenceBottomRightT);
                                var localElementRect = new Rect(uiTopLeftT, uiButtomLeftT);
                                
                                if (uiElement.IsHitTestVisible && fenceRectT.IntersectsWith(localElementRect))
                                    elementsToSelect.Add(selectableWidget);
                            }
                        }
                    }
                    switch (m_SelectMode) {
                        case SelectMode.Add:
                            m_SelectionHandler.AddElements(elementsToSelect);
                            break;

                        case SelectMode.Remove:
                            m_SelectionHandler.RemoveElements(elementsToSelect);
                            break;

                        case SelectMode.Replace:
                            m_SelectionHandler.SetElements(elementsToSelect);
                            break;
                    }
                }
            }
        }

        public void HandleDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            m_SelectionStarted = false;

            if (Math.Abs(e.HorizontalChange) + Math.Abs(e.HorizontalChange) < 3) {
                m_SelectionHandler.Clear();
            }

            Visibility = System.Windows.Visibility.Collapsed;
        }

        private enum SelectMode {
            Add=0,
            Remove,
            Replace,
        }

        private SelectionHandler m_SelectionHandler;
        private Point m_DragStartPosition;
        private Canvas m_ItemsContainer;
        private Point m_FenceTopLeft;
        private FenceSelection.SelectMode m_SelectMode;
        private bool m_SelectionStarted = false; // Set to true after DragThreshold reached
    }
}
