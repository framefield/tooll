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
    /// Interaction logic for RenderUpdateDebugViz.xaml
    /// </summary>
    public partial class RenderUpdateDebugViz : UserControl
    {
        private int m_updateCount =0;
        
        public RenderUpdateDebugViz()
        {
            InitializeComponent();
            

            var parent = this.VisualParent as UIElement;
        }

        private void SelectionChangedHandler(object sender, SelectionHandler.FirstSelectedChangedEventArgs e)
        {
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            m_updateCount++;
            Canvas.SetLeft(XRect, m_updateCount % ActualWidth);
            return base.ArrangeOverride(arrangeBounds);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            this.UpdateLayout();
            Canvas.SetLeft(XRect, m_updateCount % ActualWidth);
        }

    }
}
