// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Framefield.Core;
using Framefield.Core.Commands;


namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for RgbaParameterValue.xaml
    /// </summary>
    public partial class RgbaParameterValue : IColorPickingComponent
    {
        public ColorPickerView LinkedColorPicker { get; set; }

        public RgbaParameterValue(OperatorPart[] valueHolders)
        {
            InitializeComponent();

            _rFloatEdit = new FloatParameterControl(valueHolders[0]) { Precision = 2 };
            Grid.SetColumn(_rFloatEdit, 0);
            XGrid.Children.Add(_rFloatEdit);
            _rFloatEdit.TabMoveEvent += TabMoveEventHandler;
            _parameterControls.Add(_rFloatEdit);

            _gFloatEdit = new FloatParameterControl(valueHolders[1]) { Precision = 2 };
            Grid.SetColumn(_gFloatEdit, 1);
            XGrid.Children.Add(_gFloatEdit);
            _gFloatEdit.TabMoveEvent += TabMoveEventHandler;
            _parameterControls.Add(_gFloatEdit);

            _bFloatEdit = new FloatParameterControl(valueHolders[2]) { Precision = 2 };
            Grid.SetColumn(_bFloatEdit, 2);
            XGrid.Children.Add(_bFloatEdit);
            _bFloatEdit.TabMoveEvent += TabMoveEventHandler;
            _parameterControls.Add(_bFloatEdit);

            _aFloatEdit = new FloatParameterControl(valueHolders[3]) { Precision = 2 };
            Grid.SetColumn(_aFloatEdit, 4);
            XGrid.Children.Add(_aFloatEdit);
            _aFloatEdit.TabMoveEvent += TabMoveEventHandler;
            _parameterControls.Add(_aFloatEdit);


            // Bind color fields
            var multiBinding = new MultiBinding { Converter = RgbToBrushConverter };
            multiBinding.Bindings.Add(new Binding("Value") { Source = _rFloatEdit });
            multiBinding.Bindings.Add(new Binding("Value") { Source = _gFloatEdit });
            multiBinding.Bindings.Add(new Binding("Value") { Source = _bFloatEdit });
            BindingOperations.SetBinding(XColorThumb, Border.BackgroundProperty, multiBinding);

            var binding = new Binding("Value") { Source = _aFloatEdit };
            XPatternOverlay.SetBinding(OpacityProperty, binding);
            

            Loaded += RgbaParameterValue_Loaded;
            Unloaded += RgbaParameterValue_Unloaded;
        }

        void RgbaParameterValue_Unloaded(object sender, RoutedEventArgs e) {
            Loaded -= RgbaParameterValue_Loaded;
            Unloaded -= RgbaParameterValue_Unloaded;
        }

        void RgbaParameterValue_Loaded(object sender, RoutedEventArgs e) {
            AttachColorFieldToPicker();
        }

        public static readonly DependencyProperty ThumbColorProperty = DependencyProperty.Register("ThumbColor", typeof(Color), typeof(RgbaParameterValue));
        public Color ThumbColor
        {
            get { return (Color) GetValue(ThumbColorProperty); }
            set { SetValue(ThumbColorProperty, value); }
        }

        static readonly RgbToBrushConverter RgbToBrushConverter = new RgbToBrushConverter();

        private MacroCommand _updateColorsMacroCommand;
        private Dictionary<FloatParameterControl, ICommand> _commandsForControls; 


        private ICommand BuildManipulationCommand(FloatParameterControl control, float newValue)
        {
            ICommand cmd;
            if(control.IsAnimated)
            {
                cmd= new AddOrUpdateKeyframeCommand(App.Current.Model.GlobalTime, newValue, control.ValueHolder);
            }
            else
            {
                cmd= new UpdateOperatorPartValueFunctionCommand(control.ValueHolder, new Float(newValue));
            }
            _commandsForControls[control] = cmd;
            return cmd;
        }


        private void UpdateManipulationCommand(FloatParameterControl control, float newValue)
        {
            var cmd = _commandsForControls[control];
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

        
        public void CreateAndExecuteColorManipulationCommand(float r, float g, float b, float a)
        {
            _commandsForControls = new Dictionary<FloatParameterControl, ICommand>();

            var commandList = new List<ICommand>();
            commandList.Add(BuildManipulationCommand(_rFloatEdit, r));
            commandList.Add(BuildManipulationCommand(_gFloatEdit, g));
            commandList.Add(BuildManipulationCommand(_bFloatEdit, b));
            commandList.Add(BuildManipulationCommand(_aFloatEdit, a));
            _updateColorsMacroCommand = new MacroCommand("Update color parameters", commandList);
            _updateColorsMacroCommand.Do();            
        }

        public void StartColorManipulation() 
        {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = true;

            CreateAndExecuteColorManipulationCommand(_rFloatEdit.Value, _gFloatEdit.Value, _bFloatEdit.Value, _aFloatEdit.Value);
        }

        public void ManipulateColor(float r, float g, float b, float a)
        {
            UpdateManipulationCommand(_rFloatEdit, r);
            UpdateManipulationCommand(_gFloatEdit, g);
            UpdateManipulationCommand(_bFloatEdit, b);
            UpdateManipulationCommand(_aFloatEdit, a);
            _updateColorsMacroCommand.Do();   
        }

        public void EndColorManipulation() 
        {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = false;
            App.Current.UndoRedoStack.Add(_updateColorsMacroCommand);
            _updateColorsMacroCommand = null;
        }

        /**
         * Notes notes on group manipulation:
         * - This is triggered by the XParameterName Button in OperatorParameterViewRow. This emits events for reset and value manipulation.
         * This binding between these events and the following handlers is done in Constructor of the OperatorParameterView
         */
        public void ResetToDefaultHandler(object sender, EventArgs e)
        {
            var entries = new List<OperatorPart>
                              {
                                  _rFloatEdit.ValueHolder,
                                  _gFloatEdit.ValueHolder,
                                  _bFloatEdit.ValueHolder,
                                  _aFloatEdit.ValueHolder,
                              };
            var command = new ResetInputToGroupCommand(entries);
            App.Current.UndoRedoStack.AddAndExecute(command);
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private float _r, _g, _b, _a;
        public void StartManipulationHandler(object sender, EventArgs e)
        {
            _r = _rFloatEdit.Value;
            _g = _gFloatEdit.Value;
            _b = _bFloatEdit.Value;
            _a = _aFloatEdit.Value;
            StartColorManipulation();
        }

        public void UpdateManipulationHandler(object sender, ParameterGroupManipulatedEventArgs e)
        {

            var hsl = Utils.HSL.FromRGB(_r, _g, _b);
            var v1 = hsl.L;
            
            hsl.L += e.Offset/255f;
            var rgb = Utils.RGB.FromHSL(hsl);

            ManipulateColor(rgb.R, rgb.G, rgb.B, _a);
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        public void EndManipulationHandler(object sender, EventArgs e)
        {
            EndColorManipulation();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AttachColorFieldToPicker();
        }

        public void AttachColorFieldToPicker() {
            App.Current.MainWindow.XColorPickerView.LinkedColorPickingComponent = this;

            BindingOperations.SetBinding(App.Current.MainWindow.XColorPickerView, ColorPickerView.RProperty, new Binding("Value") { Source = _rFloatEdit, Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(App.Current.MainWindow.XColorPickerView, ColorPickerView.GProperty, new Binding("Value") { Source = _gFloatEdit, Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(App.Current.MainWindow.XColorPickerView, ColorPickerView.BProperty, new Binding("Value") { Source = _bFloatEdit, Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(App.Current.MainWindow.XColorPickerView, ColorPickerView.AProperty, new Binding("Value") { Source = _aFloatEdit, Mode = BindingMode.TwoWay });
        }


        readonly FloatParameterControl _rFloatEdit;
        readonly FloatParameterControl _gFloatEdit;
        readonly FloatParameterControl _bFloatEdit;
        readonly FloatParameterControl _aFloatEdit;

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try {
                App.Current.MainWindow.XColorPickerView.LinkedColorPickingComponent = null;
            }
            catch {
            }
        }


        /**
         * FloatParameterControl requested to move focus.
         */
        void TabMoveEventHandler(FloatParameterControl sender, bool backwards)
        {
            int index = _parameterControls.IndexOf(sender);
            if (index != -1)
            {
                index += backwards ? -1 : 1;
                index += _parameterControls.Count;
                index %= _parameterControls.Count();
                _parameterControls[index].SwitchToTextEdit();
            }
        }
        private readonly List<FloatParameterControl> _parameterControls = new List<FloatParameterControl>();

    }


    #region Value converter
    [ValueConversion(typeof(double), typeof(double))]
    public class RgbToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Count() != 3 || values.Contains(DependencyProperty.UnsetValue)) {
                return "binding error";
            }

            var r = (byte) (Math.Min(1.0f, Math.Max(0.0f, (float)values[0])) * 255);
            var g = (byte) (Math.Min(1.0f, Math.Max(0.0f, (float) values[1])) * 255);
            var b = (byte) (Math.Min(1.0f, Math.Max(0.0f, (float) values[2])) * 255);
            var br = new SolidColorBrush( Color.FromArgb(255,r, g, b));
            br.Freeze();
            return br;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion



}
