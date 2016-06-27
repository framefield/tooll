// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for TextButtonEdit.xaml
    /// </summary>
    public partial class TextButtonEdit : UserControl
    {
        public TextButtonEdit()
        {
            InitializeComponent();
            CreateBindings();
            AddMouseHandler();
        }

        public TextButtonEdit(string text)
        {
            InitializeComponent();

            CreateBindings();
            AddMouseHandler();
            Text = text;
        }

        public bool DropFocusAfterEdit = true;
        private bool _allowLinebreaks = false;

        private static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register("Watermark", typeof(string), typeof(TextButtonEdit), new UIPropertyMetadata(""));
        public string Watermark
        {
            get
            {
                return (string)GetValue(WatermarkProperty);
            }
            set
            {
                SetValue(WatermarkProperty, value);
            }
        }

        public void EnableLineBreaks() {
            _allowLinebreaks = true;
            XButtonText.TextWrapping = TextWrapping.Wrap;
            XTextEdit.TextWrapping = TextWrapping.Wrap;
            XTextEdit.AcceptsReturn = true;
            XButton.MaxHeight = 400;
            XButton.VerticalContentAlignment = System.Windows.VerticalAlignment.Top;
        } 

        private static readonly DependencyProperty DefaultProperty = DependencyProperty.Register("Default", typeof(string), typeof(TextButtonEdit), new UIPropertyMetadata(""));
        public string Default
        {
            get
            {
                return (string)GetValue(DefaultProperty);
            }
            set
            {
                SetValue(DefaultProperty, value);
            }
        }

        protected Button Button { get { return XButton; } }
        protected TextBox TextEdit { get { return XTextEdit; } }

        public delegate void ValueChangedToDefaultDelegate();
        public event ValueChangedToDefaultDelegate ValueChangedToDefaultEvent;

        public delegate void EditingStartedDelegate();
        public event EditingStartedDelegate EditingStartedEvent;

        public event EventHandler<EventArgs> EditingUpdatedEvent;

        public delegate void EditingCompletedDelegate();
        public event EditingCompletedDelegate EditingCompletedEvent;

        private string m_Text;

        public string Text
        {
            get { return m_Text; }
            set
            {
                m_Text = value;
                TextEdit.Text = value;
            }
        }

        protected void AddMouseHandler()
        {
            Button.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Button_MouseLeftButtonDown), true);
            Button.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Button_MouseLeftButtonUp), true);
            Button.AddHandler(MouseMoveEvent, new MouseEventHandler(Button_MouseMove), true);
            /* 
             * PreviewMouseDownEvent required, because the 
             * System.Windows.Controls.TextBox MouseLeftButtonUpEvent is buggy
             * -> e.ClickCount always stays at "1"
             */
            TextEdit.AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(TextEdit_PreviewMouseDown), true);
        }

        protected void TextEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            if (EditingCompletedEvent != null)
            {
                EditingCompletedEvent();
            }
            Button.Visibility = Visibility.Visible;
            Button.Focusable = true;


            if (DropFocusAfterEdit)
            {
                var operatorWidget = App.Current.MainWindow.CompositionView.XCompositionGraphView.SelectionHandler
                        .FirstSelectedElementsOfType<OperatorWidget>();
                if (operatorWidget != null)
                {
                    operatorWidget.Focusable = true;
                    operatorWidget.Focus();
                }

            }
            TextEdit.Visibility = Visibility.Collapsed;
        }
        
        protected void TextEdit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 3)
            {
                XTextEdit.SelectAll();
            }
        }

        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_MouseDownOnButton = true;
            m_ValueModified = false;
            m_MouseRefPosition = e.GetPosition(this);
        }

        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_MouseDownOnButton = false;
            if (!m_ValueModified)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (ValueChangedToDefaultEvent != null)
                    {
                        ValueChangedToDefaultEvent();
                    }
                }
                else
                {
                    if(IsEnabled)   // editting can be disabled if connected
                        EnableTextEdit();
                }
            }
            m_ValueModified = false;
        }

        protected void Button_MouseMove(object sender, MouseEventArgs e)
        {
            var diff = e.GetPosition(this).X - m_MouseRefPosition.X;
            if (m_MouseDownOnButton)
            {

                if (!m_ValueModified && Math.Abs(diff) > CLICK_THRESHOLD)
                {
                    m_ValueModified = true;
                }
            }
        }

        private bool m_MouseDownOnButton = false;
        private bool m_ValueModified = false;
        private Point m_MouseRefPosition = new Point();
        private const float CLICK_THRESHOLD = 5;

        private void TextButtonEditKeyUp_Handler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape 
            || (e.Key == Key.Enter && (!_allowLinebreaks || Keyboard.Modifiers.HasFlag( ModifierKeys.Control))))
            {
                Button.Visibility = Visibility.Visible;
                Button.Focusable = true;
                Button.Focus();
            }

            if (EditingUpdatedEvent != null)
            {
                EditingUpdatedEvent(sender, e);
            }

            e.Handled = true;
        }

        private void ButtonKeyUp_Handler(object sender, KeyEventArgs e) 
        {
            if (e.Key == Key.Enter) 
            {                
                var o = UIHelper.FindParent<TextButtonEdit>(sender as UIElement);
                if (o != null) {                    
                    o.EnableTextEdit();
                }
            }
        }

        public void EnableTextEdit()
        {
            Button.Visibility = Visibility.Collapsed;
            TextEdit.Visibility = Visibility.Visible;
            TextEdit.SelectAll();
            TextEdit.Focus();
            if (EditingStartedEvent != null)
            {
                EditingStartedEvent();
            }
        }

        private void CreateBindings()
        {
            // Watermark text
            MultiBinding multiBinding = new MultiBinding();
            multiBinding.Converter = m_TextAndWatermarkToButtonTextConverter;
            multiBinding.Bindings.Add(new Binding("Text") { Source = XTextEdit });
            multiBinding.Bindings.Add(new Binding("Watermark") { Source = textEditButton });
            BindingOperations.SetBinding(XButtonText, TextBlock.TextProperty, multiBinding);

            // Watermark color
            MultiBinding multiBinding2 = new MultiBinding();
            multiBinding2.Converter = m_WatermarkToColorConverter;
            multiBinding2.Bindings.Add(new Binding("Text") { Source = XTextEdit });
            multiBinding2.Bindings.Add(new Binding("Default") { Source = textEditButton });
            BindingOperations.SetBinding(XButton, Button.ForegroundProperty, multiBinding2);
        }

        #region Value converter
        static TextWatermarkToButtonTextConverter m_TextAndWatermarkToButtonTextConverter = new TextWatermarkToButtonTextConverter();
        public class TextWatermarkToButtonTextConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (values.Count() != 2 || values.Contains(DependencyProperty.UnsetValue))
                {
                    return "binding error";
                }

                string text = (string)values[0];
                string watermark = (string)values[1];
                if (text != "")
                {
                    return text;
                }
                else
                {
                    return watermark;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
                System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        static TextWatermarkToColorConverter m_WatermarkToColorConverter = new TextWatermarkToColorConverter();
        public class TextWatermarkToColorConverter : IMultiValueConverter
        {
            static private SolidColorBrush white = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            static private SolidColorBrush faded = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));

            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (values.Count() != 2 || values.Contains(DependencyProperty.UnsetValue))
                {
                    return "binding error";
                }

                string text = (string)values[0];
                string def = (string)values[1];

                if (text != def)
                {
                    return white;
                }
                else
                {
                    return faded;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
                System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
