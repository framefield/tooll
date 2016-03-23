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
    /// Interaction logic for TimeMarkerControl.xaml
    /// </summary>
    public partial class TimeMarkerControl : UserControl, IValueSnapAttractor
    {
        public TimeMarkerControl() {
            InitializeComponent();
            //CreateMappingShapeGeometry();
        }

        public static readonly DependencyProperty ClipNameProperty = DependencyProperty.Register("ClipName", typeof(string), typeof(TimeMarkerControl), new UIPropertyMetadata(""));
        public string ClipName {
            get { return (string) GetValue(ClipNameProperty); }
            set { SetValue(ClipNameProperty, value); }
        }


        /**
         *  Note: This is a very unfortunate solution to get the Operator title into the TimeMarkerControl.
         *  To make this possible, we have to introduce ClipNamePropertry as a mediator and bind this to 
         *  the OperatorWidget. To make it worse, the OperatorWidget is only available in the DataContext 
         *  after TimeMarkerControl ist loaded.
         *  
         *  This function creates the second binding in the chain:
         *  
         *    TimeMarkerControl.TextBlock.Text ---> TimeMarkerControl.ClipName  ---> DataContext(ViewModel).OperatorWidget.XOperatorLabel.Text
         */
        private void CreateNameBinding() {
            TimeMarkerViewModel vm = DataContext as TimeMarkerViewModel;
            if (vm != null) {

                var nameBinding = new Binding() {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Source = vm.OperatorWidget.XOperatorLabel,
                    Path = new PropertyPath("Text"),
                };
                this.SetBinding(TimeMarkerControl.ClipNameProperty, nameBinding);
            }
        }


        #region implement snap attractor
        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (TV != null) {
                TV.TimeSnapHandler.AddSnapAttractor(this);
            }

            CreateNameBinding();
        }


        private void OnUnloaded(object sender, RoutedEventArgs e) {
            if (TV != null) {
                TV.TimeSnapHandler.RemoveSnapAttractor(this);
            }            
        }

        const double SNAP_THRESHOLD = 8;

        public SnapResult CheckForSnap(double time) {
            TimeMarkerViewModel vm = DataContext as TimeMarkerViewModel;
            if (vm != null) {

                double distanceToStartTime = Math.Abs( time - vm.Time) * TV.TimeScale;
                if (distanceToStartTime < SNAP_THRESHOLD) {
                    return new SnapResult() { SnapToValue=vm.Time, Force=distanceToStartTime };
                }
            }
            return null;
        }
        #endregion


        #region XAML event handler
        private double m_HorizontalOffsetAtDragStart=0;

        private void XTimeClip_Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            m_HorizontalOffsetAtDragStart = 0;
        }

        private void XTimeClip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) {
            if (Keyboard.Modifiers == ModifierKeys.Control) {
                TimeMarkerViewModel vm = DataContext as TimeMarkerViewModel;
                if (vm == null)
                    return;

                double orgTime = vm.Time;

                double delta = TV.XToTime(e.HorizontalChange) - TV.XToTime(0);
                double snapTime = TV.TimeSnapHandler.CheckForSnapping(vm.Time + delta, this);

                if (!Double.IsNaN(snapTime)) {
                    delta = snapTime - orgTime;
                }
                vm.Time += delta;

                App.Current.UpdateRequiredAfterUserInteraction = true;
                e.Handled = true;
            }
        }

        private void XTimeClip_Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            if (Math.Abs(e.VerticalChange) < SystemParameters.MinimumVerticalDragDistance || Math.Abs(e.HorizontalChange) < SystemParameters.MinimumHorizontalDragDistance) {
                TimeMarkerViewModel vm = DataContext as TimeMarkerViewModel;
                if (vm != null) {
                    var list =new List<ISelectable>();
                    list.Add(vm.OperatorWidget);
                    App.Current.MainWindow.CompositionView.CompositionGraphView.SelectedElements= list;
                }
            }
        }

        private void XStartThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) {
            TimeMarkerViewModel vm = DataContext as TimeMarkerViewModel;
            if (vm == null)
                return;

            var orgStartTime = vm.Time;
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt)) {
                m_HorizontalOffsetAtDragStart = e.HorizontalChange;
            }
            else {
                vm.Time += TV.XToTime(e.HorizontalChange) - TV.XToTime(0);
                if (Keyboard.Modifiers != ModifierKeys.Shift) {
                    double snapTime= TV.TimeSnapHandler.CheckForSnapping(vm.Time, this);
                    if (!Double.IsNaN(snapTime)) {
                        vm.Time = snapTime;
                    }
                }
            }
            //UpdateMappingShape();
            App.Current.UpdateRequiredAfterUserInteraction = true;
            e.Handled = true;
        }

        private void XTimeClip_Thumb_DoubleClicked(object sender, MouseButtonEventArgs e) {
            App.Current.MainWindow.CompositionView.CompositionGraphView.CenterSelectedElements();
        }
        #endregion

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            //UpdateMappingShape();
        }


        #region dirty stuff
        private TimeView m_TV;
        public TimeView TV
        {
            get
            {
                if (m_TV == null) {
                    m_TV = UIHelper.FindVisualParent<TimeView>(this);

                }

                return m_TV;
            }
        }
        #endregion


        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
        }

    }

    #region Value converter
    [ValueConversion(typeof(double), typeof(double))]
    public class ColorToBrushConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == DependencyProperty.UnsetValue) {
                return "binding error";
            }
            SharpDX.Color4 SharpDXColor = (SharpDX.Color4)value;

            var br = new SolidColorBrush(Color.FromScRgb(SharpDXColor.Alpha,
                                                         SharpDXColor.Red,
                                                         SharpDXColor.Green,
                                                         SharpDXColor.Blue));
            br.Freeze();
            return br;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new SharpDX.Color4();
        }
    }
    #endregion
}
