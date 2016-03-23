// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Framefield.Core.Commands;
using Framefield.Core.Curve;
using Framefield.Core;
using ICommand = Framefield.Core.ICommand;
using Key = System.Windows.Input.Key;
using System.Windows.Media.Animation;

using Newtonsoft.Json;
using Path = System.Windows.Shapes.Path;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for CurveEditor.xaml
    /// </summary>
    public partial class CurveEditor : UserControl
    {
        private SortedList<Core.Curve.ICurve, Path> _curvesWithPaths = new SortedList<Core.Curve.ICurve, Path>();
        private SortedList<Core.Curve.ICurve, List<CurvePointControl>> _curvesWithPointControls = new SortedList<Core.Curve.ICurve, List<CurvePointControl>>();

        public SelectionHandler _SelectionHandler = new SelectionHandler();
        private List<CurvePointControl> _pointControlRecyclePool = new List<CurvePointControl>();

        

        private FenceSelection _fenceSelection;
        private CurveEditBox _curveEditBox;

        public CurveEditor() {
            InitializeComponent();

            _fenceSelection = new FenceSelection(_SelectionHandler, XCurvePointCanvas);
            _SelectionHandler.SelectionChanged += SelectionChangedHandler;
            XFenceCanvas.Children.Add(_fenceSelection);

            _curveEditBox = new CurveEditBox(_SelectionHandler, this);
            XFenceCanvas.Children.Add(_curveEditBox);

            _USnapHandler.AddSnapAttractor( XHorizontalScaleLines);
            _ValueSnapHandler.AddSnapAttractor(XVerticalScaleLines);
            _ValueSnapHandler.SnappedEvent += ValueSnapHandler_SnappedEventHandler;
        }

        void SelectionChangedHandler(object sender, SelectionHandler.SelectionChangedEventArgs e) {
            UpdateCurveHighlight();
        }

        void ValueSnapHandler_SnappedEventHandler(object sender, ValueSnapHandler.SnapEventArgs e)
        {
            XValueSnapMarker.Visibility = Visibility.Visible;

            var _snapMarkerAnimation = new DoubleAnimation() { From = 0.8, To = 0, Duration = TimeSpan.FromSeconds(0.4) };
            _snapMarkerAnimation.BeginAnimation(UIElement.OpacityProperty, _snapMarkerAnimation);

            XValueSnapMarker.RenderTransform = new TranslateTransform(0, vToY(e.Value));
            XValueSnapMarker.Opacity = 1;

            XValueSnapMarker.BeginAnimation(UIElement.OpacityProperty, _snapMarkerAnimation);
        }

        /**
         * Iterate over all curves and change thickness if any of it's ControlPoint is selected
         * This is slow and could easily be optimized by adding an additional curve selection Handler
         */
        void UpdateCurveHighlight() 
{
            foreach (var pair in _curvesWithPointControls) {
                var curve = pair.Key;
                var pointControls = pair.Value;

                bool doHighlight= false;

                foreach( var pc in pointControls ) {
                    if( pc.IsSelected) {
                        doHighlight = true;
                        break;
                    }
                }

                var path = _curvesWithPaths[curve];
                path.StrokeThickness= doHighlight ? 3 : 0.5;
            }
        }


        // Is called when curve visibility is changed. E.g. when another operator is selected.
        public void SetCurveOperators(List<ICurve> curves) 
        {
            // Disable event for this operation...
            foreach (var curve in _curvesWithPaths.Keys)
                curve.ChangedEvent -= CurveChangedHandler;

            _SelectionHandler.SelectedElements.Clear();

            _curvesWithPaths.Clear();
            _curvesWithPointControls.Clear();
            XCurveLineCanvas.Children.Clear();

            // ask to optimize...
            if (curves.Any())
            {
                var overallKeyCount = 0;
                foreach (var c in curves)
                {
                    overallKeyCount += c.GetPoints().Count;
                }

                if (overallKeyCount > 2000)
                {
                    var message = String.Format(
                        "These curves have {0} keyframes which will be very slow to render. Do you want to optimize them?",
                        overallKeyCount);

                    if (MessageBox.Show(message, "Optimize", MessageBoxButton.OKCancel, MessageBoxImage.Question) ==
                        MessageBoxResult.OK)
                    {
                        var curves2 = curves.Select(ic => ic as Curve).Where(c => curves != null).ToList();
                        var optimizer = new CurveOptimizer(curves2);
                        optimizer.OptimizeCurves(200);
                    }
                }
            }

            foreach (var c in XCurvePointCanvas.Children) {
                var cep = c as CurvePointControl;
                if (cep != null) {
                    _pointControlRecyclePool.Add(cep);
                }
            }
            XCurvePointCanvas.Children.Clear();

            // Reenable event 
            foreach (var curve in curves) {
                curve.ChangedEvent += CurveChangedHandler;
                RebuildCurve(curve);
            }
            UpdateCurveLinesAndEditBox();
        }


        public void RebuildCurrentCurves() {
            //xRenderUpdateViz.InvalidateVisual();

            //Copy list first, because RebuildCurve modifies the collection
            var curvesToRebuild = new List<Core.Curve.ICurve>();
            foreach (var curve in _curvesWithPaths.Keys)
                curvesToRebuild.Add(curve);

            foreach (var curve in curvesToRebuild)
                RebuildCurve(curve);
        }


        private void CurveChangedHandler(object o, EventArgs e) {
            Core.Curve.ICurve curve = o as Core.Curve.ICurve;
            RebuildCurve(curve);
        }

        #region properties
        public static readonly DependencyProperty UOffsetProperty = DependencyProperty.Register(
          "UOffset",
            typeof(double),
            typeof(CurveEditor),
            new FrameworkPropertyMetadata(-0.5,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender)    
        );
        public virtual double UOffset { get { return (double) GetValue(UOffsetProperty); } set { SetValue(UOffsetProperty, value); } }

        public static readonly DependencyProperty UScaleProperty = DependencyProperty.Register(
          "UScale",
            typeof(double),
            typeof(CurveEditor),
            new FrameworkPropertyMetadata(100.0,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double UScale { get { return (double) GetValue(UScaleProperty); } set { SetValue(UScaleProperty, value); } }

        public static readonly DependencyProperty MinVProperty = DependencyProperty.Register("MinV", typeof(double), typeof(CurveEditor), new UIPropertyMetadata(-1.5));
        public double MinV { get { return (double) GetValue(MinVProperty); } set { SetValue(MinVProperty, value); } }

        public static readonly DependencyProperty MaxVProperty = DependencyProperty.Register("MaxV", typeof(double), typeof(CurveEditor), new UIPropertyMetadata(1.5));
        public double MaxV { get { return (double) GetValue(MaxVProperty); } set { SetValue(MaxVProperty, value); } }
        #endregion



        #region update children

        private void RebuildCurve(Core.Curve.ICurve curve)
        {
            if (!_updatingCurveEnabled) {
                return;
            }

            // Keep original selection
            var selectedKeys = new List< Tuple<ICurve,double>>();
            foreach (var e in _SelectionHandler.SelectedElements)
            {
                var cpc = e as CurvePointControl;
                if (cpc == null)
                    continue;
                selectedKeys.Add(new Tuple<ICurve, double>(cpc.Curve,cpc.U));
            }

            //_SelectionHandler.Clear();

            if( !_curvesWithPointControls.ContainsKey(curve))
                _curvesWithPointControls[curve]= new List<CurvePointControl>();

            var curvePointControls = _curvesWithPointControls[curve];

            int cepIndex = 0;
            bool reusingControls = true;
            
            foreach (var pair in curve.GetPoints())
            {
                double u = pair.Key;
                var vDefinition = pair.Value;


                // Reuse existing control
                if (reusingControls && cepIndex < curvePointControls.Count)
                {
                    var reusedPointControl = curvePointControls[cepIndex];
                    reusedPointControl.U = u;
                    reusedPointControl.InitFromVDefinition(vDefinition);
                    reusedPointControl.Curve = curve;
                    reusedPointControl.IsSelected = false;

                    // Was it selected?
                    foreach (var curveTime in selectedKeys)
                    {
                        if (reusedPointControl.Curve == curveTime.Item1 && reusedPointControl.U == curveTime.Item2)
                        {
                            reusedPointControl.IsSelected = true;
                            _SelectionHandler.AddElement(reusedPointControl);
                            break;
                        }
                    }       

                    cepIndex++;
                }
                else
                {
                    reusingControls = false;
                    CurvePointControl newPointControl;
                    // Reuse from pool...
                    if (_pointControlRecyclePool.Count > 0)
                    {

                        newPointControl = _pointControlRecyclePool[0];
                        _pointControlRecyclePool.RemoveAt(0);

                        newPointControl.U = u;
                        newPointControl.InitFromVDefinition(vDefinition);
                        newPointControl.Curve = curve;
                        newPointControl.IsSelected = false;
                    }
                    // Create new control
                    else
                    {
                        newPointControl = new CurvePointControl(u, vDefinition, curve, this);
                        newPointControl.U = u;
                    }
                  
                    _curvesWithPointControls[curve].Add(newPointControl);
                    XCurvePointCanvas.Children.Add(newPointControl);
                }
            }
            

            // Move obsolete control points to pool
            if (reusingControls)
            {
                List<CurvePointControl> obsoletePoints = new List<CurvePointControl>();
                while (cepIndex < _curvesWithPointControls[curve].Count)
                {
                    var obsoletePointControl = curvePointControls[cepIndex];
                    _pointControlRecyclePool.Add(obsoletePointControl);
                    XCurvePointCanvas.Children.Remove(obsoletePointControl);
                    obsoletePoints.Add(obsoletePointControl);
                    cepIndex++;
                }
                foreach (var removeThis in obsoletePoints)
                {
                    curvePointControls.Remove(removeThis);
                }                
            }
                    

            // Update curve line (Path)
            if (_curvesWithPaths.ContainsKey(curve))
            {
                XCurveLineCanvas.Children.Remove(_curvesWithPaths[curve]);
            }
            var newPath = new Path();
            newPath.Stroke = Brushes.DarkGray;
            newPath.StrokeThickness = 1;
            _curvesWithPaths[curve] = newPath;
            XCurveLineCanvas.Children.Add(newPath);
            
            UpdateCurveHighlight();
            UpdateLine(curve);
        }


        
        const double CURVE_VALUE_PADDING = 0.6;


        public void FitValueRange()
        {
            ViewAllKeys(KeeyURange: true);
        }


        private void ViewAllKeys(bool KeeyURange= false)
        {
            double minU= double.PositiveInfinity;
            double maxU= double.NegativeInfinity;
            double minV= double.PositiveInfinity;
            double maxV= double.NegativeInfinity;
            int numPoints =0;


            if (_SelectionHandler.SelectedElements.Count == 0) {
                foreach (var pair in _curvesWithPaths) {
                    Core.Curve.ICurve curve = pair.Key;

                    foreach (var pair2 in curve.GetPoints()) {
                        numPoints++;
                        double u = pair2.Key;
                        var vDef = pair2.Value;
                        minU = Math.Min(minU, u);
                        maxU = Math.Max(maxU, u);
                        minV = Math.Min(minV, vDef.Value);
                        maxV = Math.Max(maxV, vDef.Value);
                    }
                }
            }
            else {
                foreach (var element in _SelectionHandler.SelectedElements) {
                    var cpc = element as CurvePointControl;
                    if (cpc != null) {
                        numPoints++;
                        minU = Math.Min(minU, cpc.U);
                        maxU = Math.Max(maxU, cpc.U);
                        minV = Math.Min(minV, cpc.m_vdef.Value);
                        maxV = Math.Max(maxV, cpc.m_vdef.Value);
                    }
                }
            }

            if (numPoints==0) {
                minV = -3;
                maxV = +3;
                minU = -2;
                maxU = 10;
            }

            double scaleV= ActualHeight / (maxV-minV);

            if (minV != maxV) {
                MinV= minV - CURVE_VALUE_PADDING * (maxV-minV);
                MaxV= maxV + CURVE_VALUE_PADDING * (maxV-minV);
            }
            else {
                MinV = minV - 1.0;
                MaxV = maxV + 1.0;
            }

            if (!KeeyURange) {
                if (maxU != minU) {
                    UScale= (ActualWidth) / ((maxU - minU) * (1 + 2 * CURVE_VALUE_PADDING));
                    UOffset= minU - CURVE_VALUE_PADDING * (maxU - minU);
                }
                else {
                    UOffset= 0.5 * (minU + maxU);
                }
            }
            UpdateCurveLinesAndEditBox();
        }

        protected void UpdateCurveLinesAndEditBox()
        {
            UpdateLines();
            _curveEditBox.Update();
        }

        public void UpdateLines()
        {
            foreach (var pair in _curvesWithPointControls) {
                UpdateLine(pair.Key);
                //RebuildCurve()
            }
        }

        public void UpdateEditBox()
        {
            _curveEditBox.Update();
        }


        private System.Diagnostics.Stopwatch m_Stopwatch = new System.Diagnostics.Stopwatch();

        public void UpdateLine(Core.Curve.ICurve curve)
        {

            m_Stopwatch.Restart();
            
            foreach (var cpc in _curvesWithPointControls[curve]) {
                cpc.UpdateControlTangents();
            }
            m_Stopwatch.Stop();
            m_Stopwatch.Restart();

            var path= _curvesWithPaths[curve];

            PathGeometry myPathGeometry = new PathGeometry();
            PathFigure pathFigure2 = new PathFigure();

            const int SAMPLE_STEP = 1;
            int steps= (int) this.ActualWidth / SAMPLE_STEP;

            Point[] polyLinePointArray = new Point[steps];
            for (int i=0; i< steps; i++) {
                double u= xToU(i * SAMPLE_STEP);
                double v= curve.GetSampledValue(u);
                polyLinePointArray[i]=new Point(i * SAMPLE_STEP, vToY(v));
            }
            if (steps == 0) {
                return;
            }
            pathFigure2.StartPoint = polyLinePointArray[0];

            PolyLineSegment myPolyLineSegment = new PolyLineSegment();

            myPolyLineSegment.Points = new PointCollection(polyLinePointArray);
            pathFigure2.Segments.Add(myPolyLineSegment);
            myPathGeometry.Figures.Add(pathFigure2);
            path.Data = myPathGeometry;

            m_Stopwatch.Stop();

        }

        public double yToV(double y)
        {
            double v=  (ActualHeight - y) * (MaxV - MinV) / ActualHeight + MinV;
            return v;
        }

        public double dyToV(double dy)
        {
            return -dy / ActualHeight * (MaxV - MinV);
        }

        public double vToY(double v)
        {
            double y = ActualHeight - (v - MinV)/(MaxV-MinV) * ActualHeight;
            return y;
        }

        public double xToU(double x)
        {
            return x / UScale + UOffset;
        }

        public double dxToU(double dx)
        {
            return dx / UScale;
        }

        public double UToX(double t) {
            return (t - UOffset) * UScale;
        }
        #endregion



        #region XAML-events handlers
        private Point m_DragStartPosition;
        private double m_DragStartTimeOffset;
        private bool m_IsRightMouseDragging = false;
        private double m_DragStartMinV;
        private double m_DragStartMaxV;




        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) {
                UIElement el = sender as UIElement;
                if (el != null) {
                    el.CaptureMouse();
                    m_DragStartPosition = e.GetPosition(this);
                    m_DragStartTimeOffset = UOffset;
                    m_DragStartMinV= MinV;
                    m_DragStartMaxV= MaxV;
                    m_IsRightMouseDragging = true;
                }
                this.Focus();
            }
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_IsRightMouseDragging = false;
            UIElement thumb = sender as UIElement;
            if (thumb != null) {
                thumb.ReleaseMouseCapture();
                double dragDelta = Math.Abs(m_DragStartPosition.X - e.GetPosition(this).X)  + Math.Abs(m_DragStartPosition.Y - e.GetPosition(this).Y);
                if (dragDelta > 3) {
                    XGrid.ContextMenu = null;
                }
                else {
                    XGrid.ContextMenu= Resources["XCurveEditContextMenu"] as System.Windows.Controls.ContextMenu;
                    CheckmarkSelectedInterpolationTypes();
                }                
            }            
        }



        private void OnMouseMove(object sender, MouseEventArgs e) {
            if (m_IsRightMouseDragging) {
                UOffset = m_DragStartTimeOffset + (m_DragStartPosition.X - e.GetPosition(this).X) / UScale;

                double deltaY=(m_DragStartPosition.Y - e.GetPosition(this).Y);
                double deltaV=  yToV( deltaY ) - yToV(0);
                MinV = m_DragStartMinV + deltaV;
                MaxV = m_DragStartMaxV + deltaV;
                UpdateCurveLinesAndEditBox();
            }
        }

        private void OnDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _fenceSelection.HandleDragStarted(sender, e);
            this.Focus();
        }

        private void OnDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            _fenceSelection.HandleDragDelta(sender, e);
        }

        private void OnDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _fenceSelection.HandleDragCompleted(sender, e);

        }


        private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
            double mouseWheelZoomSpeed = 1.15;
            double scale = (e.Delta > 0) ? mouseWheelZoomSpeed : 1.0 / mouseWheelZoomSpeed;


            if ((Keyboard.Modifiers & (ModifierKeys.Control)) == ModifierKeys.Control) {
                double dv= (MaxV - MinV) * (1.0 - scale);
                double factor = e.GetPosition(this).Y / ActualHeight;
                MaxV += dv * factor;
                MinV -= dv * (1.0 - factor);
            }
            if (Keyboard.Modifiers == ModifierKeys.None 
                || (Keyboard.Modifiers & (ModifierKeys.Shift)) == ModifierKeys.Shift) {
                UScale *= scale;
                UOffset += (scale - 1.0) * (xToU(ActualWidth) - xToU(0)) * (e.GetPosition(this).X / ActualWidth);
            }
            UpdateCurveLinesAndEditBox();
            e.Handled = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.F) {
                ViewAllKeys();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete || e.Key == Key.Back)  {
                DeleteSelectedKeys();
                e.Handled = true;
            }
        }

        private void DoubleClick(object sender, MouseButtonEventArgs e)
        {
            double u = xToU(e.GetPosition(this).X);
            u = Math.Round(u, Curve.CURVE_U_PRECISION_DIGITS);
            var curvesToUpdate = InsertCurvePoint(u);

            foreach (var curve in curvesToUpdate) {
                RebuildCurve(curve);                
            }

            var pointsToSelect = new List<ISelectable>();

            foreach( var pair in _curvesWithPointControls) {
                var controls = pair.Value;

                foreach (var cpc in controls) {
                    if (cpc.U == u) {
                        cpc.IsSelected= true;
                        pointsToSelect.Add(cpc);
                    }
                }
            }

            _SelectionHandler.SetElements(pointsToSelect);
            e.Handled=true;
        }

        private void SizeChangedEventHandler(object sender, SizeChangedEventArgs e)
        {
            RebuildCurrentCurves();
        }

        #endregion

        #region internal stuff


        // Prevent unnecessary curve updates while dragging curve controls
        internal bool _updatingCurveEnabled= true;
        public void EnableRebuildOnCurveChangeEvents()
        {
            foreach (var curve in _curvesWithPointControls.Keys) {
                curve.ChangedEvent += CurveChangedHandler;
            }
            _updatingCurveEnabled= true;
        }


        public void DisableRebuildOnCurveChangeEvents()
        {
            foreach (var curve in _curvesWithPointControls.Keys) {
                curve.ChangedEvent -= CurveChangedHandler;
            }
            _updatingCurveEnabled= false;
        }
        #endregion    

        #region context menu handlers

        private void OnAddKeyframe(object sender, RoutedEventArgs e)
        {
            double time = App.Current.Model.GlobalTime;
            var curvesToUpdate = InsertCurvePoint(time);

            foreach (var curve in curvesToUpdate)
            {
                RebuildCurve(curve);
            }
        }




        private List<Core.Curve.ICurve> InsertCurvePoint(double u)
        {
            var curvesToUpdate = new List<Core.Curve.ICurve>();

            _updatingCurveEnabled= false;
            foreach (var curve in _curvesWithPointControls.Keys) {
                if (!curve.HasVAt(u)) {
                    var newKey = new Core.Curve.VDefinition();
                    double? prevU = curve.GetPreviousU(u);
                    if (prevU != null)
                        newKey = curve.GetV(prevU.Value).Clone();

                    newKey.Value = curve.GetSampledValue(u);
                    var command = new AddOrUpdateKeyframeCommand(u, newKey, curve);
                    App.Current.UndoRedoStack.AddAndExecute(command);

                    curvesToUpdate.Add(curve);
                }
            }
            _updatingCurveEnabled= true;
            return curvesToUpdate;
        }


        #region set interpolation types
        private void OnFocusSelected(object sender, RoutedEventArgs e) {
            ViewAllKeys();
        }

        private void OnSmooth(object sender, RoutedEventArgs e) {
            ForSelectedOrAllPointsDo((vDef) => {
                vDef.BrokenTangents= false;
                vDef.InEditMode = Core.Curve.VDefinition.EditMode.Smooth;
                vDef.InType = Core.Curve.VDefinition.Interpolation.Spline;
                vDef.OutEditMode = Core.Curve.VDefinition.EditMode.Smooth;
                vDef.OutType = Core.Curve.VDefinition.Interpolation.Spline;
            });
            UpdateCurveLinesAndEditBox();
            CheckmarkSelectedInterpolationTypes();
        }

        private void OnCubic(object sender, RoutedEventArgs e)
        {
            ForSelectedOrAllPointsDo((vDef) =>
            {
                vDef.BrokenTangents = false;
                vDef.InEditMode = Core.Curve.VDefinition.EditMode.Cubic;
                vDef.InType = Core.Curve.VDefinition.Interpolation.Spline;
                vDef.OutEditMode = Core.Curve.VDefinition.EditMode.Cubic;
                vDef.OutType = Core.Curve.VDefinition.Interpolation.Spline;
            });
            UpdateCurveLinesAndEditBox();
            CheckmarkSelectedInterpolationTypes();
        }


        private void OnHorizontal(object sender, RoutedEventArgs e) {
            ForSelectedOrAllPointsDo((vDef) => {
                vDef.BrokenTangents= false;

                vDef.InEditMode = Core.Curve.VDefinition.EditMode.Horizontal;
                vDef.InType = Core.Curve.VDefinition.Interpolation.Spline;
                vDef.InTangentAngle=0;

                vDef.OutEditMode = Core.Curve.VDefinition.EditMode.Horizontal;
                vDef.OutType = Core.Curve.VDefinition.Interpolation.Spline;
                vDef.OutTangentAngle=Math.PI;
            });
            UpdateCurveLinesAndEditBox();
            CheckmarkSelectedInterpolationTypes();
        }

        private void OnConstant(object sender, RoutedEventArgs e) {
            ForSelectedOrAllPointsDo((vDef) => {
                vDef.BrokenTangents= true;
                vDef.OutType = Core.Curve.VDefinition.Interpolation.Constant;
                vDef.OutEditMode = VDefinition.EditMode.Constant;
            });
            UpdateCurveLinesAndEditBox();
            CheckmarkSelectedInterpolationTypes();
        }

        private void OnLinear(object sender, RoutedEventArgs e) {
            ForSelectedOrAllPointsDo((vDef) => {
                vDef.BrokenTangents= false;
                vDef.InEditMode = Core.Curve.VDefinition.EditMode.Linear;
                vDef.InType = Core.Curve.VDefinition.Interpolation.Linear;
                vDef.OutEditMode = Core.Curve.VDefinition.EditMode.Linear;
                vDef.OutType = Core.Curve.VDefinition.Interpolation.Linear;
            });
            UpdateCurveLinesAndEditBox();
            CheckmarkSelectedInterpolationTypes();
        }

        private IEnumerable<VDefinition.EditMode> SelectedKeyframeInterpolationTypes
        {
            get
            {
                var checkedInterpolationTypes = new HashSet<VDefinition.EditMode>();
                foreach (var pair in getSelectedOrAllVDefinitions())
                {
                    var curve = pair.Key;
                    foreach (var vDefinition in pair.Value.Select(curve.GetV))
                    {
                        checkedInterpolationTypes.Add(vDefinition.OutEditMode);
                        checkedInterpolationTypes.Add(vDefinition.InEditMode);
                    }
                }
                return checkedInterpolationTypes;
            }
        }

        private void CheckmarkSelectedInterpolationTypes()
        {
            UncheckAllContextMenuItems();
            var menuItems = XGrid.ContextMenu.Items.OfType<MenuItem>();
            MenuItem menuItem;
            foreach (var selectedKeyframeInterpolationType in SelectedKeyframeInterpolationTypes)
            {
                switch (selectedKeyframeInterpolationType)
                {
                    case VDefinition.EditMode.Horizontal:
                        menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Horizontal");
                        menuItem.IsChecked = true;
                        break;
                    case VDefinition.EditMode.Linear:
                        menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Linear");
                        menuItem.IsChecked = true;
                        break;
                    case VDefinition.EditMode.Smooth:
                        menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Smooth");
                        menuItem.IsChecked = true;
                        break;
                    case VDefinition.EditMode.Cubic:
                        menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Cubic");
                        menuItem.IsChecked = true;
                        break;
                    case VDefinition.EditMode.Constant:
                        menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Constant");
                        menuItem.IsChecked = true;
                        break;
                }
            }
        }

        private void UncheckAllContextMenuItems()
        {
            var menuItems = (XGrid.ContextMenu).Items.OfType<MenuItem>();
            foreach (var menuItem in menuItems)
            {
                menuItem.IsChecked = false;
            }
        }
        #endregion


        #region before after
        private void OnBeforeConstant(object sender, RoutedEventArgs e) {
            foreach (var curve in AllOrSelectedCurves()) {
                curve.PreCurveMapping = Core.Curve.Utils.OutsideCurveBehavior.Constant;
            }
            RebuildCurrentCurves();
        }

        private void OnBeforePingPong(object sender, RoutedEventArgs e) {
            foreach (var curve in AllOrSelectedCurves()) {
                curve.PreCurveMapping = Core.Curve.Utils.OutsideCurveBehavior.Oscillate;
            }
            RebuildCurrentCurves();
        }

        private void OnBeforeRepeat(object sender, RoutedEventArgs e) {
            foreach (var curve in AllOrSelectedCurves()) {
                curve.PreCurveMapping = Core.Curve.Utils.OutsideCurveBehavior.Cycle;
            }
            RebuildCurrentCurves();
        }

        private void OnBeforeRepeatContinously(object sender, RoutedEventArgs e) {
            foreach (var curve in AllOrSelectedCurves()) {
                curve.PreCurveMapping = Core.Curve.Utils.OutsideCurveBehavior.CycleWithOffset;
            }
            RebuildCurrentCurves();
        }

        private void OnAfterConstant(object sender, RoutedEventArgs e) {
            foreach (var curve in AllOrSelectedCurves()) {
                curve.PostCurveMapping = Core.Curve.Utils.OutsideCurveBehavior.Constant;
            }
            RebuildCurrentCurves();
        }

        private void OnAfterPingPong(object sender, RoutedEventArgs e) {
            foreach (var curve in AllOrSelectedCurves()) {
                curve.PostCurveMapping = Core.Curve.Utils.OutsideCurveBehavior.Oscillate;
            }
            RebuildCurrentCurves();
        }

        private void OnAfterRepeat(object sender, RoutedEventArgs e) {
            foreach (var curve in AllOrSelectedCurves()) {
                curve.PostCurveMapping = Core.Curve.Utils.OutsideCurveBehavior.Cycle;
            }
            RebuildCurrentCurves();
        }

        private void OnAfterRepeatContinously(object sender, RoutedEventArgs e) {
            foreach (var curve in AllOrSelectedCurves()) {
                curve.PostCurveMapping = Core.Curve.Utils.OutsideCurveBehavior.CycleWithOffset;
            }
            RebuildCurrentCurves();
        }

        // Copy keyframes
        private void OnCopyKeyframes(object sender, RoutedEventArgs e)
        {
            CopyKeyframes();
        }

        public virtual void CopyKeyframes()
        {
            MessageBox.Show("Sorry, you can only copy keyframes from the timeline window.");
        }

        // Paste keyframes
        private void OnPasteKeyframes(object sender, RoutedEventArgs e)
        {
            PasteKeyframes();
        }

        public virtual void PasteKeyframes()
        {
            MessageBox.Show("Sorry, because keyframes will be inserted at the current time, this does only work in the timeline window.");
        }



        // Duplicate Keyframes
        private void OnDuplicateKeyframes(object sender, RoutedEventArgs e) {
            DuplicateKeyframes();
        }


        public virtual void DuplicateKeyframes() {
            MessageBox.Show("Sorry, because keyframes will be inserted at the current time, this does only work in the timeline window.");
        }


        protected void DuplicateKeyframesToU(double minU) {
            _updatingCurveEnabled= false;

            // duplicate values
            SortedList<Core.Curve.ICurve, List<double>> newCurveUPoints= new SortedList<Core.Curve.ICurve, List<double>>();
            foreach (var curveVdefPair in getSelectedOrAllVDefinitions()) {
                var curve = curveVdefPair.Key;
                var newUPoints= new List<double>();
                newCurveUPoints[curve] = newUPoints;

                foreach (var u in curveVdefPair.Value) {
                    var newU = u + App.Current.Model.GlobalTime - minU;
                    curve.AddOrUpdateV(newU, curve.GetV(u).Clone());
                    newUPoints.Add(newU);
                }
            }
            _updatingCurveEnabled= true;
            RebuildCurrentCurves();

            // select new keys
            _SelectionHandler.SelectedElements.Clear();
            foreach (var curveUListPair in newCurveUPoints) {
                var curve= curveUListPair.Key;
                var uList = curveUListPair.Value;

                foreach (var cpc in _curvesWithPointControls[curve]) {
                    if (uList.Contains(cpc.U)) {
                        _SelectionHandler.AddElement(cpc);
                    }
                }
            }

        }


        /// <summary>
        /// A helper function that pastes a number of keyframes to the first visible curve. 
        /// This is currently used by get KeyFramesFromLogfile but might also be a first stup of copy/pasting keyframes.
        /// </summary>
        /// <param name="valuesOverTime"></param>
        public void AddKeyframesToFirstCurve(List<KeyValuePair<double, float>> valuesOverTime)
        {
            if (_curvesWithPointControls.Keys.Count == 0)
            {
                UIHelper.ShowErrorMessageBox("To add keyframes to a curve, you have to selected an animated operator.","Cannot paste keyframes.");
                return;
            }

            _updatingCurveEnabled = false;
            var curve = _curvesWithPointControls.Keys[0];

            foreach (var valueAndTime in valuesOverTime)
            {
                double time = valueAndTime.Key;
                float value = valueAndTime.Value;

                curve.AddOrUpdateV(time, new VDefinition() {Value = value});
            }
            _updatingCurveEnabled = true;
            RebuildCurrentCurves();
        }


        #endregion

        private List<Core.Curve.ICurve> AllOrSelectedCurves()
        {
            List<Core.Curve.ICurve> curves = new List<Core.Curve.ICurve>();
            if (_SelectionHandler.SelectedElements.Count == 0) {
                foreach (var curve in _curvesWithPaths.Keys) {
                    curves.Add(curve);
                }
            }
            else {
                foreach (var el in _SelectionHandler.SelectedElements) {
                    var cpc= el as CurvePointControl;
                    if (cpc != null) {
                        curves.Add(cpc.Curve);
                    }
                }
            }
            return curves;
        }


        #endregion


        #region helper functions

        /**
        * Helper function to extract vdefs from all or selected UI controls across all curves in CurveEditor
        * 
        * Returns a list curves with a list of vDefs...
        * 
        */
        protected Dictionary<Core.Curve.ICurve, List<double>> getSelectedOrAllVDefinitions() {
            var curveUs = new Dictionary<Core.Curve.ICurve, List<double>>();

            if (_SelectionHandler.SelectedElements.Count > 0) {
                foreach (CurvePointControl cp in _SelectionHandler.SelectedElements) {

                    if (curveUs.ContainsKey(cp.Curve)) {
                        curveUs[cp.Curve].Add(cp.U);
                    }
                    else {
                        var list = new List<double>();
                        list.Add(cp.U);
                        curveUs[cp.Curve] = list;
                    }
                }
            }
            else {
                foreach (var curve in _curvesWithPointControls.Keys) {
                    var list = new List<double>();

                    foreach (var pair in curve.GetPoints()) {
                        var u = pair.Key;
                        list.Add(u);
                    }
                    curveUs[curve] = list;
                }
            }
            return curveUs;
        }

        delegate void DoSomethingDelegate(VDefinition v);

        private void ForSelectedOrAllPointsDo(DoSomethingDelegate doFunc) {
            _updatingCurveEnabled = false;
            UpdateCurveAndMakeUpdateKeyframeCommands(doFunc);
            _updatingCurveEnabled = true;
            RebuildCurrentCurves();

        }

        private void UpdateCurveAndMakeUpdateKeyframeCommands(DoSomethingDelegate doFunc)
        {
            var commandList = new List<ICommand>();
            foreach (var pair in getSelectedOrAllVDefinitions())
            {
                var curve = pair.Key;
                foreach (var u in pair.Value)
                {
                    var vDefinition = curve.GetV(u);
                    commandList.Add(new AddOrUpdateKeyframeCommand(u, vDefinition, curve));
                    doFunc(vDefinition);
                }
            }
            if (commandList.Any())
                App.Current.UndoRedoStack.AddAndExecute(new MacroCommand("ForSelectedOrAllPointsDo", commandList));
        }

        public void DeleteSelectedKeys() {
            _updatingCurveEnabled = false;
            var pointsToDelete = new List<ISelectable>(_SelectionHandler.SelectedElements);
            MakeAndExecuteCommandForDeletion(pointsToDelete);
            _updatingCurveEnabled = true;
            RebuildCurrentCurves();
        }

        private void MakeAndExecuteCommandForDeletion(IEnumerable<ISelectable> pointsToDelete)
        {
            var keyFramesToDelete = new Tuple<double, ICurve>[pointsToDelete.Count()];
            for (var i = 0; i < pointsToDelete.Count(); i++)
            {
                var cpc = pointsToDelete.ElementAt(i) as CurvePointControl;
                if (cpc != null)
                {
                    keyFramesToDelete[i] = new Tuple<double, ICurve>(cpc.U, cpc.Curve);
                }
            }
            _SelectionHandler.Clear();
            App.Current.UndoRedoStack.AddAndExecute(new RemoveKeyframeCommand(keyFramesToDelete, App.Current.Model.GlobalTime));
        }

        #endregion

        private void XCurveEditor_Loaded(object sender, RoutedEventArgs e)
        {
            _SelectionHandler.SelectionChanged += SelectionChangedHandler;
        }

        private void XCurveEditor_Unloaded(object sender, RoutedEventArgs e) {
            _SelectionHandler.SelectionChanged -= SelectionChangedHandler;
        }

        private void IsVisibleChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
                return;

            RebuildCurrentCurves();
        }

        public ValueSnapHandler _USnapHandler = new ValueSnapHandler();
        public ValueSnapHandler _ValueSnapHandler = new ValueSnapHandler();

        private void OnOptimizeKeyframes(object sender, RoutedEventArgs e)
        {
            var curves = _curvesWithPaths.Select(pair => pair.Key).OfType<Curve>().ToList();
            var optimizer = new CurveOptimizer(curves);
            optimizer.OptimizeCurves(30);
            e.Handled = true;
        }
    }
}
