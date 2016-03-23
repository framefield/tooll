// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Framefield.Core;
using Framefield.Helper;
using Framefield.Tooll.Components.CompositionView;

namespace Framefield.Tooll
{

    /// <summary>
    /// The ui representation of an Operator in CompositionGraphView
    /// </summary>
    public partial class OutputWidget : UserControl, IConnectionLineTarget
    {
        #region events
        public event EventHandler<RoutedEventArgs> SelectedEvent
        {
            add { _MoveHandler.SelectedEvent += value; }
            remove { _MoveHandler.SelectedEvent -= value; }
        }
        #endregion

        #region properties
        public List<OperatorPart> Inputs { get { return _Inputs; } }

        public CompositionView CV
        {
            get
            {
                if (_CV == null)
                    _CV = UIHelper.FindParent<CompositionView>(this);
                return _CV;
            }
        }

        public Point Position { get { return _MoveHandler.Position; } set { _MoveHandler.Position = value; } }
        public Point PositionOnCanvas
        {
            get
            {
                Panel panel = UIHelper.FindParent<Panel>(this);
                Point p = new Point();
                if (panel != null)
                    p = panel.TranslatePoint(Position, CV.CompositionGraphView.XOperatorCanvas);
                return p;
            }
            set
            {
                Panel panel = UIHelper.FindParent<Panel>(this);
                if (panel != null)
                    Position = CV.CompositionGraphView.XOperatorCanvas.TranslatePoint(value, panel);
            }
        }
        public List<ConnectionLine> ConnectionsIn { get { return _ConnectionsIn; } }

        public bool IsSelected { get { return (bool)GetValue(m_IsSelectedProperty); } set { SetValue(m_IsSelectedProperty, value); } }

        public OperatorPart OperatorPart
        {
            get
            {
                return _Inputs[0];
            }
            set
            {
                if (_Inputs.Count > 0)
                    _Inputs[0].ManipulatedEvent -= Input_ManipulatedHandler;

                _Inputs.Clear();
                _Inputs.Add(value);
                NameLabel.Text = value.Name;

                _Inputs[0].ManipulatedEvent += Input_ManipulatedHandler;
            }
        }


        public List<OperatorPart> VisibleInputs
        {
            get
            {
                return _Inputs;
            }
        }

        #endregion



        public OutputWidget(OperatorPart opPart)
        {
            InitializeComponent();

            OperatorPart = opPart;

            _MoveHandler = new MoveHandler(this);

            Height = CompositionGraphView.GRID_SIZE;
            Width = CompositionGraphView.GRID_SIZE * 3;

            UpdateColor();

            Loaded += OutputWidget_Loaded;
            Unloaded += OutputWidget_Unloaded;
        }

        private void UpdateColor()
        {
            operatorContent.Background = new SolidColorBrush(UIHelper.ColorFromType(OperatorPart.Type));
            operatorContent.Background.Freeze();
            NameLabel.Foreground = new SolidColorBrush(UIHelper.BrightColorFromType(OperatorPart.Type));
            NameLabel.Foreground.Freeze();
        }

        void OutputWidget_Loaded(object sender, RoutedEventArgs e)
        {
            OperatorPart.ManipulatedEvent += opPart_ManipulatedEvent;
        }

        void OutputWidget_Unloaded(object sender, RoutedEventArgs e)
        {
            OperatorPart.ManipulatedEvent -= opPart_ManipulatedEvent;
        }


        void opPart_ManipulatedEvent(object sender, EventArgs e)
        {
            UpdateColor();
        }

        public void UpdateConnections()
        {
            ConnectionsIn.ForEach(cl => cl.Update());
        }

        public double GetVerticalOverlapWith(IConnectableWidget op)
        {
            return Math.Min(Position.Y + Height, op.Position.Y + op.Height) - Math.Max(Position.Y, op.Position.Y);
        }

        public double GetHorizontalOverlapWith(IConnectableWidget op)
        {
            return Math.Min(Position.X + Width, op.Position.X + op.Width) - Math.Max(Position.X, op.Position.X);
        }

        public Rect GetRangeForInputConnectionLine(OperatorPart input, int multiInputIndex, bool insertConnection = false)
        {
            int inputIndex = 0;
            double inputWidth = Width;

            double bottom = Height;
            double minX = inputIndex * inputWidth;
            double maxX = minX + inputWidth;

            if (input.IsMultiInput)
            {
                double slotWidth = inputWidth / (input.Connections.Count() + 0.5);

                double slotLeft = minX + multiInputIndex * slotWidth;
                double slotRight = slotLeft + slotWidth;

                // This needs not be validated
                if (insertConnection)
                {
                    slotLeft += 0.5 * slotWidth;
                }
                else
                {
                    slotRight = slotLeft = slotLeft + 0.25 * slotWidth;
                }
                return new Rect(slotLeft, Height, slotRight - slotLeft, 0);
            }
            else
            {
                return new Rect(minX, Height, maxX - minX, 0);
            }
        }

        public void ClearHighlightInput()
        {
            OperatorHighlightInput.Children.Clear();
        }


        public List<IConnectableWidget> GetWidgetsConnectedToInputs(bool onlySnapped = false)
        {
            return (from cl in ConnectionsIn
                    where !onlySnapped || cl.IsSnapped
                    select cl.Source as IConnectableWidget).ToList();
        }

        #region protected/private stuff

        #region moving event handlers
        private void DragStartHandler(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _MoveHandler.Start();
        }

        private void DragDeltaHandler(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var delta = new Vector(e.HorizontalChange, 0);

            _MoveHandler.Update(delta);

            UpdateConnections();
        }

        private void DragCompletedHandler(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _MoveHandler.Stop(new Vector(e.HorizontalChange, e.VerticalChange));
        }


        private void XOperatorWidgetThumb_Drop(object sender, DragEventArgs e)
        {
            var mousePosition = e.GetPosition(this);
            var zone = new OperatorWidgetInputZone()
            {
                Width = Width,
                Input = OperatorPart

            };
            CV.CompositionGraphView.ConnectionDragHelper.Stop(new Vector(0, 0), zone);
            e.Handled = true;
        }

        private void XOperatorWidgetThumb_DragOver(object sender, DragEventArgs e)
        {
            CV.CompositionGraphView.ConnectionDragHelper.HandleDragOverEvent(e, this);
        }

        #endregion

        #region other event handlers
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateConnections();
        }
        #endregion


        #region helper stuff
        private double GetVerticalOverlapWith(OutputWidget op)
        {
            return Math.Min(Position.Y + Height, op.Position.Y + op.Height) - Math.Max(Position.Y, op.Position.Y);
        }

        private double GetHorizontalOverlapWith(OutputWidget op)
        {
            return Math.Min(Position.X + Width, op.Position.X + op.Width) - Math.Max(Position.X, op.Position.X);
        }

        private void Input_ManipulatedHandler(object sender, System.EventArgs e)
        {
            UpdateBindingsToOutput();
        }

        void UpdateBindingsToOutput()
        {
            var binding = new Binding("OutputName")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = OperatorPart,
                Path = new PropertyPath("Name"),
            };
            NameLabel.SetBinding(TextBlock.TextProperty, binding);

        }

        #endregion

        public void UpdateInputZonesUIFromDescription(IEnumerable<OperatorWidgetInputZone> inputZones)
        {            
            //not implemented;
        }


        private static readonly DependencyProperty m_IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool),
                                                                                                      typeof(OutputWidget), new UIPropertyMetadata(false));

        private CompositionView _CV;
        private MoveHandler _MoveHandler;
        //private OperatorSnappingHelper m_SnapHandler;
        private List<OperatorPart> _Inputs = new List<OperatorPart>();
        private List<ConnectionLine> _ConnectionsIn = new List<ConnectionLine>();
        #endregion

    }
}
