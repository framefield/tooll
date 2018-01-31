// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Core.OperatorPartTraits;
using Framefield.Core.Testing;
using Framefield.Tooll.Components;
using Framefield.Tooll.Components.QuickCreate;
using SharpDX;
using Matrix = System.Windows.Media.Matrix;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;
using Utilities = Framefield.Core.Utilities;
using Framefield.Tooll.Utils;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for CompositionGraphView.xaml
    /// </summary>
    public partial class CompositionGraphView : UserControl, ISelectable
    {
        // Op which content is represented by the composition view
        public Operator CompositionOperator { get { return _compositionOperator; } set { SetCompositionOperator(value); } }

        public SelectionHandler SelectionHandler { get; private set; }
        public List<ConnectionLine> UnfinishedConnections { get; set; }
        public IngredientsManager IngredientsManager { get; private set; }

        /**
         * Selected Operators
         *
         * Note by pixtur: This property is quite important and couple of other components rely on it.
         * The relationship between this property, the FirstSelectedChanged event and m_selectionHandler
         * seems not obvious and should be refactored to something like an ObservableCollection
         * other components can bind to.
         */

        public List<ISelectable> SelectedElements
        {
            get { return SelectionHandler.SelectedElements.Cast<ISelectable>().ToList(); }
            set { SelectionHandler.SetElements(value.Cast<ISelectable>().ToList()); }
        }

        private bool AutoSelectAddedOperators { get; set; }

        /**
         * This property can be used to disable automatic selection of new Operators.
         */
        public bool SelectionChangeEnabled { get; set; }

        public ObservableCollection<TimeClipViewModel> TimeClips { get; private set; }
        public ObservableCollection<TimeMarkerViewModel> TimeMarkers { get; private set; }
        public ObservableCollection<AnnotationViewModel> Annotations { get; private set; }

        public CompositionGraphView()
        {
            InitializeComponent();

            IsSelected = false;
            UnfinishedConnections = new List<ConnectionLine>();
            SelectionHandler = new SelectionHandler();
            SelectionChangeEnabled = true;
            AutoSelectAddedOperators = false;

            IngredientsManager = new IngredientsManager();

            m_FenceSelection = new FenceSelection(SelectionHandler, XOperatorCanvas);
            XFenceCanvas.Children.Add(m_FenceSelection);

            TimeClips = new ObservableCollection<TimeClipViewModel>();
            TimeMarkers = new ObservableCollection<TimeMarkerViewModel>();
            Annotations = new ObservableCollection<AnnotationViewModel>();

            CompositionTarget.Rendering += CompositionTarget_RenderingHandler;

            Loaded += CompositionGraphView_Loaded;
            Unloaded += CompositionGraphView_Unloaded;
        }

        private void CompositionGraphView_Loaded(object sender, RoutedEventArgs e)
        {
            CV.XCompositionToolBar.XBreadCrumbsView.JumpOutEvent += XBreadCrumbs_JumpOutEvent;
            SelectionHandler.SelectionChanged += SelectionChangedHandler;
            KeyUp += KeyUpHandler;
            SizeChanged += CompositionGraphView_SizeChanged;

            var annotationBinding = new Binding()
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = App.Current.MainWindow.CompositionView.CompositionGraphView,
                Path = new PropertyPath("Annotations")
            };

            BindingOperations.SetBinding(XAnnotationsControl, ItemsControl.ItemsSourceProperty, annotationBinding);

            ConnectionDragHelper = new ConnectionDragHelper(this);
        }

        private void CompositionGraphView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateConnectionsFromInputs();
            UpdateConnectionsToOutputs();
        }

        private void CompositionGraphView_Unloaded(object sender, RoutedEventArgs e)
        {
            CV.XCompositionToolBar.XBreadCrumbsView.JumpOutEvent -= XBreadCrumbs_JumpOutEvent;
            SelectionHandler.SelectionChanged -= SelectionChangedHandler;
            KeyUp -= KeyUpHandler;
            SizeChanged -= CompositionGraphView_SizeChanged;
        }

        private void SelectionChangedHandler(object sender, SelectionHandler.SelectionChangedEventArgs e)
        {
            ResetAllOperatorVisuals();
            HighlightConnectionsOfSelectedOperators();
        }

        private void XBreadCrumbs_JumpOutEvent(object sender, EventArgs e)
        {
            var level = sender as BreadCrumbsView.Level;
            CompositionOperator = level.Operator;

            SetupZoomOutTransition(level.SubOperator);
            SelectionHandler.SetElement(FindCorrespondingWidget(level.SubOperator));

            CV.XTimeView.ApplyState(level.TimeViewState);
        }



        public void ReorderInputs()
        {
            var inputsSortedByPosition = new List<InputWidget>();
            foreach (var iw in InputView.Panel.Children)
            {
                if (iw is InputWidget x)
                {
                    inputsSortedByPosition.Add(x);
                }
            }
            inputsSortedByPosition.Sort((a, b) => (a.Position.X.CompareTo(b.Position.X)));

            var needsReorder = false;
            for (int index = 0; index < InputView.Panel.Children.Count; index++)
            {
                if (inputsSortedByPosition[index].OperatorPart != CompositionOperator.Inputs[index])
                {
                    needsReorder = true;
                    break;
                }
            }
            if (!needsReorder)
                return;

            var reorderedMetaInputIds = new List<Guid>();
            foreach (var i in inputsSortedByPosition)
            {
                var opPart = i.OperatorPart;
                var metaInput = CompositionOperator.GetMetaInput(opPart);
                reorderedMetaInputIds.Add(metaInput.ID);
            }
            var reorderCommand = new ReorderInputsCommand(CompositionOperator, reorderedMetaInputIds);
            App.Current.UndoRedoStack.AddAndExecute(reorderCommand);

            App.Current.UpdateRequiredAfterUserInteraction = true;
            SelectionHandler.SetElements(SelectionHandler.SelectedElements);
            SetCompositionOperator(CompositionOperator);
        }

        #region public command methods

        public void CopySelectionToClipboard()
        {
            var owsToDuplicate = (from el in SelectionHandler.SelectedElements
                                  let opWidget = el as OperatorWidget
                                  where opWidget != null
                                  select opWidget).ToList();

            var owsWithConnectedHiddenOps = new HashSet<OperatorWidget>(owsToDuplicate);
            foreach (var ow in owsToDuplicate)
                GetHiddenWidgetsConnectedToInputs(ow, true).ForEach(el => owsWithConnectedHiddenOps.Add(el));

            var opsToDuplicate = (from opWidget in owsWithConnectedHiddenOps select opWidget.Operator.ID).ToArray();

            if (opsToDuplicate.Any())
            {
                var containerOp = new MetaOperator(Guid.NewGuid()) { Name = "ClipboardContainer" };

                var cmd = new CopyOperatorsCommand(opsToDuplicate, CompositionOperator.Definition, containerOp, ScreenCenter);
                cmd.Do();

                using (var writer = new StringWriter())
                {
                    MetaManager.WriteOpWithWriter(containerOp, writer);
                    try
                    {
                        Clipboard.SetText(writer.ToString(), TextDataFormat.UnicodeText);
                    }
                    catch (Exception)
                    {
                        Logger.Error("Could not copy elements to clipboard. Perhaps a tool like Teamviewer locks it.");
                    }
                }
            }
        }

        public void PasteClipboard()
        {
            var commandString = Clipboard.GetText(TextDataFormat.UnicodeText);
            using (var reader = new StringReader(commandString))
            {
                try
                {
                    var containerOp = MetaManager.Instance.ReadMetaOpFromReader(reader);

                    SelectionHandler.Clear();
                    AutoSelectAddedOperators = true;

                    var pointInCanvas = XBackgroundThumb.IsMouseOver ? Mouse.GetPosition(XOperatorCanvas) : ScreenCenter;
                    var cmd = new CopyOperatorsCommand(containerOp, CompositionOperator.Definition, pointInCanvas);
                    cmd.Do();

                    AutoSelectAddedOperators = false;
                }
                catch (Exception ex)
                {
                    Logger.Error(String.Format("Pasting clipboard failed:\n {0}", ex));
                }
            }
        }

        public Point ScreenCenter
        {
            get
            {
                var m = ViewMatrix;
                m.Invert();
                return m.Transform(new Point(XBackgroundGrid.ActualWidth * 0.5, XBackgroundGrid.ActualWidth * 0.5));
            }
        }

        public void RemoveSelectedElements()
        {
            MainWindow mainWindow = App.Current.MainWindow;

            if (this._CV.XTimeView.XAnimationCurveEditor._SelectionHandler.SelectedElements.Count > 0)
            {
                this._CV.XTimeView.XAnimationCurveEditor.DeleteSelectedKeys();
                return;
            }

            SelectionHandler.Enabled = false;
            // Make a copy because removing elements causes a change of the SelectedElements
            var selectedElements = new HashSet<IConnectableWidget>();
            SelectionHandler.SelectedElements.ForEach(el =>
                                                      {
                                                          selectedElements.Add(el as IConnectableWidget);

                                                          var ow = el as OperatorWidget;
                                                          if (ow != null)
                                                              GetHiddenWidgetsConnectedToInputs(ow, true).ForEach(el2 => selectedElements.Add(el2));
                                                      });

            foreach (var element in selectedElements)
            {
                var inputWidget = element as InputWidget;
                if (inputWidget != null)
                {
                    App.Current.UndoRedoStack.AddAndExecute(new RemoveInputCommand(CompositionOperator, inputWidget.OperatorPart));
                }

                var outputWidget = element as OutputWidget;
                if (outputWidget != null)
                    mainWindow.RemoveOutput(outputWidget.OperatorPart);
            }

            // todo: make whole selection delete operation one macro command combined of deleteOps, deleteConnections, delete inputs and delete outputs
            var opsToDelete = from element in selectedElements
                              let opWidget = element as OperatorWidget
                              where opWidget != null
                              select opWidget.Operator;

            mainWindow.RemoveOperators(opsToDelete);

            SelectionHandler.Enabled = true;

            SelectCompositionOperator();
        }

        public void CenterAllOrSelectedElements()
        {
            var operatorWidgetsToCenter = new List<OperatorWidget>();
            var someAreSelected = SelectionHandler.SelectedElements.Count > 0
                                  && !SelectionHandler.SelectedElements.Contains(this);
            if (someAreSelected)
            {
                operatorWidgetsToCenter.AddRange(from e in SelectionHandler.SelectedElements
                                                 let opWidget = e as OperatorWidget
                                                 where opWidget != null && opWidget.Operator.Visible
                                                 select opWidget);
            }
            else
            {
                operatorWidgetsToCenter.AddRange(from object e in XOperatorCanvas.Children
                                                 let opWidget = e as OperatorWidget
                                                 where opWidget != null && opWidget.Operator.Visible
                                                 select opWidget);
            }

            if (!operatorWidgetsToCenter.Any())
                return;

            // Average Position
            var newCanvasCenter = new Point(0, 0);
            foreach (var ow in operatorWidgetsToCenter)
            {
                newCanvasCenter.X += ow.Operator.Position.X + ow.Operator.Width * 0.5;
                newCanvasCenter.Y += ow.Operator.Position.Y;
            }
            newCanvasCenter.X /= operatorWidgetsToCenter.Count;
            newCanvasCenter.Y /= operatorWidgetsToCenter.Count;

            // Compute new center-matrix
            var newMatrix = new Matrix();
            var halfCanvasSize = new Point(XOperatorCanvas.ActualWidth * 0.5, XOperatorCanvas.ActualHeight * 0.5);
            var newTopLeft = newCanvasCenter - halfCanvasSize;
            newMatrix.Translate(-newTopLeft.X, -newTopLeft.Y);
            _viewMatrixSmoother.SetTransitionTarget(newMatrix);

            UpdateConnectionsFromInputs();
            UpdateConnectionsToOutputs();
        }

        public void SetTransitionTarget(Matrix matrix)
        {
            _viewMatrixSmoother.SetTransitionTarget(matrix);
        }

        public void StickySelectedElement()
        {
            if (!SelectionHandler.SelectedElements.Any())
                return;

            var ow = SelectionHandler.SelectedElements[0] as OperatorWidget;
            if (ow != null)
            {
                App.Current.MainWindow.XRenderView.XLockedButton.IsChecked = false;
                App.Current.MainWindow.XRenderView.SetOperatorWidget(ow);
                App.Current.MainWindow.XRenderView.XLockedButton.IsChecked = true;
            }
        }

        public AddOperatorAndConnectToInputsCommand AddOperatorAtCenter(MetaOperator metaOp)
        {
            var oldSelectedOperators = (from op in SelectedElements
                                        where op is OperatorWidget
                                        select (op as OperatorWidget).Operator);
            var command = new AddOperatorAndConnectToInputsCommand(CompositionOperator, metaOp, oldSelectedOperators, ScreenCenter);
            App.Current.UndoRedoStack.AddAndExecute(command);
            App.Current.UpdateRequiredAfterUserInteraction = true;
            return command;
        }

        public void AddOperatorAtPosition(MetaOperator metaOp, Point mousePos)
        {
            foreach (var level in CV.XCompositionToolBar.XBreadCrumbsView.Hierarchy)
            {
                if (level.Operator.Definition == metaOp)
                {
                    MessageBox.Show("You cannot create an Operator within itself");
                    return;
                }
            }

            var oldSelectedOperators = (from op in SelectedElements
                                        where op is OperatorWidget
                                        select (op as OperatorWidget).Operator);
            var command = new AddOperatorAndConnectToInputsCommand(CompositionOperator, metaOp, oldSelectedOperators, mousePos);
            App.Current.UndoRedoStack.AddAndExecute(command);
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        public void ShowShadowOps()
        {
            SetShadowOpOpacity(0.3);
        }

        public void HideShadowOps()
        {
            SetShadowOpOpacity(0.0);
        }

        #endregion public command methods

        #region UI update handlers

        /**
         * This update handler is called on every frame to interpolate the _ViewMatrix into
         * the _ViewMatrixInterpolationTarget.
         */

        private void CompositionTarget_RenderingHandler(object sender, EventArgs e)
        {
            if (_viewMatrixSmoother.Update())
            {
                ViewMatrix = _viewMatrixSmoother.ViewMatrix;
                UpdateConnectionsFromInputs();
                UpdateConnectionsToOutputs();
            }

            if (_opWidgetToFocusAfterRender != null)
            {
                FocusNewlyAddedAnnotation(_opWidgetToFocusAfterRender);
                _opWidgetToFocusAfterRender = null;
            }
        }

        #endregion UI update handlers

        #region Selection Event handling

        private void OpWidget_OperatorSelectedHandler(object sender, RoutedEventArgs e)
        {
            var opWidget = sender as OperatorWidget;
            if (opWidget != null)
            {
                SelectElement(opWidget);
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
        }

        private void OpWidget_OperatorOpenedHandler(object sender, RoutedEventArgs e)
        {
            var opWidget = sender as OperatorWidget;

            var isLibOperator = opWidget.Operator.Definition.Namespace.StartsWith("lib.");

            if (isLibOperator && !App.Current.UserSettings.GetOrSetDefault("UI.SkipLibOperatorWarnings", false))
            {
                var dialog = new Components.Dialogs.OpenLockedOperatorDialog();
                dialog.XText.Text = dialog.XText.Text.Replace("[OperatorName]", opWidget.Operator.Definition.Namespace + "." + opWidget.Operator.Definition.Name);

                var opUsages = App.Current.MainWindow.GetUsagesOfOperator(opWidget.Operator.Definition);
                dialog.XText.Text = dialog.XText.Text.Replace("[Count]", String.Format("{0}", opUsages.Count()));
                dialog.ShowDialog();

                // Process data entered by user if dialog box is accepted
                if (dialog.DialogResult != true)
                {
                    return;
                }

                if (dialog.DontAskAgain)
                {
                    App.Current.UserSettings["UI.SkipLibOperatorWarnings"] = true;
                }
            }

            CV.XCompositionToolBar.XBreadCrumbsView.Push(opWidget.Operator);

            CompositionOperator = opWidget.Operator;

            var timeClip = FindConnectedTimeClipInOutputs(opWidget);
            if (timeClip != null)
            {
                const double margin = 0.1;
                CV.XTimeView.StartTime = timeClip.StartTime;
                CV.XTimeView.EndTime = timeClip.EndTime;
                var scale = CV.XTimeView.ActualWidth / (timeClip.EndTime - timeClip.StartTime);
                CV.XTimeView.TimeOffset = timeClip.StartTime - CV.XTimeView.ActualWidth / scale * margin;
                CV.XTimeView.TimeScale = CV.XTimeView.ActualWidth / ((timeClip.EndTime - timeClip.StartTime) * (1.0 + 2 * margin));
            }
        }

        public static ITimeClip FindConnectedTimeClipInOutputs(OperatorWidget ow)
        {
            if (ow == null)
                return null;

            if (ow.Operator.InternalParts.Count > 0)
            {
                var tc = ow.Operator.InternalParts[0].Func as ITimeClip;
                if (tc != null)
                    return tc;
            }

            foreach (var conLine in ow.ConnectionsOut)
            {
                var target = conLine.Target as OperatorWidget;
                var tc = FindConnectedTimeClipInOutputs(target);
                if (tc != null)
                    return tc;
            }

            return null;
        }

        private void ConnectionLine_SelectedHandler(object sender, RoutedEventArgs e)
        {
            SelectElement(sender as ConnectionLine);
        }

        private void OnInputSelected(object sender, RoutedEventArgs e)
        {
            SelectElement(sender as InputWidget);
        }

        private void OnOutputSelected(object sender, RoutedEventArgs e)
        {
            SelectElement(sender as OutputWidget);
        }

        #endregion Selection Event handling

        #region model change event handles

        private void CompositionOperator_OperatorAddedHandler(object sender, OperatorChangedEventArgs e)
        {
            var opWidget = AddOperatorWidget(e.Operator);
            if (e.Operator.Visible)
                SelectElement(opWidget);
        }

        private void CompositionOperator_OperatorRemovedHandler(object sender, OperatorChangedEventArgs e)
        {
            var opWidgetToRemove = FindCorrespondingWidget(e.Operator);
            XOperatorCanvas.Children.Remove(opWidgetToRemove);
            SelectionHandler.RemoveElement(opWidgetToRemove);

            if (e.Operator.InternalParts.Count > 0)
            {
                if (e.Operator.InternalParts[0].Func is ITimeClip)
                {
                    var tcvm = TimeClips.FirstOrDefault(el => el.OperatorWidget == opWidgetToRemove);
                    if (tcvm != null)
                        TimeClips.Remove(tcvm);
                }
                if (e.Operator.InternalParts[0].Func is ITimeMarker)
                {
                    var tmvm = TimeMarkers.FirstOrDefault(el => el.OperatorWidget == opWidgetToRemove);
                    if (tmvm != null)
                        TimeMarkers.Remove(tmvm);
                }
                if (e.Operator.InternalParts[0].Func is IAnnotation)
                {
                    var anvm = Annotations.FirstOrDefault(el => el.OperatorWidget == opWidgetToRemove);
                    if (anvm != null)
                        Annotations.Remove(anvm);
                }
            }
        }

        private void CompositionOperator_ConnectionInsertedHandler(object sender, ConnectionChangedEventArgs e)
        {
            AddConnectionLine(e.Connection);
        }

        private void CompositionOperator_ConnectionRemovedHandler(object sender, ConnectionChangedEventArgs e)
        {
            IConnectionLineSource source = null;
            if (e.Connection.SourceOp != null)
                source = FindCorrespondingWidget(e.Connection.SourceOp);
            else
                source = FindCorrespondingInputWidget(e.Connection.SourceOpPart);

            IConnectionLineTarget target = null;
            if (e.Connection.TargetOp != null)
                target = FindCorrespondingWidget(e.Connection.TargetOp);
            else
                target = FindCorrespondingOutputWidget(e.Connection.TargetOpPart);

            var opWidget = target as OperatorWidget;

            if ((source == null) || (target == null))
                return; // newConnection has no corresponding widget (eg connections to op parts)

            ConnectionLine connectionToRemove = FindCorrespondingWidget(e.Connection.ID);
            source.ConnectionsOut.Remove(connectionToRemove);
            target.ConnectionsIn.Remove(connectionToRemove);

            XOperatorCanvas.Children.Remove(connectionToRemove);
            SelectionHandler.RemoveElement(connectionToRemove);
        }

        private void CompositionOperator_InputAddedHandler(object sender, OperatorPartChangedEventArgs e)
        {
            var op = sender as Operator;
            if (op != null)
            {
                AddInputAndSelect(e.OperatorPart);

                //FIXME: The following code is a stub
                //Framefield.Tooll.Components.CodeEditor.ParameterCodeGenerator.UpdateParameterCode(op.Definition);
                //XCodeEditor.MetaOperator= op.Definition;
            }
        }

        private void CompositionOperator_InputRemovedHandler(object sender, OperatorPartChangedEventArgs e)
        {
            RemoveInput(e.OperatorPart);
        }

        private void RemoveInput(OperatorPart opPart)
        {
            var input = FindCorrespondingInputWidget(opPart);
            InputView.Panel.Children.Remove(input);
            SelectionHandler.RemoveElement(input);
        }

        private void CompositionOperator_OutputAddedHandler(object sender, OperatorPartChangedEventArgs e)
        {
            AddOutputAndSelect(e.OperatorPart);
        }

        #endregion model change event handles



        #region XAML event handlers

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            UpdateConnectionsFromInputs();
            UpdateConnectionsToOutputs();

            Matrix m = ViewMatrix;
            Point p = e.MouseDevice.GetPosition(XOperatorCanvas);
            Point p2 = m.Transform(p);

            double scale = (e.Delta > 0) ? MOUSE_WHEEL_ZOOM_SPEED : 1.0 / MOUSE_WHEEL_ZOOM_SPEED;
            m.ScaleAt(scale, scale, p2.X, p2.Y);

            _viewMatrixSmoother.SetTransitionTarget(m);
            UpdateConnectionsFromInputs();
            UpdateConnectionsToOutputs();
        }

        // Fence selection
        private void OnDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                _isRightMouseDragging = true;
                _viewMatrixSmoother.StartDragging();
            }
            else
            {
                m_FenceSelection.HandleDragStarted(sender, e);
            }
            m_DragStartPosition = new Point(e.HorizontalOffset, e.VerticalOffset);
        }

        private void OnDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (_isRightMouseDragging)
            {
                _viewMatrixSmoother.DragDelta(e.HorizontalChange, e.VerticalChange);
            }
            else
            {
                m_FenceSelection.HandleDragDelta(sender, e);
            }
            UpdateConnectionsFromInputs();
            UpdateConnectionsToOutputs();
        }

        private List<AnnotationViewModel> FindAnnotationsInsideSelectionFence()
        {
            var visualParent = this.VisualParent as UIElement;

            var selectedAnnotations = new List<AnnotationViewModel>();
            var fenceBounds = m_FenceSelection.Bounds;

            foreach (var a in Annotations)
            {
                var bounds = new Rect(
                    point1: XOperatorCanvas.TranslatePoint(a.Position, XBackgroundGrid),
                    point2: XOperatorCanvas.TranslatePoint(a.Position + new Vector(a.Width, a.Height), XBackgroundGrid)
                );

                if (fenceBounds.Contains(bounds))
                {
                    selectedAnnotations.Add(a);
                }
            }
            return selectedAnnotations;
        }

        private void OnDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (_isRightMouseDragging)
            {
                _viewMatrixSmoother.StopDragging(true);
                _isRightMouseDragging = false;
                e.Handled = false;
            }
            else
            {
                m_FenceSelection.HandleDragCompleted(sender, e);
                var dragWasClick = (Math.Abs(e.HorizontalChange) + Math.Abs(e.VerticalChange)) < SystemParameters.MinimumHorizontalDragDistance;
                if (dragWasClick)
                {
                    Point pointInCanvas = new Point(m_DragStartPosition.X + e.HorizontalChange, m_DragStartPosition.Y + e.VerticalChange);
                    Point pointInCompositionView = (sender as UIElement).TranslatePoint(pointInCanvas, XOperatorCanvas);
                    bool wasHandled = ProcessClickEventOnConnections(pointInCompositionView);

                    if (!wasHandled && SelectionHandler != null)
                    {
                        SelectionHandler.SetElement(this);
                    }
                    else
                    {
                        e.Handled = false;
                    }
                }
                else
                {
                    foreach (var a in FindAnnotationsInsideSelectionFence())
                    {
                        SelectionHandler.AddElement(a.OperatorWidget);
                    }
                }
            }
        }

        private bool _isRightMouseDragging = false;
        private bool m_IsRightMouseZoom = false;
        private Point m_DragStartPosition;
        private Matrix m_MatrixOnDragStart;

        private void OnMouseRightDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                _viewMatrixSmoother.StartDragging();

                m_MatrixOnDragStart = ViewMatrix;
                m_IsRightMouseZoom = (Keyboard.Modifiers == ModifierKeys.Alt);

                UIElement el = sender as UIElement;
                if (el != null)
                {
                    el.CaptureMouse();
                    m_DragStartPosition = e.GetPosition(this);
                    _isRightMouseDragging = true;
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isRightMouseDragging)
            {
                double deltaX = -(m_DragStartPosition.X - e.GetPosition(this).X);
                double deltaY = -(m_DragStartPosition.Y - e.GetPosition(this).Y);

                if (m_IsRightMouseZoom)
                {
                    Point p = e.MouseDevice.GetPosition(XOperatorCanvas);
                    Point p2 = m_DragStartPosition;
                    double scale = deltaY == 0
                                       ? 1.0
                                       : Math.Max(0.2, Math.Min(10.0, Math.Pow(1.1, -deltaY * 0.06)));

                    Matrix m = m_MatrixOnDragStart;
                    m.ScaleAt(scale, scale, p2.X, p2.Y);
                    _viewMatrixSmoother.SetTransitionTarget(m);
                }
                else
                {
                    _viewMatrixSmoother.DragDelta(deltaX, deltaY);
                }
                UpdateConnectionsFromInputs();
                UpdateConnectionsToOutputs();
            }
        }

        private Point _contextMenuPosition;

        private void OnMouseRightUp(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDragging = false;
            UIElement thumb = sender as UIElement;
            if (thumb != null)
            {
                thumb.ReleaseMouseCapture();
                bool showContextMenu = Math.Abs(m_DragStartPosition.X - e.GetPosition(this).X) + Math.Abs(m_DragStartPosition.Y - e.GetPosition(this).Y) < 3;

                XBackgroundGrid.ContextMenu = null;
                if (showContextMenu)
                {
                    XBackgroundGrid.ContextMenu = Resources["GraphContextMenu"] as System.Windows.Controls.ContextMenu;
                    _contextMenuPosition = Mouse.GetPosition(this.XOperatorCanvas);
                }

                _viewMatrixSmoother.StopDragging(startFlicking: !showContextMenu);
            }
        }

        protected void OnMouseDoubleClick(object sender, MouseButtonEventArgs args)
        {
            SelectedElements.Clear();
            CustomCommands.SelectAndAddOperatorAtCursorCommand.Execute(null, this);
        }

        private void OnButtomSplitterDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            UpdateConnectionsFromInputs();
        }

        #endregion XAML event handlers

        #region XAML Drop handling

        private void WorkspaceCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("METAOP") || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void WorkspaceCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("METAOP"))
            {
                MetaOperator metaOp = e.Data.GetData("METAOP") as MetaOperator;
                var position = e.GetPosition(XOperatorCanvas);

                // NOTE by Pixtur: The command pattern now prevents us from selecting the newly created operator.
                App.Current.UndoRedoStack.AddAndExecute(new AddOperatorCommand(CompositionOperator, metaOp.ID, (int)position.X, (int)position.Y));

                // Close QuickCreateWindow
                foreach (var w in App.Current.Windows)
                {
                    var QCW = w as QuickCreateWindow;
                    if (QCW != null)
                    {
                        QCW.Close();
                        break;
                    }
                }
            }
        }

        // This is only a stub to later potentially update the newConnection line
        // currently constructed. The problem is caused by the fact, that during
        // "real" drag and drop operations between different controls, the
        // mouse position can't be easily captured. So far, this event is the
        // only handler that is been updated on mouse move. However, this updated position
        // still has to be feed back to the original OperatorWidget triggering
        // the new Connection to be constructed.
        //
        // This mess needs to be cleaned up once we refactor the CompositionGraph
        // view together with the drag and drop implementation.
        //
        // Also see http://bea.stollnitz.com/blog/?p=53
        //
        private void WorkspaceCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Tooll.ConnectionDragHelper.CONNECTION_LINE_OUTPUT_IDENTIFIER))
            {
                Point pos = e.GetPosition(this);
            }
        }

        #endregion XAML Drop handling

        #region command event handlers

        private void CombineSelectionEventHandler(object sender, RoutedEventArgs e)
        {
            var owsToCombine = (from el in SelectionHandler.SelectedElements
                                let opWidget = el as OperatorWidget
                                where opWidget != null
                                select opWidget).ToList();

            if (owsToCombine.Count == 0)
                return;

            var dialog = new Components.Dialogs.NewOperatorDialog();
            dialog.XText.Text = String.Format("Combine {0} objects into a new object type?", owsToCombine.Count);
            dialog.Title = "Combine Operators";
            dialog.XNamespace.Text = _compositionOperator.Definition.Namespace;
            dialog.XName.Text = "CombinedOp";
            dialog.XName.SelectAll();
            dialog.XName.Focus();
            dialog.ShowDialog();

            // Process data entered by user if dialog box is accepted
            if (dialog.DialogResult == true)
            {
                HashSet<OperatorWidget> owsWithConnectedHiddenOps = new HashSet<OperatorWidget>(owsToCombine);
                foreach (var ow in owsToCombine)
                    GetHiddenWidgetsConnectedToInputs(ow, true).ForEach(el => owsWithConnectedHiddenOps.Add(el));

                Point newPos = new Point(0, 0);
                foreach (var ow in owsToCombine)
                {
                    newPos.X += ow.Operator.Position.X + ow.Operator.Width * 0.5;
                    newPos.Y += ow.Operator.Position.Y;
                }
                newPos.X /= owsToCombine.Count;
                newPos.Y /= owsToCombine.Count;

                var newOpName = dialog.XName.Text;
                var newOpNameSpace = dialog.XNamespace.Text;
                var newOpDescription = dialog.XDescription.Text;
                var idsToCombine = from op in owsWithConnectedHiddenOps
                                   select op.Operator;
                App.Current.UndoRedoStack.AddAndExecute(new CombineToNewOperatorCommand(CompositionOperator, idsToCombine, newOpName, newOpNameSpace, newOpDescription, newPos));
            }
        }

        private void UngroupHandler(object sender, RoutedEventArgs e)
        {
            var owsToUngroup = (from el in SelectionHandler.SelectedElements
                                let opWidget = el as OperatorWidget
                                where opWidget != null
                                select opWidget).ToList();

            if (owsToUngroup.Count != 1)
                return;

            var opToUngroup = owsToUngroup.First().Operator;
            SelectedElements.Clear();
            AutoSelectAddedOperators = true;
            App.Current.UndoRedoStack.AddAndExecute(new UngroupOperatorCommand(opToUngroup));
            AutoSelectAddedOperators = false;
        }

        private void DuplicateAsNewTypeHandler(object sender, RoutedEventArgs e)
        {
            if (SelectionHandler.SelectedElements.Count != 1 || !(SelectionHandler.SelectedElements[0] is OperatorWidget))
            {
                MessageBox.Show("You need to select a single operator");
                return;
            }

            var originalOperator = SelectionHandler.SelectedElements[0] as OperatorWidget;
            var copiedMetaOp = UIHelper.DuplicateOperatorTypeWithDialog(originalOperator.Operator.Definition);
            if (copiedMetaOp == null)
                return;

            var compoGraphView = App.Current.MainWindow.CompositionView.CompositionGraphView;

            const int initialOffset = 100;
            var position = new Point(originalOperator.Position.X + initialOffset, originalOperator.Position.Y + initialOffset);

            var command = new AddOperatorCommand(compoGraphView.CompositionOperator, copiedMetaOp.ID, position.X, position.Y);
            App.Current.UndoRedoStack.AddAndExecute(command);

            // apply current parameter values of original op to new op
            var newOperator = (SelectionHandler.SelectedElements[0] as OperatorWidget).Operator;
            for (var inputIdx = 0; inputIdx < originalOperator.Inputs.Count; ++inputIdx)
            {
                var valueFunc = originalOperator.Operator.Inputs[inputIdx].Func as Utilities.ValueFunction;
                if (valueFunc != null)
                    newOperator.Inputs[inputIdx].Func = Utilities.CreateValueFunction(valueFunc.Value);
            }

            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void RunTestsHandler(object sender, RoutedEventArgs e)
        {
            if (SelectionHandler.SelectedElements.Count != 1)
            {
                MessageBox.Show("You need to select a single operator");
            }
            else
            {
                var selectedOp = SelectionHandler.SelectedElements[0] as OperatorWidget;
                if (selectedOp != null)
                {
                    using (new Utils.SetWaitCursor())
                    {
                        var result = TestUtilities.EvaluateTests(selectedOp.Operator, "");
                        Logger.Info(result.Item2);
                    }
                }
            }
        }

        #endregion command event handlers

        #region private helper methods for UI

        private void SetCompositionOperator(Operator value)
        {
            _preventUIUpdates = true;

            _compositionOperator.OperatorAddedEvent -= CompositionOperator_OperatorAddedHandler;
            _compositionOperator.OperatorRemovedEvent -= CompositionOperator_OperatorRemovedHandler;
            _compositionOperator.ConnectionAddedEvent -= CompositionOperator_ConnectionInsertedHandler;
            _compositionOperator.ConnectionRemovedEvent -= CompositionOperator_ConnectionRemovedHandler;
            _compositionOperator.InputAddedEvent -= CompositionOperator_InputAddedHandler;
            _compositionOperator.InputRemovedEvent -= CompositionOperator_InputRemovedHandler;
            _compositionOperator.OutputAddedEvent -= CompositionOperator_OutputAddedHandler;
            _compositionOperator.OutputRemovedEvent -= CompositionOperator_OutputRemovedHandler;

            _compositionOperator = value;

            /**
             * List list of timeClips in the current compositionOperator needs to be updated
             * every time we enter/leave a sub operator. This will then automatically
             * update the TimeClipEditor.
             */
            TimeClips.Clear();
            TimeMarkers.Clear();
            Annotations.Clear();

            ClearGUI();

            if (_compositionOperator.Parent == null)
            {
                CV.XCompositionToolBar.XBreadCrumbsView.Clear();
                CV.XCompositionToolBar.XBreadCrumbsView.Push(_compositionOperator);
            }

            _compositionOperator.InternalOps.ForEach(op => AddOperatorWidget(op));
            _compositionOperator.Outputs.ForEach(opPart => AddOutput(opPart));
            LayoutOutputOperatorWidgets();


            _compositionOperator.Inputs.ForEach(opPart => AddInput(opPart));


            if (_compositionOperator.InternalParts.Count > 0)
            {
                XOperatorCanvas.Visibility = Visibility.Hidden;
                XCodeEditor.Visibility = Visibility.Visible;
                XCodeEditor.Operator = _compositionOperator;
            }
            else
            {
                foreach (var connection in _compositionOperator.Connections)
                    AddConnectionLine(connection);

                XOperatorCanvas.Visibility = Visibility.Visible;
                XCodeEditor.Compile();
                XCodeEditor.Operator = null;
                XCodeEditor.Visibility = Visibility.Hidden;
            }

            _compositionOperator.OperatorAddedEvent += CompositionOperator_OperatorAddedHandler;
            _compositionOperator.OperatorRemovedEvent += CompositionOperator_OperatorRemovedHandler;
            _compositionOperator.ConnectionAddedEvent += CompositionOperator_ConnectionInsertedHandler;
            _compositionOperator.ConnectionRemovedEvent += CompositionOperator_ConnectionRemovedHandler;
            _compositionOperator.InputAddedEvent += CompositionOperator_InputAddedHandler;
            _compositionOperator.InputRemovedEvent += CompositionOperator_InputRemovedHandler;
            _compositionOperator.OutputAddedEvent += CompositionOperator_OutputAddedHandler;
            _compositionOperator.OutputRemovedEvent += CompositionOperator_OutputRemovedHandler;

            _preventUIUpdates = false;

            SelectCompositionOperator();
            ResetAllOperatorVisuals();

            var viewMatrices = App.Current.UserSettings["View.WorkspaceMatrices"] as Dictionary<Guid, Matrix>;
            if (viewMatrices.ContainsKey(_compositionOperator.Definition.ID))
            {
                ViewMatrix = viewMatrices[_compositionOperator.Definition.ID];
                _viewMatrixSmoother.SetMatrix(ViewMatrix);
            }
            else
            {
                CenterAllOrSelectedElements();
            }
        }

        private void LayoutOutputOperatorWidgets()
        {
            double x = 10;
            foreach (var child in OutputView.Panel.Children)
            {
                IConnectableWidget e = child as IConnectableWidget;
                e.Position = new Point(x, 25);
                x += e.Width + 5;
            }
        }

        private void SetupZoomOutTransition(Core.Operator originalCompOp)
        {
            var originalView = ViewMatrix;

            _viewMatrixSmoother.FreezeTransition();

            Matrix zoomInView = new Matrix();
            Point p1 = new Point(originalCompOp.Position.X, originalCompOp.Position.Y);

            zoomInView.Translate(-p1.X - 0.5 * originalCompOp.Width, -p1.Y - 0.5 * GRID_SIZE);
            zoomInView.Scale(10, 10);
            zoomInView.Translate(XOperatorCanvas.ActualWidth * 0.5, XOperatorCanvas.ActualHeight * 0.5);
            ViewMatrix = zoomInView;
            _viewMatrixSmoother.SetMatrix(zoomInView);
            _viewMatrixSmoother.SetTransitionTarget(originalView);
            _viewMatrixSmoother.WaitingForUiRebuild = true;
        }

        private void AddOutputAndSelect(OperatorPart opPart)
        {
            var output = AddOutput(opPart);
            SelectElement(output);
        }

        private OutputWidget AddOutput(OperatorPart opPart)
        {
            var output = new OutputWidget(opPart);
            OutputView.Panel.Children.Add(output);
            output.SelectedEvent += OnOutputSelected;
            return output;
        }

        private void CompositionOperator_OutputRemovedHandler(object sender, OperatorPartChangedEventArgs e)
        {
            RemoveOutput(e.OperatorPart);
        }

        private void AddInputAndSelect(OperatorPart opPart)
        {
            var input = AddInput(opPart);
            SelectElement(input);
        }

        //private List<InputWidget> _inputsWidgets = new List<InputWidget>();

        private InputWidget AddInput(OperatorPart opPart)
        {
            var newInputWidget = new InputWidget(opPart);
            var inputIndex = InputView.Panel.Children.Count;

            InputView.Panel.Children.Add(newInputWidget);


            var posX = 10 + inputIndex * (newInputWidget.Width + 5);
            newInputWidget.Position = new Point(posX, 0);

            newInputWidget.Selected += OnInputSelected;
            return newInputWidget;
        }

        private void RemoveOutput(OperatorPart opPart)
        {
            var output = FindCorrespondingOutputWidget(opPart);
            OutputView.Panel.Children.Remove(output);
            SelectionHandler.RemoveElement(output);
        }

        private List<OperatorWidget> GetHiddenWidgetsConnectedToInputs(OperatorWidget ow, bool ignoreVisibilityOfCurrentWidget)
        {
            List<OperatorWidget> foundOWs = new List<OperatorWidget>();
            if (ow == null)
                return foundOWs;

            if (!ignoreVisibilityOfCurrentWidget && ow.Operator.Visible)
                return foundOWs;

            if (!ow.Operator.Visible)
                foundOWs.Add(ow);

            foreach (var cl in ow.ConnectionsIn)
                foundOWs = foundOWs.Union(GetHiddenWidgetsConnectedToInputs(cl.Source as OperatorWidget, false)).ToList();

            return foundOWs;
        }

        private void SelectElement(IConnectableWidget e)
        {
            if (!SelectionChangeEnabled)
                return;

            bool addSelectionMode = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || AutoSelectAddedOperators;
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && !addSelectionMode)
            {
                SelectionHandler.SetElement(e);
            }
            else if (addSelectionMode)
            {
                SelectionHandler.AddElement(e);
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SelectionHandler.ToggleElement(e);
            }
        }

        public OperatorWidget FindOperatorWidgetById(Guid id)
        {
            foreach (var element in XOperatorCanvas.Children)
            {
                var opWidget = element as OperatorWidget;
                if (opWidget != null && opWidget.Operator.ID == id)
                {
                    return opWidget;
                }
            }
            return null;
        }

        private OperatorWidget FindCorrespondingWidget(Operator op)
        {
            return (from UIElement child in XOperatorCanvas.Children
                    let opWidget = child as OperatorWidget
                    where (opWidget != null) && (opWidget.Operator == op)
                    select opWidget).First();
        }

        private ConnectionLine FindCorrespondingWidget(Guid connectionID)
        {
            return (from UIElement child in XOperatorCanvas.Children
                    let connection = child as ConnectionLine
                    where connection != null && connection.ID == connectionID
                    select connection).First();
        }

        private InputWidget FindCorrespondingInputWidget(OperatorPart opPart)
        {
            return (from UIElement child in InputView.Panel.Children
                    let iw = child as InputWidget
                    where (iw != null) && (iw.OperatorPart == opPart)
                    select iw).FirstOrDefault();
        }

        private OutputWidget FindCorrespondingOutputWidget(OperatorPart opPart)
        {
            return (from UIElement child in OutputView.Panel.Children
                    let ow = child as OutputWidget
                    where (ow != null) && (ow.OperatorPart == opPart)
                    select ow).FirstOrDefault();
        }

        public void UpdateConnectionsFromInputs()
        {
            foreach (var child in InputView.Panel.Children)
            {
                var iw = child as InputWidget;
                if (iw != null)
                    iw.ConnectionsOut.ForEach(c => c.Update());
            }
        }

        public void UpdateConnectionsToOutputs()
        {
            foreach (var child in OutputView.Panel.Children)
            {
                var ow = child as OutputWidget;
                if (ow != null)
                    ow.ConnectionsIn.ForEach(c => c.Update());
            }
        }

        private void SelectCompositionOperator()
        {
            SelectionHandler.Clear();
            SelectionHandler.AddElement(this);
        }

        private OperatorWidget AddOperatorWidget(Operator op)
        {
            var opWidget = new OperatorWidget(op);
            opWidget.SelectedEvent += OpWidget_OperatorSelectedHandler;
            opWidget.OpenedEvent += OpWidget_OperatorOpenedHandler;

            XOperatorCanvas.Children.Add(opWidget);

            if (op.InternalParts.Count > 0)
            {
                ITimeClip tc = op.InternalParts[0].Func as ITimeClip;
                if (tc != null)
                {
                    TimeClipViewModel tcvm = TimeClips.FirstOrDefault(el => el.OperatorWidget == opWidget);
                    if (tcvm == null)
                        TimeClips.Add(new TimeClipViewModel(opWidget));
                }
                ITimeMarker tm = op.InternalParts[0].Func as ITimeMarker;
                if (tm != null)
                {
                    TimeMarkerViewModel tmvm = TimeMarkers.FirstOrDefault(el => el.OperatorWidget == opWidget);
                    if (tmvm == null)
                        TimeMarkers.Add(new TimeMarkerViewModel(opWidget));
                }

                IAnnotation an = op.InternalParts[0].Func as IAnnotation;
                if (an != null)
                {
                    AnnotationViewModel anvm = Annotations.FirstOrDefault(el => el.OperatorWidget == opWidget);
                    if (anvm == null)
                        Annotations.Add(new AnnotationViewModel(opWidget));
                }
            }
            return opWidget;
        }

        private void AddConnectionLine(Connection connection)
        {
            IConnectionLineSource source = null;
            if (connection.SourceOp != null)
                source = FindCorrespondingWidget(connection.SourceOp);
            else
                source = FindCorrespondingInputWidget(connection.SourceOpPart);

            IConnectionLineTarget target = null;
            if (connection.TargetOp != null)
                target = FindCorrespondingWidget(connection.TargetOp);
            else
                target = FindCorrespondingOutputWidget(connection.TargetOpPart);

            if (source == null || target == null)
                return;

            ConnectionLine cl = new ConnectionLine(connection.ID, _compositionOperator, source, target, connection.SourceOpPart, connection.TargetOpPart);
            source.ConnectionsOut.Add(cl);
            target.ConnectionsIn.Insert(connection.Index, cl);
            if (!_preventUIUpdates)
                target.UpdateConnections();
            var targetWidget = target as OperatorWidget;
            if (targetWidget != null)
            {
                targetWidget.GetAndDrawInputZones();
                targetWidget.UpdateColors();
            }
            cl.SelectedEvent += ConnectionLine_SelectedHandler;

            XOperatorCanvas.Children.Add(cl);
        }

        private bool _preventUIUpdates = false;

        private void UpdateCornerRadiusOfOperators()
        {
            foreach (var c in XOperatorCanvas.Children)
            {
                var opWidget = c as OperatorWidget;
                if (opWidget != null)
                {
                    opWidget.UpdateCornerRadius();
                }
            }
        }

        private void HighlightConnectionsOfSelectedOperators()
        {
            var connectionsToHighlight = new List<ConnectionLine>();
            foreach (var selectedOp in SelectionHandler.GetSelectedElementsOfType<OperatorWidget>())
            {
                connectionsToHighlight.AddRange(selectedOp.ConnectionsIn);
                connectionsToHighlight.AddRange(selectedOp.ConnectionsOut);
            }

            foreach (var child in CV.XCompositionGraphView.XOperatorCanvas.Children)
            {
                var cl = child as ConnectionLine;
                if (cl != null)
                {
                    cl.IsSelected = connectionsToHighlight.Contains(cl);
                }
            }
        }

        private void ResetAllOperatorVisuals()
        {
            foreach (var element in XOperatorCanvas.Children)
            {
                var opWidget = element as OperatorWidget;
                if (opWidget != null)
                {
                    opWidget.IsInactive = true;
                    opWidget.IsDisabled = opWidget.Operator.Disabled;
                }
            }
        }

        private void SetShadowOpOpacity(double opacity)
        {
            foreach (var c in XOperatorCanvas.Children)
            {
                var opWidget = c as OperatorWidget;
                if (opWidget != null)
                {
                    if (!opWidget.Operator.Visible)
                    {
                        opWidget.Opacity = opacity;
                        opWidget.IsHitTestVisible = opacity > 0 ? true : false;
                    }
                }

                var cl = c as ConnectionLine;
                if (cl != null)
                {
                    if (!cl.Output.Parent.Visible ||
                        !cl.Input.Parent.Visible)
                    {
                        cl.Opacity = opacity;

                        cl.IsHitTestVisible = opacity > 0 ? true : false;
                    }
                }
            }
        }

        #endregion private helper methods for UI

        #region interface IConnectableWidget

        public CompositionView CV
        {
            get
            {
                if (_CV == null)
                    _CV = UIHelper.FindParent<CompositionView>(this);
                return _CV;
            }
        }

        public Point Position { get { return new Point(); } set { } }
        public Point PositionOnCanvas { get { return new Point(); } }

        private bool m_IsSelected = false;

        public bool IsSelected
        {
            get { return m_IsSelected; }
            set
            {
                m_IsSelected = value;
                if (IsInitialized)
                {
                    if (m_IsSelected)
                    {
                        IsSelectedIndicator.BorderThickness = new Thickness(1);
                        //m_SelectionHandler.SelectedElements.Clear();                // Todo: Get rid of this update call
                        //ShowCurvesForSelectedOperators();                           // Todo: Get rid of this update call
                    }
                    else
                    {
                        IsSelectedIndicator.BorderThickness = new Thickness(0);
                    }
                }
            }
        }

        public bool CurrentlyDragged()
        {
            return false;
        }

        public bool IsInDragGroup(IConnectableWidget el)
        {
            return false;
        }

        public double GetVerticalOverlapWith(IConnectableWidget el)
        {
            return 0;
        }

        public double GetHorizontalOverlapWith(IConnectableWidget el)
        {
            return 0;
        }

        public void UpdateConnections()
        {
        }

        #endregion interface IConnectableWidget

        #region interface IElementView

        public bool ProcessClickEventOnConnections(Point p)
        {
            bool eventWasHandled = false;
            List<HitTestResult> hitResults = UIHelper.HitTestFor<Path>(XOperatorCanvas, p, 10.0);
            foreach (HitTestResult r in hitResults)
            {
                foreach (UIElement child in XOperatorCanvas.Children)
                {
                    if (child is ConnectionLine cl && cl.ConnectionPath == r.VisualHit as Path)
                    {
                        cl.HandleClicked();
                        eventWasHandled = true;
                    }
                }
            }
            return eventWasHandled;
        }

        private void ClearGUI()
        {
            XOperatorCanvas.Children.Clear();
            InputView.Panel.Children.Clear();
            OutputView.Panel.Children.Clear();
            SelectionHandler.Clear();
        }

        #endregion interface IElementView

        #region private members

        private Matrix ViewMatrix
        {
            get { return XViewTransform.Matrix; }
            set
            {
                if (_compositionOperator != null)
                {
                    var viewMatrices = App.Current.UserSettings["View.WorkspaceMatrices"] as Dictionary<Guid, Matrix>;
                    viewMatrices[_compositionOperator.Definition.ID] = value;
                    App.Current.UserSettings["View.WorkspaceMatrices"] = viewMatrices;
                }
                XViewTransform.Matrix = value;
            }
        }

        //private Matrix _ViewMatrixInterpolationTarget = new Matrix();
        private Components.ViewMatrixSmoother _viewMatrixSmoother = new ViewMatrixSmoother();

        private CompositionView _CV;
        private Operator _compositionOperator = Utilities.CreateEmptyOperator();

        private FenceSelection m_FenceSelection;
        //private BreadCrumbHandler _BreadCrumbsHandler = new BreadCrumbHandler();

        #endregion private members

        public const double CLICK_THRESHOLD = 2;
        public const double MOUSE_WHEEL_ZOOM_SPEED = 1.7;
        public const double GRID_SIZE = 25;

        /**
         * Rename Operator instances on return
         */

        private void KeyUpHandler(object sender, KeyEventArgs e)
        {
            if (XCodeEditor.Visibility == Visibility.Visible)
            {
                return;
            }
            switch (e.Key)
            {
                case Key.Enter:
                    App.Current.MainWindow.XParameterView.FocusNameField();
                    e.Handled = true;
                    break;

                case Key.D:
                    ToggleDisabledForSelectedOperators();
                    e.Handled = true;
                    break;

                case Key.Up:
                    SelectConnectedOutputWidget();
                    e.Handled = true;
                    break;

                case Key.Down:
                    SelectFirstWidgetConnectedBelow();
                    e.Handled = true;
                    break;

                case Key.G:
                    AlignSelectedOperators();
                    e.Handled = true;
                    break;

                case Key.A:
                    AddAnnotation(Mouse.GetPosition(this));
                    e.Handled = true;
                    break;
            }
        }

        private List<OperatorWidget> _newlySelectedOpWidgets = new List<OperatorWidget>();
        private List<OperatorWidget> _freshlySnappedOpWidgets = new List<OperatorWidget>();

        private void AlignSelectedOperators()
        {
            _newlySelectedOpWidgets.Clear();
            _freshlySnappedOpWidgets.Clear();

            foreach (var widget in SelectedElements)
            {
                var opWidget = widget as OperatorWidget;
                if (opWidget == null)
                    continue;

                if (_freshlySnappedOpWidgets.Contains(opWidget))
                    continue;

                var newWidth = RecursivelyAlignChildrenOfSelectedOps(opWidget);
                opWidget.Width = newWidth;
            }

            foreach (var op in _newlySelectedOpWidgets)
            {
                SelectionHandler.AddElement(op);
            }
            App.Current.UpdateRequiredAfterUserInteraction = true;

            // Create command
            var entries = new List<UpdateOperatorPropertiesCommand.Entry>();
            var operators = new List<Operator>();

            foreach (var widget in _freshlySnappedOpWidgets)
            {
                entries.Add(new UpdateOperatorPropertiesCommand.Entry(widget.Operator)
                {
                    Position = widget.Position,
                    Width = widget.Width
                });
                operators.Add(widget.Operator);
            }

            var updatePropertiesCmd = new UpdateOperatorPropertiesCommand(operators, entries);
            App.Current.UndoRedoStack.AddAndExecute(updatePropertiesCmd);

            // Update UI
            foreach (var widget in _freshlySnappedOpWidgets)
            {
                widget.UpdateUI();
            }
            UpdateConnections();
        }

        private double RecursivelyAlignChildrenOfSelectedOps(OperatorWidget selectedOrSnappedWidget)
        {
            _freshlySnappedOpWidgets.Add(selectedOrSnappedWidget);
            var widthOffset = 0.0;
            var inputConnectionsFromLeftToRight = selectedOrSnappedWidget.ConnectionsIn.OrderBy(cl => cl.m_TargetPoint.X);

            // Snap sourceOps
            foreach (var cl in inputConnectionsFromLeftToRight)
            {
                var sourceOp = cl.Source as OperatorWidget;
                if (sourceOp == null)
                {
                    widthOffset += OP_WIDTH;    // keep gaps for published parameters
                    continue;
                }

                // Ignore shadow ops
                if (!sourceOp.Operator.Visible)
                    continue;

                if (_freshlySnappedOpWidgets.Contains(sourceOp))
                {
                    sourceOp.Width += OP_WIDTH;
                    widthOffset -= OP_WIDTH;
                    continue;
                }

                sourceOp.Position = selectedOrSnappedWidget.Position + new Vector(widthOffset, GRID_SIZE);
                _newlySelectedOpWidgets.Add(sourceOp);
                _freshlySnappedOpWidgets.Add(sourceOp);

                if (sourceOp.IsSelected)
                {
                    var widthOpsBelow = RecursivelyAlignChildrenOfSelectedOps(sourceOp);
                    sourceOp.Width = widthOpsBelow;
                    widthOffset += widthOpsBelow;
                }
                else
                {
                    sourceOp.Width = OP_WIDTH;
                    widthOffset += OP_WIDTH;
                }
            }
            if (!selectedOrSnappedWidget.IsSelected)
                _newlySelectedOpWidgets.Add(selectedOrSnappedWidget);

            return Math.Max(widthOffset, OP_WIDTH);
        }

        private const double OP_WIDTH = GRID_SIZE * 4;

        #region navigate with selection through graph

        public void SelectConnectedOutputWidget()
        {
            SelectConnectedWidget(ow => ow.GetOpsConnectedToOutputs());
        }

        private void SelectFirstWidgetConnectedBelow()
        {
            SelectConnectedWidget(ow => ow.GetOpsConnectedToInputs());
        }

        private void SelectConnectedWidget(Func<OperatorWidget, List<OperatorWidget>> getRelevantOpsConnectedFunc)
        {
            var selectedOps = SelectionHandler.GetSelectedElementsOfType<OperatorWidget>().ToArray();
            if (selectedOps.Count() == 1)
            {
                var opsAbove = getRelevantOpsConnectedFunc(selectedOps.First());
                if (opsAbove.Count > 0)
                {
                    SelectionHandler.SetElement(opsAbove.First());
                    opsAbove.First().Focus();
                }
                else
                {
                    selectedOps.First().Focus();
                }
            }
            CenterAllOrSelectedElements();
        }

        private List<OperatorWidget> GetSiblingsOfSelectedOperator()
        {
            var selectedOp = SelectionHandler.GetSelectedElementsOfType<OperatorWidget>().SingleOrDefault();
            if (selectedOp == null)
                return null;

            var opsAbove = selectedOp.GetOpsConnectedToOutputs();
            if (opsAbove.Count == 0)
                return null;

            return opsAbove[0].GetOpsConnectedToInputs();
        }

        #endregion navigate with selection through graph

        private void DisableHandler(object sender, RoutedEventArgs e)
        {
            ToggleDisabledForSelectedOperators();
        }

        public void ToggleDisabledForSelectedOperators()
        {
            ResetAllOperatorVisuals();

            var widgets = SelectionHandler.GetSelectedElementsOfType<OperatorWidget>().ToArray();
            var entries = new List<UpdateOperatorPropertiesCommand.Entry>();
            var operators = new List<Operator>();

            foreach (var widget in widgets)
            {
                entries.Add(new UpdateOperatorPropertiesCommand.Entry(widget.Operator) { Disabled = !widget.Operator.Disabled });
                operators.Add(widget.Operator);
            }

            var updatePropertiesCmd = new UpdateOperatorPropertiesCommand(operators, entries);
            App.Current.UndoRedoStack.AddAndExecute(updatePropertiesCmd);
            App.Current.UpdateRequiredAfterUserInteraction = true;

            foreach (var widget in widgets)
            {
                widget.IsDisabled = widget.Operator.Disabled;
            }
        }

        private void AddAnnotation(Point position)
        {
            var selectedOps = GetSelectedOps();
            Rect bounds = new Rect(
                x: _contextMenuPosition.X,
                y: _contextMenuPosition.Y,
                width: 200,
                height: 200
            );

            if (selectedOps.Any())
            {
                bounds = selectedOps[0].Bounds;
                for (int i = 1; i < selectedOps.Count(); i++)
                {
                    bounds.Union(selectedOps[i].Bounds);
                }
                bounds.Inflate(new Size(50, 50));
            }

            // Note: We only provide the width here, because the height will be stored as Op-Parameter
            var addOpCommand = new AddOperatorCommand(
                CompositionOperator,
                SpecialOperators.ANNOTATION,
                bounds.X,
                bounds.Y,
                bounds.Width,
                false);

            App.Current.UndoRedoStack.AddAndExecute(addOpCommand);

            // Set Height
            var newOpWidget = FindOperatorWidgetById(addOpCommand.AddedInstanceID);
            _opWidgetToFocusAfterRender = newOpWidget;
            foreach (var param in newOpWidget.Operator.Inputs)
            {
                if (param.Name != "Height")
                    continue;

                var setHeightCommand = new SetFloatValueCommand(
                    param,
                    (float)bounds.Height
                    );
                App.Current.UndoRedoStack.AddAndExecute(setHeightCommand);
                break;
            }

            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private OperatorWidget _opWidgetToFocusAfterRender = null;

        private void FocusNewlyAddedAnnotation(OperatorWidget newOpWidget)
        {
            for (int i = 0; i < XAnnotationsControl.Items.Count; i++)
            {
                var item = XAnnotationsControl.Items[i] as AnnotationViewModel;
                if (item.OperatorWidget != newOpWidget)
                    continue;

                var element = (UIElement)XAnnotationsControl.ItemContainerGenerator.ContainerFromIndex(i);

                if (element != null)
                {
                    var annotationControl = UIHelper.FindVisualChild<AnnotationControl>(element);
                    if (annotationControl != null)
                    {
                        annotationControl.EnableTextEdit();
                    }
                }
            }
        }

        private void AddAnnotationEventHandler(object sender, RoutedEventArgs e)
        {
            AddAnnotation(_contextMenuPosition);
        }

        private List<OperatorWidget> GetSelectedOps()
        {
            var operatorWidgetsToCenter = new List<OperatorWidget>();
            var someAreSelected = SelectionHandler.SelectedElements.Count > 0
                                  && !SelectionHandler.SelectedElements.Contains(this);
            if (someAreSelected)
            {
                operatorWidgetsToCenter.AddRange(from e in SelectionHandler.SelectedElements
                                                 let opWidget = e as OperatorWidget
                                                 where opWidget != null && opWidget.Operator.Visible
                                                 select opWidget);
            }
            return operatorWidgetsToCenter;
        }

        private void SaveAsIngredientHandler(object sender, RoutedEventArgs e)
        {
            //var command = new AddOperatorCommand(CompositionOperator, ANNOTATION_OP_META_ID, (int)_contextMenuPosition.X, (int)_contextMenuPosition.Y, 200, false);
            //App.Current.UndoRedoStack.AddAndExecute(command);
            e.Handled = true;

            App.Current.UpdateRequiredAfterUserInteraction = true;
            if (SelectionHandler.SelectedElements.Count != 1)
            {
                MessageBox.Show("Please select a single Operator to add as ingredient");
                return;
            }

            if (SelectionHandler.SelectedElements.First() is OperatorWidget opWidget)
            {
                var hasBeenAdded = IngredientsManager.TryAddOperatorAsIngredientToDefaultPalette(opWidget.Operator);
                if (!hasBeenAdded)
                {
                    MessageBox.Show("Couldn't add this to default Palette.");
                }
            }
        }

        public void SelectOperators(List<Operator> ops, bool center = false)
        {
            var opWidgetsToSelect = new List<ISelectable>();

            foreach (var el in XOperatorCanvas.Children)
            {
                if (el is OperatorWidget opWidget)
                {
                    if (ops.Contains(opWidget.Operator))
                    {
                        opWidgetsToSelect.Add(opWidget);
                    }
                }
            }
            if (opWidgetsToSelect.Count == 0)
                return;

            if (center)
            {
                SelectionHandler.SetElements(opWidgetsToSelect);
            }
            CenterAllOrSelectedElements();
        }

        public ConnectionDragHelper ConnectionDragHelper;
    }
}