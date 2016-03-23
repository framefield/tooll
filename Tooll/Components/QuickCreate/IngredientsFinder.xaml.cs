// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace Framefield.Tooll.Components.QuickCreate
{
    /// <summary>
    /// Interaction logic for IngredientsFinder.xaml
    /// </summary>
    public partial class IngredientsFinder : UserControl
    {
        public IngredientsFinder()
        {
            // Initialize Ingredient collection with default palette
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            var ingredientManager = cgv.IngredientsManager;
            if (ingredientManager.DefaultPalette != null)
            {
                Ingredients = ingredientManager.DefaultPalette.Ingredients;
            }                
            else
            {
                Ingredients = new ObservableCollection<IngredientViewModel>();    
            }
                        
            InitializeComponent();

            Loaded += IngredientsFinder_Loaded;
            
        }

        void IngredientsFinder_Loaded(object sender, RoutedEventArgs e)
        {
            var binding = new Binding()
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = this,
                Path = new PropertyPath("Ingredients")
            };
            BindingOperations.SetBinding(XIngredientsFinderControl, ItemsControl.ItemsSourceProperty, binding);            
        }

        public ObservableCollection<IngredientViewModel> Ingredients  { get; private set; } 
    }
}
