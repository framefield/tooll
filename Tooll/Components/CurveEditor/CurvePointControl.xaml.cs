// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Framefield.Core.Commands;
using Framefield.Core.Curve;
using System.Windows.Controls.Primitives;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for CurvePointControl.xaml
    /// </summary>

    /**
     * NOTE: In the long run this class should be refactored MVVM wise into a viewmodel and a UserControl
     * 
     */
    public partial class CurvePointControl : UserControl, ISelectable
    {
        public VDefinition m_vdef;
        public ICurve Curve { get; set; }

        public static int createCount = 0;
        private const double NON_WEIGHT_TANGENT_LENGTH = 50;

        public CurvePointControl(double defaultU, VDefinition vdef, ICurve curve, CurveEditor ce)
        {
            InitializeComponent();
            m_CE = ce;
            U = defaultU;
            InitFromVDefinition(vdef);
            Curve = curve;
            createCount++;
            createBindingsForPositioning();
            XCenterThumb.Cursor = Cursors.Arrow;
            Loaded += CurvePointControl_Loaded;
        }

        void CurvePointControl_Loaded(object sender, RoutedEventArgs e)
        {
            //UpdateControlTangents();
        }

        static VToYConverter m_VToYConverter = new VToYConverter();
        static UToXConverter m_UToXConverter = new UToXConverter();


        private void createBindingsForPositioning()
        {
            MultiBinding multiBinding = new MultiBinding();
            multiBinding.Converter = m_VToYConverter;

            multiBinding.Bindings.Add(new Binding("V") { Source = curveEditPoint });
            multiBinding.Bindings.Add(new Binding("MinV") { Source = CurveEditor });
            multiBinding.Bindings.Add(new Binding("MaxV") { Source = CurveEditor });
            multiBinding.Bindings.Add(new Binding("ActualHeight") { Source = CurveEditor });
            BindingOperations.SetBinding(XTranslateTransform, TranslateTransform.YProperty, multiBinding);

            MultiBinding multiBinding2 = new MultiBinding();
            multiBinding2.Converter = m_UToXConverter;

            multiBinding2.Bindings.Add(new Binding("U") { Source = curveEditPoint });
            multiBinding2.Bindings.Add(new Binding("UScale") { Source = CurveEditor });
            multiBinding2.Bindings.Add(new Binding("UOffset") { Source = CurveEditor });
            BindingOperations.SetBinding(XTranslateTransform, TranslateTransform.XProperty, multiBinding2);
        }


        private static readonly DependencyProperty m_IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(CurvePointControl), new UIPropertyMetadata(false));
        public bool IsSelected { get { return (bool)GetValue(m_IsSelectedProperty); } set { SetValue(m_IsSelectedProperty, value); } }

        private static readonly DependencyProperty UProperty = DependencyProperty.Register("U", typeof(Double), typeof(CurvePointControl), new UIPropertyMetadata(0.0));
        public Double U { get { return (Double)GetValue(UProperty); } set { SetValue(UProperty, value); } }

        private static readonly DependencyProperty VProperty = DependencyProperty.Register("V", typeof(Double), typeof(CurvePointControl), new UIPropertyMetadata(0.0));
        public Double V
        {
            get { return (Double)GetValue(VProperty); }
            set
            {
                SetValue(VProperty, value);
                m_vdef.Value = value;
            }
        }

        private static readonly DependencyProperty StrokeColorProperty = DependencyProperty.Register("StrokeColor", typeof(Brush), typeof(CurvePointControl), new UIPropertyMetadata(Brushes.Azure));
        public Brush StrokeColor { get { return (Brush)GetValue(StrokeColorProperty); } set { SetValue(StrokeColorProperty, value); } }

        private static readonly DependencyProperty TangentVisibilityProperty = DependencyProperty.Register("TangentVisibility", typeof(Visibility), typeof(CurvePointControl), new UIPropertyMetadata(Visibility.Visible));
        public Visibility TangentVisibility { get { return (Visibility)GetValue(TangentVisibilityProperty); } set { SetValue(TangentVisibilityProperty, value); } }


        private static readonly DependencyProperty LeftTangentPositionProperty = DependencyProperty.Register("LeftTangentPosition", typeof(Point), typeof(CurvePointControl), new UIPropertyMetadata(new Point(-NON_WEIGHT_TANGENT_LENGTH, 0)));
        public Point LeftTangentPosition { get { return (Point)GetValue(LeftTangentPositionProperty); } set { SetValue(LeftTangentPositionProperty, value); } }

        private static readonly DependencyProperty RightTangentPositionProperty = DependencyProperty.Register("RightTangentPosition", typeof(Point), typeof(CurvePointControl), new UIPropertyMetadata(new Point(NON_WEIGHT_TANGENT_LENGTH, 0)));
        public Point RightTangentPosition { get { return (Point)GetValue(RightTangentPositionProperty); } set { SetValue(RightTangentPositionProperty, value); } }

        private static readonly DependencyProperty LeftInterpolationTypeProperty = DependencyProperty.Register("LeftInterpolationType", typeof(VDefinition.EditMode), typeof(CurvePointControl), new UIPropertyMetadata(VDefinition.EditMode.Linear));
        public VDefinition.EditMode LeftInterpolationType { get { return (VDefinition.EditMode)GetValue(LeftInterpolationTypeProperty); } set { SetValue(LeftInterpolationTypeProperty, value); } }

        private static readonly DependencyProperty RightInterpolationTypeProperty = DependencyProperty.Register("RightInterpolationType", typeof(VDefinition.EditMode), typeof(CurvePointControl), new UIPropertyMetadata(VDefinition.EditMode.Linear));
        public VDefinition.EditMode RightInterpolationType { get { return (VDefinition.EditMode)GetValue(RightInterpolationTypeProperty); } set { SetValue(RightInterpolationTypeProperty, value); } }


        public void InitFromVDefinition(VDefinition vdef)
        {
            m_vdef = vdef;
            V = vdef.Value;
            LeftInterpolationType = vdef.InEditMode;
            RightInterpolationType = vdef.OutEditMode;
            UpdateControlTangents();
        }


        /**
         * For now, this is only required to fulfill ISelectable interface. Later this can be used to implement fenceSelection
         */
        public Point Position
        {
            get { return new Point(CurveEditor.UToX(U), CurveEditor.vToY(V)); }
            set { U = CurveEditor.xToU(value.X); V = CurveEditor.yToV(value.Y); }
        }

        #region moving event handlers


        private enum MoveDirection
        {
            Undecided = 0,
            Vertical,
            Horizontal,
            Both
        }
        private MoveDirection m_MoveDirection = MoveDirection.Undecided;


        private void OnDragStart(object sender, DragStartedEventArgs e)
        {
            XCenterThumb.Cursor = Cursors.Cross;
            m_MoveDirection = MoveDirection.Undecided;
            if (Keyboard.Modifiers != ModifierKeys.Shift)
            {
                var alreadySelected = CurveEditor._SelectionHandler.SelectedElements.Count == 1 &&
                                      Equals(CurveEditor._SelectionHandler.SelectedElements.First(), this);

                if(!alreadySelected)
                    CurveEditor._SelectionHandler.Clear();
            }
            CurveEditor._SelectionHandler.AddElement(this);
            _addOrUpdateKeyframeCommand = new AddOrUpdateKeyframeCommand(U, m_vdef, Curve);
            _moveKeyframeCommand = new MoveKeyframeCommand(U, U, Curve);
        }

        private AddOrUpdateKeyframeCommand _addOrUpdateKeyframeCommand;
        private MoveKeyframeCommand _moveKeyframeCommand;

        const double DRAG_THRESHOLD = 4;
        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var delta = new Vector(e.HorizontalChange, e.VerticalChange);

            double deltaU = CurveEditor.xToU(delta.X) - CurveEditor.xToU(0);
            double deltaV = CurveEditor.yToV(delta.Y) - CurveEditor.yToV(0);

            if (m_MoveDirection == MoveDirection.Undecided)
            {
                if (Math.Abs(delta.X) + Math.Abs(delta.Y) > DRAG_THRESHOLD)
                {
                    if (Math.Abs(delta.X) > Math.Abs(delta.Y))
                    {
                        m_MoveDirection = MoveDirection.Horizontal;
                        XCenterThumb.Cursor = Cursors.ScrollWE;
                    }
                    else
                    {
                        m_MoveDirection = MoveDirection.Vertical;
                        XCenterThumb.Cursor = Cursors.ScrollNS;
                    }
                }
            }
            else
            {
                CurveEditor.DisableRebuildOnCurveChangeEvents();

                if (m_MoveDirection == MoveDirection.Vertical)
                {
                    V += deltaV;
                }

                if (m_MoveDirection == MoveDirection.Horizontal)
                {
                    // Snap when pressing Shift
                    if (TV != null &&
                        TV.TimeSnapHandler != null &&
                        Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        var snapU = TV.TimeSnapHandler.CheckForSnapping(U + deltaU);
                        if (!Double.IsNaN(snapU))
                        {
                            deltaU = snapU - U;
                        }
                    }

                    // Prevent overwriting existing keys
                    ManipulateU(U + deltaU);
                }

                switch (m_MoveDirection)
                {
                    case MoveDirection.Vertical:
                        _addOrUpdateKeyframeCommand.KeyframeValue = m_vdef;
                        _addOrUpdateKeyframeCommand.Do();
                        break;
                    case MoveDirection.Horizontal:
                        _moveKeyframeCommand.NewTime = U;
                        //_moveKeyframeCommand.Do();
                        break;
                }

                m_vdef = Curve.GetV(U);     // since SetOrUpdateV clones vdef, we have to get a new value

                if (TV != null)
                    TV.TriggerRepaint();

                UpdateControlTangents();
                CurveEditor.EnableRebuildOnCurveChangeEvents();
                App.Current.UpdateRequiredAfterUserInteraction = true;
                CurveEditor.UpdateLine(Curve);               
                //CurveEditor.UpdateEditBox();
            }
        }


        public void ManipulateV(double newV)
        {
            V = newV;
        }

        /// <summary>
        /// Important: the caller has to handle undo/redo and make sure to remove/restore potentially overwritten keyframes
        /// </summary>
        public void ManipulateU(double newU)
        {
            var newURounded = RoundU(newU);
            U = newURounded;
        }

        private static double RoundU(double u)
        {
            return Math.Round(u, 6);
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            XCenterThumb.Cursor = Cursors.Arrow;
            if (m_MoveDirection == MoveDirection.Vertical)
            {
                App.Current.UndoRedoStack.Add(_addOrUpdateKeyframeCommand);                
            }
            else if ( m_MoveDirection == MoveDirection.Horizontal)
            {
                App.Current.UndoRedoStack.Add(_moveKeyframeCommand);
            }
            _addOrUpdateKeyframeCommand = null;
            _moveKeyframeCommand = null;
            m_CE.RebuildCurrentCurves();
        }

        private Vector LimitWeightTanget(Vector tangent)
        {
            return tangent / tangent.Length * NON_WEIGHT_TANGENT_LENGTH;
        }


        private void OnDragTangentDeltaStarted(object sender, DragStartedEventArgs e)
        {
            _addOrUpdateKeyframeCommand = new AddOrUpdateKeyframeCommand(U, m_vdef, Curve);
        }

        private void OnDragLeftTangentDelta(object sender, DragDeltaEventArgs e)
        {
            CurveEditor.DisableRebuildOnCurveChangeEvents();
            LeftTangentPosition += new Vector(e.HorizontalChange, e.VerticalChange);
            var v = LimitWeightTanget(new Vector(Math.Min(LeftTangentPosition.X, 0), LeftTangentPosition.Y));
            LeftTangentPosition = new Point(v.X, v.Y);
            m_vdef.InType = VDefinition.Interpolation.Spline;
            m_vdef.InEditMode = VDefinition.EditMode.Tangent;

            double angleIn = Math.PI/2 - Math.Atan2(CurveEditor.xToU(0.0) - CurveEditor.xToU(v.X), CurveEditor.yToV(0.0) - CurveEditor.yToV(v.Y));

            m_vdef.InTangentAngle = angleIn;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                m_vdef.BrokenTangents = true;
            }

            if (!m_vdef.BrokenTangents)
            {
                m_vdef.OutType = VDefinition.Interpolation.Spline;
                m_vdef.OutEditMode = VDefinition.EditMode.Tangent;

                RightTangentPosition = new Point(-v.X, -v.Y);
                m_vdef.OutTangentAngle = angleIn + Math.PI;
            }

            _addOrUpdateKeyframeCommand.KeyframeValue = m_vdef;
            _addOrUpdateKeyframeCommand.Do();

            if (TV != null)
                TV.TriggerRepaint();
            CurveEditor.EnableRebuildOnCurveChangeEvents();
            CurveEditor.UpdateLine(Curve);
        }

        private void OnDragRightTangentDelta(object sender, DragDeltaEventArgs e)
        {
            CurveEditor.DisableRebuildOnCurveChangeEvents();

            RightTangentPosition += new Vector(e.HorizontalChange, e.VerticalChange);
            var v = LimitWeightTanget(new Vector(Math.Max(RightTangentPosition.X, 0), RightTangentPosition.Y));
            RightTangentPosition = new Point(v.X, v.Y);
            m_vdef.OutType = VDefinition.Interpolation.Spline;
            m_vdef.OutEditMode = VDefinition.EditMode.Tangent;

            double angleOut = Math.PI/2 - Math.Atan2(CurveEditor.xToU(0.0) - CurveEditor.xToU(v.X), CurveEditor.yToV(0.0) - CurveEditor.yToV(v.Y));
            m_vdef.OutTangentAngle = angleOut;
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                m_vdef.BrokenTangents = true;
            }

            if (!m_vdef.BrokenTangents)
            {
                m_vdef.InType = VDefinition.Interpolation.Spline;
                m_vdef.InEditMode = VDefinition.EditMode.Tangent;
                LeftTangentPosition = new Point(-v.X, -v.Y);
                m_vdef.InTangentAngle = angleOut - Math.PI;
            }

            _addOrUpdateKeyframeCommand.KeyframeValue = m_vdef;
            _addOrUpdateKeyframeCommand.Do();

            if (TV != null)
                TV.TriggerRepaint();

            CurveEditor.EnableRebuildOnCurveChangeEvents();
            CurveEditor.UpdateLine(Curve);
        }

        private void OnDragTangentDeltaCompleted(object sender, DragCompletedEventArgs e)
        {
            App.Current.UndoRedoStack.Add(_addOrUpdateKeyframeCommand);
            _addOrUpdateKeyframeCommand = null;
        }
        #endregion


        /**
         * This function updates the TangetOrientation after changing the scale of the CurveEditor
         */
        public void UpdateControlTangents()
        {
            if (CurveEditor == null)
                return;

            var normVector = new Vector(-Math.Cos(m_vdef.InTangentAngle), Math.Sin(m_vdef.InTangentAngle));
            var scaleCorrectedVector = LimitWeightTanget(new Vector(normVector.X * CurveEditor.UScale, CurveEditor.vToY(0) - CurveEditor.vToY(normVector.Y)));
            LeftTangentPosition = new Point(scaleCorrectedVector.X, scaleCorrectedVector.Y);

            normVector = new Vector(-Math.Cos(m_vdef.OutTangentAngle), Math.Sin(m_vdef.OutTangentAngle));
            scaleCorrectedVector = LimitWeightTanget(new Vector(normVector.X * CurveEditor.UScale, CurveEditor.vToY(0) - CurveEditor.vToY(normVector.Y)));
            RightTangentPosition = new Point(scaleCorrectedVector.X, scaleCorrectedVector.Y);

            LeftInterpolationType = m_vdef.InEditMode;
            RightInterpolationType = m_vdef.OutEditMode;
        }


        #region dirty stuff
        private TimeView m_TV;
        public TimeView TV
        {
            get
            {
                if (m_TV == null)
                    m_TV = UIHelper.FindParent<TimeView>(this);
                return m_TV;
            }
        }

        private CurveEditor m_CE;
        public CurveEditor CurveEditor
        {
            get
            {
                if (m_CE == null)
                    m_CE = UIHelper.FindParent<CurveEditor>(this);
                return m_CE;
            }
        }
        #endregion


    }

    #region Value converter
    public class UToXConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Count() != 3 || values.Contains(DependencyProperty.UnsetValue))
            {
                return 0.0;
            }

            double u = (double)values[0];
            double timeScale = (double)values[1];
            double timeOffset = (double)values[2];
            return (u - timeOffset) * timeScale;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /**
     * Binds U, minY, maxY and actualHeight
     */
    public class VToYConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (values.Count() != 4 || values.Contains(DependencyProperty.UnsetValue))
            {
                return 10.0;
            }

            double v = (double)values[0];
            double minV = (double)values[1];
            double maxV = (double)values[2];
            double height = (double)values[3];
            double y = height - (v - minV) / (maxV - minV) * height;
            return y;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }



    public class SelectedToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value == true)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((Visibility)value == Visibility.Visible)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }


    public class LeftInterpolationTypeToPathDataConverter : IValueConverter
    {
        static private Geometry linearFace = Geometry.Parse("M 0, 5 L -5,0 0,-5");
        static private Geometry splineFace = Geometry.Parse("M 0, 5 L -3.4,3.4 -5,0 -3.4,-3.4 0,-5");
        static private Geometry horizontalFace = Geometry.Parse("M 0, 5 L -5,5 -5,-5 0,-5 ");

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            VDefinition.EditMode type = (VDefinition.EditMode)value;
            switch (type)
            {
                case VDefinition.EditMode.Linear:
                    return linearFace;
                case VDefinition.EditMode.Tangent:
                case VDefinition.EditMode.Smooth:
                    return splineFace;
                default:
                    return horizontalFace;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return VDefinition.Interpolation.Linear;
        }

    }

    public class RightInterpolationTypeToPathDataConverter : IValueConverter
    {
        static private Geometry linearFace = Geometry.Parse("M 0, 5 L 5,0 0,-5");
        static private Geometry splineFace = Geometry.Parse("M 0, 5 L 3.4,3.4   5,0   3.4,-3.4   0,-5");
        static private Geometry horizontalFace = Geometry.Parse("M 0, 5 L 5,5 5,-5 0,-5 ");

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            VDefinition.EditMode type = (VDefinition.EditMode)value;
            switch (type)
            {
                case VDefinition.EditMode.Linear:
                    return linearFace;
                case VDefinition.EditMode.Tangent:
                case VDefinition.EditMode.Smooth:
                    return splineFace;
                default:
                    return horizontalFace;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return VDefinition.Interpolation.Linear;
        }

    }

    #endregion
}
