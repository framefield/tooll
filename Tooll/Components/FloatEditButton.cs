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
using System.Globalization;
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// Component that combines button functionality with a button to quickly edit float values.
    /// 
    /// The XAML template is defined in Generic.xaml
    /// </summary>
    public class FloatEditButton : Control
    {
        public float Scale { get; set; }
        public int Precision { get; set; }
        public bool ClampToMin { get; set; }
        public bool ClampToMax { get; set; }
        public float Default { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public bool DisplayAsTimeStamp { get; set; }
        public virtual bool IsLocked() { return false; }

        public FloatEditButton()
        {
            Scale = 0.1f;
            Precision = 4;

            Min = 0.0f;
            Max = 10.0f;
            ClampToMin = false;
            ClampToMax = false;

            Loaded += LoadedHandler;

            System.Windows.Resources.StreamResourceInfo info = Application.GetResourceStream(new Uri("Images/cursors/CursorSlideNormal.cur", UriKind.Relative));
            this.Cursor = new System.Windows.Input.Cursor(info.Stream);
        }

        #region dependency properties
        public float Value { get { return (float) GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(float), typeof(FloatEditButton),
            new UIPropertyMetadata() { DefaultValue = float.NaN, PropertyChangedCallback = OnValueChanged });

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var obj = d as FloatEditButton;
            if (obj != null) {
                if(obj._textBlock != null)
                    obj._textBlock.Text= obj.ValueAsString();
                
                obj.updateSlider();
            }
        }
        #endregion    

        #region setup custom control
        static FloatEditButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FloatEditButton), new FrameworkPropertyMetadata(typeof(FloatEditButton)));
        }

        /**
         * Since we're using a CostumControl, we need to get the relevant UI children for the current instance
         * for changing their properties later and assigning eventhandlers.
         */
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _textBox = GetTemplateChild("PART_TextBox") as TextBox;  // NOTE: FindName("PART_TextBox");  does NOT work here

            MouseLeftButtonDown+= MouseLeftButtonDownHandler;
            MouseLeftButtonUp+= MouseLeftButtonUpHandler;
            MouseMove+= MouseMoveHandler;
            MouseWheel+= MouseWheelHandler;
            LayoutUpdated+=LayoutUpdatedHandler;            

            if (_textBox !=null) {
                _textBox.TextChanged += TextChangedHandler;
                _textBox.KeyUp += KeyUpHandler;
                _textBox.LostFocus += LostFocusHandler;
            }

            _sliderFillRectangle =    GetTemplateChild("PART_SliderFillRectangle") as Rectangle;
            _sliderDefaultRectangle = GetTemplateChild("PART_SliderDefaultRectangle") as Rectangle;
            _sliderMarkerRectangle =  GetTemplateChild("PART_SliderMarkerRectangle") as Rectangle;
            _textBlock = GetTemplateChild("PART_TextBlock") as TextBlock;
            //updateSlider();
        }

        #endregion


        #region events

        /**
        * attach to this event to be notified on user manipulation of this value
        */
        public delegate void ValueChangedDelegate(float val);
        public event ValueChangedDelegate ValueChangedEvent;

        public delegate void EditingStartedDelegate();
        public event EditingStartedDelegate EditingStartedEvent;

        public delegate void EditingEndedDelegate();
        public event EditingEndedDelegate EditingEndedEvent;

        public delegate void EditingCanceledDelegate();
        public event EditingCanceledDelegate EditingCancelledEvent;

        public delegate void ResetToDefaultDelegate();
        public event ResetToDefaultDelegate ResetToDefaultEvent;

        /// <summary>
        /// To wrap manipulation of Value-parameters into valid Undo/Redo-Commands,
        /// all Classes that call ManipulateValue have to call the following functions...
        /// </summary>
        public void StartManipulation() {
            _manipulating = true;
            if (EditingStartedEvent != null)
                EditingStartedEvent();
        }
        public void EndManipulation() {
            _manipulating = false;
            if (EditingEndedEvent != null)
                EditingEndedEvent();
        }
        public void CancelManipulation() {
            _manipulating = false;
            if (EditingCancelledEvent != null)
                EditingCancelledEvent();
        }
        private bool _manipulating = false;

        /**
         * Value has been manually modified we have raise the ValueChangedEvent to notify
         * connected listers to update the model and potential animations.
         */
        public void ManipulateValue(float newValue) {
            if (IsLocked())
                return;

            if (!_manipulating)
            {
                Logger.Info("Can't manipulate float value without calling StartManipulation() first.");
                return;
            }
                

            if (ClampToMin)
                newValue = Math.Max(Min, newValue);
            if (ClampToMax)
                newValue = Math.Min(Max, newValue);

            Value = newValue;

            if (ValueChangedEvent != null) {
                ValueChangedEvent(Value);
            }
        }

        public void ResetToDefault() {
            if (ResetToDefaultEvent != null)
                ResetToDefaultEvent();
        }
        #endregion


        #region XAML Handlers
        private void TextChangedHandler(object sender, TextChangedEventArgs e) {
            if (_textBox != null && _textBox.Visibility == Visibility.Visible)
                UpdateValueFromTextEdit();

            e.Handled= true;
        }

        private void LostFocusHandler(object sender, RoutedEventArgs e) {
            if (UpdateValueFromTextEdit())
            {
                EndManipulation();
            }
            else
            {
                /**
                 * FIXME: Sadly, manipulating _textBox.Text will implicitly trigger a Manipulate event
                 * which means that the original value is overwritten with a new interpation of the
                 * current one and so, "default" values are replaced by values.
                 **/
                _textBox.Text = Value.ToString();  
                CancelManipulation();
            }

            _textBox.Visibility = Visibility.Collapsed;
            e.Handled= true;
        }

        private void KeyUpHandler(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) 
            {
                if (!_manipulating) 
                {
                    Core.Logger.Warn("Ignored untracked manipulation of FloatEditControl. Please report this bug.");
                }
                else if (!UpdateValueFromTextEdit())
                {
                    _textBox.Text = Value.ToString();
                }

                _textBox.Visibility = Visibility.Collapsed;
                e.Handled= true;
            }
            if (e.Key == Key.Escape)
            {
                _textBox.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

        /// <summary>
        ///  Tries to update the value from the current XTextEdit.Text. If conversion fails value stays unchanged.
        /// </summary>
        /// <returns>true if conversion was successful otherwise false</returns>
        private bool UpdateValueFromTextEdit() {
            float newValue = Value;
            if (Single.TryParse(_textBox.Text, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out newValue)) {
                ManipulateValue(newValue);
                return true;
            }
            return false;
        }



        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount > 1)
            {
                Logger.Info("Ignoring double click on parameter.");
            }
            
            var b = Mouse.Capture(this);
            m_MousePressedOnButton = true;
            m_DragHasModifiedValue = false;
            m_MouseRefPosition = e.GetPosition(this);
            m_ValueRefOnDown = Value;
            e.Handled = true;
            StartManipulation();
        }

        protected void MouseMoveHandler(object sender, MouseEventArgs e) {
            if (m_MousePressedOnButton) {
                var diff = e.GetPosition(this).X - m_MouseRefPosition.X;
                if (!m_DragHasModifiedValue && Math.Abs(diff) > SystemParameters.MinimumHorizontalDragDistance) {
                    m_DragHasModifiedValue = true;
                }

                if (m_DragHasModifiedValue) {                    
                    ManipulateValue(m_ValueRefOnDown + (float) (diff * Scale * UIHelper.SubScaleFromKeyboardModifiers()));
                }
            }
            e.Handled = true;
        }

        private void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e) 
        {
            Mouse.Capture(null);
            m_MousePressedOnButton = false;
            if (!m_DragHasModifiedValue) 
            {
                if (Keyboard.Modifiers == ModifierKeys.Control) 
                {
                    CancelManipulation();
                    ResetToDefault();
                }
                else if(!IsLocked()) {
                    CancelManipulation();
                    if (_textBox.IsVisible)
                    {                        
                        Logger.Info("Ignoring switch to edit-mode again.");
                    }
                    else
                    {
                        SwitchToTextEdit();                        
                    }
                }
            }
            else
            {
                EndManipulation();
            }

            m_DragHasModifiedValue = false;
            e.Handled = true;
        }

        private void MouseWheelHandler(object sender, MouseWheelEventArgs e) {
            float delta = (e.Delta > 0) ? 0.1f : -0.1f;
            if (!_textBox.IsVisible && !_manipulating) {
                StartManipulation();
                ManipulateValue(Value + Scale * (float)(delta * UIHelper.SubScaleFromKeyboardModifiers()));
                EndManipulation();
            }
            e.Handled = true;
        }


        void LayoutUpdatedHandler(object sender, EventArgs e) {
            updateSlider();
        }

        void LoadedHandler(object sender, RoutedEventArgs e) {
            if(_textBlock != null)
                _textBlock.Text = this.ValueAsString();            
        }
        #endregion



        public void SwitchToTextEdit()
        {
            _textBox.Text = ValueAsString();
            _textBox.Visibility = Visibility.Visible;
            _textBox.Focus();
            _textBox.SelectAll();
            StartManipulation();
        }

        #region private helper methods
        private string ValueAsString() {
            if (DisplayAsTimeStamp) {
                return ((int) (Value / 60 % 60)).ToString("D2") + ":" +
                       ((int) (Value      % 60)).ToString("D2") + ":" +
                       ((int) (Value * 30 % 30)).ToString("D2");
            }
            else {
                String formatter= String.Format("G{0:D}", Precision);
                return Value.ToString(formatter, CultureInfo.InvariantCulture);
            }
        }


        private void updateSlider() {
            if (_sliderDefaultRectangle == null || _sliderFillRectangle == null || _sliderMarkerRectangle == null)
                return;

            double x;
            double zeroX;
            double defaultX;
            double width= this.ActualWidth -2;
            double MAX_VALUE = 100000;

            if (Value == float.NaN || Value == float.PositiveInfinity || Value == float.NegativeInfinity) {
                return;
            }

            x=  (Value - Min) / (Max - Min) * width;
            zeroX = (-Min) / (Max - Min) * width;
            defaultX = (Default - Min) / (Max - Min) * width;


            if (Math.Abs(x) > MAX_VALUE || Math.Abs(zeroX) > MAX_VALUE || defaultX > MAX_VALUE) {
                //throw new System.InvalidOperationException("Computed slider ranges exceeds value range.");
                return;
            }

            if (x < zeroX) {
                Canvas.SetLeft(_sliderFillRectangle, x);
                _sliderFillRectangle.Width = zeroX - x;
            }
            else {
                Canvas.SetLeft(_sliderFillRectangle, zeroX);
                _sliderFillRectangle.Width = x - zeroX;
            }

            Canvas.SetLeft(_sliderMarkerRectangle, x);
            Canvas.SetLeft(_sliderDefaultRectangle, defaultX);
        }
        #endregion


        #region private members
        private bool m_MousePressedOnButton = false;    // required to emulate drag in MouseMove
        private bool m_DragHasModifiedValue = false;
        private Point m_MouseRefPosition = new Point();
        private float m_ValueRefOnDown;


        private TextBox _textBox;
        private TextBlock _textBlock;
        private Rectangle _sliderFillRectangle;
        private Rectangle _sliderMarkerRectangle;
        private Rectangle _sliderDefaultRectangle;

        #endregion
    }
}
