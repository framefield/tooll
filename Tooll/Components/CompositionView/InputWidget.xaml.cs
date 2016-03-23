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

namespace Framefield.Tooll
{

    /// <summary>
    /// The obj representation of an Operator in CompositionGraphView
    /// </summary>
    public partial class InputWidget : UserControl, IConnectionLineSource
    {
        #region events
        public event EventHandler<RoutedEventArgs> Selected {
           add { m_MoveHandler.SelectedEvent += value; }
           remove { m_MoveHandler.SelectedEvent -= value; }
        }
        #endregion

        #region properties
        public List<OperatorPart> Outputs { get { return m_Outputs; } }

        public CompositionView CV {
            get {
                if (m_CV == null)
                    m_CV = UIHelper.FindParent<CompositionView>(this);
                return m_CV;
            }
        }

        public Point Position { get { return m_MoveHandler.Position; } set { m_MoveHandler.Position = value; } }
        public Point PositionOnCanvas { 
            get {
                Panel panel = UIHelper.FindParent<Panel>(this);
                Point p = new Point();
                if (panel != null)
                    p = panel.TranslatePoint(Position, CV.CompositionGraphView.XOperatorCanvas);
                return p;
            }
        }
        public List<Thumb> OutputThumbs { get { return new List<Thumb>() { FirstOutputThumb }; } }

        public List<ConnectionLine> ConnectionsOut { get { return m_ConnectionsOut; } }

        public FunctionType Type {
            get {
                var functionType = FunctionType.Generic;
                if (Outputs.Count > 0)
                    functionType = Outputs[0].Type;
                return functionType;
            }
        }

        public bool IsSelected
        {
            get { return (bool) GetValue(m_IsSelectedProperty); }
            set
            {
                SetValue(m_IsSelectedProperty, value);
                if (IsSelected) {
                    MyOutputNose.Fill = Brushes.White;
                }
                else {
                    MyOutputNose.Fill = new SolidColorBrush(UIHelper.ColorFromType(Type));
                    MyOutputNose.Fill.Freeze();
                }
            }
        }

        public OperatorPart OperatorPart {
            get {
                return m_Outputs[0];
            }
            set {
                if (m_Outputs.Count > 0)
                    m_Outputs[0].ManipulatedEvent -= UpdateBindingsToOutput;

                m_Outputs.Clear();
                m_Outputs.Add(value);
                NameLabel.Text = value.Name;

                m_Outputs[0].ManipulatedEvent += UpdateBindingsToOutput;
            }
        }
        #endregion



        public InputWidget(OperatorPart opPart) {
            InitializeComponent();

            OperatorPart = opPart;

            m_MoveHandler = new MoveHandler(this);

            
            Height = CompositionGraphView.GRID_SIZE;
            Width = CompositionGraphView.GRID_SIZE * 3;

            operatorContent.Background = new SolidColorBrush(UIHelper.ColorFromType(OperatorPart.Type));
            operatorContent.Background.Freeze();
            NameLabel.Foreground = new SolidColorBrush(UIHelper.BrightColorFromType(OperatorPart.Type));
            NameLabel.Foreground.Freeze();
        }

        public void UpdateConnections() {
            ConnectionsOut.ForEach(cl => cl.Update());
        }

        public double GetVerticalOverlapWith(IConnectableWidget op) {
            return Math.Min(Position.Y + Height, op.Position.Y + op.Height) - Math.Max(Position.Y, op.Position.Y);
        }

        public double GetHorizontalOverlapWith(IConnectableWidget op) {
            return Math.Min(Position.X + Width, op.Position.X + op.Width) - Math.Max(Position.X, op.Position.X);
        }

        public List<IConnectableWidget> GetWidgetsConnectedToOutputs(bool onlySnapped = false) {
            return (from cl in ConnectionsOut
                    where !onlySnapped || cl.IsSnapped
                    select cl.Target as IConnectableWidget).ToList();
        }


        #region protected/private stuff

        #region moving event handlers
        private void OnDragStart(object sender, DragStartedEventArgs e) {
            m_MoveHandler.Start();
            //m_SnapHandler.Start(new List<IConnectableWidget>());
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e) {
            var delta = new Vector(e.HorizontalChange, 0);

            //m_SnapHandler.UpdateBeforeMoving(delta);
            //if (m_SnapHandler.SnappedGroupIsMoving)
            //    return;

            m_MoveHandler.Update(delta);
            UpdateConnections();
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e) {
            m_MoveHandler.Stop(new Vector(e.HorizontalChange, e.VerticalChange));
            //m_SnapHandler.Stop();
        }
        #endregion

        #region connection event handlers
        private void OnDragConnectionStart(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            CV.CompositionGraphView.ConnectionDragHelper.DoDragDropNewConnection(this, Outputs[0]);
        }

        /**
         * Alternative hack to allow dragging to parameter-view with SHIFT
         * This needs to be refactored!
         */
        private void NoseThumb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Store the mouse position
            m_StartPoint = e.GetPosition(null);
            m_MousePressed = true;
            m_Dragging = false;
        }

        private void NoseThumb_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = m_StartPoint - mousePos;

            if (!m_Dragging 
                &&  m_MousePressed 
                &&  e.LeftButton == MouseButtonState.Pressed 
                && (Math.Abs(diff.X) + Math.Abs(diff.Y)) > SystemParameters.MinimumHorizontalDragDistance 
                && Keyboard.Modifiers == ModifierKeys.Shift

            ) {
                // Initialize the drag & drop operation
                m_Dragging = true;
                DataObject dragData = new DataObject(ConnectionDragHelper.CONNECTION_LINE_OUTPUT_IDENTIFIER, this.Outputs[0]);
                DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);                
            }
        }

        private void NoseThumb_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_MousePressed = false;
            m_Dragging = false;
        }

        private Point m_StartPoint;
        private bool m_Dragging= false;
        private bool m_MousePressed= false;

        #endregion

        #region other event handlers
        private void OnLoaded(object sender, RoutedEventArgs e) {
            UpdateConnections();
        }
        #endregion


        #region helper stuff
        private double GetVerticalOverlapWith(InputWidget op) {
            return Math.Min(Position.Y + Height, op.Position.Y + op.Height) - Math.Max(Position.Y, op.Position.Y);
        }

        private double GetHorizontalOverlapWith(InputWidget op) {
            return Math.Min(Position.X + Width, op.Position.X + op.Width) - Math.Max(Position.X, op.Position.X);
        }

        private void UpdateBindingsToOutput(object sender, System.EventArgs e) {
            var binding = new Binding("OutputName") {
                              UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                              Source = OperatorPart,
                              Path = new PropertyPath("Name"),
                          };
            NameLabel.SetBinding(TextBlock.TextProperty, binding);
        }
        #endregion

        private static readonly DependencyProperty m_IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), 
                                                                                                      typeof(InputWidget), new UIPropertyMetadata(false));

        private CompositionView m_CV;
        private MoveHandler m_MoveHandler;
        private List<OperatorPart> m_Outputs = new List<OperatorPart>();
        private List<ConnectionLine> m_ConnectionsOut = new List<ConnectionLine>();
        #endregion

        #region XAML events
        private void OnMouseEnterOutputNose(object sender, MouseEventArgs e)
        {
            MyOutputNose.Fill = new SolidColorBrush(UIHelper.BrightColorFromType(Type));
            MyOutputNose.Fill.Freeze();
        }

        private void OnMouseLeaveOutputNose(object sender, MouseEventArgs e)
        {
            if (IsSelected) {
                MyOutputNose.Fill = Brushes.White;
            }
            else {
                MyOutputNose.Fill = new SolidColorBrush(UIHelper.ColorFromType(Type));
                MyOutputNose.Fill.Freeze();
            }
        }
        #endregion
    }

}
