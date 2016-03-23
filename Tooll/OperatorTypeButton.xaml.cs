// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Framefield.Core;
using Framefield.Core.Testing;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for OperatorTypeButton.xaml
    /// </summary>
    public partial class OperatorTypeButton : Button
    {
        public OperatorTypeButton()
        {
            InitializeComponent();
        }

        private readonly bool _namespaceVisible = true;

        public OperatorTypeButton(MetaOperator metaOp, bool namespaceVisible = true)
        {
            InitializeComponent();
            _namespaceVisible = namespaceVisible;

            MetaOp = metaOp;

            var nameBinding = new Binding
                                  {
                                      UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, 
                                      Source = metaOp, 
                                      Path = new PropertyPath("Name")
                                  };

            var combinedBinding = new MultiBinding { Converter = new BindingConverter() };
            combinedBinding.Bindings.Add(nameBinding);
            SetBinding(ContentProperty, combinedBinding);

            ToolTip = metaOp.Namespace;

            Click += OperatorButton_ClickHandler;
            MouseUp += OperatorButton_MouseUpHandler;

            if (metaOp.Outputs.Count > 0)
            {
                Background = new SolidColorBrush(UIHelper.ColorFromType(metaOp.Outputs[0].OpPart.Type)) { Opacity = 0.6 };
                Background.Freeze();
                Foreground = new SolidColorBrush(UIHelper.BrightColorFromType(metaOp.Outputs[0].OpPart.Type));
                Foreground.Freeze();
            }
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (!_namespaceVisible)
            {
                var textBlock = GetTemplateChild("PART_XNamespaceText") as TextBlock;
                if (textBlock != null)
                {
                    textBlock.Visibility = Visibility.Collapsed;
                }                                            
            }
        }


        private void OperatorButton_ClickHandler(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button != null)
            {
                var compoGraphView = App.Current.MainWindow.CompositionView.CompositionGraphView;
                compoGraphView.AddOperatorAtCenter(MetaOp);
                var qcw = UIHelper.FindParent<QuickCreateWindow>(this);
                if (qcw != null)
                {
                    qcw.Close();
                }
            }
        }

        private void OperatorButton_MouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var contextMenu = new ContextMenu();

            var item = new MenuItem { Header = "Duplicate" };
            item.Click += (o, a) => UIHelper.DuplicateOperatorTypeWithDialog(MetaOp);
            contextMenu.Items.Add(item);

            contextMenu.Items.Add(new Separator());

            item = new MenuItem { Header = "Run Tests" };
            item.Click += (o, a) =>
            {
                using (new Utils.SetWaitCursor())
                {
                    var result = TestUtilities.EvaluateTests(MetaOp.ID, "");
                    Logger.Info("Test results: " + result.Item2);
                }
            };
            contextMenu.Items.Add(item);
           
            item = new MenuItem { Header = "Find Usages" };
            item.Click += (o, a) =>
            {
                using (new Utils.SetWaitCursor())
                {
                    App.Current.MainWindow.ListMetaOpUsages(MetaOp);
                }
            };
            contextMenu.Items.Add(item);

            item = new MenuItem { Header = "Find Dependencies" };
            item.Click += (o, a) =>
            {
                using (new Utils.SetWaitCursor())
                {
                    App.Current.MainWindow.ListMetaOpDependencies(MetaOp);
                }
            };
            contextMenu.Items.Add(item);

            button.ContextMenu = contextMenu;
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            _mousePressed = true;
            _dragging = false;
        }

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (!_dragging &&
                _mousePressed &&
                e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) + Math.Abs(diff.Y)) > SystemParameters.MinimumHorizontalDragDistance)
            {
                // Initialize Drag & Drop operation
                _dragging = true;
                var dragData = new DataObject("METAOP", MetaOp);
                DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
            }
        }

        private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _mousePressed = false;
            _dragging = false;
        }


        public class BindingConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                return String.Format("{0}", values[0]);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }


        public MetaOperator MetaOp { get; private set; }
        private Point _startPoint;
        private bool _mousePressed;
        private bool _dragging;
    }
}
