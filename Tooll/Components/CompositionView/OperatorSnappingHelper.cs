// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Framefield.Core.Commands;
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// This handler provides the snapping functionality for OperatorWidgets. The heart of this
    /// feature is a recursive method AddSnappedNeighboursToPool that starts from an OperatorWidgets 
    /// and and finds other OperatorWidgets snapped to a a block.
    /// 
    /// </summary>
    class OperatorSnappingHelper
    {
        public OperatorWidget MovingOperator { get; private set; }
        public bool SnappedGroupIsMoving { get; private set; }
        public List<OperatorWidget> DragGroup { get { return _dragGroup; } }

        public OperatorSnappingHelper(OperatorWidget movedOperator)
        {
            MovingOperator = movedOperator;
        }

        public void Start(List<ISelectable> selectedElements)
        {
            SnappedGroupIsMoving = false;
            _dragGroup.Clear();
            
            // Move either block or all selected operators
            if (!MovingOperator.IsSelected)
                AddSnappedNeighboursToPool(_dragGroup, MovingOperator);
            else
            {
                foreach (var se in selectedElements)
                {
                    var ow = se as OperatorWidget;
                    if (ow != null) {
                        _dragGroup.Add(ow);
                        
                    }
                }
            }

            // Keep Original Positions
            _widgetPositionBeforeDrag.Clear();
            foreach (var ow in _dragGroup)
            {
                _widgetPositionBeforeDrag[ow] = ow.Position;
            }

            var dragOps = (from opWidget in _dragGroup select opWidget.Operator).ToArray();
            var properties = (from op in dragOps
                             let propertyEntry = new UpdateOperatorPropertiesCommand.Entry(op)
                             select propertyEntry).ToArray();
            _updateOperatorPropertiesCommand = new UpdateOperatorPropertiesCommand(dragOps, properties);
        }

        public void UpdateBeforeMoving(Vector offset)
        {
            SnappedGroupIsMoving = TryToMoveSnappedGroupWhenNotSelected(offset);
        }

        public void Stop(Vector offset)
        {
            SnappedGroupIsMoving = false;
            _dragGroup.Clear();
            var wasDragged = offset.Length > 0.0;
            if (wasDragged)
                App.Current.UndoRedoStack.AddAndExecute(_updateOperatorPropertiesCommand);
        }

        #region implementation
        private static void AddSnappedNeighboursToPool(List<OperatorWidget> pool, OperatorWidget el)
        {
            pool.Add(el);
            var parentsAndChildren = new List<OperatorWidget>();
            parentsAndChildren.AddRange((el as OperatorWidget).GetOperatorsSnappedAbove());
            parentsAndChildren.AddRange((el as OperatorWidget).GetOperatorsSnappedBelow());

            foreach (var opWi in parentsAndChildren)
            {
                if (!pool.Contains(opWi))
                    AddSnappedNeighboursToPool(pool, opWi);
            }
        }

        private bool TryToMoveSnappedGroupWhenNotSelected(Vector offset)
        {
            if (_dragGroup.Count() < 1) // include self
                return false;

            var somethingSnapped = false;

            // First move all to new position
            foreach (var opWidget in _dragGroup)
            {
                opWidget.Position = _widgetPositionBeforeDrag[opWidget] - offset;
            }

            // Then check if one would snap
            foreach (var opWidgetForSnapping in _dragGroup)
            {
                Vector positionBeforeSnapping = opWidgetForSnapping.Position.ToVector();

                if (TrySnappingToOperatorsConnectedAbove(opWidgetForSnapping, _dragGroup) || TrySnappingToOperatorsConnectedBelow(opWidgetForSnapping, _dragGroup)) 
                {
                    var offsetBySnapping = opWidgetForSnapping.Position.ToVector() - positionBeforeSnapping;
                    
                    // Adjust all other positions to snapped one
                    foreach (var opWidget in _dragGroup)
                    {
                        if (opWidget != opWidgetForSnapping)
                        {
                            opWidget.Position += offsetBySnapping;
                        }
                    }
                    somethingSnapped = true;
                    break;
                }
            }

            for (int idx = 0; idx < _dragGroup.Count; ++idx)
            {
                var opWidget = _dragGroup[idx];
                _updateOperatorPropertiesCommand.ChangeEntries[idx].Position = opWidget.Position;
            }

            _updateOperatorPropertiesCommand.Do();

            return somethingSnapped;
        }

        private static bool TrySnappingToOperatorsConnectedAbove(OperatorWidget el, List<OperatorWidget> dragGroup)
        {
            var src = el as OperatorWidget;
            if (src == null)
                return false;

            var op = (from opAbove in src.GetOpsConnectedToOutputs()
                      where !dragGroup.Contains(opAbove) &&
                            !opAbove.CurrentlyDragged() &&
                            (Math.Abs(opAbove.Position.Y + CompositionGraphView.GRID_SIZE - el.Position.Y) < VERTICAL_DISTANCE_SNAP_THRESHOLD) &&
                            (el.GetHorizontalOverlapWith(opAbove) > HORIZONTAL_OVERLAP_SNAP_THRESHOD)
                      select opAbove).FirstOrDefault();

            if (op == null)
                return false;

            double dx = el.Position.X - op.Position.X;
            el.Position = new Point(el.Position.X - ((dx + 0.5 * CompositionGraphView.GRID_SIZE) % CompositionGraphView.GRID_SIZE) + 0.5 * CompositionGraphView.GRID_SIZE,
                                               op.Position.Y + CompositionGraphView.GRID_SIZE);
            return true;
        }

        private static bool TrySnappingToOperatorsConnectedBelow(OperatorWidget el, List<OperatorWidget> dragGroup)
        {
            var tgt = el as OperatorWidget;
            if (tgt == null)
                return false;

            var op = (from opBelow in tgt.GetOpsConnectedToInputs()
                      where !dragGroup.Contains(opBelow) &&
                            !opBelow.CurrentlyDragged() &&
                            Math.Abs(el.Position.Y + CompositionGraphView.GRID_SIZE - opBelow.Position.Y) < VERTICAL_DISTANCE_SNAP_THRESHOLD &&
                            el.GetHorizontalOverlapWith(opBelow) > HORIZONTAL_OVERLAP_SNAP_THRESHOD
                      select opBelow).FirstOrDefault();

            if (op == null)
                return false;

            double dx = el.Position.X - op.Position.X;
            el.Position = new Point(el.Position.X - ((dx + 0.5 * CompositionGraphView.GRID_SIZE) % CompositionGraphView.GRID_SIZE) + 0.5 * CompositionGraphView.GRID_SIZE,
                                               op.Position.Y - CompositionGraphView.GRID_SIZE);
            return true;
        }
        #endregion

        #region constants
        private const double SNAP_RANGE = 15;
        private const double VERTICAL_OVERLAP_SNAP_THRESHOLD = 0.4 * CompositionGraphView.GRID_SIZE;
        private const double HORIZONTAL_OVERLAP_SNAP_THRESHOLD = 0.4 * CompositionGraphView.GRID_SIZE;
        private const double HORIZONTAL_OVERLAP_SNAP_THRESHOD = 1 * CompositionGraphView.GRID_SIZE;
        private const double VERTICAL_DISTANCE_SNAP_THRESHOLD = 0.4 * CompositionGraphView.GRID_SIZE;
        #endregion

        private UpdateOperatorPropertiesCommand _updateOperatorPropertiesCommand;

        private List<OperatorWidget> _dragGroup = new List<OperatorWidget>();
        private Dictionary<OperatorWidget, Point> _widgetPositionBeforeDrag = new Dictionary<OperatorWidget,Point>();
    }
}
