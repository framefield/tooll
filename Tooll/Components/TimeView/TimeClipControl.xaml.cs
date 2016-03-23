// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Framefield.Core;
using Framefield.Core.Commands;
using SharpDX;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using ICommand = Framefield.Core.ICommand;


namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for TimeClipControl.xaml
    /// </summary>
    public partial class TimeClipControl : UserControl, IValueSnapAttractor
    {
        public TimeClipControl() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ClipNameProperty = DependencyProperty.Register("ClipName", typeof(string), typeof(TimeClipControl), new UIPropertyMetadata(""));

        public string ClipName {
            get { return (string) GetValue(ClipNameProperty); }
            set { SetValue(ClipNameProperty, value); }
        }

        /**
         *  Note: This is a very unfortunate solution to get the Operator title into the TimeClipControl.
         *  To make this possible, we have to introduce ClipNamePropertry as a mediator and bind this to 
         *  the OperatorWidget. To make it worse, the OperatorWidget is only available in the DataContext 
         *  after TimeClipControl ist loaded.
         *  
         *  This function creates the second binding in the chain:
         *  
         *    TimeClipControl.TextBlock.Text ---> TimeClipControl.ClipName  ---> DataContext(ViewModel).OperatorWidget.XOperatorLabel.Text
         */
        private void CreateNameBinding() {
            

            var vm = DataContext as TimeClipViewModel;
            if (vm == null) 
                return;

            var nameBinding = new Binding() {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = vm.OperatorWidget.XOperatorLabel,
                Path = new PropertyPath("Text"),
            };
            this.SetBinding(TimeClipControl.ClipNameProperty, nameBinding);
        }


        #region implement snap attractor
        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (TV != null) {
                TV.TimeSnapHandler.AddSnapAttractor(this);
            }
            _visualParent = this.VisualParent as UIElement;
            CreateNameBinding();
        }


        private void OnUnloaded(object sender, RoutedEventArgs e) {
            if (TV != null) {
                TV.TimeSnapHandler.RemoveSnapAttractor(this);
            }            
        }

        const double SNAP_THRESHOLD = 10;

        public SnapResult CheckForSnap(double time) {
            var vm = DataContext as TimeClipViewModel;
            if (vm == null) 
                return null;

            if (_beingDragged)
            {
                var distanceToOriginaltTime = Math.Abs(time - _startTimeBeforeDrag) * TV.TimeScale;
                if (distanceToOriginaltTime < SNAP_THRESHOLD)
                {
                    return new SnapResult() { SnapToValue = _startTimeBeforeDrag, Force = distanceToOriginaltTime };
                }
                return null;
            }
                

            var distanceToStartTime = Math.Abs( time - vm.StartTime) * TV.TimeScale;
            if (distanceToStartTime < SNAP_THRESHOLD) {
                return new SnapResult() { SnapToValue=vm.StartTime, Force=distanceToStartTime };
            }

            var distanceToEndTime = Math.Abs(time - vm.EndTime) * TV.TimeScale;
            if (distanceToEndTime < SNAP_THRESHOLD) {
                return new SnapResult() { SnapToValue=vm.EndTime, Force=distanceToEndTime };
            }
            return null;
        }
        #endregion

        const int HACK_TIMECLIP_STARTTIME_PARAM_INDEX = 1;
        const int HACK_TIMECLIP_ENDTIME_PARAM_INDEX = 2;
        const int HACK_TIMECLIP_SOURCEIN_PARAM_INDEX = 3;
        const int HACK_TIMECLIP_SOURCEOUT_PARAM_INDEX = 4;
        const int HACK_TIMECLIP_SOURCEOUT_LAYER_ID = 5;
        const float MIN_SEGMENT_DURATION = 1.0f / 60.0f;

        #region XAML event handler
        private double m_HorizontalOffsetAtDragStart=0;

        private MacroCommand _updateValueGroupMacroCommand;

        private ICommand BuildManipulationCommand(OperatorPart input, float newValue =Single.NaN)
        {
            ICommand cmd;
            
            var isAnimated = Animation.GetRegardingAnimationOpPart(input) != null;

            if (isAnimated)            
                cmd = new AddOrUpdateKeyframeCommand(App.Current.Model.GlobalTime, newValue, input);
            
            else            
                cmd = new UpdateOperatorPartValueFunctionCommand(input, new Float(newValue));
            
            _commandsForInputs[input] = cmd;
            return cmd;
        }

        private void UpdateManipulationCommand(OperatorPart input, float newValue)
        {
            var cmd = _commandsForInputs[input];
            if (cmd is AddOrUpdateKeyframeCommand)
            {
                var addKeyframeCommand = cmd as AddOrUpdateKeyframeCommand;
                addKeyframeCommand.KeyframeValue.Value = newValue;
            }
            else
            {
                var updateValueCommand = cmd as UpdateOperatorPartValueFunctionCommand;
                updateValueCommand.Value = new Float(newValue);
            }
        }

        private Dictionary<OperatorPart, ICommand> _commandsForInputs;

        private Point _mousePositionAtDragStart;
        private UIElement _visualParent;
        private double _startTimeBeforeDrag;
        private double _endTimeBeforeDrag;
        private double _sourceInTimeBeforeDrag;
        private double _sourceOutTimeBeforeDrag;
        private double _layerBeforeDrag;
        private bool _exceededDragThreshold = false;
        private bool _beingDragged;
        
        
        private void XTimeClip_Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            var vm = DataContext as TimeClipViewModel;
            if (vm == null)
                return;

            _mousePositionAtDragStart = Mouse.GetPosition(_visualParent);
            _startTimeBeforeDrag = vm.StartTime;
            _endTimeBeforeDrag = vm.EndTime;
            _sourceInTimeBeforeDrag = vm.SourceStartTime;
            _sourceOutTimeBeforeDrag = vm.SourceEndTime;
            _layerBeforeDrag = vm.Layer;
            _exceededDragThreshold = false;
            _beingDragged = true;

            _commandsForInputs = new Dictionary<OperatorPart, ICommand>();
            var commandList = new List<ICommand>();
            commandList.Add(BuildManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_STARTTIME_PARAM_INDEX], (float)vm.StartTime));
            commandList.Add(BuildManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_ENDTIME_PARAM_INDEX], (float)vm.EndTime));
            commandList.Add(BuildManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_SOURCEIN_PARAM_INDEX], (float)vm.SourceStartTime));
            commandList.Add(BuildManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_SOURCEOUT_PARAM_INDEX], (float)vm.SourceEndTime));
            commandList.Add(BuildManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_SOURCEOUT_LAYER_ID], (float)vm.Layer));
            _updateValueGroupMacroCommand = new MacroCommand("Update timeclip parameters", commandList);
            _updateValueGroupMacroCommand.Do();
        }


        private void XTimeClip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) 
        {
            var vm = DataContext as TimeClipViewModel;
            if (vm == null)
                return;

            var horizontalDelta = (float)(Mouse.GetPosition(_visualParent).X - _mousePositionAtDragStart.X);
            var verticalDelta = (float)(Mouse.GetPosition(_visualParent).Y - _mousePositionAtDragStart.Y);
            var timeDelta = TV.XToTime(horizontalDelta) - TV.XToTime(0);

            if (!_exceededDragThreshold
                && Math.Abs(horizontalDelta) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(verticalDelta)   < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }
                
            _exceededDragThreshold = true;

            // Drag inside
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                UpdateManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_SOURCEIN_PARAM_INDEX], (float)(_sourceInTimeBeforeDrag + timeDelta));
                UpdateManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_SOURCEOUT_PARAM_INDEX], (float)(_sourceOutTimeBeforeDrag + timeDelta));
            }  
            else {
                var newStartTime = _startTimeBeforeDrag + timeDelta;
                var newEndTime = _startTimeBeforeDrag + timeDelta;

                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    var snapStartTime = TV.TimeSnapHandler.CheckForSnapping(newStartTime);
                    if (!Double.IsNaN(snapStartTime))
                    {
                        timeDelta += snapStartTime - newStartTime;
                    }
                    else
                    {
                        var snapEndTime = TV.TimeSnapHandler.CheckForSnapping(newEndTime);
                        if (!Double.IsNaN(snapEndTime))
                        {
                            timeDelta += snapEndTime - newEndTime;
                        }
                    }                
                }
                
                UpdateManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_STARTTIME_PARAM_INDEX], (float)(_startTimeBeforeDrag + timeDelta));
                UpdateManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_ENDTIME_PARAM_INDEX], (float)(_endTimeBeforeDrag + timeDelta));

                const int MAX_LAYER_COUNT = 20;

                var newLayer = (int)(_layerBeforeDrag + verticalDelta / 21);
                newLayer = (int)MathUtil.Clamp(newLayer, 0, MAX_LAYER_COUNT);

                UpdateManipulationCommand(vm.OperatorWidget.Operator.Inputs[HACK_TIMECLIP_SOURCEOUT_LAYER_ID], newLayer);                
            }
            _updateValueGroupMacroCommand.Do();

            App.Current.UpdateRequiredAfterUserInteraction = true;
            e.Handled= true;
        }

        private void XTimeClip_Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) 
        {
            _beingDragged = false;

            var vm = DataContext as TimeClipViewModel;
            if (vm == null)
                return;

            var dragDistance = (float)(Mouse.GetPosition(_visualParent) - _mousePositionAtDragStart).Length;

            // Select only
            if (dragDistance < SystemParameters.MinimumVerticalDragDistance )
            {                
                var newSelection = new List<ISelectable> {vm.OperatorWidget};
                App.Current.MainWindow.CompositionView.CompositionGraphView.SelectedElements = newSelection;
                _updateValueGroupMacroCommand.Undo();
            }
            else
            {
                App.Current.UndoRedoStack.Add(_updateValueGroupMacroCommand);
                _updateValueGroupMacroCommand = null;                
            }
        }

        private void XStartThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) 
        {
            var vm = DataContext as TimeClipViewModel;
            if (vm == null)
                return;

            var orgStartTime = vm.StartTime;
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt)) {

                vm.SourceStartTime += TV.XToTime(e.HorizontalChange - m_HorizontalOffsetAtDragStart) - TV.XToTime(0);
                m_HorizontalOffsetAtDragStart = e.HorizontalChange;
            }
            else {
                vm.StartTime += TV.XToTime(e.HorizontalChange) - TV.XToTime(0);
                if (Keyboard.Modifiers != ModifierKeys.Shift) {
                    double snapTime= TV.TimeSnapHandler.CheckForSnapping(vm.StartTime, this);
                    if (!Double.IsNaN(snapTime)) {
                        vm.StartTime= snapTime;
                    }
                }

                if (vm.StartTime > vm.EndTime - MIN_CLIP_DURATION) {
                    vm.StartTime = vm.EndTime - MIN_CLIP_DURATION;
                }
                if (Keyboard.Modifiers != ModifierKeys.Control) {
                    vm.SourceStartTime -= (orgStartTime - vm.StartTime) * (vm.SourceEndTime - vm.SourceStartTime) / (vm.EndTime - orgStartTime);
                }
            }            
            App.Current.UpdateRequiredAfterUserInteraction = true;
            e.Handled = true;
        }


        private void XEndThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) 
        {
            var vm = DataContext as TimeClipViewModel;
            if (vm == null)
                return;

            var orgEndTime= vm.EndTime;

            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt)) 
            {                
                vm.SourceEndTime += TV.XToTime(e.HorizontalChange - m_HorizontalOffsetAtDragStart) - TV.XToTime(0);
                m_HorizontalOffsetAtDragStart = e.HorizontalChange;
            }
            else {
                var absolutePosition = Mouse.GetPosition(TV);

                vm.EndTime = TV.XToTime(absolutePosition.X);
                if (Keyboard.Modifiers != ModifierKeys.Shift) {
                    var snapTime= TV.TimeSnapHandler.CheckForSnapping(vm.EndTime, this);
                    if (!Double.IsNaN(snapTime)) {
                        vm.EndTime = snapTime;
                    }
                }

                if (vm.StartTime > vm.EndTime - MIN_CLIP_DURATION) {
                    vm.EndTime = vm.StartTime + MIN_CLIP_DURATION;
                }
                if (Keyboard.Modifiers != ModifierKeys.Control) {
                    vm.SourceEndTime -= (orgEndTime - vm.EndTime) * (vm.SourceEndTime - vm.SourceStartTime) / (orgEndTime - vm.StartTime) ;
                }
            }
            
            App.Current.UpdateRequiredAfterUserInteraction = true; 
            e.Handled = true;
        }

        private void XTimeClip_Thumb_DoubleClicked(object sender, MouseButtonEventArgs e) {
            App.Current.MainWindow.CompositionView.CompositionGraphView.CenterSelectedElements();
        }
        #endregion



        #region dirty stuff
        private TimeView m_TV;
        public TimeView TV
        {
            get
            {
                if (m_TV == null) {
                    m_TV = UIHelper.FindVisualParent<TimeView>(this);

                }

                return m_TV;
            }
        }
        #endregion


        const double MIN_CLIP_DURATION = 1/60.0;
    }
}
