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
using Newtonsoft.Json;


namespace Framefield.Tooll.Components.QuickCreate
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IngredientViewModel : DependencyObject, INotifyPropertyChanged
    {
        public IngredientViewModel(Operator op)        
        {
            if (op!=null)
            {
                MetaOperator = op.Definition;
                Name = op.Name;
                if (Name == "")
                {
                    Name = MetaOperator.Name;
                }                
            }
        }

        ~IngredientViewModel()
        {
            //_metaOperator.InternalParts[0].ChangedEvent -= ForwardChangedNotification;
            //_metaOperator.PositionChangedEvent -= Operator_PositionChangedEvent;
            //_metaOperator.WidthChangedEvent -= Operator_WidthChangedEvent;            
        }

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(IngredientViewModel),
                                                                                                   new UIPropertyMetadata() { DefaultValue=false });

        [JsonProperty] 
        public String Name { get { return _name; } set { _name = value; NotifyPropertyChanged("Name"); } }
        private String _name;

        [JsonProperty] 
        public int GridPositionX { get { return _gridPositionX; } set { _gridPositionX = value; NotifyPropertyChanged("GridPositionX"); } }
        private int _gridPositionX;

        [JsonProperty]
        public int GridPositionY { get { return _gridPositionY; } set { _gridPositionY = value; NotifyPropertyChanged("GridPositionY"); } }
        private int _gridPositionY;

        public event EventHandler<RoutedEventArgs> RemovedEvent;

        public void TriggerRemoved()
        {
            if (RemovedEvent != null)
                RemovedEvent(this, new RoutedEventArgs());
        }

        public MetaOperator MetaOperator
        {
            get
            {
                MetaOperator metaOp;
                App.Current.Model.MetaOpManager.MetaOperators.TryGetValue(_metaOpId, out metaOp);
                return metaOp;
            }
            set
            {
                _metaOperator = value;                
                _metaOpId = _metaOperator != null 
                          ? _metaOperator.ID 
                          : Guid.Empty;
                NotifyPropertyChanged("MetaOperator");
            }
        }

        private MetaOperator _metaOperator;

        [JsonProperty]
        private Guid _metaOpId;


        #region event notifier and forwarder
                
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion
        
        //private IAnnotation _operatorFunc;
    }
}
