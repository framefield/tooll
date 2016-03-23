// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Framefield.Core;
using Newtonsoft.Json;

namespace Framefield.Tooll.Components.QuickCreate
{
    /// <summary>
    /// Ingredients are OperatorDefinitions stored with preset configurations
    /// and arranged inside IngredientsPalettes presented and accessible from
    /// the QuickCreateWindow.
    /// 
    /// As a member of the CompositionGraphView the IngredientsManager handles
    /// the creation and serialization of Ingredients. It also provide the neccessary 
    /// ObservableCollections to the IngredientsFinder
    /// </summary>
    
    [JsonObject(MemberSerialization.OptIn)]
    public class IngredientsManager
    {
        public const int GRID_ROWS = 20;
        public const int GRID_COLUMNS = 20;
        public const int INGREDIENT_GRID_WIDTH = 4;

        public IngredientsManager()
        {
            LoadOrCreateIngredientsConfiguration();
        }

        public IngredientsPalette DefaultPalette { get; set; }


        [JsonProperty] 
        public ObservableCollection<IngredientsPalette> IngredientsPalettes = new ObservableCollection<IngredientsPalette>();


        public bool TryAddOperatorAsIngredientToDefaultPalette(Operator op)
        {
            var ingredient = new IngredientViewModel(op);
            ingredient.RemovedEvent += vm_RemovedHandler;

            if (DefaultPalette == null)
                throw new Exception("Default palette is not supposed to be null");

            var success = DefaultPalette.TryToAddIngredientAtEmptySlot(ingredient);
            SaveConfiguration();
            return success;
        }

        private const string PRESETS_FILENAME = @"Config/Ingredients.json";

        private void LoadOrCreateIngredientsConfiguration()
        {

            IngredientsPalettes = new ObservableCollection<IngredientsPalette>();
            if (File.Exists(PRESETS_FILENAME))
            {
                using (var reader = new StreamReader(PRESETS_FILENAME))
                {
                    var json = reader.ReadToEnd();
                    IngredientsPalettes = JsonConvert.DeserializeObject< ObservableCollection<IngredientsPalette>>(json);
                    if (IngredientsPalettes == null || IngredientsPalettes.Count == 0)
                    {
                        Logger.Warn("Loading ingredients palletes failed");
                        return;
                    }

                    DefaultPalette = IngredientsPalettes.First();

                    foreach (var vm in DefaultPalette.Ingredients)
                    {                        
                        vm.RemovedEvent += vm_RemovedHandler;
                    }
                }
            }
            else
            {
                DefaultPalette = new IngredientsPalette()
                {
                    Ingredients = new ObservableCollection<IngredientViewModel>(),
                    Name = "Default"
                };
                IngredientsPalettes = new ObservableCollection<IngredientsPalette>() { DefaultPalette };
            }            
        }

        void vm_RemovedHandler(object sender, RoutedEventArgs e)
        {
            var vm = sender as IngredientViewModel;
            if (vm == null) return;

            foreach (var palette in IngredientsPalettes)
            {
                foreach (var ing in palette.Ingredients)
                {
                    if (ing == vm)
                    {
                        palette.Ingredients.Remove(ing);
                        return;
                    }
                }
            }            
        }

        public void SaveConfiguration()
        {
            var serializedPresets = JsonConvert.SerializeObject(IngredientsPalettes, Formatting.Indented);
            using (var sw = new StreamWriter(PRESETS_FILENAME))
            {
                sw.Write(serializedPresets);
            }            
        }
    }

    public class IngredientsPalette
    {
        [JsonProperty]
        public string Name { get; set; }
        
        [JsonProperty]
        public ObservableCollection<IngredientViewModel> Ingredients = new ObservableCollection<IngredientViewModel>();

        /**
         * Returns false, if no free slot found
         */
        public bool TryToAddIngredientAtEmptySlot(IngredientViewModel ingredient)
        {
            var field = IngredientField;    // Initialize on the fly

            for (var col = 0;
                col < IngredientsManager.GRID_COLUMNS - IngredientsManager.INGREDIENT_GRID_WIDTH;
                col++)
            {
                for (var row = 0; row < IngredientsManager.GRID_ROWS; row++)
                {
                    var slotFree = true;
                    for (var slotCelIndex = 0; slotCelIndex < IngredientsManager.INGREDIENT_GRID_WIDTH; slotCelIndex++)
                    {
                        var slotContent = field[col + slotCelIndex, row];
                        if (slotContent != null)
                        {
                            slotFree = false;
                            break;
                        }
                    }
                    if (!slotFree) 
                        continue;

                    ingredient.GridPositionX = col;
                    ingredient.GridPositionY = row;
                    Ingredients.Add(ingredient);
                    return true;
                }
            }
            return false;
        }

        private IngredientViewModel[,] IngredientField
        {
            get
            {
                var field = new IngredientViewModel[IngredientsManager.GRID_COLUMNS, IngredientsManager.GRID_ROWS];

                foreach (var ing in Ingredients)
                {
                    for (var i = 0; i < IngredientsManager.INGREDIENT_GRID_WIDTH; i++)
                    {
                        if (ing.GridPositionX + i >= IngredientsManager.GRID_COLUMNS ||
                            ing.GridPositionY >= IngredientsManager.GRID_ROWS)
                        {
                            Logger.Warn("Ingredient position exceeds palette's grid size");
                            continue;
                        }

                        field[ing.GridPositionX + i, ing.GridPositionY] = ing;
                    }
                }
                return field;
            }
        }
    }
}
