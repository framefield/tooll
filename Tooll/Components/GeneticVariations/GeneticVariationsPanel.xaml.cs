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

        void Selection_SelectionChangedHandler(object sender, SelectionHandler.SelectionChangedEventArgs e)
        {
            _variationManager.SetupFirstGeneration();
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
            _variationManager.SetupFirstGeneration(randomStrength: (float)XRandomSlider.Value);
        }

        private void EvolveButton_ClickHandler(object sender, RoutedEventArgs e)
        {
            _variationManager.EvolveNextGeneration(randomStrength: (float)XRandomSlider.Value );
        }

        private void UseButton_ClickHandler(object sender, RoutedEventArgs e)
        {
            foreach (var v in _variationManager.Variations)
            {
                if (!v.IsSelected) 
                    continue;

                App.Current.UndoRedoStack.AddAndExecute(v.SetValueCommand);
                _variationManager.SetupFirstGeneration();
                App.Current.UpdateRequiredAfterUserInteraction = true;
                return;
            }
        }


        readonly VariationManager _variationManager= new VariationManager();

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

        private void GeneticVariationsPanel_OnKeyUp(object sender, KeyEventArgs e)
        {
            Logger.Info("Keyup!");
            e.Handled = true;
        }
    }
}
