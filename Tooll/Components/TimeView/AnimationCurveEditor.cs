// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Core.Curve;
using Framefield.Helper;
using Framefield.Tooll;
using Newtonsoft.Json;
using NSch;
using Sharpen;
using Logger = Framefield.Core.Logger;

namespace Framefield.Tooll
{
    /**
     * Connects a CurveEditor with a CompositionGraphView and its selected Operators
     * 
     * When selection or definition of a selected operators change, updating the curve editor 
     * is triggered. This separation is introduced because the CurveEditor only knows about
     * Curves (not about Operators and their Operator parts).
     */
    class AnimationCurveEditor: CurveEditor
    {



        ObservableCollection<ICurve> m_FocusedCurves  = new ObservableCollection<ICurve>();
        public ObservableCollection<ICurve> FocusedCurves
        { 
            get { 
                return m_FocusedCurves; 
            } 
            private set { 
                m_FocusedCurves = value; 
            } 
        }


        public AnimationCurveEditor() : base()
        {
            m_FocusedCurves.CollectionChanged +=new System.Collections.Specialized.NotifyCollectionChangedEventHandler(m_FocusedCurves_CollectionChanged);
            XHorizontalScaleLines.Visibility= System.Windows.Visibility.Collapsed;
          
  
        }

        void m_FocusedCurves_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ShowCurvesForSelectedOperators();
            //CustomCommands.FitCurveValueRangeCommand.Execute(null, this);
        }


        private List<ISelectable> m_ObservedOperatorWidgets= new List<ISelectable>();

        /**
         * Note: It's interesting that this handler does not use the event properties but rather
         * directly accesses the sender's internal property to get a complete list of all selected elements.
         */
        public void SelectionChangedEventHandler(object sender, SelectionHandler.SelectionChangedEventArgs args) {
            var selectionHandler = sender as SelectionHandler;
            if (selectionHandler != null) {

                // Clear up all handlers
                foreach (var item in m_ObservedOperatorWidgets) {
                    OperatorWidget op = item as OperatorWidget;
                    if (op != null) {
                        op.Operator.ModifiedEvent -= OperatorModifiedHandler;
                    }
                }
                m_FocusedCurves.Clear();
                m_ObservedOperatorWidgets.Clear();

                // Listen to changes of selected ops
                foreach (var item in selectionHandler.SelectedElements) {
                    var op = item as OperatorWidget;
                    if (op != null) {
                        op.Operator.ModifiedEvent += OperatorModifiedHandler;
                        m_ObservedOperatorWidgets.Add(op);
                    }
                }
                
                //CustomCommands.FitCurveValueRangeCommand.Execute(null, this);
                ShowCurvesForSelectedOperators();
                FitValueRange();
            }
        }

        // It's very unfortunate, that this is public, but AnimationCurveEditor triggers
        // a complete rebuild of all curves if the any func (e.I. non-animated value) of 
        // the current selected Operators are changed. Disabling CurveUpdates for non-animated
        // parameters in the parameter was the easiest solution.
        public bool DisableCurveUpdatesOnModifiedEvent = false;

        private void OperatorModifiedHandler(object sender, System.EventArgs args) {
            if (!DisableCurveUpdatesOnModifiedEvent)
                ShowCurvesForSelectedOperators();
        }

        public new void FitValueRange()
        {
            if(!DisableCurveUpdatesOnModifiedEvent)
                base.FitValueRange();
        }

        public void JumpToNextKey() 
        {
            Double minNextTime = Double.PositiveInfinity;

            foreach (var curveVdefPair in getSelectedOrAllVDefinitions()) {
                foreach( var u in curveVdefPair.Value) {
                    if (u > App.Current.Model.GlobalTime && u < minNextTime) {
                        minNextTime = u;
                    }
                }
            }
            if( !Double.IsPositiveInfinity( minNextTime )) {
                App.Current.Model.GlobalTime = minNextTime;
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
        }


        public void JumpToPreviousKey()
        {
            Double maxPreviousTime = Double.NegativeInfinity;

            foreach (var curveVdefPair in getSelectedOrAllVDefinitions()) {
                foreach (var u in curveVdefPair.Value) {
                    if (u < App.Current.Model.GlobalTime && u > maxPreviousTime) {
                        maxPreviousTime = u;
                    }
                }
            }
            if (!Double.IsNegativeInfinity(maxPreviousTime)) {
                App.Current.Model.GlobalTime = maxPreviousTime;
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
        }

        Dictionary<OperatorPart, ICurve> curvesByInput = new Dictionary<OperatorPart, ICurve>();
        List<OperatorPart> curveInputs = new List<OperatorPart>();

        private void ShowCurvesForSelectedOperators()
        {
            var animationCurves = new List<ICurve>();

            curvesByInput.Clear();
            curveInputs.Clear();

            foreach (var se in m_ObservedOperatorWidgets) {
                var opWidget = se as OperatorWidget;
                if (opWidget != null) {
                    foreach (var input in opWidget.Operator.Inputs) {
                        var animationOpPart = Animation.GetRegardingAnimationOpPart(input);
                        if (animationOpPart == null) 
                            continue;

                        var animationCurve = animationOpPart.Func as ICurve;
                        
                        if (animationCurve == null)
                            continue;

                        animationCurve.ComponentIndex = GetComponentIndexFromParameterName(input.Name);

                        if (m_FocusedCurves.Count == 0 ||m_FocusedCurves.Contains(animationCurve))
                        {
                            curvesByInput[input] = animationCurve;
                            animationCurves.Add(animationCurve);
                            curveInputs.Add(input);
                        }
                    }
                }
            }
            SetCurveOperators(animationCurves);
        }

        private int GetComponentIndexFromParameterName(string name)
        {
            var parts = name.Split('.');
            if (parts.Length == 0)
                return 0;

            var suffix = parts[parts.Length - 1];

            var suffixesWithIndeces = new Dictionary<string,int>() 
            {
                {"x", 1},
                {"y", 2},
                {"z", 3},
                {"r", 1},
                {"g", 2},
                {"b", 3},
                {"width", 1},
                {"height", 2},
                {"depth", 3},
            };

            var index = 0;
            suffixesWithIndeces.TryGetValue(suffix.ToLower(), out index);
            return index;
        }

        public override double UOffset
        {
            get { return base.UOffset; }
            set
            {
                base.UOffset= value;
                if (TV != null) {
                    TV.TimeOffset = value;
                }
            }
        }

        public override void CopyKeyframes()
        {
            var parameterNamesWithKeys = new Dictionary<String, List<Keyframe>>();
            var curvesWithSelectedTimes = getSelectedOrAllVDefinitions();

            foreach (var input in curveInputs)
            {                
                var curve = curvesByInput[input];
                if (!curvesWithSelectedTimes.ContainsKey(curve))
                    continue;

                var vDefinitions = new List< Keyframe>();

                foreach (var u in curvesWithSelectedTimes[curve])
                {
                    var vDef = curve.GetV(u).Clone();
                    vDefinitions.Add(new Keyframe(){Time=u, VDefinition= vDef});
                }

                parameterNamesWithKeys[input.Name] = vDefinitions;
            }

            var json = JsonConvert.SerializeObject(parameterNamesWithKeys, Formatting.Indented);
            Clipboard.SetText(json);
        }


        public override void PasteKeyframes()
        {
            var addedNewCurves = false;

            List<ICommand> pasteKeyframeCommands = new List<ICommand>();
            var curvesWithSelectedTimes = getSelectedOrAllVDefinitions();
            
            var parameterNamesWithKeys = new Dictionary<String, List<Keyframe>>();

            try
            {
                parameterNamesWithKeys =
                    JsonConvert.DeserializeObject<Dictionary<String, List<Keyframe>>>(
                        Clipboard.GetText());
            }
            catch ( Exception e)
            {
                Logger.Warn("Inserting keyframes failed: {0}",e.Message);
                return;
            }

            _updatingCurveEnabled = false;  // prevent update triggered by Curve.Move

            // Compute start and end times
            var minTime = double.PositiveInfinity;
            var maxTime = double.NegativeInfinity;

            foreach (var keyframe in parameterNamesWithKeys.Select(inputNameAndKeys => inputNameAndKeys.Value)
                                                           .SelectMany(keyframes => keyframes))
            {
                minTime = Math.Min(minTime, keyframe.Time);
                maxTime = Math.Max(maxTime, keyframe.Time);
            }

            // Collect matching curves
            var curvesWithMatchingInputs = new List<ICurve>();
            foreach (var inputNameAndKeys in parameterNamesWithKeys)
            {
                var inputName = inputNameAndKeys.Key;
                var keys = inputNameAndKeys.Value;

                foreach (var input in curveInputs)
                {
                    if (input.Name != inputName)
                        continue;

                    var curve = curvesByInput[input];
                    curvesWithMatchingInputs.Add(curve);
                }
            }

            var curveMapping = new  List<  Tuple< ICurve, List<Tuple<double, VDefinition>>>>();

            if (curvesWithMatchingInputs.Count == 0)
            {
                // Check if non-animated inputs exists.
                var parametersToAnimate = parameterNamesWithKeys.Keys.ToList();

                foreach (var se in m_ObservedOperatorWidgets)
                {
                    var opWidget = se as OperatorWidget;
                    if (opWidget == null) 
                        continue;
                    
                    foreach (var input in opWidget.Operator.Inputs)
                    {
                        if (parametersToAnimate.IndexOf(input.Name) == -1)
                            continue;

                        // Skip if already connected
                        if (input.Connections.Count > 0) 
                            continue;
                        
                        // Animate parameter
                        var addAnimiationCommand = new SetupAnimationCommand(input, App.Current.Model.GlobalTime);
                        App.Current.UndoRedoStack.AddAndExecute(addAnimiationCommand);

                        // Add to list of curves
                        var animationOpPart = Animation.GetRegardingAnimationOpPart(input);
                        var animationCurve = animationOpPart.Func as ICurve;
                        curvesByInput[input] = animationCurve;
                        curveInputs.Add(input);
                        parametersToAnimate.Remove(input.Name);
                        addedNewCurves = true;
                    }
                }
            }

            // Insert gap to all animated parameters
            foreach (var curve in curvesByInput.Values)
            {
                var points = curve.GetPoints();
                points.Sort((a, b) => a.Key > b.Key ? -1:1);        // If moving unsorted, later keyframes could be overwritten.

                foreach (var timeWithKey in points)
                {
                    var time = timeWithKey.Key;
                    if (time > App.Current.Model.GlobalTime)
                    {
                        var c = new MoveKeyframeCommand(time, time + maxTime - minTime, curve);
                        pasteKeyframeCommands.Add(c);                        
                    }
                }
            }

            foreach ( var inputNameAndKeys in parameterNamesWithKeys)
            {
                var inputName = inputNameAndKeys.Key;
                var keyframes = inputNameAndKeys.Value;

                foreach (var input in curveInputs)
                {
                    if (input.Name != inputName)
                        continue;
                        
                    var curve = curvesByInput[input];

                    foreach (var keyframe in keyframes)
                    {
                        pasteKeyframeCommands.Add(new AddOrUpdateKeyframeCommand(keyframe.Time -  minTime + App.Current.Model.GlobalTime, keyframe.VDefinition, curve));
                    }
                }
            }

            var macroCommand = new MacroCommand("Paste Keyframes", pasteKeyframeCommands); 
            App.Current.UndoRedoStack.AddAndExecute(macroCommand);
            _updatingCurveEnabled = true;
            if (addedNewCurves)
            {
                ShowCurvesForSelectedOperators();
            }
            else
            {
                RebuildCurrentCurves();    
            }            
        }
        
        public override void DuplicateKeyframes()
        {
            if (_SelectionHandler.SelectedElements.Count > 0) {
                var minTime = Double.PositiveInfinity;
                               
                // Find start time
                foreach (var curveVdefPair in getSelectedOrAllVDefinitions()) {
                    foreach (var u in curveVdefPair.Value) {
                        minTime = Math.Min( minTime, u);
                    }
                }

                DuplicateKeyframesToU(minTime);
            }
        }

        #region dirty stuff
       
        private TimeView m_TV;
        public TimeView TV
        {
            get
            {
                if (m_TV == null) {
                    m_TV = UIHelper.FindParent<TimeView>(this);
                    _USnapHandler= TV.TimeSnapHandler;
                }
                return m_TV;
            }
        }
        #endregion




    }
}
