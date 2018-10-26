// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for TimeView.xaml
    /// </summary>
    public partial class TimeView : UserControl, IValueSnapAttractor
    {
        public class State
        {
            public double StartTime { get; set; }
            public double EndTime { get; set; }
            public double TimeOffset { get; set; }
            public double TimeScale { get; set; }
            public bool LoopMode { get; set; }
        }


        public TimeView()
        {
            InitializeComponent();
            XTimelineThumb.Cursor = Cursors.ScrollWE;

            m_TimeToXConverter = new TimeToXConverter(this);

            TimeSnapHandler.AddSnapAttractor(this);
            TimeSnapHandler.SnappedEvent += TimeSnapHandler_SnappedEventHandler;

            TimeScale = 13;
            TimeOffset = -2;

            CreateBindings();
        }

        void TimeSnapHandler_SnappedEventHandler(object sender, ValueSnapHandler.SnapEventArgs e)
        {
            XTimeSnapMarker.Visibility = Visibility.Visible;

            var _snapMarkerAnimation = new DoubleAnimation() { From = 0.8, To = 0, Duration = TimeSpan.FromSeconds(0.4) };
            _snapMarkerAnimation.BeginAnimation(UIElement.OpacityProperty, _snapMarkerAnimation);

            XTimeSnapMarker.RenderTransform = new TranslateTransform(TimeToX(e.Value), 0);
            XTimeSnapMarker.Opacity = 1;


            XTimeSnapMarker.BeginAnimation(UIElement.OpacityProperty, _snapMarkerAnimation);
        }



        #region Properties        
        public static readonly DependencyProperty TimeOffsetProperty = DependencyProperty.Register(
          "TimeOffset",
            typeof(double),
            typeof(TimeView),
            new FrameworkPropertyMetadata(-2.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double TimeOffset
        {
            get { return (double)GetValue(TimeOffsetProperty); }
            set
            {
                SetValue(TimeOffsetProperty, value);
                //force to update playhead position
                //App.Current.Model.GlobalTime = App.Current.Model.GlobalTime;
            }
        }


        public static readonly DependencyProperty TimeScaleProperty = DependencyProperty.Register("TimeScale", typeof(double), typeof(TimeView),
            new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double TimeScale
        {
            get { return (double)GetValue(TimeScaleProperty); }
            set
            {
                SetValue(TimeScaleProperty, value);
                //App.Current.Model.GlobalTime = App.Current.Model.GlobalTime;
            }
        }

        public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register("StartTime", typeof(double), typeof(TimeView), new UIPropertyMetadata(0.0));
        public double StartTime
        {
            get { return (double)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }

        public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register("EndTime", typeof(double), typeof(TimeView), new UIPropertyMetadata(30.0));
        public double EndTime
        {
            get { return (double)GetValue(EndTimeProperty); }
            set { SetValue(EndTimeProperty, value); }
        }

        public static readonly DependencyProperty LoopModeProperty = DependencyProperty.Register("LoopMode", typeof(bool), typeof(TimeView), new UIPropertyMetadata(false));
        public bool LoopMode
        {
            get { return (bool)GetValue(LoopModeProperty); }
            set { SetValue(LoopModeProperty, value); }
        }
        #endregion



        public State CreateState()
        {
            return new State()
            {
                StartTime = StartTime,
                EndTime = EndTime,
                TimeOffset = TimeOffset,
                TimeScale = TimeScale,
                LoopMode = LoopMode
            };
        }

        public void ApplyState(State state)
        {
            StartTime = state.StartTime;
            EndTime = state.EndTime;
            TimeOffset = state.TimeOffset;
            TimeScale = state.TimeScale;
            LoopMode = state.LoopMode;
        }


        #region implement snap attractor
        const double SNAP_THRESHOLD = 8;


        public SnapResult CheckForSnap(double time)
        {
            double distanceToTime = Math.Abs(time - App.Current.Model.GlobalTime) * TimeScale;
            if (distanceToTime < SNAP_THRESHOLD)
            {
                return new SnapResult() { SnapToValue = App.Current.Model.GlobalTime, Force = distanceToTime };
            }

            double distanceToOrigin = Math.Abs(time) * TimeScale;
            if (distanceToOrigin < SNAP_THRESHOLD)
            {
                return new SnapResult() { SnapToValue = 0.0, Force = distanceToOrigin };
            }

            return null;
        }
        #endregion


        public ValueSnapHandler TimeSnapHandler { get { return _timeSnapHandler; } }
        private readonly ValueSnapHandler _timeSnapHandler = new ValueSnapHandler();

        private void CreateBindings()
        {
            // Playhead
            MultiBinding plagheadBinding = new MultiBinding();
            plagheadBinding.Converter = new TimeScaleOffsetToXConverter();
            plagheadBinding.Bindings.Add(new Binding("GlobalTime") { Source = App.Current.Model });
            plagheadBinding.Bindings.Add(new Binding("TimeScale") { Source = timeView });
            plagheadBinding.Bindings.Add(new Binding("TimeOffset") { Source = timeView });
            plagheadBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(XPlayhead.XPlayheadTranslateTransform, TranslateTransform.XProperty, plagheadBinding);

            // PlayheadMarker
            MultiBinding plagheadMarkerBinding = new MultiBinding();
            plagheadMarkerBinding.Converter = new TimeScaleOffsetToXConverter();
            plagheadMarkerBinding.Bindings.Add(new Binding("GlobalTime") { Source = App.Current.Model });
            plagheadMarkerBinding.Bindings.Add(new Binding("TimeScale") { Source = timeView });
            plagheadMarkerBinding.Bindings.Add(new Binding("TimeOffset") { Source = timeView });
            plagheadMarkerBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(XPlayheadMarker.XPlayheadTranslateTransform, TranslateTransform.XProperty, plagheadMarkerBinding);

            // StartTimeMarker
            MultiBinding multiBinding = new MultiBinding();
            multiBinding.Converter = StartTimeMarker.m_TimeToLeftMarginConverter;
            multiBinding.Bindings.Add(new Binding("StartTime") { Source = timeView });
            multiBinding.Bindings.Add(new Binding("TimeScale") { Source = timeView });
            multiBinding.Bindings.Add(new Binding("TimeOffset") { Source = timeView });
            multiBinding.Bindings.Add(new Binding("ActualWidth") { Source = timeView });
            multiBinding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(XStartTimeMarker, Canvas.MarginProperty, multiBinding);

            // EndTimeMarker
            MultiBinding endTimeBinding = new MultiBinding();
            endTimeBinding.Converter = m_TimeToRightMarginConverter;
            endTimeBinding.Bindings.Add(new Binding("EndTime") { Source = timeView });
            endTimeBinding.Bindings.Add(new Binding("TimeScale") { Source = timeView });
            endTimeBinding.Bindings.Add(new Binding("TimeOffset") { Source = timeView });
            BindingOperations.SetBinding(XEndTimeMarker, Canvas.MarginProperty, endTimeBinding);


            // TimeImage
            MultiBinding timeImageBinding = new MultiBinding();
            timeImageBinding.Converter = new TimeScaleToXConverter();
            timeImageBinding.Bindings.Add(new Binding("TimeScale") { Source = timeView });
            timeImageBinding.Bindings.Add(new Binding("TimeOffset") { Source = timeView });
            BindingOperations.SetBinding(XTimeImage.XTimeImageTranslateTransform, TranslateTransform.XProperty, timeImageBinding);


            // TimeImage
            MultiBinding timeImageWidthBinding = new MultiBinding();
            timeImageWidthBinding.Converter = new TimeScaleDurationToWidthConverter();
            timeImageWidthBinding.Bindings.Add(new Binding("TimeScale") { Source = timeView });
            timeImageWidthBinding.Bindings.Add(new Binding("TimeScale") { Source = timeView });
            BindingOperations.SetBinding(XTimeImage.XTimeImageScaleTransform, ScaleTransform.ScaleXProperty, timeImageWidthBinding);
        }


        public void TriggerRepaint()
        {
            timelineRuler.InvalidateVisual();
            XAnimationCurveEditor.InvalidateVisual();
            XAnimationCurveEditor.UpdateLines();
        }


        #region eventhandlers
        private void OnDragTimelineStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            m_DragStartPosition = new Point(e.HorizontalOffset, e.VerticalOffset);
            m_DragStartTimeOffset = TimeOffset;

            double currentTime = XToTime(e.HorizontalOffset);
            var TV = App.Current.MainWindow.CompositionView.XTimeView;
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                double snapTime = TV.TimeSnapHandler.CheckForSnapping(currentTime, new List<IValueSnapAttractor>() { this, XPlayhead });
                if (!Double.IsNaN(snapTime))
                {
                    currentTime = snapTime;
                }
            }
            App.Current.MainWindow.CompositionView.XCompositionToolBar.ManipulateTime(currentTime);
        }

        private void OnDragTimelineDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double currentTime = XToTime(m_DragStartPosition.X + e.HorizontalChange);

            var TV = App.Current.MainWindow.CompositionView.XTimeView;
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                double snapTime = TV.TimeSnapHandler.CheckForSnapping(currentTime, new List<IValueSnapAttractor>() { this, XPlayhead });
                if (!Double.IsNaN(snapTime))
                {
                    currentTime = snapTime;
                }
            }
            App.Current.MainWindow.CompositionView.XCompositionToolBar.ManipulateTime(currentTime);
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                UIElement el = sender as UIElement;
                if (el != null)
                {
                    el.CaptureMouse();
                    m_DragStartPosition = e.GetPosition(this);
                    m_DragStartTimeOffset = TimeOffset;
                    m_IsRightMouseDragging = true;
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (m_IsRightMouseDragging)
            {
                TimeOffset = m_DragStartTimeOffset + (m_DragStartPosition.X - e.GetPosition(this).X) / TimeScale;
                TriggerRepaint();
            }
            else if (m_IsLeftMouseDragging)
            {
                double currentTime = XToTime(m_DragStartPosition.X + e.GetPosition(this).X - m_DragStartPosition.X);

                var TV = App.Current.MainWindow.CompositionView.XTimeView;
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    double snapTime = TV.TimeSnapHandler.CheckForSnapping(currentTime, new List<IValueSnapAttractor>() { this, XPlayhead });
                    if (!Double.IsNaN(snapTime))
                    {
                        currentTime = snapTime;
                    }
                }
                App.Current.MainWindow.CompositionView.XCompositionToolBar.ManipulateTime(currentTime);
            }
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_IsRightMouseDragging = false;
            UIElement el = sender as UIElement;
            if (el != null)
                el.ReleaseMouseCapture();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double mouseWheelZoomSpeed = 1.15;
            double scale = (e.Delta > 0) ? mouseWheelZoomSpeed : 1.0 / mouseWheelZoomSpeed;
            TimeScale *= scale;
            TimeOffset += (scale - 1.0) * (XToTime(ActualWidth) - XToTime(0)) * (e.GetPosition(this).X / ActualWidth);
            TriggerRepaint();
        }
        #endregion


        public double XToTime(double x)
        {
            return m_TimeToXConverter.XToTime(x);
        }

        public double TimeToX(double t)
        {
            return m_TimeToXConverter.TimeToX(t);
        }





        #region members
        private TimeToXConverter m_TimeToXConverter;
        private Point m_DragStartPosition;
        private double m_DragStartTimeOffset;
        private bool m_IsRightMouseDragging = false;
        private bool m_IsLeftMouseDragging = false;
        #endregion

        #region Value converter


        static TimeToRightMarginConverter m_TimeToRightMarginConverter = new TimeToRightMarginConverter();
        public class TimeToRightMarginConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (values.Count() != 3 || values.Contains(DependencyProperty.UnsetValue))
                {
                    return "binding error";
                }

                double time = (double)values[0];
                double timeScale = (double)values[1];
                double timeOffset = (double)values[2];

                double x = (time - timeOffset) * timeScale;
                return new Thickness(x, 0, 0, 0);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
                System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_IsLeftMouseDragging = true;
            m_DragStartPosition = e.GetPosition(this);
            m_DragStartTimeOffset = TimeOffset;

            double currentTime = XToTime(m_DragStartPosition.X);
            var TV = App.Current.MainWindow.CompositionView.XTimeView;
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                double snapTime = TV.TimeSnapHandler.CheckForSnapping(currentTime, new List<IValueSnapAttractor>() { this, XPlayhead });
                if (!Double.IsNaN(snapTime))
                {
                    currentTime = snapTime;
                }
            }
            App.Current.MainWindow.CompositionView.XCompositionToolBar.ManipulateTime(currentTime);
            e.Handled = true;
            UIElement el = sender as UIElement;
            if (el != null)
                el.CaptureMouse();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_IsLeftMouseDragging = false;
            UIElement el = sender as UIElement;
            if (el != null)
                el.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void GotFocusHandler(object sender, RoutedEventArgs e)
        {
            this.BorderThickness = new Thickness(1);
            this.BorderBrush = Brushes.DarkGray;
        }

        private void LostFocusHandler(object sender, RoutedEventArgs e)
        {
            this.BorderThickness = new Thickness(1);
            this.BorderBrush = Brushes.Black;
        }


        private async void XTimelineThumb_OnDrop(object sender, DragEventArgs e)
        {
            var fileList = ((DataObject)e.Data).GetFileDropList();
            if (fileList == null || fileList.Count != 1)
                return;

            var soundFilePath = fileList[0];
            soundFilePath = UIHelper.ConvertToRelativeFilepath(soundFilePath);
            if (!soundFilePath.EndsWith(".mp3") && !soundFilePath.EndsWith(".ogg"))
            {
                MessageBox.Show("You can only drop .mp3, .ogg, and timeline-images here.");
                return;
            }

            var imageFilePath = await GenerateSoundImageInBackground(soundFilePath);
            if (imageFilePath == null)
            {
                Logger.Warn("Creating sound image failed.");
            }

            var bpmMatchResult = Regex.Match(soundFilePath, @"(\d+)bpm");
            if (bpmMatchResult.Success)
            {
                var bpm = (float)Double.Parse(bpmMatchResult.Groups[1].Value);
                if (!Double.IsNaN(bpm))
                {
                    Logger.Info("Setting {0} BPM  from filename..", bpm);
                    XBeatMarker.BPM = bpm;
                    XBeatMarker.BPMTimeOffset = 0;

                    App.Current.ProjectSettings["Soundtrack.BPM"] = bpm;
                    App.Current.ProjectSettings["Soundtrack.BPMOffset"] = 0;
                    //App.Current.UpdateRequiredAfterUserInteraction = true;
                    this.InvalidateVisual();
                }
            }

            XTimeImage.SetTimelineImagePath(imageFilePath);
            App.Current.SetProjectSoundFile(soundFilePath);
            Logger.Info("Sound-Image completed");
            App.Current.ProjectSettings.Save();
        }

        async Task<string> GenerateSoundImageInBackground(string filename)
        {
            return await Task.Run(() =>
            {
                var imageGenerator = new Components.Helper.SoundImageGenerator();

                var imageFilePath = imageGenerator.GenerateSoundSpectrumAndVolume(filename);

                if (imageFilePath == null)
                {
                    return null;
                }
                return imageFilePath;
            });
        }
    }


    #region Value converter
    [ValueConversion(typeof(double), typeof(double))]
    public class TimeToXConverter : IValueConverter
    {
        public TimeToXConverter(TimeView cv)
        {
            m_TimeView = cv;
        }

        public double TimeToX(double t)
        {
            return (t - m_TimeView.TimeOffset) * m_TimeView.TimeScale;
        }

        public double XToTime(double x)
        {
            return x / m_TimeView.TimeScale + m_TimeView.TimeOffset;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return TimeToX((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return XToTime((double)value);
        }

        private TimeView m_TimeView;
    }



    #endregion



}
