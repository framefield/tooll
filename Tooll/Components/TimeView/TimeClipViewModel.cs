// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Text;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using System.Windows.Data;

namespace Framefield.Tooll
{
    // We derive from DependencyObject to link to Operator widget select state
    public class TimeClipViewModel : DependencyObject, INotifyPropertyChanged
    {
        public TimeClipViewModel(OperatorWidget op)
        {
            OperatorWidget = op;
            m_TimeClip = op.Operator.InternalParts[0].Func as ITimeClip;

            op.Operator.InternalParts[0].ChangedEvent += ForwardChangedNotification;

            BindingOperations.SetBinding(op, OperatorWidget.IsSelectedProperty, new Binding("IsSelected") { Source = this, Mode = BindingMode.TwoWay });
        }

        ~TimeClipViewModel()
        {
            OperatorWidget.Operator.InternalParts[0].ChangedEvent -= ForwardChangedNotification;
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(TimeClipViewModel),
                                                                                                   new UIPropertyMetadata() { DefaultValue=false });

        public OperatorWidget OperatorWidget { get; private set; }
        public double StartTime
        {
            get { return m_TimeClip.StartTime; }
            set { m_TimeClip.StartTime = value; }
        }
        public double EndTime
        {
            get { return m_TimeClip.EndTime; }
            set { m_TimeClip.EndTime = value; }
        }
        public double SourceStartTime
        {
            get { return m_TimeClip.SourceStartTime; }
            set { m_TimeClip.SourceStartTime = value; }
        }
        public double SourceEndTime
        {
            get { return m_TimeClip.SourceEndTime; }
            set { m_TimeClip.SourceEndTime = value; }
        }
        public int Layer
        {
            get { return m_TimeClip.Layer; }
            set { m_TimeClip.Layer = value; }
        }
        public double Duration
        {
            get { return EndTime - StartTime; }
            set { EndTime += value; }
        }

        #region event forwarder
        private void ForwardChangedNotification(object sender, OperatorPart.ChangedEventArgs e)
        {
            NotifyPropertyChanged("StartTime");
            NotifyPropertyChanged("EndTime");
            NotifyPropertyChanged("SourceStartTime");
            NotifyPropertyChanged("SourceEndTime");
            NotifyPropertyChanged("Layer");
            NotifyPropertyChanged("Duration");
        }
        #endregion

        #region notifier
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion

        private ITimeClip m_TimeClip;
    }
}
