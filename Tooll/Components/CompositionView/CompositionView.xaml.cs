// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for CompositionView.xaml
    /// </summary>
    public partial class CompositionView : UserControl
    {
        #region Properties
        public CompositionGraphView CompositionGraphView { get { return XCompositionGraphView; } }

        public double PlaySpeed {
            get { return m_PlaySpeed; }
            set { 
                m_PlaySpeed = value;
                if (m_PlaySpeed > 0.0) {
                    m_Stopwatch.Restart();
                    m_Timer.Start();
                }
                else if (m_PlaySpeed < 0.0) {
                    m_Stopwatch.Restart();
                    m_Timer.Start();
                }
                else {
                    m_Stopwatch.Stop();
                    m_Timer.Stop();
                }
            }
        }
        #endregion

        #region constructors
        public CompositionView() {
            InitializeComponent();
            XCompositionGraphView.SelectionHandler.SelectionChanged+= XTimeView.XAnimationCurveEditor.SelectionChangedEventHandler;
        }
        #endregion

        //public void Clear() {
        //    CompositionGraphView.Clear();
        //}

        #region members
        private System.Windows.Threading.DispatcherTimer m_Timer = new System.Windows.Threading.DispatcherTimer();
        private System.Diagnostics.Stopwatch m_Stopwatch = new System.Diagnostics.Stopwatch();
        private double m_PlaySpeed =0;
        #endregion
    }
}
