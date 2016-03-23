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
using SharpDX;

namespace Framefield.Tooll
{

    public class TimeMarkerViewModel : DependencyObject, INotifyPropertyChanged       // We derive from DependencyObject to link to Operator widget select state
    {

        public TimeMarkerViewModel(OperatorWidget op) {
            OperatorWidget = op;
            _TimeMarker = op.Operator.InternalParts[0].Func as ITimeMarker;

            //FixMe: Binding the ForwardChangedNotification to the ChangedEvent is extremely slow. 
            // Additionally it's unclear what would be the reason for triggering an update of the operator's parameters
            // depending on it's input.
            //op.Operator.InternalParts[0].ChangedEvent += ForwardChangedNotification;

            //FixMe: Binding to magic numbers of the TimeClip parameters is evil.
            op.Operator.Inputs[0].ChangedEvent += (o, a) => ForwardChangedNotification();
            op.Operator.Inputs[1].ChangedEvent += (o, a) => ForwardChangedNotification();
            op.Operator.Inputs[2].ChangedEvent += (o, a) => ForwardChangedNotification();
            op.Operator.Inputs[3].ChangedEvent += (o, a) => ForwardChangedNotification();
            op.Operator.Inputs[4].ChangedEvent += (o, a) => ForwardChangedNotification();
            op.Operator.PropertyChanged += (o, a) => ForwardChangedNotification();
            BindingOperations.SetBinding(op, OperatorWidget.IsSelectedProperty, new Binding("IsSelected") { Source = this, Mode= BindingMode.TwoWay});
        }

        public bool IsSelected { get { return (bool) GetValue(IsSelectedProperty); } set { SetValue(IsSelectedProperty, value); } }
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(TimeMarkerViewModel),
            new UIPropertyMetadata() { DefaultValue=false });


        public OperatorWidget OperatorWidget { get; private set; }
        public double Time { get { return _TimeMarker.Time; } set { _TimeMarker.Time = value; NotifyPropertyChanged("Time"); NotifyPropertyChanged("IsSelected"); } }
        public Color4 Color { get { return _TimeMarker.Color; } set { _TimeMarker.Color = value; NotifyPropertyChanged("Color"); NotifyPropertyChanged("IsSelected"); } }
        public String Name { get { return OperatorWidget.Operator.Name; } set { OperatorWidget.Operator.Name = value; NotifyPropertyChanged("Name");  } }

        #region event forwarder
        private void ForwardChangedNotification()
        {
            NotifyPropertyChanged("Time");
            NotifyPropertyChanged("Color");
            NotifyPropertyChanged("Name");
        }

        private void ForwardChangedNotification(object sender, OperatorPart.ChangedEventArgs e)
        {
            ForwardChangedNotification();
        }
        #endregion

        #region notifier
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion



        private ITimeMarker _TimeMarker;
    }
}
