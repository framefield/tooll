// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;
using Framefield.Core;

namespace Framefield.Tooll
{

    [JsonObject(MemberSerialization.OptIn)]
    public class OperatorPreset : DependencyObject, INotifyPropertyChanged
    {
        public OperatorPreset()
        {
            Id = Guid.NewGuid();
        }

        public bool IsSelected { get { return (bool)GetValue(IsSelectedProperty); } set { SetValue(IsSelectedProperty, value); } }
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(OperatorPreset),
            new UIPropertyMetadata() { DefaultValue = false });

        [JsonProperty]
        public Guid OperatorInstanceID { get; set; }

        [JsonProperty]
        public Guid MetaOperatorID { get; set; }

        [JsonProperty]
        public bool IsInstancePreset { get; set; }

        [JsonProperty]
        public String Name { get; set; }

        [JsonProperty]
        public Guid Id { get; set; }

        [JsonProperty]
        public SortedDictionary<Guid, float> ValuesByParameterID = new SortedDictionary<Guid, float>();

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
    }
}
