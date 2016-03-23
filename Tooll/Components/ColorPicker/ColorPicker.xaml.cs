// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using AvalonDock;
using Framefield.Tooll.Utils;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;

namespace Framefield.Tooll
{
    public partial class ColorPickerView : DocumentContent
    {
        public IColorPickingComponent LinkedColorPickingComponent { get; set; }

        #region dependency properties

        public static readonly DependencyProperty AProperty = DependencyProperty.Register("A", typeof(float), typeof(ColorPickerView), new UIPropertyMetadata {DefaultValue = 0.4f});

        public float A
        {
            get { return (float) GetValue(AProperty); }
            set { SetValue(AProperty, value); }
        }

        // --- R -----
        public static readonly DependencyProperty RProperty = DependencyProperty.Register("R", typeof(float), typeof(ColorPickerView), new UIPropertyMetadata {DefaultValue = 1.0f, PropertyChangedCallback = OnRGBChanged});

        public float R
        {
            get { return (float) GetValue(RProperty); }
            set { SetValue(RProperty, value); }
        }

        //--- G ---------
        public static readonly DependencyProperty GProperty = DependencyProperty.Register("G", typeof(float), typeof(ColorPickerView), new UIPropertyMetadata {DefaultValue = 1.0f, PropertyChangedCallback = OnRGBChanged});

        public float G
        {
            get { return (float) GetValue(GProperty); }
            set { SetValue(GProperty, value); }
        }

        //--- B ---------
        public static readonly DependencyProperty BProperty = DependencyProperty.Register("B", typeof(float), typeof(ColorPickerView), new UIPropertyMetadata {DefaultValue = 1.0f, PropertyChangedCallback = OnRGBChanged});

        public float B
        {
            get { return (float) GetValue(BProperty); }
            set { SetValue(BProperty, value); }
        }


        private static void OnRGBChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = d as ColorPickerView;
            if (!view.m_DisableRGBUpdate)
            {
                view.UpdateHSVFromRGB();
                view.UpdateSliders();
            }
        }

        // V
        public static readonly DependencyProperty VProperty = DependencyProperty.Register("V", typeof(float), typeof(ColorPickerView), new UIPropertyMetadata {DefaultValue = 1.0f, PropertyChangedCallback = OnVChanged});

        public float V
        {
            get { return (float) GetValue(VProperty); }
            set { SetValue(VProperty, value); }
        }

        private static void OnVChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = d as ColorPickerView;
            if (view != null)
            {
                if (!view.m_DisableHSVUpdate)
                {
                    view.UpdateRgbFromHSV();
                }
            }
        }

        // S
        public static readonly DependencyProperty SProperty = DependencyProperty.Register("S", typeof(float), typeof(ColorPickerView), new UIPropertyMetadata(1.0f));

        public float S
        {
            get { return (float) GetValue(SProperty); }
            set { SetValue(SProperty, value); }
        }

        // H
        public static readonly DependencyProperty HProperty = DependencyProperty.Register("H", typeof(float), typeof(ColorPickerView), new UIPropertyMetadata(0.4f));

        public float H
        {
            get { return (float) GetValue(HProperty); }
            set { SetValue(HProperty, value); }
        }

        #endregion

        public ColorPickerView()
        {
            InitializeComponent();
            Title = "Color";

            BindingOperations.SetBinding(XVSlider, RangeBase.ValueProperty, new Binding("V") {Source = this});
            BindingOperations.SetBinding(XOpacitySlider, RangeBase.ValueProperty, new Binding("A") {Source = this});
            BindingOperations.SetBinding(XColorCircleThumb, OpacityProperty,
                new Binding("V") {Source = this});

            // Bind color fields
            var multiBinding = new MultiBinding();
            multiBinding.Converter = new RgbToBrushConverter();
            multiBinding.Bindings.Add(new Binding("R") { Source = this });
            multiBinding.Bindings.Add(new Binding("G") { Source = this });
            multiBinding.Bindings.Add(new Binding("B") { Source = this });
            BindingOperations.SetBinding(XColorThumb, Border.BackgroundProperty, multiBinding);

            UpdateSliders();
        }

        private float m_HueDragStarted;
        private float m_SaturationDragStarted;
        private Point m_DragStartPos;
        private bool _isDragging;

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            m_HueDragStarted = H;
            m_SaturationDragStarted = S;
            m_DragStartPos = new Point(e.HorizontalOffset, e.VerticalOffset);
            if (LinkedColorPickingComponent != null)
            {
                LinkedColorPickingComponent.StartColorManipulation();
            }
            _isDragging = true;
        }


        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var elem = sender as FrameworkElement;
            double min = Math.Min(elem.ActualHeight, elem.ActualWidth);

            double dx = ((e.HorizontalChange + m_DragStartPos.X) - 0.5*elem.ActualWidth)/min*2;
            double dy = ((e.VerticalChange + m_DragStartPos.Y) - 0.5*elem.ActualHeight)/min*2;

            double angle = Math.PI/2 - Math.Atan2(dx, -dy);
            H = (float) (angle*360/Math.PI/2);
            if (H < 0)
            {
                H += 360;
            }
            var length = Math.Pow( new Vector(dx, -dy).Length, 2);
            S = (float) Math.Min(1.0, length);

            UpdateRgbFromHSV();
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (LinkedColorPickingComponent != null)
            {
                LinkedColorPickingComponent.EndColorManipulation();
            }
            _isDragging = false;
        }


        private void UpdateRgbFromHSV()
        {
            m_DisableRGBUpdate = true;
            RGB rgb = RGB.FromHSV(H, S, V);
            R = Math.Min(1, Math.Max(0, rgb.R));
            G = Math.Min(1, Math.Max(0, rgb.G));
            B = Math.Min(1, Math.Max(0, rgb.B));
            UpdateLinkedColorField();
            UpdateSliders();
            m_DisableRGBUpdate = false;
        }

        private void UpdateHSVFromRGB()
        {
            m_DisableHSVUpdate = true;
            HSV hsv = HSV.FromRGB(R, G, B);
            H = hsv.H;
            S = Math.Min(1, Math.Max(0, hsv.S));
            V = Math.Min(1, Math.Max(0, hsv.V));
            UpdateSliders();
            m_DisableHSVUpdate = false;
        }

        private bool m_DisableRGBUpdate;
        private bool m_DisableHSVUpdate;

        private void UpdateSliders()
        {
            double minSize = Math.Min(XColorCircleThumb.ActualHeight, XColorCircleThumb.ActualWidth);

            XColorTranslateTransform.X = Math.Sin((H + 90) / 180 * Math.PI) * minSize * (float)Math.Pow(S, 0.5) * 0.5f;
            XColorTranslateTransform.Y = Math.Cos((H + 90) / 180 * Math.PI) * minSize * (float)Math.Pow(S, 0.5) * 0.5f;

            XOpacityGradientColor1.Color = UIHelper.ColorFromFloatRGBA(R, G, B, 0.0f);
            XOpacityGradientColor2.Color = UIHelper.ColorFromFloatRGBA(R, G, B, 1.0f);

            RGB brightRgb = RGB.FromHSV(H, S, 1.0f);
            XVSlider.Background = new SolidColorBrush(UIHelper.ColorFromFloatRGBA(brightRgb.R, brightRgb.G, brightRgb.B, 1.0f));
            XVSlider.Background.Freeze();
            if (_EnableTextUpdate)
            {
                _EnableTextUpdate = false;

                XRGBTextBox.Text = String.Format("{0:X2}{1:X2}{2:X2}", (int) (R*255), (int) (G*255), (int) (B*255));
                _EnableTextUpdate = true;
            }
        }

        private bool _EnableTextUpdate = true;

        private void UpdateLinkedColorField()
        {
            if (LinkedColorPickingComponent != null)
            {
                // Dragging update Command, Manipulation has been started in drag handler
                if (_isDragging)
                {
                    LinkedColorPickingComponent.ManipulateColor(R, G, B, A);
                    App.Current.UpdateRequiredAfterUserInteraction = true;
                }
                    // Single Click -> One command
                else
                {
                    LinkedColorPickingComponent.StartColorManipulation();
                    LinkedColorPickingComponent.ManipulateColor(R, G, B, A);
                    LinkedColorPickingComponent.EndColorManipulation();
                    App.Current.UpdateRequiredAfterUserInteraction = true;
                }
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_EnableTextUpdate)
            {
                if (XRGBTextBox.Text.Length == 6 || XRGBTextBox.Text.Length == 8)
                {
                    try
                    {
                        Color c = ColorTranslator.FromHtml("#" + XRGBTextBox.Text);
                        R = c.R/255.0f;
                        G = c.G/255.0f;
                        B = c.B/255.0f;
                    }
                    catch
                    {
                    }
                    UpdateHSVFromRGB();
                }
            }
            e.Handled = true;
        }

        private void SliderOnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isDragging = false;
            if( LinkedColorPickingComponent != null)
                LinkedColorPickingComponent.EndColorManipulation();
        }

        private void SliderOnDragStarted(object sender, DragStartedEventArgs e)
        {
            _isDragging = true;
            if(LinkedColorPickingComponent != null)
                LinkedColorPickingComponent.StartColorManipulation();
        }

        private void OnColorSet(object sender, EventArgs e)
        {
            UpdateLinkedColorField();
        }
    }
}