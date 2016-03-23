// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Framefield.Core;
using Framefield.Helper;

namespace Framefield.Tooll
{
    static class ShakeDetector
    {
        public static bool TestForShaking(Point newPosition) {
            if (m_LastDragPoint == null) 
                return false;

            var angle = Math.Atan2(m_SmoothedDragDirection.X, m_SmoothedDragDirection.Y) * 180 / Math.PI;

            var dx = m_LastDragPoint.X - newPosition.X;
            var dy = m_LastDragPoint.Y - newPosition.Y;
            m_SmoothedDragDirection.X = BLEND_FACTOR * m_SmoothedDragDirection.X + (1-BLEND_FACTOR) * dx;
            m_SmoothedDragDirection.Y = BLEND_FACTOR * m_SmoothedDragDirection.Y + (1-BLEND_FACTOR) * dy;
            var dAngle = Utilities.getAngleDifference(angle, m_LastAngle);
            var now = DateTime.Now.Ticks;

            m_LastDragPoint = newPosition;
            m_LastAngle = angle;


            if (dAngle > 100) {
                if (m_TurningPoints.Count() == 0) {
                    m_TurningPoints.Add(new TurningPoint() {Time = now, Position = newPosition});
                }
                else {
                    var distance = (m_TurningPoints.Last().Position - newPosition).Length;
                    var dTime = TimeSpan.FromTicks(now - m_TurningPoints.Last().Time).TotalMilliseconds;

                    if (dTime < 40) {
                        m_TurningPoints.Clear();
                    }
                    else if (dTime > 200) {
                        m_TurningPoints.Clear();
                        m_TurningPoints.Add(new TurningPoint() {Time = now, Position = newPosition});
                    }
                    else if (distance < 4) {
                        m_TurningPoints.Clear();
                    }
                    else if (m_TurningPoints.Count() > 1) {
                        m_TurningPoints.Clear();
                        return true;
                    }
                    else {
                        m_TurningPoints.Add(new TurningPoint() {Time = now, Position = newPosition});
                    }
                }
            }
            return false;
        }

        private class TurningPoint
        {
            public Point Position { get; set; }
            public long Time { get; set; }
        }

        private const double BLEND_FACTOR = 0.8;
        private static Point m_LastDragPoint;
        private static double m_LastAngle;
        private static Point m_SmoothedDragDirection;
        private static List<TurningPoint> m_TurningPoints = new List<TurningPoint>();
    }

}
