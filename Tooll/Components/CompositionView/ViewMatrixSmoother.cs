// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Framefield.Core;

namespace Framefield.Tooll.Components
{
    class ViewMatrixSmoother
    {
        public ViewMatrixSmoother()
        {
            _stopwatch.Start();
        }

        public Matrix ViewMatrix { 
            get { return _viewMatrix; }
            private set { _viewMatrix= value; }
        }

        public bool Update()
        {
            double _time = _stopwatch.ElapsedMilliseconds/1000.0;       // Keeping the time running for other measurements
            double elapsed = _time - _lastTime;
            bool needsUpdate= false;
            _lastTime = _time;

            if (WaitingForUiRebuild) {
                WaitingForUiRebuild = false;
                return false;
            }

            double speed= _isDragging ? 100
                                                : DEFAULT_VIEW_ANIMATION_SPEED;

            double stepFactor= Math.Min(1, elapsed * speed); // prevent overshooting
            var m = ViewMatrix;
            double delta= (Math.Abs(_viewMatrixInterpolationTarget.M11 - m.M11) 
                         + Math.Abs(_viewMatrixInterpolationTarget.M12 - m.M12) 
                         + Math.Abs(_viewMatrixInterpolationTarget.M21 - m.M21)
                         + Math.Abs(_viewMatrixInterpolationTarget.M22 - m.M22)) * 0.01
                         + Math.Abs(_viewMatrixInterpolationTarget.OffsetX - m.OffsetX)
                         + Math.Abs(_viewMatrixInterpolationTarget.OffsetY - m.OffsetY);

            const double VISIBLE_TRANSITION_THRESHOLD = 1.0;
            if (delta > VISIBLE_TRANSITION_THRESHOLD) {
                m.M11 += (_viewMatrixInterpolationTarget.M11 - m.M11) * stepFactor;
                m.M12 += (_viewMatrixInterpolationTarget.M12 - m.M12) * stepFactor;
                m.M21 += (_viewMatrixInterpolationTarget.M21 - m.M21) * stepFactor;
                m.M22 += (_viewMatrixInterpolationTarget.M22 - m.M22) * stepFactor;
                m.OffsetX += (_viewMatrixInterpolationTarget.OffsetX - m.OffsetX) * stepFactor;
                m.OffsetY += (_viewMatrixInterpolationTarget.OffsetY - m.OffsetY) * stepFactor;
                ViewMatrix = m;

                needsUpdate= true;
            }
            else {
                ViewMatrix = _viewMatrixInterpolationTarget;
            }
            return needsUpdate;
        }

        public void StartDragging()
        {
            _viewMatrixInterpolationTarget= ViewMatrix;
            _matrixOnDragStart = ViewMatrix;
            _dragTimePositions.Clear();
            _isDragging = true;
        }

        public void DragDelta(double deltaX, double deltaY)
        {
            Matrix m = _matrixOnDragStart;
            m.Translate(deltaX, deltaY);
            _viewMatrixInterpolationTarget = m;
            _dragTimePositions.Insert(0, new DragTimePosition() { X=deltaX, Y=deltaY, Time= _stopwatch.ElapsedMilliseconds/1000.0f });
            if (_dragTimePositions.Count > MAX_DRAG_TIME_POSITION_COUNT)
                _dragTimePositions.RemoveAt(MAX_DRAG_TIME_POSITION_COUNT);
        }

        public void StopDragging(bool startFlicking)
        {
            _isDragging= false;
            if (startFlicking && _dragTimePositions.Count > 4) {
                double forceX;
                double forceY;
                CalculateFlickingForce(out forceX, out forceY);

                _viewMatrixInterpolationTarget = ViewMatrix;
                _viewMatrixInterpolationTarget.Translate(forceX, forceY);
            }
        }

        public void SetMatrix(Matrix newMatrix)
        {
            ViewMatrix= _viewMatrixInterpolationTarget= newMatrix;
        }

        public void SetTransitionTarget( Matrix newMatrix) {
            _viewMatrixInterpolationTarget= newMatrix;
        }

        public void FreezeTransition()
        {
            _viewMatrixInterpolationTarget= ViewMatrix;
        }

        // Note: Because rebuilding the UI after a step in/out might take way longer than the
        // actual transition, DecrementNestingStep sets this variable. This will then delay
        // the beginning of the transition until the first update()-call after this flag has been set.
        public bool WaitingForUiRebuild = false;

        #region private methods
        private void CalculateFlickingForce(out double forceX, out double forceY)
        {
            double _now = _stopwatch.ElapsedMilliseconds / 1000.0;
            forceX=0;
            forceY=0;

            const double DAMPING = 3;
            const double FLICK_THRESHOLD = 1;
            const double FLICK_SPEED = 2.0;

            // ignore last delta, because WPF often add false delta before mouse up)
            for (int i=2; i< _dragTimePositions.Count; ++i) {

                double dampFactor = Math.Pow(Math.E, -DAMPING * (_now - _dragTimePositions[i].Time));
 
                var dX = (_dragTimePositions[i-1].X - _dragTimePositions[i].X) * dampFactor * FLICK_SPEED;
                var dY = (_dragTimePositions[i-1].Y - _dragTimePositions[i].Y) * dampFactor * FLICK_SPEED;
                forceX += dX;
                forceY += dY;
            }
            if (Math.Abs(forceX) + Math.Abs(forceY) < FLICK_THRESHOLD) {
                forceX =0;
                forceY= 0;
            }

        }

        class DragTimePosition
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Time { get; set; }
        }
        List<DragTimePosition> _dragTimePositions = new List<DragTimePosition>();
        const int MAX_DRAG_TIME_POSITION_COUNT = 10;


        private System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private double _lastTime;
        private Matrix _matrixOnDragStart;
        private Matrix _viewMatrix = new Matrix();
        private Matrix _viewMatrixInterpolationTarget= new Matrix();
        private bool _isDragging = false;

        const double DEFAULT_VIEW_ANIMATION_SPEED= 5;// Reasonable value range is 3 ... 10 (very fast)
        #endregion

    }
}
