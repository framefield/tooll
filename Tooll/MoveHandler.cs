// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Framefield.Tooll
{
    public class MoveHandler
    {
        public event EventHandler<RoutedEventArgs> SelectedEvent;

        public UserControl UserControl { get; private set; }
        public Point Position {
            get { return m_Position; }
            set {
                m_Position = value;
                Canvas.SetLeft(UserControl, m_Position.X);
                Canvas.SetTop(UserControl, m_Position.Y);
            }
        }

        public MoveHandler(UserControl control) {
            UserControl = control;
            m_Position = new Point(Canvas.GetLeft(UserControl),
                                   Canvas.GetTop(UserControl));
        }

        public void Start() {
        }

        public void Update(Vector delta) {
            Position += delta;
        }

        public void Stop(Vector delta) {
            if ((delta.Length < 3) && (SelectedEvent != null))
                SelectedEvent(UserControl, new RoutedEventArgs());
        }

        private Point m_Position;
    }
}
