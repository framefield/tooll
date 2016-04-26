using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Framefield.Core;
using Framefield.Core.Commands;

namespace Framefield.Tooll.Components.GeneticVariations
{
    public class VariationManager : DependencyObject, INotifyPropertyChanged
    {
        public VariationManager()
        {
            _randomStrength = 50f;
            Variations = new ObservableCollection<Variation>();
            Favorites = new ObservableCollection<Variation>();            
        }
        public ObservableCollection<Variation> Variations { get; private set; }
        public ObservableCollection<Variation> Favorites { get; private set; }

        // We have to keep this list be evolve more this this, if RandomStrength changes.
        public List<Variation> LastUsedVariations = new List<Variation>();

        #region iniatial generation


        public void SetupFirstGeneration(float randomStrength)
        {
            _randomStrength = randomStrength;

            Variations.Clear();
            LastUsedVariations.Clear();

            for (var i = 0; i < NUMBER_OF_THUMBS; i++)
            {
                var newVariation = new Variation(this) { SetValueCommand = GenerateFirstGenerationCommand(_random.Next()) };
                Variations.Add(newVariation);
            }
            ActiveVariation = null;
        }

        public void AdjustRandomStrength(float randomStrength)
        {
            _randomStrength = randomStrength;
            EvolveOrInitialize(_randomStrength);
        }


        public void ToggleVariationAsFavorite(Variation variation)
        {
            if (Favorites.Contains(variation))
            {
                Favorites.Remove(variation);
            }
            else
            {
                Favorites.Add(variation);
            }
        }

        public bool IsFavoriteButNotVariation(Variation variation)
        {
            return Favorites.Contains(variation) && !Variations.Contains(variation);
        }

        private Variation _activeVariation;
        public Variation ActiveVariation {
            get
            {
                return _activeVariation;
            }
            set
            {       
                if (value == null)
                {
                    if (_activeVariation != null)
                    {
                        _activeVariation.IsActive = false;
                        _activeVariation.SetValueCommand.Undo();
                        App.Current.UpdateRequiredAfterUserInteraction = true;
                    }
                        
                    _activeVariation = null;
                    return;
                }

                if (!Variations.Contains(value) && !Favorites.Contains(value))
                {
                    _activeVariation = null;
                    return;
                }
                    //return;
                    //throw new Exception("Try to set undefined variation as active");

                if (_activeVariation == value)
                    return;

                if (_activeVariation != null)
                {
                    _activeVariation.IsActive = false;
                    _activeVariation.SetValueCommand.Undo();
                }
                    
                _activeVariation = value;
                _activeVariation.IsActive = true;
                _activeVariation.SetValueCommand.Do();
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
        }

        public void ActivateNextVariation()
        {
            if (Variations == null || !Variations.Any())
                throw new Exception("Can't activate next variation if none are defined.");

            var activeIndex = Variations.IndexOf(ActiveVariation);

            var nextIndex = activeIndex + 1;
            if (nextIndex >= Variations.Count)
                nextIndex = 0;

            ActiveVariation = Variations[nextIndex];
        }

        public void ActivatePreviousVariation()
        {
            if (Variations == null || !Variations.Any())
                throw new Exception("Can't activate next variation if none are defined.");


            var activeIndex = Variations.IndexOf(ActiveVariation);
            
            var nextIndex = activeIndex - 1;
            if (nextIndex < 0)
                nextIndex = Variations.Count-1;

            ActiveVariation = Variations[nextIndex];
        }


        private SetValueGroupCommand GenerateFirstGenerationCommand(int seed)
        {
            var random = new Random(seed);

            var entries = new List<SetValueGroupCommand.Entry>();
            foreach (var el in App.Current.MainWindow.CompositionView.XCompositionGraphView.SelectionHandler.SelectedElements)
            {
                var op = el as OperatorWidget;
                if (op == null)
                    continue;

                foreach (var input in op.Operator.Inputs)
                {
                    if (input.Type != FunctionType.Float)
                        continue;

                    var metaInput = input.Parent.GetMetaInput(input);
                    var scale = metaInput.Scale;
                    var randomOffset = ((float)random.NextDouble() - 0.5f) * scale * _randomStrength;
                    var currentValue = OperatorPartUtilities.GetInputFloatValue(input);
                    var newValue = Utilities.Clamp(currentValue + randomOffset, metaInput.Min, metaInput.Max);

                    entries.Add(new SetValueGroupCommand.Entry
                    {
                        OpPart = input,
                        Value = new Float(newValue)
                    });
                }
            }
            return new SetValueGroupCommand(entries, App.Current.Model.GlobalTime, "Variation");
        }


        #endregion

        private SetValueGroupCommand GenerateNextGenerationCommand(List<Variation> ancestors)
        {
            var randomAncestor = ancestors[(int)(_random.NextDouble() * ancestors.Count)];
            var entries = new List<SetValueGroupCommand.Entry>();
            var inputIdx = 0;

            foreach (var el in App.Current.MainWindow.CompositionView.XCompositionGraphView.SelectionHandler.SelectedElements)
            {
                var op = el as OperatorWidget;
                if (op == null)
                    continue;

                foreach (var input in op.Operator.Inputs)
                {
                    if (input.Type != FunctionType.Float) 
                        continue;

                    var subCmd = randomAncestor.SetValueCommand[inputIdx];

                    var metaInput = input.Parent.GetMetaInput(input);
                    var oldValue = (subCmd.Value as Float).Val;
                    var newValue = oldValue;

                    if (_random.NextDouble() < CROSSOVER_RATE)
                    {
                        var randomSibling = ancestors[(int)(_random.NextDouble() * ancestors.Count)];
                        var siblingSubCmd = randomSibling.SetValueCommand[inputIdx];
                        var crossOverValue = (siblingSubCmd.Value as Float).Val;
                        newValue = crossOverValue;
                    }

                    var scale = metaInput.Scale;
                    var randomOffset = ((float)_random.NextDouble() - 0.5f) * scale * (float)Math.Pow(_randomStrength,2);
                    var mutationFactor = (float)Math.Pow(0.1, MUTATION_STRENGTH * _random.NextDouble());

                    newValue += randomOffset * mutationFactor;
                    newValue = Utilities.Clamp(newValue, metaInput.Min, metaInput.Max);

                    entries.Add(new SetValueGroupCommand.Entry()
                    {
                        OpPart = input,
                        Value = new Float(newValue)
                    });

                    inputIdx++;
                }
            }
            return new SetValueGroupCommand(entries, App.Current.Model.GlobalTime, "Set Variation");
        }

        public void EvolveOrInitialize(float randomStrength)
        {
            _randomStrength = randomStrength;

            var useAsAncestors = (from v in Variations
                                  where v.IsSelected
                                  select v).ToList();

            if (!useAsAncestors.Any())
                useAsAncestors = LastUsedVariations;

            if (!useAsAncestors.Any())
            {
                SetupFirstGeneration(randomStrength);
                return;                
            }

            GenerateVariations(useAsAncestors);
        }

        private void GenerateVariations(List<Variation> useAsAncestors)
        {
            Variations.Clear();
            for (var i = 0; i < NUMBER_OF_THUMBS; i++)
            {
                var newVariation =
                    new Variation(this)
                    {
                        SetValueCommand = GenerateNextGenerationCommand(useAsAncestors)
                    };
                Variations.Add(newVariation);
            }
            LastUsedVariations = useAsAncestors;
        }

        const int NUMBER_OF_THUMBS = 60;
        const float MUTATION_STRENGTH = 3;
        const float CROSSOVER_RATE = 0.9f;
        float _randomStrength;

        readonly Random _random = new Random();
        



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
