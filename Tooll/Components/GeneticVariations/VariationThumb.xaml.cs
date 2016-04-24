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
using Framefield.Tooll;
using Framefield.Core;
using Framefield.Core.Commands;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for PresetThumb.xaml
    /// </summary>
    public partial class VariationThumb : UserControl
    {
        public VariationThumb() {
            InitializeComponent();
        }

        private void Clicked(object sender, RoutedEventArgs e) {
         
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e) {
            var o = sender as FrameworkElement;
            var variation = o.DataContext as Variation;
            if (variation != null )
            {
                variation.Preview();
            }
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            var o = sender as FrameworkElement;
            var variation = o.DataContext as Variation;
            if (variation != null)
            {
                variation.EndPreview();
            }
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var o = sender as FrameworkElement;
            var variation = o.DataContext as Variation;
            if (variation != null)
            {
                variation.Select();

                var item = UIHelper.FindVisualParent<ListViewItem>(this);
                if (item == null)
                    throw new Exception("Can't handle click on VariationThumbnail outside of ListItem");

                item.IsSelected = variation.IsSelected;
            }
        }

        private void UIElement_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void UIElement_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var o = sender as FrameworkElement;
            var variation = o.DataContext as Variation;
            if (variation != null)
            {
                variation.MarkSomething();
            }
            e.Handled = true;
        }
    }
}
