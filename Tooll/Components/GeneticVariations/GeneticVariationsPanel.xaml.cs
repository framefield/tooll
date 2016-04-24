using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AvalonDock;
using Framefield.Core;
using Framefield.Core.Commands;

namespace Framefield.Tooll.Components.GeneticVariations
{
    public partial class GeneticVariationsPanel : DocumentContent
    {

        public GeneticVariationsPanel()
        {
            Initialized += GeneticVariationsPanel_Initialized;
            InitializeComponent();
            Title = "Genetic Variations";
            Loaded += GeneticVariationsPanel_Loaded;
            Unloaded += GeneticVariationsPanel_Unloaded;
        }

        void GeneticVariationsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.CompositionView.XCompositionGraphView.SelectionHandler.SelectionChanged += Selection_SelectionChangedHandler;
        }

        void GeneticVariationsPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.CompositionView.XCompositionGraphView.SelectionHandler.SelectionChanged -= Selection_SelectionChangedHandler;
        }


        void GeneticVariationsPanel_Initialized(object sender, EventArgs e)
        {
            var binding = new Binding()
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = this._variationManager,
                Path = new PropertyPath("Variations")
            };
            BindingOperations.SetBinding(XVariationGrid, ItemsControl.ItemsSourceProperty, binding);
            _variationManager.SetupFirstGeneration(randomStrength: RandomStrength);
        }

        float RandomStrength
        {
            get
            {
                var index = XRandomDropbox.SelectedIndex;
                switch (index)
                {
                    case 0:
                        return 5f;
                    case 1:
                        return 15f;
                    case 2:
                        return 30f;
                    case 3:
                        return 50f;
                    case 4:
                        return 75f;
                    default:
                        throw new Exception("Unexpected variation random strength.");
                }
            }
        }

        void Selection_SelectionChangedHandler(object sender, SelectionHandler.SelectionChangedEventArgs e)
        {
            _variationManager.SetupFirstGeneration(RandomStrength);
        }


        private void MoreButton_ClickHandler(object sender, RoutedEventArgs e)
        {
            _variationManager.SetupFirstGeneration(randomStrength: RandomStrength);
        }


        private void XRandomDropbox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _variationManager.AdjustRandomStrength(RandomStrength);
        }


        private void EvolveButton_ClickHandler(object sender, RoutedEventArgs e)
        {
            _variationManager.EvolveOrInitialize(RandomStrength/3);
        }



        private void UseButton_ClickHandler(object sender, RoutedEventArgs e)
        {
            foreach (var v in _variationManager.Variations)
            {
                if (!v.IsSelected) 
                    continue;

                App.Current.UndoRedoStack.AddAndExecute(v.SetValueCommand);
                _variationManager.SetupFirstGeneration(RandomStrength);
                App.Current.UpdateRequiredAfterUserInteraction = true;
                return;
            }
        }


        private void GeneticVariationsPanel_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _variationManager.ActivateNextVariation();
            e.Handled = true;
        }


        private void UIElement_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                _variationManager.ActivateNextVariation();
            }
            else
            {
                _variationManager.ActivatePreviousVariation();
                
            }
            
            e.Handled = true;
        }

        readonly VariationManager _variationManager = new VariationManager();
    }
}
