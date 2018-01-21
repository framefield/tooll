// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using Newtonsoft.Json;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Core.Rendering;
using Framefield.Tooll.Rendering;
using SharpDX.Direct3D11;

namespace Framefield.Tooll.Components.ParameterView.OperatorPresets
{
    [JsonObject(MemberSerialization.OptIn)]
    /**
     * The preset manager handles the creation, display and application of operator presets
     * in the ParamaterView. It users the following components:
     * 
     * OperatorPreset - Model of a preset, gets serialized into a long list serialized to Config folder
     * PresetImageManager - Loads and saves preset-images to disks
     * PresetThumb - UserControl that handles rendering of a preset and forwards interaction to Manager
     * 
    */
    public class OperatorPresetManager : DependencyObject, INotifyPropertyChanged
    {


        public OperatorPresetManager()
        {
            CurrentOperatorPresets = new ObservableCollection<OperatorPreset>();

            if (File.Exists(PRESETS_FILENAME))
            {
                using (var reader = new StreamReader(PRESETS_FILENAME))
                {
                    var json = reader.ReadToEnd();
                    _operatorPresetsByMetaOpID = JsonConvert.DeserializeObject<SortedDictionary<Guid, List<OperatorPreset>>>(json);
                }
            }
            else
            {
                _operatorPresetsByMetaOpID = new SortedDictionary<Guid, List<OperatorPreset>>();
            }
        }

        public bool LivePreviewEnabled { get; set; }


        private OperatorPreset CreatePresetFromCurrentOperator()
        {
            var op = App.Current.MainWindow.XParameterView.ShownOperator; // todo: remove access to parameter view!
            if (op == null)
                return null;
            return CreatePresetFromOperator(op);
        }


        public void SavePresetFromCurrentlyShownOperator()
        {
            var newPreset = CreatePresetFromCurrentOperator();
            PresetImageManager.RenderAndSaveThumbnail(newPreset);

            if (newPreset.ValuesByParameterID.Count > 0)
            {
                AddPreset(newPreset, 0);
                FilterPresetsForSelectedOperator();
                SavePresets();
            }
        }


        public void UpdateAllThumbnails()
        {
            var keepList = CurrentOperatorPresets.ToArray();
            CurrentOperatorPresets.Clear(); // We rebuild the list to trigger update notification of the observable collection

            foreach (var preset in keepList)
            {
                PreviewPreset(preset);
                PresetImageManager.RenderAndSaveThumbnail(preset);
                RestorePreviewPreset();
                CurrentOperatorPresets.Add(preset);
            }
        }


        private void AddPreset(OperatorPreset newPreset, int idx)
        {
            if (!_operatorPresetsByMetaOpID.ContainsKey(newPreset.MetaOperatorID))
            {
                _operatorPresetsByMetaOpID[newPreset.MetaOperatorID] = new List<OperatorPreset>();
            }
            _operatorPresetsByMetaOpID[newPreset.MetaOperatorID].Insert(idx, newPreset);
        }


        public void CopyPresetsOfOpToAnother(MetaOperator source, MetaOperator target)
        {
            int idx = 0;
            if (_operatorPresetsByMetaOpID.ContainsKey(source.ID))
            {
                foreach (var srcPreset in _operatorPresetsByMetaOpID[source.ID])
                {
                    var newPreset = new OperatorPreset { MetaOperatorID = target.ID };
                    foreach (var srcEntry in srcPreset.ValuesByParameterID)
                    {
                        try
                        {
                            var srcInputIdx = (from input in source.Inputs
                                               where input.ID == srcEntry.Key
                                               select source.Inputs.IndexOf(input)).Single();
                            newPreset.ValuesByParameterID[target.Inputs[srcInputIdx].ID] = srcEntry.Value;
                        }
                        catch (Exception)
                        {
                            Logger.Warn("Could not copy preset parameter. This can happen when the preset contains obsolete parameters.");
                        }
                    }
                    if (newPreset.ValuesByParameterID.Count > 0)
                        AddPreset(newPreset, idx++);
                    else
                        Logger.Debug("Skipped a preset without any matching parameters");
                }
            }
            FilterPresetsForSelectedOperator();
            SavePresets();
        }

        public void SavePresets()
        {
            SavePresetsAs(PRESETS_FILENAME);
        }

        public void SavePresetsAs(string filePath)
        {
            var serializedPresets = JsonConvert.SerializeObject(_operatorPresetsByMetaOpID, Formatting.Indented);
            using (var sw = new StreamWriter(filePath))
            {
                sw.Write(serializedPresets);
            }
        }

        public void FilterPresetsForSelectedOperator()
        {
            var op = App.Current.MainWindow.XParameterView.ShownOperator;
            if (op == null || op.Definition == null || _operatorPresetsByMetaOpID == null)
                return;

            if (_operatorPresetsByMetaOpID.ContainsKey(op.Definition.ID))
            {
                CurrentOperatorPresets.Clear();
                foreach (var p in _operatorPresetsByMetaOpID[op.Definition.ID])
                {
                    CurrentOperatorPresets.Add(p); // FIXME: This triggers update events for each preset. Needs refactoring to new custom observable collection that enables range setting
                }
                SelectActivePreset();
            }
            else
            {
                CurrentOperatorPresets.Clear();
            }
        }

        // Called after click
        public void ApplyPreset(OperatorPreset preset)
        {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = true;

            _blending = false;
            Operator op = App.Current.MainWindow.XParameterView.ShownOperator;
            if (op == null || _setValueGroupCommand == null || op.Definition.ID != preset.MetaOperatorID)
                return;

            App.Current.UndoRedoStack.Add(_setValueGroupCommand);
            _tempOperatorPresetBeforePreview = null;
            _setValueGroupCommand = null;
            App.Current.UpdateRequiredAfterUserInteraction = true;
            SelectActivePreset();
        }

        private bool _blending = false;

        public void StartBlending()
        {
            _blending = true;
        }

        public void BlendPreset(OperatorPreset preset, float factor)
        {
            Operator op = App.Current.MainWindow.XParameterView.ShownOperator;
            if (op == null || op.Definition.ID != preset.MetaOperatorID || _setValueGroupCommand == null)
                return;

            var index = 0;
            foreach (var input in op.Inputs)
            {
                var metaInput = input.Parent.GetMetaInput(input);
                if (preset.ValuesByParameterID.ContainsKey(metaInput.ID) && _tempOperatorPresetBeforePreview != null)
                {
                    float presetValue = preset.ValuesByParameterID[metaInput.ID];
                    float opValue = _tempOperatorPresetBeforePreview.ValuesByParameterID[metaInput.ID];
                    var newFloatValue = opValue + factor * (presetValue - opValue);
                    _setValueGroupCommand.UpdateFloatValueAtIndex(index, newFloatValue);
                    index++;
                }
            }

            _setValueGroupCommand.Do();
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        public void CompleteBlendPreset(OperatorPreset preset)
        {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = false;
            _blending = false;
            SelectActivePreset();
            if (_setValueGroupCommand == null)
                return;

            App.Current.UndoRedoStack.Add(_setValueGroupCommand);
            _tempOperatorPresetBeforePreview = null;
        }


        private SetValueGroupCommand _setValueGroupCommand;
        private OperatorPreset _tempOperatorPresetBeforePreview;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="preset"></param>
        /// <returns>false if preview was refused</returns>
        public bool PreviewPreset(OperatorPreset preset)
        {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = true;

            if (_blending)
                return false;

            var entries = new List<SetValueGroupCommand.Entry>();

            Operator op = App.Current.MainWindow.XParameterView.ShownOperator;
            if (op != null && op.Definition.ID == preset.MetaOperatorID)
            {
                _tempOperatorPresetBeforePreview = CreatePresetFromOperator(op);

                foreach (var input in op.Inputs)
                {
                    var metaInput = input.Parent.GetMetaInput(input);
                    if (preset.ValuesByParameterID.ContainsKey(metaInput.ID))
                    {
                        entries.Add(new SetValueGroupCommand.Entry { OpPart = input, Value = new Float(preset.ValuesByParameterID[metaInput.ID]) });
                    }
                }
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
            _setValueGroupCommand = new SetValueGroupCommand(entries, App.Current.Model.GlobalTime);
            _setValueGroupCommand.Do();
            SelectActivePreset();
            return true;
        }

        public void RestorePreviewPreset()
        {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = false;

            if (_blending)
                return;

            if (_setValueGroupCommand != null)
            {
                _setValueGroupCommand.Undo();
                _setValueGroupCommand = null;
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
        }

        public void DeletePreset(OperatorPreset preset)
        {
            Operator op = App.Current.MainWindow.XParameterView.ShownOperator; // todo: remove access to parameter view
            if (op != null && op.Definition.ID == preset.MetaOperatorID)
            {
                _blending = false;
                RestorePreviewPreset();

                if (_operatorPresetsByMetaOpID.ContainsKey(op.Definition.ID))
                {
                    _operatorPresetsByMetaOpID[op.Definition.ID].Remove(preset);
                    FilterPresetsForSelectedOperator();
                    SavePresets();
                    App.Current.UpdateRequiredAfterUserInteraction = true;
                }
            }
        }

        public void SelectActivePreset()
        {
            var preset = CreatePresetFromCurrentOperator();
            foreach (var p in CurrentOperatorPresets)
            {
                bool matching = true;
                foreach (var paramID in p.ValuesByParameterID.Keys)
                {
                    if (preset.ValuesByParameterID.ContainsKey(paramID))
                    {
                        if (preset.ValuesByParameterID[paramID] != p.ValuesByParameterID[paramID])
                        {
                            matching = false;
                            break;
                        }
                    }
                }
                p.IsSelected = matching;
            }
        }


        [JsonProperty]
        private SortedDictionary<Guid, List<OperatorPreset>> _operatorPresetsByMetaOpID = new SortedDictionary<Guid, List<OperatorPreset>>();

        private OperatorPreset CreatePresetFromOperator(Operator op)
        {
            var newPreset = new OperatorPreset { MetaOperatorID = op.Definition.ID };

            foreach (var input in op.Inputs)
            {
                if (input.Type == FunctionType.Float)
                {
                    var metaInput = input.Parent.GetMetaInput(input);
                    var currentValue = OperatorPartUtilities.GetInputFloatValue(input);
                    newPreset.ValuesByParameterID[metaInput.ID] = currentValue;
                }
            }
            return newPreset;
        }

        #region notifier

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        #endregion



        public ObservableCollection<OperatorPreset> CurrentOperatorPresets { get; private set; }
        internal PresetImageManager PresetImageManager = new PresetImageManager();

        private static readonly string PRESETS_FILENAME = @"Config/Presets.json";
    }
}
