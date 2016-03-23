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
using Point = System.Windows.Point;

namespace Framefield.Tooll
{
    public class AnnotationViewModel : DependencyObject, INotifyPropertyChanged
    {
        public AnnotationViewModel(OperatorWidget opWidget)
        {
            OperatorWidget = opWidget;
            _operator = OperatorWidget.Operator;
            _operatorFunc = _operator.InternalParts[0].Func as IAnnotation;
            Position = _operator.Position;
            Width    = _operator.Width;

            _operator.InternalParts[0].ChangedEvent += ForwardChangedNotification;
            _operator.PositionChangedEvent          += Operator_PositionChangedEvent;
            _operator.WidthChangedEvent             += Operator_WidthChangedEvent;

            BindingOperations.SetBinding(opWidget, OperatorWidget.IsSelectedProperty, new Binding("IsSelected") { Source = this, Mode = BindingMode.TwoWay });
        }

        void Operator_WidthChangedEvent(object o, WidthChangedEventArgs e)
        {
            Width = _operator.Width;
        }

        void Operator_PositionChangedEvent(object o, PositionChangedEventArgs e)
        {
            Position = _operator.Position;            
        }

        ~AnnotationViewModel()
        {
            _operator.InternalParts[0].ChangedEvent -= ForwardChangedNotification;
            _operator.PositionChangedEvent -= Operator_PositionChangedEvent;
            _operator.WidthChangedEvent -= Operator_WidthChangedEvent;            
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(AnnotationViewModel),
                                                                                                   new UIPropertyMetadata() { DefaultValue=false });

        public Point Position
        {
            get { return (Point)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(Point), typeof(AnnotationViewModel),
                                                                                                   new UIPropertyMetadata() { DefaultValue = new Point() });

        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }
        private static readonly DependencyProperty WidthProperty = DependencyProperty.Register("Width", typeof(double), typeof(AnnotationViewModel),
                                                                                                   new UIPropertyMetadata() { DefaultValue = 150.0 });

        // Operator parameters
        public double Height
        {
            get { return _operatorFunc.Height; }
            set { _operatorFunc.Height = value; }
        }

        public double FontSize
        {
            get { return _operatorFunc.FontSize; }
            set { _operatorFunc.FontSize = value; }
        }

        public string Text
        {
            get { return _operatorFunc.Text; }
            set { _operatorFunc.Text = value; }
        }

        public Color4 Color 
        {
            get { return _operatorFunc.Color; }
            set { _operatorFunc.Color = value; NotifyPropertyChanged("Color"); } 
        }

        #region event notifier and forwarder
        private void ForwardChangedNotification(object sender, OperatorPart.ChangedEventArgs e)
        {
            NotifyPropertyChanged("Height");
            NotifyPropertyChanged("FontSize");
            NotifyPropertyChanged("Text");
            NotifyPropertyChanged("Color");
        }
                
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion

        public OperatorWidget OperatorWidget;
        private Operator _operator;
        private IAnnotation _operatorFunc;
    }
}
