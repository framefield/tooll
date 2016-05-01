using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms.VisualStyles;
using Framefield.Tooll.Components.GeneticVariations;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using Framefield.Core;
using Framefield.Core.Commands;

namespace Framefield.Tooll
{

    [JsonObject(MemberSerialization.OptIn)]
    public class Variation : DependencyObject, INotifyPropertyChanged
    {
        // Disable constructur without arguments
        private Variation()
        {
            
        }

        public Variation(VariationManager variationManager)
        {
            _variationManager = variationManager;
        }

        private VariationManager _variationManager;

        public bool IsSelected { get { return (bool) GetValue(IsSelectedProperty); } set { SetValue(IsSelectedProperty, value); } }
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(Variation),
            new UIPropertyMetadata() { DefaultValue=false });

        public bool IsActive { get { return (bool)GetValue(IsActiveProperty); } set { SetValue(IsActiveProperty, value); } }
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(Variation),
            new UIPropertyMetadata() { DefaultValue = false });


        public void Preview()
        {
            _variationManager.ActiveVariation = this;
        }

        public void EndPreview()
        {
            if(_variationManager.ActiveVariation == this)
                _variationManager.ActiveVariation = null;
        }

        public void SelectFavorite()
        {
            EndPreview();

            App.Current.UndoRedoStack.AddAndExecute(SetValueCommand);
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        public void SelectVariation()
        {
            IsSelected = !IsSelected;
            _variationManager.ActiveVariation = this;
            
            _variationManager.ToggleVariationAsFavorite(this);
        }

        public void MarkSomething()
        {
            _variationManager.ActiveVariation.IsSelected = !_variationManager.ActiveVariation.IsSelected;
        }

        [JsonProperty]
        public Guid MetaOperatorID { get; set; }

        [JsonProperty]
        public SortedDictionary<Guid, float> ValuesByParameterID = new SortedDictionary<Guid, float>();

        public SetValueGroupCommand SetValueCommand { get; set; }

        #region notifier
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion
    }
}
