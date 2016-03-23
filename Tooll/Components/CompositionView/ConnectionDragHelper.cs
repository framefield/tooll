// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Framefield.Core;
using Framefield.Tooll.Components.CompositionView;

namespace Framefield.Tooll
{
    /**
     * Helps to draw new or rewire the target of existing connections. ConnectionLine, OperatorWidget and 
     * InputWidget each have their own instance of the helper.
     * 
     **/
    public class ConnectionDragHelper
    {

        public ConnectionDragHelper(CompositionGraphView cgv)
        {
            _cgv = cgv;
        }

        private readonly CompositionGraphView _cgv;


        /// <summary>
        /// Start drag a new connection. Called from InputWidget and OperatorWidget when staring to drag from output thumb
        /// </summary>
        public void Start(Point p, IConnectionLineSource src, int outputIdx)
        {
            _sourceOutputIndex = -1;
            if (outputIdx < 0 || outputIdx >= src.Outputs.Count)
                return;

            _sourceOutputIndex = outputIdx;
            _sourceWidget = src;
            _startPosition = p;

            // NOTE: In the long term, this should be capable of handling simultaneously creating multiple connection
            _lastPosition = _startPosition;

            var cl = new ConnectionLine(_sourceWidget.CV.CompositionGraphView.CompositionOperator, _sourceWidget, _sourceWidget.Outputs[outputIdx]);
            _sourceWidget.CV.CompositionGraphView.UnfinishedConnections.Add(cl);
            _sourceWidget.CV.CompositionGraphView.XOperatorCanvas.Children.Add(cl);

            cl.UpdateDuringConstruction(_lastPosition);
        }

        public void RewireExistingConnection(ConnectionLine cl, Point startPoint, int outputIdx)
        {
            _sourceOutputIndex = -1;
            if (outputIdx < 0 || outputIdx >= cl.Source.Outputs.Count)
                return;

            _sourceWidget = cl.Source;
            _sourceOutputIndex = outputIdx;

            _startPosition = startPoint;
            _lastPosition = _startPosition; 

            // Delete old connection from model...
            cl.Remove();
            cl.Target.UpdateConnections();

            // ...and restart as unfinished connection line
            _sourceWidget.CV.CompositionGraphView.UnfinishedConnections.Add(cl);
            _sourceWidget.CV.CompositionGraphView.XOperatorCanvas.Children.Add(cl);
            cl.UpdateDuringConstruction(_lastPosition);
            cl.IsSelected = true;            
        }


        public void UpdateConnectionUI(Point pointOnCanvas)
        {
            if (_sourceOutputIndex < 0)
                return;

            _lastPosition = pointOnCanvas; 

            foreach (var cl in _sourceWidget.CV.CompositionGraphView.UnfinishedConnections)
                cl.UpdateDuringConstruction(_lastPosition);
        }


        public void Stop(Vector delta, OperatorWidgetInputZone zone)
        {
            MainWindow mainWindow = App.Current.MainWindow;
            foreach (var cl in _sourceWidget.CV.CompositionGraphView.UnfinishedConnections)
            {
                _sourceWidget.CV.CompositionGraphView.XOperatorCanvas.Children.Remove(cl);

                if (zone == null || _sourceOutputIndex < 0)
                    continue;
                
                var output = _sourceWidget.Outputs[_sourceOutputIndex];
                var parent = zone.Input.Parent;
                var outputsOfInputOp = parent.Outputs;

                // Check for Cycle
                var cycleChecker = new OperatorPart.CycleChecker(outputsOfInputOp);
                output.TraverseWithFunction(cycleChecker, null);
                if (cycleChecker.HasCycle)
                {
                    if (zone.Input.Parent != output.Parent)
                    {
                        MessageBox.Show("You cannot connect, as this would build a cycle.",
                                        "Sorry");                        
                    }
                    continue;
                }

                var connection = new Connection(output.Parent, output,
                                                zone.Input.Parent, zone.Input,
                                                zone.MultiInputIndex);


                if (zone.Input == null)
                {
                    Logger.Error("Ignoring insufficiently initialized input-zone. Please report this.");
                    continue;
                }

                if (zone.Input.IsMultiInput)
                {
                    if (zone.InsertAtMultiInputIndex)
                        mainWindow.InsertConnectionAt(connection);
                    else
                        mainWindow.ReplaceConnectionAt(connection);

                }
                else {
                    if (zone.Input.Connections.Any())
                        mainWindow.ReplaceConnectionAt(connection);
                    else
                        mainWindow.InsertConnectionAt(connection);                                
                }
            }
            _sourceWidget.CV.CompositionGraphView.UnfinishedConnections.Clear();
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        Point _dragStartPositionOnCanvas;

        public const string CONNECTION_LINE_OUTPUT_IDENTIFIER = "OUTPUT_CONNECTION";

        public void DoDragDropRewireConnection(ConnectionLine cl)
        {
            _dragStartPositionOnCanvas = Win32RawInput.MousePointInUiElement(_cgv.XOperatorCanvas);
            _snappedToValidInput = false;

            var dragData = new DataObject(CONNECTION_LINE_OUTPUT_IDENTIFIER, cl.Output);

            // We have to restart the temp connection, because DoDragDrop triggers DragCompleted
            cl.HitTestDisabledDuringDrag = false;
            
            RewireExistingConnection(cl, _dragStartPositionOnCanvas, cl.Output.Func.EvaluationIndex);  
            DragDrop.AddGiveFeedbackHandler(_cgv, GiveFeedbackHandler);
            DragDrop.DoDragDrop(_cgv, dragData, DragDropEffects.All);

            // Important! This line is not reached until drop is completed.      

            DragDrop.RemoveGiveFeedbackHandler(_cgv, GiveFeedbackHandler);
            Stop(new Vector(), null);
        }

        
        public void DoDragDropNewConnection(IConnectionLineSource sourceWidget, OperatorPart output)
        {
            _dragStartPositionOnCanvas = Win32RawInput.MousePointInUiElement(_cgv.XOperatorCanvas);
            _snappedToValidInput = false;

            var dragData = new DataObject(CONNECTION_LINE_OUTPUT_IDENTIFIER, output);

            // We have to restart the temp connection, because DoDragDrop triggers DragCompleted
            Start(_dragStartPositionOnCanvas, sourceWidget, output.Func.EvaluationIndex); 
            DragDrop.AddGiveFeedbackHandler(_cgv, GiveFeedbackHandler);
            DragDrop.DoDragDrop(_cgv, dragData, DragDropEffects.All);

            // Importat! This line is not reached until drop is completed.      

            DragDrop.RemoveGiveFeedbackHandler(_cgv, GiveFeedbackHandler);
            Stop(new Vector(), null);
        }

        private Point _snappedPointOnCanvas;
        private bool _snappedToValidInput;

        private void GiveFeedbackHandler(object sender, GiveFeedbackEventArgs e)
        {
            var element = sender as IInputElement;
            if (element == null) 
                return;

            var endPositionForConnection = e.Effects == DragDropEffects.Copy && _snappedToValidInput
                ? _snappedPointOnCanvas 
                : Win32RawInput.MousePointInUiElement(_cgv.XOperatorCanvas);

            UpdateConnectionUI(endPositionForConnection);
            _snappedToValidInput = false;   // Sadly this can lead to jittering because the feedback-handler is called more frequently then the DragOver event            
        }


        Point _lastDragOverPosition;

        /**
         * This function is called by IConnectionTargets when
         * the end of a connection is dragged over them.
         * 
         * To "snap" valid connections, we save the zone so the
         * GiveFeedbackHandler can use its position.
         */
        public void HandleDragOverEvent(DragEventArgs e, IConnectionLineTarget targetWidget)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            
            var connectionStartOutput = e.Data.GetData(CONNECTION_LINE_OUTPUT_IDENTIFIER) as OperatorPart;
            if (connectionStartOutput == null)
                return;  // Skip non-connection drop data

            // We can't use Mouse.GetPosition() also see
            // http://msdn.microsoft.com/en-us/library/system.windows.input.mouse.getposition(v=vs.110).aspx
            var mousePosition = e.GetPosition(targetWidget as UIElement);
            var inputZonesForDrag = OperatorWidgetInputZoneManager.ComputeInputZonesForOp(targetWidget);
            var zoneBelowMouse = OperatorWidgetInputZoneManager.FindZoneBelowMouse(inputZonesForDrag, mousePosition);

            if (zoneBelowMouse != null)
            {
                zoneBelowMouse.IsBelowMouse = true;
                _snappedToValidInput = zoneBelowMouse.Input.Type == connectionStartOutput.Type ||
                                       zoneBelowMouse.Input.Type == FunctionType.Generic;

                if(_snappedToValidInput)
                {
                    e.Effects = DragDropEffects.Copy;
                    _snappedPointOnCanvas = targetWidget.PositionOnCanvas + new Vector(zoneBelowMouse.LeftPosition + 0.5*zoneBelowMouse.Width, targetWidget.Height);
                }
            }

            if (mousePosition == _lastDragOverPosition) 
                return;

            _lastDragOverPosition = mousePosition;
            targetWidget.UpdateInputZonesUIFromDescription(inputZonesForDrag);
        }

        private Point _startPosition;
        private Point _lastPosition;

        private IConnectionLineSource _sourceWidget;
        private int _sourceOutputIndex;

    }
}
