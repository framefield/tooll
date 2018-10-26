// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using SharpDX;
using Brush = SharpDX.Direct2D1.Brush;
using Point = System.Windows.Point;


namespace Framefield.Tooll.Components.QuickCreate
{
    /// <summary>
    /// Interaction logic for IngredientControl.xaml
    /// </summary>
    public partial class IngredientControl : UserControl
    {
        public IngredientControl()
        {
            InitializeComponent();

            Loaded += IngredientControl_Loaded;
        }

        void IngredientControl_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as IngredientViewModel;
            if (vm == null)
                return;


            if (vm.MetaOperator.Outputs.Count > 0)
                _primaryOutputType= vm.MetaOperator.Outputs.First().OpPart.Type;
                            
            this.Background    = new SolidColorBrush(UIHelper.ColorFromType(_primaryOutputType));
            this.Foreground    = new SolidColorBrush(UIHelper.BrightColorFromType(_primaryOutputType));
        }

        private FunctionType _primaryOutputType= FunctionType.Dynamic;


        private Point _dragStartPosition;
        private bool _dragged = false;
        private bool _draggedOutside = false;

        const double PALLETE_GRID_SIZE = 25;

        private void Thumb_OnDragStarted(object sender, DragStartedEventArgs e)
        {
            _dragStartPosition = Mouse.GetPosition(this);
            e.Handled = true;
            _dragged = false;
        }

        private void Thumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var vm = DataContext as IngredientViewModel;
            if (vm == null)
                return;

            var delta = Mouse.GetPosition(this) - _dragStartPosition;
            if (delta.Length > SystemParameters.MinimumHorizontalDragDistance)
                _dragged = true;

            if (!_dragged)
                return;

            var gridDeltaX = (int)(delta.X / 25);
            var gridDeltaY = (int)(delta.Y / 25);

            var targetPosX = vm.GridPositionX + gridDeltaX;
            var targetPosY = vm.GridPositionY + gridDeltaY;

            _draggedOutside = targetPosX < 0 ||
                                  targetPosX >
                                  IngredientsManager.GRID_COLUMNS - IngredientsManager.INGREDIENT_GRID_WIDTH + 2
                                  || targetPosY < -1 || targetPosY > IngredientsManager.GRID_ROWS;


            vm.GridPositionX = (int)MathUtil.Clamp(vm.GridPositionX + gridDeltaX,0, IngredientsManager.GRID_COLUMNS - IngredientsManager.INGREDIENT_GRID_WIDTH);
            vm.GridPositionY = (int)MathUtil.Clamp(vm.GridPositionY + gridDeltaY, 0, IngredientsManager.GRID_ROWS);
            
            // Adjust Opacity to indicate if ingredient is going to be deleted
            this.Opacity = _draggedOutside ? 0.2 : 1;
            
            e.Handled = true;
        }

        private void Thumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            var vm = DataContext as IngredientViewModel;
            if (vm == null)
                return;

            if (!_dragged)
            {
                _quickCreateWindow.AddOperatorToWorkspace(vm.MetaOperator);
            }
            else if (_draggedOutside)
            {
                vm.TriggerRemoved();
            }            
            e.Handled = true;
        }

        private void CloseWindow()
        {
            var parentWindow = FindParentWindow();
            if (parentWindow != null)
                parentWindow.Close();
        }


        private Window FindParentWindow()
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (!(parent is Window))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return (parent as Window);
        }


        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
        {            
            var vm = DataContext as IngredientViewModel;
            if (vm == null)
                return;

            XTextBlock.Foreground= new SolidColorBrush(UIHelper.DarkColorFromType(_primaryOutputType));
            this.Background= new SolidColorBrush(UIHelper.BrightColorFromType(_primaryOutputType));
            
            _quickCreateWindow.ShowOpDescription(vm.MetaOperator);
        }


        private void UIElement_OnMouseLeave(object sender, MouseEventArgs e)
        {
            XTextBlock.Foreground = new SolidColorBrush(UIHelper.BrightColorFromType(_primaryOutputType));
            this.Background = new SolidColorBrush(UIHelper.ColorFromType(_primaryOutputType));

            var vm = DataContext as IngredientViewModel;

            _quickCreateWindow.EndShowOpPreview(vm == null ? null : vm.MetaOperator);
        }

        #region dirty stuff

        private QuickCreateWindow _quickCreateWindow
        {
            get
            {
                if (_ccw == null)
                {
                    _ccw = UIHelper.FindVisualParent<QuickCreateWindow>(this);
                
                }
                return _ccw;
            }
        }

        private QuickCreateWindow _ccw;



        #endregion
    }



    #region Value converter
    [ValueConversion(typeof(int), typeof(double))]
    public class GridIndexToPositionConverter : IValueConverter
    {
        const double PALLETE_GRID_SIZE = 25;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == DependencyProperty.UnsetValue)
            {
                return "binding error";
            }
            var index= (int)value;
            return index*PALLETE_GRID_SIZE;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }
    #endregion


}
