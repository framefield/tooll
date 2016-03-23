// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using Framefield.Core;
using Framefield.Core.Commands;

namespace Framefield.Tooll
{

    public class ConnectionLine : Canvas, IConnectableWidget
    {
        public event EventHandler<RoutedEventArgs> SelectedEvent;

        #region public properties

        //private GradientPath GradientPath { get; set; }
        public Path ConnectionPath { get; set; } // needs to be public because of picking
        public Path ConnectionPath2 { get; set; } // needs to be public because of picking
        public Operator ParentOp { get; set; }
        public IConnectionLineSource Source { get; set; }
        public IConnectionLineTarget Target { get; set; }
        public Guid ID { get; private set; }

        public OperatorPart Output { get; set; }
        public OperatorPart Input { get; private set; }

        public bool IsSnapped 
        {
            get 
            {
                var targetWidget = Target as OperatorWidget;
                var sourceWidget = Source as OperatorWidget;

                if (targetWidget == null || sourceWidget == null) 
                    return false;

                double targetY = targetWidget.Operator.Position.Y + 25 + 1.0;
                double sourceY = sourceWidget.Operator.Position.Y + 1.0;

                double targetX = targetWidget.Operator.Position.X;
                double sourceX = sourceWidget.Operator.Position.X;

                int index = this.GetMultiInputIndex();
                Rect range = Target.GetRangeForInputConnectionLine(Input, index, false);
                double targetXmin = targetX + range.Left;
                double targetXmax = targetX + range.Right;
                double targetXcenter = targetXmin + 0.5 * (targetXmax - targetXmin);

                int outputCount = 1;    // Todo: needs implementation
                int outputIndex = 0;    // Todo: needs implementation
                double outputWidth = Source.Width / outputCount;
                double sourceXmin = sourceX + (double)outputIndex * outputWidth;
                double sourceXmax = sourceXmin + outputWidth;
                double sourceXcenter = sourceXmin + 0.5 * (sourceXmax - sourceXmin);

                // Calculate straight factor from overlap
                const double BLEND_RANGE = 20;
                const double BLEND_BORDER = 10;
                double overlapp = Math.Min(sourceXmax, targetXmax) - Math.Max(sourceXmin, targetXmin);
                double straightFactor = Math.Min(BLEND_RANGE, Math.Max(0, overlapp - BLEND_BORDER)) / BLEND_RANGE;

                // Limit straight connection to a certain y range...
                const double STRAIGHT_MIN_DISTANCE = 80;
                const double STRAIGHT_DISTANCE_BLEND = 50;
                double dy = sourceY - targetY;
                if (dy < -2) {
                    straightFactor = 0.0;
                }
                else {
                    double f = 1 - (Utilities.Clamp(dy, STRAIGHT_MIN_DISTANCE, STRAIGHT_MIN_DISTANCE + STRAIGHT_DISTANCE_BLEND) - STRAIGHT_MIN_DISTANCE) / STRAIGHT_DISTANCE_BLEND;
                    straightFactor *= f;
                }
                return Math.Abs(dy) < 0.001 && straightFactor.CompareTo(1.0) == 0;
            }
        }

        public bool IsSelected {
            get { return m_IsSelected; }
            set
            {
                m_IsSelected = value;
                ConnectionPath.Stroke = value ? new SolidColorBrush(UIHelper.BrightColorFromType(Output.Type))
                                              : new SolidColorBrush(UIHelper.ColorFromType(Output.Type));
                ConnectionPath.Stroke.Freeze();
            }
        }

        public CompositionView CV
        {
            get
            {
                return UIHelper.FindParent<CompositionView>(this);
            }
        }
        public Point Position { get { return new Point(); } set { } }
        public Point PositionOnCanvas
        {
            get
            {
                return new Point();
            }
            set
            {
            }
        }
        public bool CurrentlyDragged() { return false; }
        public bool IsInDragGroup(IConnectableWidget el) { return false; }
        public double GetVerticalOverlapWith(IConnectableWidget el) { return 0; }
        public double GetHorizontalOverlapWith(IConnectableWidget el) { return 0; }

        public void UpdateConnections() { }

        #endregion

        #region constructors

        // Creates an unfinished Connection that should be updated during DragDelta events and either removed or connected
        public ConnectionLine(Operator parentOp, IConnectionLineSource source, OperatorPart output)
        {
            ParentOp = parentOp;
            Source = source;
            Output = output;
            CreateLineGeometry();
            IsSelected = false;
            ID = Guid.Empty;
            IsHitTestVisible = false;
            _thumb.IsHitTestVisible = false;
            MouseLeftButtonDown += ConnectionLine_MouseLeftButtonDownHandler;
        }

        void ConnectionLine_MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            HandleClicked();
        }

        public ConnectionLine(Guid id, Operator parentOp, IConnectionLineSource source, IConnectionLineTarget target, OperatorPart output, OperatorPart input)
        {
            //IsHitTestVisible = false;
            ID = id;
            ParentOp = parentOp;
            Source = source;
            Target = target;
            Output = output;
            Input = input;
            CreateLineGeometry();
            IsSelected = false;

            if (!Output.Parent.Visible || !Input.Parent.Visible) {
                Opacity = 0;
                IsHitTestVisible = false;
            }
            ToolTip = Output.ToString() + " -> " + Input.ToString() + "\n" + "(Drag with CTRL to disconnect)";
            MouseLeftButtonDown += ConnectionLine_MouseLeftButtonDownHandler;

            Loaded += ConnectionLine_Loaded;
            Unloaded += OnUnloaded;
        }
        #endregion

        #region public methods

        /*
         * Calculates connection curve as described in http://streber.pixtur.de/index.php?go=fileDownloadAsImage&file=4964
         */
        public void Update()
        {
            if (Target == null)
                return;

            if (Source.Outputs.Count <= 0)
                return;
            if (Target.Inputs.Count <= 0)
                return;

            var targetUI = Target as UIElement;
            if (UIHelper.FindParent<CompositionGraphView>(targetUI) == null)
            {
                Logger.Warn("Tried to connect to TargetWidget that is not in current view (" + Input.Parent.Name + "). Please report this bug.");
                return;
            }
            var sourceUI = Source as UIElement;
            if (UIHelper.FindParent<CompositionGraphView>(sourceUI) == null)
            {
                Logger.Warn("Tried to connect to source that is not in current view (" + Output.Parent.Name + "). Please report this bug.");
                return;
            }

            Point targetPositionOnCanvas = Target.PositionOnCanvas;
            Point sourcePointInMainPanel = Source.PositionOnCanvas;

            double targetY = targetPositionOnCanvas.Y + Target.Height + 1.0;
            double sourceY = sourcePointInMainPanel.Y + 1.0;

            int index = this.GetMultiInputIndex();
            Rect range = Target.GetRangeForInputConnectionLine(Input, index, false);
            double targetXmin = targetPositionOnCanvas.X + range.Left;
            double targetXmax = targetPositionOnCanvas.X + range.Right;
            double targetXcenter = targetXmin + 0.5 * (targetXmax - targetXmin);

            int outputCount = Source.Outputs.Count;
            int outputIndex = Output.Func.EvaluationIndex;
            double outputWidth = Source.Width / outputCount;
            double sourceXmin = sourcePointInMainPanel.X + (double)outputIndex * outputWidth;
            double sourceXmax = sourceXmin + outputWidth;
            double sourceXcenter = sourceXmin + 0.5 * (sourceXmax - sourceXmin);

            // Calculate straight factor from overlap
            const double BLEND_RANGE = 20;
            const double BLEND_BORDER = 10;
            double overlapp = Math.Min(sourceXmax, targetXmax) - Math.Max(sourceXmin, targetXmin);
            double straightFactor = Math.Min(BLEND_RANGE, Math.Max(0, overlapp - BLEND_BORDER)) / BLEND_RANGE;

            // limit straight connection to a certain y range...
            const double STRAIGHT_MIN_DISTANCE = 80;
            const double STRAIGHT_DISTANCE_BLEND = 50;
            double dy = sourceY - targetY;
            if (dy < -2)
            {
                straightFactor = 0.0;
            }
            else
            {
                straightFactor *= 1 - (Utilities.Clamp(dy, STRAIGHT_MIN_DISTANCE, STRAIGHT_MIN_DISTANCE + STRAIGHT_DISTANCE_BLEND) - STRAIGHT_MIN_DISTANCE) / STRAIGHT_DISTANCE_BLEND;
            }           

            // Calculate curviness
            double MIN_TANGENT_LENGTH = 80.0;
            double MAX_TANGENT_LENGTH = 200.0;
            double tangent = Utilities.Clamp((sourceY - targetY) * 0.4, MIN_TANGENT_LENGTH, MAX_TANGENT_LENGTH) * (1 - straightFactor);

            // Blend X depending on straight factor
            double averageX = Math.Max(sourceXmin, targetXmin) + 0.5 * overlapp;

            double sourceX = sourceXcenter * (1 - straightFactor) + straightFactor * averageX;
            double targetX = targetXcenter * (1 - straightFactor) + straightFactor * averageX;

            m_PathFigure.StartPoint = new Point(sourceX, sourceY);
            m_CurveSegment.Point1 = new Point(sourceX, sourceY - tangent);
            m_CurveSegment.Point2 = new Point(targetX, targetY + tangent);
            m_CurveSegment.Point3 = new Point(targetX, targetY);

            m_ArrowHeadPathFigure.StartPoint = new Point(targetX, targetY + 2.02);
            m_ArrowHeadLineSegment.Point = new Point(targetX, targetY + 2);

            m_TargetPoint = new Point(targetX, targetY);
            m_SourcePoint = new Point(sourceX, sourceY);

            Canvas.SetLeft(_thumb, targetX - 0.5 * CONNECTION_ARROW_THUMB_SIZE);
            Canvas.SetTop(_thumb, targetY - 0.5 * CONNECTION_ARROW_THUMB_SIZE);
        }

        public void Remove()
        {
            App.Current.UndoRedoStack.AddAndExecute(new RemoveConnectionCommand(ParentOp, new Connection(sourceOp: Output.Parent,
                                                                                                         sourceOpPart: Output,
                                                                                                         targetOp: Input.Parent,
                                                                                                         targetOpPart: Input,
                                                                                                         connectionIdx: GetMultiInputIndex())));
        }



        private bool _multiIndexNeedsUpdate = true;

        void ConnectionLine_Loaded(object sender, RoutedEventArgs e)
        {
            Input.ManipulatedEvent += Input_ManipulatedEvent;
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Input.ManipulatedEvent -= Input_ManipulatedEvent;
        }

        void Input_ManipulatedEvent(object sender, EventArgs e)
        {
            _multiIndexNeedsUpdate = true;
        }

        /*
         * Iterate over potential multi inputs to find with index matches the current
         * connection. This is important for remove and inserting connections.
         */
        private int _multiInputIndex;
        public int GetMultiInputIndex()
        {
            if (!_multiIndexNeedsUpdate)
                return _multiInputIndex;

            _multiInputIndex = 0;
            _multiIndexNeedsUpdate = false;

            if (!Input.IsMultiInput) 
                return _multiInputIndex;

            var connection = ParentOp.Connections.Single(con => con.ID == ID);
            _multiInputIndex = connection.Index;

            return _multiInputIndex;
        }

        /**
         * This is called from OperatorWidget while dragging from OperatorWidget during onDragConnectionDelta
         */
        public void UpdateDuringConstruction(Point endPoint)
        {
            if (Source.Outputs.Count <= 0)
                return;

            var sourceUI = Source as UIElement;
            if (UIHelper.FindParent<CompositionGraphView>(sourceUI) == null)
            {
                Logger.Warn("Tried to connect to source that is not in current view (" + Output.Parent.Name + ").");
                return;
            }

            Point sourcePointInMainPanel = Source.PositionOnCanvas;
            double targetY = endPoint.Y;
            double sourceY = sourcePointInMainPanel.Y;

            double targetXmin, targetXmax;
            targetXmin = endPoint.X;
            targetXmax = endPoint.X;
            IsSelected = false;

            int outputCount = Source.Outputs.Count;
            int outputIndex = Output.Func.EvaluationIndex;
            double outputWidth = Source.Width / outputCount;
            double sourceXmin = sourcePointInMainPanel.X + (double)outputIndex * outputWidth;
            double sourceXmax = sourceXmin + outputWidth;

            // Calculate straight factor from overlap
            double BLEND_RANGE = 40;
            double BLEND_BORDER = 10;
            double overlapp = Math.Min(sourceXmax, targetXmax) - Math.Max(sourceXmin, targetXmin);
            double straightFactor = Math.Min(BLEND_RANGE, Math.Max(0, overlapp - BLEND_BORDER)) / BLEND_RANGE;

            // limit straight connection to a certain y range...
            double STRAIGHT_MIN_DISTANCE = 50;
            double STRAIGHT_DISTANCE_BLEND = 50;
            double dy = sourceY - targetY;
            if (dy < -2)
            {
                straightFactor = 0.0;
            }
            else
            {
                straightFactor *= 1 - (Utilities.Clamp(dy, STRAIGHT_MIN_DISTANCE, STRAIGHT_MIN_DISTANCE + STRAIGHT_DISTANCE_BLEND) - STRAIGHT_MIN_DISTANCE) / STRAIGHT_DISTANCE_BLEND;
            }

            // Calculate curviness
            const double MIN_TANGENT_LENGTH = 80.0;
            const double MAX_TANGENT_LENGTH = 200.0;
            double tangent = Utilities.Clamp(((sourceY - targetY) * 0.4), MIN_TANGENT_LENGTH, MAX_TANGENT_LENGTH) * (1 - straightFactor);

            // Blend X depending on straightfactor
            double sourceXcenter = sourceXmin + 0.5 * (sourceXmax - sourceXmin);
            double targetXcenter = targetXmin + 0.5 * (targetXmax - targetXmin);
            double averageX = Math.Max(sourceXmin, targetXmin) + 0.5 * overlapp;

            double sourceX = sourceXcenter * (1 - straightFactor) + straightFactor * averageX;
            double targetX = targetXcenter * (1 - straightFactor) + straightFactor * averageX;

            m_TargetPoint = new Point(targetX, targetY);
            m_SourcePoint = new Point(sourceX, sourceY);

            m_PathFigure.StartPoint = new Point(sourceX, sourceY);
            m_CurveSegment.Point1 = new Point(sourceX, sourceY - tangent);
            m_CurveSegment.Point2 = new Point(targetX, targetY + tangent);
            m_CurveSegment.Point3 = new Point(targetX, targetY);

            m_ArrowHeadPathFigure.StartPoint = new Point(targetX, targetY + 2.02);
            m_ArrowHeadLineSegment.Point = new Point(targetX, targetY + 2);
        }

        public void ToggleSelection()
        {
            IsSelected = !IsSelected;
        }

        public void HandleClicked()
        {
            SelectedEvent(this, new RoutedEventArgs());
        }

        #endregion

        #region private members

        private bool m_IsSelected = false;
        private BezierSegment m_CurveSegment;
        private PathFigure m_PathFigure;

        private Path m_ArrowHeadPath;
        private LineSegment m_ArrowHeadLineSegment;
        private PathFigure m_ArrowHeadPathFigure;
        public Point m_TargetPoint;
        public Point m_SourcePoint;

        #endregion

        #region private methods

        private void CreateLineGeometry()
        {
            //GradientPath = new GradientPath();
            //GradientStop s2= new GradientStop(Colors.BrightColorFromType(Source.Type), offset:0.95);
            //GradientStop s1= new GradientStop(Colors.ColorFromType(Source.Type), offset: 0.8);
            //GradientPath.GradientStops.Add(s1);
            //GradientPath.GradientStops.Add(s2);
            //GradientPath.GradientMode = GradientMode.Parallel;
            //GradientPath.StrokeThickness = 0.5;
            //GradientPath.Tolerance=  0.2;
            //GradientPath.IsHitTestVisible= false;

            ConnectionPath = new Path();
            //ConnectionPath.IsHitTestVisible = false;
            ConnectionPath.Stroke = new SolidColorBrush(UIHelper.BrightColorFromType(Output.Type));
            ConnectionPath.Stroke.Freeze();
            ConnectionPath.StrokeThickness = 3;
            ConnectionPath.StrokeEndLineCap = PenLineCap.Triangle;
            this.Children.Add(ConnectionPath);

            ConnectionPath2 = new Path();
            //ConnectionPath2.IsHitTestVisible = false;
            ConnectionPath2.Stroke = new SolidColorBrush(UIHelper.ColorFromType(Output.Type));
            ConnectionPath2.Stroke.Freeze();
            ConnectionPath2.StrokeThickness = 2.0;
            ConnectionPath2.StrokeEndLineCap = PenLineCap.Triangle;
            this.Children.Add(ConnectionPath2);


            //this.Children.Add(GradientPath);

            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.FillRule = FillRule.Nonzero;

            // Spline curve
            m_PathFigure = new PathFigure();
            m_PathFigure.StartPoint = new Point(10, 10);
            pathGeometry.Figures.Add(m_PathFigure);

            m_CurveSegment = new BezierSegment();
            m_CurveSegment.Point1 = new Point(10, 10);
            m_CurveSegment.Point2 = new Point(10, 10);
            m_CurveSegment.Point3 = new Point(10, 10);
            m_PathFigure.Segments.Add(m_CurveSegment);

            ConnectionPath.Data = pathGeometry;
            ConnectionPath2.Data = pathGeometry;
            //GradientPath.Data = pathGeometry;

            // ArrowHead
            m_ArrowHeadPath = new Path();
            m_ArrowHeadPath.IsHitTestVisible = false;
            m_ArrowHeadPath.Stroke = new SolidColorBrush(UIHelper.BrightColorFromType(Output.Type));
            m_ArrowHeadPath.Stroke.Freeze();
            m_ArrowHeadPath.StrokeThickness = 14;
            m_ArrowHeadPath.StrokeEndLineCap = PenLineCap.Triangle;
            this.Children.Add(m_ArrowHeadPath);

            PathGeometry ArrowHeadPathGeometry = new PathGeometry();
            ArrowHeadPathGeometry.FillRule = FillRule.Nonzero;


            m_ArrowHeadPathFigure = new PathFigure();
            m_ArrowHeadPathFigure.StartPoint = new Point(10, 10);
            ArrowHeadPathGeometry.Figures.Add(m_ArrowHeadPathFigure);

            m_ArrowHeadLineSegment = new LineSegment();
            m_ArrowHeadLineSegment.Point = new Point(10, -10);
            m_ArrowHeadPathFigure.Segments.Add(m_ArrowHeadLineSegment);

            m_ArrowHeadPath.Data = ArrowHeadPathGeometry;

            // Head thumb
            _thumb = new Thumb();
            _thumb.Opacity = 0;
            this.Children.Add(_thumb);
            Canvas.SetTop(_thumb, -0.5 * CONNECTION_ARROW_THUMB_SIZE);
            _thumb.Width = CONNECTION_ARROW_THUMB_SIZE;
            Canvas.SetLeft(_thumb, -0.5 * CONNECTION_ARROW_THUMB_SIZE);
            _thumb.Height = CONNECTION_ARROW_THUMB_SIZE;

            _thumb.Loaded += Thumb_LoadedHandler;

            this.Unloaded += Thumb_UnloadEventHandler;
        }

        void Thumb_LoadedHandler(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(_thumb).PreviewKeyDown += ConnectionLine_PreviewKeyDown;
            Window.GetWindow(_thumb).PreviewKeyUp += ConnectionLine_PreviewKeyUp;
            _thumb.DragStarted += Thumb_DragStartedHandler;
            _thumb.MouseMove += Thumb_MouseMoveHandler;
        }

        void ConnectionLine_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            UpdateCursorShape();
        }

        void ConnectionLine_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            UpdateCursorShape();
        }

        void Thumb_MouseMoveHandler(object sender, MouseEventArgs e)
        {
            UpdateCursorShape();
        }


        private void Thumb_UnloadEventHandler(object sender, RoutedEventArgs e)
        {
            _thumb.DragStarted -= Thumb_DragStartedHandler;
            _thumb.MouseMove -= Thumb_MouseMoveHandler;
        }

        #endregion

        public bool HitTestDisabledDuringDrag
        {
            get { return _thumb.IsHitTestVisible; }
            set {
                IsHitTestVisible = value;
                _thumb.IsHitTestVisible = value; 
            }
        }

        private void UpdateCursorShape()
        {
            if (this.IsMouseOver)
            {
                // Connecting
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    this.Cursor = CustomCursorProvider.GetCursorStream(CustomCursorProvider.Cursors.StartConnection);
                }
                // Normal
                else
                {
                    this.Cursor = Cursors.Arrow;
                }
            }
        }


        private void Thumb_DragStartedHandler(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                CV.CompositionGraphView.ConnectionDragHelper.DoDragDropRewireConnection(this);
            }                
        }

        #region private members
        Thumb _thumb;

        const double CONNECTION_ARROW_THUMB_SIZE = 20;
        #endregion
    }
}
