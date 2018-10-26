// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Tooll.Components.CompositionView;

namespace Framefield.Tooll
{
    public partial class OperatorWidget : IConnectionLineSource, IConnectionLineTarget, INotifyPropertyChanged
    {
        #region construction

        public OperatorWidget(Operator op)
        {
            Operator = op;
            _outputs = Operator.Outputs;
            _inputs = Operator.Inputs;

            InitializeComponent();
            CreateBindingsToOperator();

            var outputFunctions = from output in _outputs
                                  where output.Func is Utilities.ValueFunction
                                  select output.Func as Utilities.ValueFunction;

            foreach (var func in outputFunctions)
                func.EvaluatedEvent += OperatorOutputFunction_EvaluatedHandler;

            _snapHandler = new OperatorSnappingHelper(this);

            Height = CompositionGraphView.GRID_SIZE;
            Width = Operator.Width;

            var inputZones = OperatorWidgetInputZoneManager.ComputeInputZonesForOp(this);
            UpdateInputRanges(inputZones);
            UpdateColors();

            UpdateOutputNoses();
            int outputIdx = 0;
            foreach (Path nosePath in XOutputThumbGrid.Children.OfType<Path>())
            {
                nosePath.Fill = new SolidColorBrush(UIHelper.ColorFromType(Operator.Outputs[outputIdx].Type));
                nosePath.Fill.Freeze();
                ++outputIdx;
            }

            if (!op.Visible)
            {
                Opacity = 0;
                IsHitTestVisible = false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _visualParent = VisualParent as UIElement;

            Operator.ModifiedEvent += Operator_ModifiedHandler;
            Operator.PositionChangedEvent += Operator_PositionChangedHandler;
            Operator.WidthChangedEvent += Operator_WidthChangedHandler;
            Operator.OutputAddedEvent += Operator_OutputsChangedHandler;
            Operator.OutputRemovedEvent += Operator_OutputsChangedHandler;
            Operator.InputAddedEvent += Operator_InputsChangedHandler;
            Operator.InputRemovedEvent += Operator_InputsChangedHandler;

            UpdateStyleAndIndicators();

            Position = Operator.Position;
            Width = Operator.Width;
            GetAndDrawInputZones();
            UpdateConnections();
            Window.GetWindow(this).PreviewKeyDown += XControl_KeyDown;
            Window.GetWindow(this).PreviewKeyUp += XControl_KeyUp;

            UpdateCornerRadius();
        }

        private void UnloadedHandler(object sender, RoutedEventArgs e)
        {
            Operator.ModifiedEvent -= Operator_ModifiedHandler;
            Operator.PositionChangedEvent -= Operator_PositionChangedHandler;
            Operator.WidthChangedEvent -= Operator_WidthChangedHandler;
            Operator.InputAddedEvent -= Operator_InputsChangedHandler;
            Operator.InputRemovedEvent -= Operator_InputsChangedHandler;
            Operator.OutputAddedEvent -= Operator_OutputsChangedHandler;
            Operator.OutputRemovedEvent -= Operator_OutputsChangedHandler;

            // PROBLEM: For some reasone, unloading theses events causes a crash.
            // Not sure, if this causes resource-leaks. In the long run, we should
            // refactor this with an updateModifierKeys-Event
            //Window.GetWindow(this).PreviewKeyDown -= XControl_KeyDown;
            //Window.GetWindow(this).PreviewKeyUp -= XControl_KeyUp;
        }

        #endregion construction

        #region events

        public event EventHandler<RoutedEventArgs> SelectedEvent;

        public event EventHandler<RoutedEventArgs> OpenedEvent;

        #endregion events

        #region Event handlers

        private void Operator_ModifiedHandler(object sender, EventArgs e)
        {
            UpdateStyleAndIndicators();
        }

        private void Operator_OutputsChangedHandler(object obj, OperatorPartChangedEventArgs args)
        {
            UpdateColors();
            UpdateOutputNoses();
        }

        private void Operator_InputsChangedHandler(object obj, OperatorPartChangedEventArgs args)
        {
            GetAndDrawInputZones();
        }

        private void OperatorOutputFunction_EvaluatedHandler(object sender, EventArgs e)
        {
            _evaluationCounter++;
            int tickAreaHeight = (int)Height - 2;
            Canvas.SetTop(XEvaluationTick, tickAreaHeight - (_evaluationCounter * 0.25 % tickAreaHeight));
            IsInactive = false;
        }

        private int _evaluationCounter;

        private void Operator_PositionChangedHandler(object sender, PositionChangedEventArgs e)
        {
            Position = e.Position;
            UpdateConnections();
            UpdateCornerRadius();
        }

        private void Operator_WidthChangedHandler(object sender, WidthChangedEventArgs e)
        {
            Width = e.Width;
        }

        private void MouseDoubleClickHandler(object sender, RoutedEventArgs e)
        {
            OpenedEvent(this, e);
            e.Handled = true;
        }

        #endregion Event handlers

        #region public properties

        public Operator Operator { get; private set; }
        public List<OperatorPart> Outputs { get { return _outputs; } }
        public List<OperatorPart> Inputs { get { return _inputs; } }

        public List<OperatorPart> VisibleInputs
        {
            get
            {
                var visibleOpParts = new List<OperatorPart>();
                foreach (var valueHolder in _inputs)
                {
                    var parent = valueHolder.Parent;
                    var parentMeta = parent.Definition;
                    int index = parent.Inputs.IndexOf(valueHolder);
                    if (index >= 0)
                    {
                        var metaInput = parentMeta.Inputs[index];
                        var relevance = metaInput.Relevance;
                        if (relevance == MetaInput.RelevanceType.Required || relevance == MetaInput.RelevanceType.Relevant)
                        {
                            visibleOpParts.Add(valueHolder);
                        }
                        else
                        {
                            if (valueHolder.Connections.Any())
                            {
                                if (Animation.GetRegardingAnimationOpPart(valueHolder.Connections[0]) == null)
                                {  // ignore animation inputs
                                    visibleOpParts.Add(valueHolder);
                                }
                            }
                        }
                    }
                }
                return visibleOpParts;
            }
        }

        public Point PositionOnCanvas
        {
            get { return Position; }
        }

        public List<Thumb> OutputThumbs
        {
            get
            {
                return XOutputThumbGrid.Children.OfType<Thumb>().ToList();
            }
        }

        public List<ConnectionLine> ConnectionsOut { get { return _connectionsOut; } }
        public List<ConnectionLine> ConnectionsIn { get { return _connectionsIn; } }

        public FunctionType Type { get { return Operator.FunctionType; } }

        public bool IsAnimated
        {
            get
            {
                foreach (var input in Operator.Inputs)
                {
                    if (Animation.GetRegardingAnimationOpPart(input) != null)
                        return true;
                }
                return false;
            }
        }

        #endregion public properties

        #region Dependency properties

        // IsSelected
        public bool IsSelected { get { return (bool)GetValue(IsSelectedProperty); } set { SetValue(IsSelectedProperty, value); } }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(OperatorWidget), new UIPropertyMetadata() { DefaultValue = false, PropertyChangedCallback = IsSelectedChangedHandler });

        private static void IsSelectedChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as OperatorWidget;
            if (obj != null)
            {
                if (obj.IsSelected)
                {
                    foreach (Path nosePath in obj.XOutputThumbGrid.Children.OfType<Path>())
                        nosePath.Fill = Brushes.White;
                }
                else
                {
                    int outputIdx = 0;
                    foreach (Path nosePath in obj.XOutputThumbGrid.Children.OfType<Path>())
                    {
                        nosePath.Fill = new SolidColorBrush(UIHelper.ColorFromType(obj.Operator.Outputs[outputIdx].Type));
                        nosePath.Fill.Freeze();
                        ++outputIdx;
                    }
                }
            }
        }

        // Sticky Count
        public int StickyCount { get { return (int)GetValue(StickyCountProperty); } set { SetValue(StickyCountProperty, value); } }

        public static readonly DependencyProperty StickyCountProperty = DependencyProperty.Register("StickyCount", typeof(int), typeof(OperatorWidget), new UIPropertyMetadata() { DefaultValue = 0, PropertyChangedCallback = StickyCountChangedHandler });

        private static void StickyCountChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as OperatorWidget;
            if (obj != null)
            {
                if (obj.StickyCount > 0)
                    obj.XStickyIndicator.Visibility = Visibility.Visible;
                else
                    obj.XStickyIndicator.Visibility = Visibility.Hidden;
            }
        }

        public bool IsInactive { get { return (bool)GetValue(IsInactiveProperty); } set { SetValue(IsInactiveProperty, value); } }
        public static readonly DependencyProperty IsInactiveProperty = DependencyProperty.Register("IsInactive", typeof(bool), typeof(OperatorWidget), new UIPropertyMetadata() { DefaultValue = true, PropertyChangedCallback = IsSelectedChangedHandler });

        public bool IsDisabled { get { return (bool)GetValue(_isDisabledProperty); } set { SetValue(_isDisabledProperty, value); } }
        private static readonly DependencyProperty _isDisabledProperty = DependencyProperty.Register("IsDisabled", typeof(bool), typeof(OperatorWidget), new UIPropertyMetadata(false));

        public Point Position
        {
            get
            {
                return (Point)GetValue(PositionProperty);
            }
            set
            {
                SetValue(PositionProperty, value);
            }
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(Point), typeof(OperatorWidget), new UIPropertyMetadata() { DefaultValue = new Point() });

        #endregion Dependency properties

        #region public update triggers and accessor

        public void UpdateConnections()
        {
            foreach (var cl in ConnectionsIn)
            {
                cl.Update();
                //var opWidget = cl.Source as OperatorWidget;
                //if (opWidget != null)
                //    opWidget.UpdateCornerRadius();
            }

            foreach (var cl in ConnectionsOut)
            {
                cl.Update();
                //var opWidget = cl.Target as OperatorWidget;
                //if (opWidget != null)
                //    opWidget.UpdateCornerRadius();
            }
        }

        public void UpdateConnectedOps()
        {
            foreach (var cl in ConnectionsIn)
            {
                var opWidget = cl.Source as OperatorWidget;
                if (opWidget != null)
                    opWidget.UpdateCornerRadius();
            }

            foreach (var cl in ConnectionsOut)
            {
                var opWidget = cl.Target as OperatorWidget;
                if (opWidget != null)
                    opWidget.UpdateCornerRadius();
            }
        }

        public double GetVerticalOverlapWith(IConnectableWidget op)
        {
            return Math.Min(Operator.Position.Y + Height, op.Position.Y + op.Height) - Math.Max(Operator.Position.Y, op.Position.Y);
        }

        public double GetHorizontalOverlapWith(IConnectableWidget op)
        {
            return Math.Min(Operator.Position.X + Width, op.Position.X + op.Width) - Math.Max(Operator.Position.X, op.Position.X);
        }

        public Rect GetRangeForInputConnectionLine(OperatorPart input, int multiInputIndex, bool insertConnection = false)
        {
            var zones = OperatorWidgetInputZoneManager.ComputeInputZonesForOp(this);

            OperatorWidgetInputZone matchingZone = null;
            foreach (var zone in zones)
            {
                if (zone.Input == input && zone.MultiInputIndex == multiInputIndex)
                {
                    if (!insertConnection && zone.InsertAtMultiInputIndex)
                        continue;

                    matchingZone = zone;
                    break;
                }
            }

            // Animations on non-relevant paraemters don't have a matching zone...
            if (matchingZone == null)
            {
                return new Rect(0, 0, 0, 0);
            }

            double minX = matchingZone.LeftPosition;
            double maxX = matchingZone.LeftPosition + matchingZone.Width;
            return new Rect(minX, Height, maxX - minX, 0);
        }

        public void ClearHighlightInput()
        {
            XInputZoneIndicators.Children.Clear();
        }

        public List<OperatorWidget> GetOpsConnectedToOutputs(bool onlySnapped = false, bool onlyVisible = true)
        {
            return (from cl in ConnectionsOut
                    where cl.Target is OperatorWidget && (!onlySnapped || cl.IsSnapped)
                                                      && (!onlyVisible || cl.Input.Parent.Visible)
                    select cl.Target as OperatorWidget).ToList();
        }

        public List<OperatorWidget> GetOpsConnectedToInputs(bool onlySnapped = false, bool onlyVisible = true)
        {
            return (from cl in ConnectionsIn
                    where cl.Source is OperatorWidget && (!onlySnapped || cl.IsSnapped)
                                                      && (!onlyVisible || cl.Output.Parent.Visible)
                    select cl.Source as OperatorWidget).ToList();
        }

        public List<OperatorWidget> GetOperatorsSnappedAbove()
        {
            return (from cl in ConnectionsOut
                    where cl.IsSnapped && cl.Target is OperatorWidget
                    select cl.Target as OperatorWidget).ToList();
        }

        public List<OperatorWidget> GetOperatorsSnappedBelow()
        {
            return (from cl in ConnectionsIn
                    where cl.IsSnapped && cl.Source is OperatorWidget
                    select cl.Source as OperatorWidget).ToList();
        }

        public bool CurrentlyDragged()
        {
            return _snapHandler.DragGroup.Contains(this);
        }

        public bool IsInDragGroup(IConnectableWidget el)
        {
            return _snapHandler.DragGroup.Contains(el);
        }

        #endregion public update triggers and accessor

        #region XAML - Dragging widget (moving around and changing width)

        private Point _dragStartPositionOnCanvas;
        private double _dragStartWidth;
        private ConnectionLine _connectionHighlightedForSplitting;

        private void XWidgetThumb_DragStartHandler(object sender, DragStartedEventArgs e)
        {
            e.Handled = true;

            _dragStartPositionOnCanvas = Mouse.GetPosition(_visualParent);

            _isDraggingNewConnection = Keyboard.Modifiers == ModifierKeys.Control;
            if (_isDraggingNewConnection)
            {
                if (Operator.Outputs.Count > 0)
                {
                    var outputIndex = (int)(Math.Floor(Mouse.GetPosition(this).X / Width * Operator.Outputs.Count));
                    CV.CompositionGraphView.ConnectionDragHelper.DoDragDropNewConnection(this, Operator.Outputs[outputIndex]);
                }
                return;
            }

            _snapHandler.Start(IsSelected ? CV.CompositionGraphView.SelectionHandler.SelectedElements
                                            : new List<ISelectable>());
            _isDraggingRightEdge = Mouse.GetPosition(this).X > Width - DRAG_HANDLE_WIDTH;
            if (_isDraggingRightEdge)
            {
                var startEntry = new UpdateOperatorPropertiesCommand.Entry(Operator);
                _updateWidthCommand = new UpdateOperatorPropertiesCommand(Operator, startEntry);
                _dragStartWidth = Width;
            }
        }

        private bool _isDraggingNewConnection = false;
        private bool _isDraggingRightEdge = false;
        private const double DRAG_HANDLE_WIDTH = 10;

        private void XWidgetThumb_DragDeltaHandler(object sender, DragDeltaEventArgs e)
        {
            e.Handled = true;

            var offset = _dragStartPositionOnCanvas - Mouse.GetPosition(_visualParent);

            if (_isDraggingNewConnection)
            {
                return;
            }

            if (_isDraggingRightEdge)
            {
                var orginalPositionInsideWidget = _visualParent.TranslatePoint(_dragStartPositionOnCanvas, this);
                var currentPositionInsideWidget = _visualParent.TranslatePoint(Mouse.GetPosition(_visualParent), this);
                var delta = currentPositionInsideWidget - orginalPositionInsideWidget;

                if ((_dragStartWidth + delta.X) > 2 * CompositionGraphView.GRID_SIZE)
                {
                    Width = Math.Round((_dragStartWidth + delta.X) / CompositionGraphView.GRID_SIZE) * CompositionGraphView.GRID_SIZE;
                    _updateWidthCommand.ChangeEntries[0].Width = Width;
                    _updateWidthCommand.Do();
                    GetAndDrawInputZones();
                    UpdateConnections();
                    UpdateCornerRadius();
                }
            }
            else
            {
                _snapHandler.UpdateBeforeMoving(offset);

                if (ShakeDetector.TestForShaking(Operator.Position))
                {
                    Logger.Debug("Disconnected with shake gesture.");
                    DisconnectGroupAfterShakingOff(_snapHandler.DragGroup);
                }

                // Check for connections that would be dropped on release
                ConnectionLine connectionToHighlight = null;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    connectionToHighlight = TryPickingConnectionLineForSplitting();
                }

                if (_connectionHighlightedForSplitting != null && _connectionHighlightedForSplitting != connectionToHighlight)
                {
                    _connectionHighlightedForSplitting.IsSelected = false;
                }

                if (connectionToHighlight != null)
                {
                    connectionToHighlight.IsSelected = true;
                }
                _connectionHighlightedForSplitting = connectionToHighlight;
            }
        }

        private void XWidgetThumb_DragCompletedHandler(object sender, DragCompletedEventArgs e)
        {
            e.Handled = true;

            if (_isDraggingNewConnection)
            {
                return;
            }

            _snapHandler.Stop(new Vector(e.HorizontalChange, e.VerticalChange));
            var offset = _dragStartPositionOnCanvas - Mouse.GetPosition(_visualParent);
            if (offset.Length < 4)
            {
                if (SelectedEvent != null)
                    SelectedEvent(this, new RoutedEventArgs());
                return;
            }

            if (_isDraggingRightEdge)
            {
                App.Current.UndoRedoStack.AddAndExecute(_updateWidthCommand);
                _updateWidthCommand = null;
                UpdateUI();

                return;
            }

            if (_isDraggingRightEdge)
                return;

            // Split connection if set by OnDragDelta
            if (_connectionHighlightedForSplitting != null &&
                _connectionHighlightedForSplitting.Output.Parent != Operator)
            {
                SplitConnection();
                return;
            }

            UpdateConnectedOps();
        }

        public void UpdateUI()
        {
            GetAndDrawInputZones();
            UpdateConnections();
            UpdateCornerRadius();
        }

        private void SplitConnection()
        {
            var index = _connectionHighlightedForSplitting.GetMultiInputIndex();
            var command = new InsertOperatorCommand(_connectionHighlightedForSplitting.Output,
                                                    _connectionHighlightedForSplitting.Input,
                                                    CV.CompositionGraphView.CompositionOperator, Operator, index);
            App.Current.UndoRedoStack.AddAndExecute(command);
            App.Current.UpdateRequiredAfterUserInteraction = true;

            _connectionHighlightedForSplitting = null;
        }

        private UpdateOperatorPropertiesCommand _updateWidthCommand;

        #endregion XAML - Dragging widget (moving around and changing width)

        #region XAML - mouse events for connection nose

        private void NoseThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            e.Handled = true;

            var outputIdx = FindIndexOfNoseThumb((Thumb)e.Source);
            if (outputIdx < 0)
                return;

            CV.CompositionGraphView.ConnectionDragHelper.DoDragDropNewConnection(this, Operator.Outputs[outputIdx]);
        }

        private void MouseEnterOutputNoseHandler(object sender, MouseEventArgs e)
        {
            int outputIdx = FindIndexOfNoseThumb((Thumb)e.Source);
            if (outputIdx < 0)
                return;
            Path nosePath = XOutputThumbGrid.Children.OfType<Path>().ToArray()[outputIdx];
            nosePath.Fill = new SolidColorBrush(UIHelper.BrightColorFromType(Operator.Outputs[outputIdx].Type));
            nosePath.Fill.Freeze();
        }

        private void MouseLeaveOutputNoseHandler(object sender, MouseEventArgs e)
        {
            int outputIdx = FindIndexOfNoseThumb((Thumb)e.Source);
            if (outputIdx < 0)
                return;
            Path nosePath = XOutputThumbGrid.Children.OfType<Path>().ToArray()[outputIdx];
            if (IsSelected)
            {
                nosePath.Fill = Brushes.White;
            }
            else
            {
                nosePath.Fill = new SolidColorBrush(UIHelper.ColorFromType(Operator.Outputs[outputIdx].Type));
                nosePath.Fill.Freeze();
            }
        }

        private void XOperatorWidgetThumb_DragEnter(object sender, DragEventArgs e)
        {
            GetAndDrawInputZones();
            e.Handled = true;
        }

        private void XOperatorWidgetThumb_DragOver(object sender, DragEventArgs e)
        {
            CV.CompositionGraphView.ConnectionDragHelper.HandleDragOverEvent(e, this);
        }

        private void XOperatorWidgetThumb_DragLeave(object sender, DragEventArgs e)
        {
            GetAndDrawInputZones();
        }

        private void XOperatorWidgetThumb_Drop(object sender, DragEventArgs e)
        {
            var mousePosition = e.GetPosition(this);
            var inputZonesForDrag = OperatorWidgetInputZoneManager.ComputeInputZonesForOp(this);
            var zoneBelowMouse = OperatorWidgetInputZoneManager.FindZoneBelowMouse(inputZonesForDrag, mousePosition);
            if (zoneBelowMouse != null)
            {
                CV.CompositionGraphView.ConnectionDragHelper.Stop(new Vector(0, 0), zoneBelowMouse);
            }
            e.Handled = true;
        }

        #endregion XAML - mouse events for connection nose

        #region update cursor-shapes with modifier keys when connecting

        private void XWidgetThumb_MouseMove(object sender, MouseEventArgs e)
        {
            var ha = BuildHoverArgs(e);
            OperatorHoverUpdateEvent?.Invoke(this, ha);

            UpdateCursorShape();
            e.Handled = true;
        }

        private void XControl_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateCursorShape();
        }

        private void XControl_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateCursorShape();
        }

        private void UpdateCursorShape()
        {
            if (!IsMouseOver)
                return;

            // Connecting
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Cursor = CustomCursorProvider.GetCursorStream(CustomCursorProvider.Cursors.StartConnection);
            }
            // Normal
            else
            {
                if (Mouse.GetPosition(this).X > Width - DRAG_HANDLE_WIDTH)
                {
                    Cursor = Cursors.SizeWE;
                }
                else
                {
                    Cursor = Cursors.Arrow;
                }
            }
        }

        #endregion update cursor-shapes with modifier keys when connecting

        #region Internal Helper Functions

        private ConnectionLine TryPickingConnectionLineForSplitting()
        {
            var numConnectedInputs = Inputs.Count(input => input.Connections.Any());

            ConnectionLine connectionToHighlight = null;

            if (numConnectedInputs == 0)
            {
                var mousePos = Mouse.GetPosition(CV.XCompositionGraphView.XOperatorCanvas);
                var hitResults = UIHelper.HitTestFor<Path>(CV.XCompositionGraphView.XOperatorCanvas, mousePos, 3.0);
                foreach (var r in hitResults)
                {
                    foreach (UIElement child in CV.XCompositionGraphView.XOperatorCanvas.Children)
                    {
                        var cl = child as ConnectionLine;
                        if (cl != null && cl.ConnectionPath == r.VisualHit as Path)
                        {
                            if (cl.Output.Parent != Operator)
                            {
                                connectionToHighlight = cl;
                            }
                        }
                    }
                }
            }
            return connectionToHighlight;
        }

        public void UpdateCornerRadius()
        {
            var snappedAtBottom = (GetOperatorsSnappedBelow().Count > 0);
            var snappedAtTop = (GetOperatorsSnappedAbove().Count > 0);

            double topRadius = snappedAtTop ? 0 : 5;
            double bottomRadius = snappedAtBottom ? 0 : 5;

            XOperatorContent.CornerRadius = new CornerRadius(topRadius, topRadius, bottomRadius, bottomRadius);

            Height = CompositionGraphView.GRID_SIZE + (snappedAtBottom ? 1 : 0);
        }

        private void DisconnectGroupAfterShakingOff(ICollection<OperatorWidget> opWidgets)
        {
            var deleteLater = new List<ConnectionLine>();
            var disconnectedInputs = new List<OperatorWidget>();
            var disconnectedOutputs = new List<OperatorWidget>();
            OperatorPart reconnectInput = null;
            var multiInputIndexForRewiring = -1;

            foreach (var thumb in opWidgets)
            {
                var connectionLineTarget = thumb as IConnectionLineTarget;
                if (connectionLineTarget != null)
                {
                    foreach (var cl in connectionLineTarget.ConnectionsIn)
                    {
                        var sourceWidget = cl.Source as OperatorWidget;
                        if (sourceWidget == null || opWidgets.Contains(sourceWidget))
                            continue;

                        deleteLater.Add(cl);
                        disconnectedInputs.Add(sourceWidget);
                    }
                }

                var src = thumb as IConnectionLineSource;
                if (src == null)
                    continue;

                foreach (var cl in src.ConnectionsOut)
                {
                    var targetWidget = cl.Target as OperatorWidget;
                    if (targetWidget == null || opWidgets.Contains(targetWidget))
                        continue;

                    deleteLater.Add(cl);
                    disconnectedOutputs.Add(targetWidget);
                    reconnectInput = cl.Input;

                    if (cl.Input.IsMultiInput)
                    {
                        multiInputIndexForRewiring = cl.GetMultiInputIndex();
                    }
                }
            }

            if (deleteLater.Any())
            {
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }

            foreach (var cl in deleteLater)
            {
                cl.Remove();
            }

            if (disconnectedInputs.Count != 1 || disconnectedOutputs.Count != 1)
                return;

            var multiIndexForAppending = reconnectInput.IsMultiInput ? multiInputIndexForRewiring
                : 0;

            App.Current.MainWindow.InsertConnectionAt(new Connection(disconnectedInputs[0].Operator.Outputs[0].Parent,
                disconnectedInputs[0].Outputs[0],
                disconnectedOutputs[0].Inputs[0].Parent,
                reconnectInput,
                multiIndexForAppending));
        }

        internal void UpdateColors()
        {
            XOperatorContent.Background = new SolidColorBrush(UIHelper.ColorFromType(Type)) { Opacity = 0.6 };
            XOperatorContent.Background.Freeze();
            XOperatorLabel.Foreground = new SolidColorBrush(UIHelper.BrightColorFromType(Type));
            XOperatorLabel.Foreground.Freeze();
        }

        private void UpdateStyleAndIndicators()
        {
            XAnimationIndicator.Visibility = IsAnimated ? Visibility.Visible : Visibility.Hidden;

            if (!Operator.Definition.Namespace.StartsWith("lib."))
            {
                XOperatorLabel.FontWeight = FontWeights.Bold;
            }

            if (Operator.Name != "")
            {
                XOperatorLabel.FontStyle = FontStyles.Italic;
            }
        }

        private void UpdateInputRanges(List<OperatorWidgetInputZone> inputZones2)
        {
            XInputSeparators.Children.Clear();

            OperatorPart lastInput = null;
            foreach (var zone in inputZones2)
            {
                if (lastInput != null && zone.Input != lastInput)
                {
                    var r = new Rectangle
                    {
                        Height = 4,
                        Width = 1,
                        Fill = Brushes.Black
                    };
                    XInputSeparators.Children.Add(r);
                    Canvas.SetLeft(r, zone.LeftPosition - 2);
                    Canvas.SetBottom(r, 0);
                }
                lastInput = zone.Input;
            }
        }

        private int FindIndexOfNoseThumb(Thumb nose)
        {
            //find starting output nose index
            var foundIdx = -1;
            var i = 0;
            foreach (var thumb in OutputThumbs)
            {
                if (thumb == nose)
                {
                    foundIdx = i;
                    break;
                }
                ++i;
            }
            return foundIdx;
        }

        private void UpdateOutputNoses()
        {
            XOutputThumbGrid.Children.Clear();
            XOutputThumbGrid.ColumnDefinitions.Clear();

            for (var i = 0; i < Operator.Outputs.Count; ++i)
            {
                var cd = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
                XOutputThumbGrid.ColumnDefinitions.Add(cd);

                var nosePath = new Path
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    StrokeThickness = 0.5,
                    Stroke = Brushes.Black,
                    Data = Geometry.Parse("M 3, 10 L 8,5 13,10")
                };
                if (IsSelected)
                {
                    nosePath.Fill = Brushes.White;
                }
                else
                {
                    nosePath.Fill = new SolidColorBrush(UIHelper.ColorFromType(Operator.Outputs[i].Type));
                    nosePath.Fill.Freeze();
                }

                Grid.SetColumn(nosePath, i);
                XOutputThumbGrid.Children.Add(nosePath);

                var nose = new Thumb
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Opacity = 0.0,
                    Focusable = true
                };

                // FIXME ME: assigning these handlers without unloading them might
                // cause memory leaks
                nose.DragStarted += NoseThumb_DragStarted;
                nose.MouseEnter += MouseEnterOutputNoseHandler;
                nose.MouseLeave += MouseLeaveOutputNoseHandler;
                nose.Width = 16;

                nose.ToolTip = String.Format("{0}", Operator.Outputs[i].Name);
                Grid.SetColumn(nose, i);
                XOutputThumbGrid.Children.Add(nose);
            }
        }

        public void UpdateInputZonesUIFromDescription(IEnumerable<OperatorWidgetInputZone> inputZones)
        {
            XInputZoneIndicators.Children.Clear();

            foreach (var zone in inputZones)
            {
                // Required but missing inputs
                if (zone.MetaInput.Relevance == MetaInput.RelevanceType.Required
                    && !zone.Input.Connections.Any())
                {
                    var inputBg = new Rectangle();
                    Canvas.SetLeft(inputBg, zone.LeftPosition + 1);
                    Canvas.SetBottom(inputBg, -2);
                    inputBg.Height = 2;
                    inputBg.Width = zone.Width - 1; // don't overlap range separator
                    inputBg.Fill = zone.IsBelowMouse ? new SolidColorBrush(UIHelper.BrightColorFromType(zone.Input.Type))
                        : new SolidColorBrush(UIHelper.ColorFromType(zone.Input.Type));

                    inputBg.Fill.Freeze();
                    XInputZoneIndicators.Children.Add(inputBg);
                }
                else if (zone.IsBelowMouse)
                {
                    var inputBg = new Rectangle();
                    Canvas.SetLeft(inputBg, zone.LeftPosition + 1);
                    Canvas.SetBottom(inputBg, 1);
                    inputBg.Height = 3;
                    inputBg.Width = zone.Width - 1; // don't overlap range separator
                    inputBg.Fill = zone.IsBelowMouse ? new SolidColorBrush(UIHelper.BrightColorFromType(zone.Input.Type))
                        : new SolidColorBrush(UIHelper.ColorFromType(zone.Input.Type));

                    inputBg.Fill.Freeze();
                    XInputZoneIndicators.Children.Add(inputBg);
                }
            }
        }

        public void GetAndDrawInputZones()
        {
            var inputZonesForDrag = OperatorWidgetInputZoneManager.ComputeInputZonesForOp(this);
            UpdateInputZonesUIFromDescription(inputZonesForDrag);
            UpdateInputRanges(inputZonesForDrag);
        }

        public Rect Bounds
        {
            get { return new Rect(x: Position.X, y: Position.Y, width: Width, height: Height); }
        }

        #endregion Internal Helper Functions

        #region Binding and Value converter

        private void CreateBindingsToOperator()
        {
            var multiBinding = new MultiBinding { Converter = MNameAndTypeToTitleConverter };

            multiBinding.Bindings.Add(new Binding("Name") { Source = Operator });
            multiBinding.Bindings.Add(new Binding("Name") { Source = Operator.Definition });
            BindingOperations.SetBinding(XOperatorLabel, TextBlock.TextProperty, multiBinding);
        }

        private static readonly NameAndTypeToTitleConverter MNameAndTypeToTitleConverter = new NameAndTypeToTitleConverter();

        public class NameAndTypeToTitleConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Count() != 2 || values.Contains(DependencyProperty.UnsetValue))
                {
                    return "binding error";
                }

                var name = (string)values[0];
                var type = (string)values[1];

                return name == string.Empty ? type : "\"" + name + "\"";
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        #endregion Binding and Value converter

        private List<OperatorPart> _outputs = new List<OperatorPart>();
        private List<OperatorPart> _inputs = new List<OperatorPart>();
        private OperatorSnappingHelper _snapHandler;
        private List<ConnectionLine> _connectionsOut = new List<ConnectionLine>();
        private List<ConnectionLine> _connectionsIn = new List<ConnectionLine>();
        private UIElement _visualParent;

        #region notifier

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        #endregion notifier

        #region find composition view

        private CompositionView _CV;

        public CompositionView CV
        {
            get
            {
                if (_CV == null)
                    _CV = UIHelper.FindParent<CompositionView>(this);
                return _CV;
            }
        }

        #endregion find composition view


        #region hover event handling
        public class HoverEventsArgs : EventArgs
        {
            public OperatorWidget OpWidget;
            public float HorizontalPosition;
        }

        public event EventHandler<OperatorWidget.HoverEventsArgs> OperatorHoverStartEvent;
        public event EventHandler<OperatorWidget.HoverEventsArgs> OperatorHoverUpdateEvent;
        public event EventHandler<OperatorWidget.HoverEventsArgs> OperatorHoverEndEvent;


        private void XControl_MouseEnter(object sender, MouseEventArgs e)
        {
            OperatorHoverStartEvent?.Invoke(this, BuildHoverArgs(e));
        }

        private void XControl_MouseMove(object sender, MouseEventArgs e)
        {
            var ha = BuildHoverArgs(e);
            OperatorHoverUpdateEvent?.Invoke(this, ha);
        }

        private void XControl_MouseLeave(object sender, MouseEventArgs e)
        {
            OperatorHoverEndEvent?.Invoke(this, BuildHoverArgs(e));
        }

        private HoverEventsArgs BuildHoverArgs(MouseEventArgs mouseArgs)
        {
            return new HoverEventsArgs() { OpWidget = this, HorizontalPosition = GetRelativeXPosition(mouseArgs) };
        }

        private float GetRelativeXPosition(MouseEventArgs mouseArgs)
        {
            var mousePosition = mouseArgs.GetPosition(this);
            var relativePosition = mousePosition.X / this.ActualWidth;
            return (float)relativePosition;
        }

        #endregion
    }
}