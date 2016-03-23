// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// <summary>
    /// Interaction logic for OutputView.xaml
    /// </summary>
    public partial class OutputView : UserControl
    {
        public static RoutedCommand ChangeKeyControlCommand = new RoutedCommand();
        public Canvas Panel { get { return XPanel; } set { XPanel = value; } }

        public OutputView() {
            InitializeComponent();
        }

        public MatrixTransform ViewTransform { get { return XViewTransform; } set { XViewTransform = value; } }

        public bool ProcessClickEventOnConnections(Point p) {
            return false;
        }

        private void OnMouseRightDown(object sender, MouseButtonEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) {
                m_MatrixOnDragStart = this.ViewTransform.Value;
                UIElement el = sender as UIElement;
                if (el != null) {
                    el.CaptureMouse();
                    m_DragStartPosition = e.GetPosition(this);
                    m_IsRightMouseDragging = true;
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e) {
            if (m_IsRightMouseDragging) {
                double deltaX = -(m_DragStartPosition.X - e.GetPosition(this).X);
                double deltaY = 0;//-(m_DragStartPosition.Y - e.GetPosition(this).Y);
                Matrix m = m_MatrixOnDragStart;
                m.Translate(deltaX, deltaY);
                this.ViewTransform.Matrix = m;
                CompositionGraphView cgv = UIHelper.FindParent<CompositionGraphView>(this);
                if (cgv != null)
                    cgv.UpdateConnectionsToOutputs();
            }
        }

        private void OnMouseRightUp(object sender, MouseButtonEventArgs e) {
            m_IsRightMouseDragging = false;
            UIElement thumb = sender as System.Windows.Controls.Primitives.Thumb;
            if (thumb != null) {
                thumb.ReleaseMouseCapture();
                if (Math.Abs(m_DragStartPosition.X - e.GetPosition(this).X) + Math.Abs(m_DragStartPosition.Y - e.GetPosition(this).Y) > 3)
                    XThumb.ContextMenu = null;
                else
                    XThumb.ContextMenu = Resources["ContextMenu"] as System.Windows.Controls.ContextMenu;
            }
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e) {
        }

        private void OnMouseDownOnCanvas(object sender, MouseButtonEventArgs e) {
        }

        private void OnAddOutput(object sender, RoutedEventArgs e) {
            var cv = UIHelper.FindParent<CompositionView>(this);
            var metaOutput = new MetaOutput(Guid.NewGuid(), "Output", BasicMetaTypes.GenericMeta);
            var metaOp = cv.CompositionGraphView.CompositionOperator.Definition;
            metaOp.AddOutput(metaOutput);
        }


        private bool m_IsRightMouseDragging = false;
        private Point m_DragStartPosition;
        private Matrix m_MatrixOnDragStart;
    }
}
