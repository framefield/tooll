// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;




namespace Framefield.Core
{
    public class Connection
    {
        public Operator SourceOp { get; set; }
        public OperatorPart SourceOpPart { get; set; }
        public Operator TargetOp { get; set; }
        public OperatorPart TargetOpPart { get; set; }
        public int Index { get; set; }
        public Guid ID { get; internal set; }

        internal Connection(Guid id, Operator sourceOp, OperatorPart sourceOpPart, Operator targetOp, OperatorPart targetOpPart, int connectionIdx) 
        {
            ID = id;
            SourceOp = sourceOp;
            SourceOpPart = sourceOpPart;
            TargetOp = targetOp;
            TargetOpPart = targetOpPart;
            Index = connectionIdx;
        }

        public Connection(Operator sourceOp, OperatorPart sourceOpPart, Operator targetOp, OperatorPart targetOpPart, int connectionIdx) 
            : this(Guid.NewGuid(), sourceOp, sourceOpPart, targetOp, targetOpPart, connectionIdx) 
        {
        }
    }

    public class OperatorChangedEventArgs : System.EventArgs
    {
        public Operator Operator { get; private set; }

        public OperatorChangedEventArgs(Operator op)
        {
            Operator = op;
        }
    }

    public delegate void OperatorChangedDelegate(object obj, OperatorChangedEventArgs args);

    public class OperatorPartChangedEventArgs : System.EventArgs
    {
        public OperatorPart OperatorPart { get; private set; }

        public OperatorPartChangedEventArgs(OperatorPart opPart)
        {
            OperatorPart = opPart;
        }
    }

    public delegate void OperatorPartChangedDelegate(object obj, OperatorPartChangedEventArgs args);

    public class ConnectionChangedEventArgs : EventArgs
    {
        public Connection Connection { get; private set; }

        public ConnectionChangedEventArgs(Connection connection)
        {
            Connection = connection;
        }
    }

    public class PositionChangedEventArgs : EventArgs
    {
        public Point Position { get; private set; }

        public PositionChangedEventArgs(Point position)
        {
            Position = position;
        }
    }

    public class WidthChangedEventArgs : EventArgs
    {
        public double Width { get; private set; }

        public WidthChangedEventArgs(double width)
        {
            Width = width;
        }
    }

    public class VisibleChangedEventArgs : EventArgs
    {
        public bool Visible { get; private set; }

        public VisibleChangedEventArgs(bool visible)
        {
            Visible = visible;
        }
    }

    public delegate void VisibleChangedDelegate(object o, VisibleChangedEventArgs e);

    public class OperatorPartStateChangedEventArgs : EventArgs
    {
        public IOperatorPartState OperatorPartState { get; private set; }

        public OperatorPartStateChangedEventArgs(IOperatorPartState state)
        {
            OperatorPartState = state;
        }
    }

    public delegate void OperatorPartStateChangedDelegate(object o, OperatorPartStateChangedEventArgs e);

    public class Operator : INotifyPropertyChanged, IDisposable
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<OperatorChangedEventArgs> OperatorAddedEvent;
        public event EventHandler<OperatorChangedEventArgs> OperatorRemovedEvent;
        public event EventHandler<OperatorPartChangedEventArgs> InputAddedEvent;
        public event EventHandler<OperatorPartChangedEventArgs> InputRemovedEvent;
        public event EventHandler<OperatorPartChangedEventArgs> OutputAddedEvent;
        public event EventHandler<OperatorPartChangedEventArgs> OutputRemovedEvent;
        public event EventHandler<ConnectionChangedEventArgs> ConnectionAddedEvent;
        public event EventHandler<ConnectionChangedEventArgs> ConnectionRemovedEvent;
        public event EventHandler<PositionChangedEventArgs> PositionChangedEvent;
        public event EventHandler<WidthChangedEventArgs> WidthChangedEvent;
        public event EventHandler<VisibleChangedEventArgs> VisibleChangedEvent;
        public event EventHandler<EventArgs> DisabledEvent;
        public event EventHandler<OperatorPartStateChangedEventArgs> OperatorPartStateChangedEvent;
        public event EventHandler<EventArgs> ModifiedEvent;

        #endregion

        #region Properties

        public Guid ID { get; private set; }
        public MetaOperator Definition { get; private set; }
        public Operator Parent { get; set; }

        public FunctionType FunctionType
        {
            get
            {
                var functionType = FunctionType.Generic;
                if (Outputs.Count > 0)
                    functionType = Outputs[0].Type;
                return functionType;
            }
        }

        public List<OperatorPart> Inputs { get; private set; }
        public List<OperatorPart> Outputs { get; private set; }
        public List<Operator> InternalOps { get; private set; }
        public List<OperatorPart> InternalParts { get; private set; }

        public string Name
        {
            get { return (Parent != null) ? Parent.Definition.GetName(ID) : String.Empty; }
            set
            {
                if (Parent != null)
                {
                    Parent.Definition.SetName(ID, value);
                    EventExt.Raise(PropertyChanged, this, new PropertyChangedEventArgs("Name"));
                }
            }
        }

        public Point Position { get { return (Parent != null) ? Parent.Definition.GetPosition(ID) : new Point(); } internal set { if (Parent != null) Parent.Definition.SetPosition(ID, value); } }
        public double Width { get { return (Parent != null) ? Parent.Definition.GetWidth(ID) : 0.0; } internal set { if (Parent != null) Parent.Definition.SetWidth(ID, value); } }
        public bool Visible { get { return (Parent == null) || Parent.Definition.GetVisible(ID); } internal set { if (Parent != null) Parent.Definition.SetVisible(ID, value); } }
        public bool Disabled { get { return (Parent != null) && Parent.Definition.GetDisabled(ID); } internal set { if (Parent != null) Parent.Definition.SetDisabled(ID, value); } }

        public IOperatorPartState GetOperatorPartState(Guid opPartId)
        {
            if (Parent != null)
                return Parent.Definition.GetOperatorPartState(ID, opPartId);

            return null;
        }

        public void SetOperatorPartState(Guid opPartId, IOperatorPartState value)
        {
            if (Parent != null)
                Parent.Definition.SetOperatorPartState(ID, opPartId, value);
        }

        #endregion

        #region ctors

        public Operator(Guid id, MetaOperator metaOp, List<OperatorPart> inputs, List<OperatorPart> outputs,
                        List<Operator> internalOps, List<OperatorPart> internalParts)
        {
            ID = id;
            Definition = metaOp;
            Inputs = inputs;
            Outputs = outputs;
            InternalOps = internalOps;
            InternalParts = internalParts;
            Name = "";
            Position = new Point(100, 100);
            Width = 75; // This should be Grid.Size * 3

            UpdateOutputIndices();

            // Forward Modified-events of parameters as Manipulated-Event
            foreach (var opPart in Inputs.Union(Outputs).Union(InternalParts))
            {
                opPart.Parent = this;
                opPart.ManipulatedEvent += (o, a) => EventExt.Raise(ModifiedEvent, this, EventArgs.Empty);
            }

            foreach (var op in InternalOps) op.Parent = this;
            ConnectTypeChangedHanderWith(Inputs);
        }

        #endregion

        public void Dispose()
        {
            Inputs.ForEach(i => i.Dispose());
            Outputs.ForEach(o => o.Dispose());
            InternalOps.ForEach(o => o.Dispose());
            InternalParts.ForEach(p => p.Dispose());
            Definition.RemoveInstance(this);
        }

        #region input/output handling

        public void RemoveInput(OperatorPart opPart)
        {
            Definition.RemoveInput(opPart.ID);
        }

        public void RemoveOutput(OperatorPart opPart)
        {
            Definition.RemoveOutput(opPart.ID);
        }

        internal void AddInput(OperatorPart input)
        {
            InsertInput(Inputs.Count, input);
        }

        internal void InsertInput(int index, OperatorPart input)
        {
            if ((index < 0) || (index > Inputs.Count))
                throw new IndexOutOfRangeException();

            Inputs.Insert(index, input);
            input.Parent = this;

            EventExt.Raise(InputAddedEvent, this, new OperatorPartChangedEventArgs(input));
        }

        internal void RemoveInputInternal(OperatorPart input)
        {
            var inputOp = input.Parent;
            var parentOp = inputOp.Parent;

            // remove possible connection to this input in parent op
            if (parentOp != null)
            {
                while (input.Connections.Count > 0)
                {
                    var sourceOpPart = input.Connections[0];
                    var sourceOp = sourceOpPart.Parent;
                    var connection = new Connection(sourceOp, sourceOpPart, this, input, 0);
                    parentOp.RemoveConnection(connection);
                }
            }

            Inputs.Remove(input);
            EventExt.Raise(InputRemovedEvent, this, new OperatorPartChangedEventArgs(input));
        }

        internal void AddOutput(OperatorPart output)
        {
            InsertOutput(Outputs.Count, output);
        }

        internal void InsertOutput(int index, OperatorPart output)
        {
            if ((index < 0) || (index > Outputs.Count))
                throw new IndexOutOfRangeException();

            Outputs.Insert(index, output);
            output.Parent = this;
            UpdateOutputIndices();

            EventExt.Raise(OutputAddedEvent, this, new OperatorPartChangedEventArgs(output));
        }

        private void UpdateOutputIndices()
        {
            for (int idx = 0; idx < Outputs.Count; ++idx)
            {
                Outputs[idx].Func.EvaluationIndex = idx;
            }
        }

        internal void RemoveOutputInternal(OperatorPart output)
        {
            var outputOp = this;
            var parentOp = outputOp.Parent;

            // remove possible connection from this output in parent op
            if (parentOp != null)
            {
                var connectionsToRemove = (from con in parentOp.Connections
                                           where con.SourceOp == outputOp
                                           where con.SourceOpPart == output
                                           select con).ToList();
                foreach (var connection in connectionsToRemove)
                {
                    parentOp.RemoveConnection(connection);
                }
            }

            Outputs.Remove(output);
            UpdateOutputIndices();
            EventExt.Raise(OutputRemovedEvent, this, new OperatorPartChangedEventArgs(output));
        }

        #endregion

        #region Operator handling

        internal void AddOperator(Operator op)
        {
            InternalOps.Add(op);
            op.Parent = this;
            EventExt.Raise(OperatorAddedEvent, this, new OperatorChangedEventArgs(op));
        }


        internal void RemoveOperator(Operator op)
        {
            Definition.RemoveOperator(op.ID);
        }


        internal void RemoveOperatorInternal(Operator op, bool disposeOpResources)
        {
            InternalOps.Remove(op);

            EventExt.Raise(OperatorRemovedEvent, this, new OperatorChangedEventArgs(op));

            if (disposeOpResources)
                op.Dispose(); // clean up all resources locked by op
        }

        internal void MoveOperatorTo(Guid opIdToMove, Guid newParentId)
        {
            var opToMove = InternalOps.Single(op => op.ID == opIdToMove);
            var newParent = InternalOps.Single(op => op.ID == newParentId);
            RemoveOperatorInternal(opToMove, disposeOpResources: false);
            newParent.AddOperator(opToMove);
        }

        #endregion

        #region Connections

        internal void InsertConnectionAt(Connection connection)
        {
            if (connection.SourceOp == this) connection.SourceOp = null;
            if (connection.TargetOp == this) connection.TargetOp = null;
            Definition.InsertConnectionAt(new MetaConnection(connection), connection.Index);
        }

        internal Tuple<Operator, OperatorPart> FindOpPart(Guid opId, Guid opPartId)
        {
            var op = (opId == Guid.Empty) ? this : (from o in InternalOps where o.ID == opId select o).Single();
            var allOpParts = op.InternalParts.Union(op.Inputs).Union(op.Outputs);
            var opPart = (from part in allOpParts where part.ID == opPartId select part).Single();

            return Tuple.Create(op, opPart);
        }

        internal void InsertConnectionAtInternal(Connection connection)
        {
            connection.TargetOpPart.InsertConnectionAt(connection.SourceOpPart, connection.Index);
            _connectionsChanged = true;
            TriggerConnectionAdded(new ConnectionChangedEventArgs(connection));
        }

        internal void ReplaceConnectionAt(Connection connection)
        {
            // first extract prev connection that should be replaced
            var prevSourceOpPart = connection.TargetOpPart.Connections[connection.Index];
            var opResult = InternalOps.Find(op => op.Outputs.Exists(opPart => opPart == prevSourceOpPart));
            var prevSourceOp = opResult ?? this;

            RemoveConnection(new Connection(prevSourceOp, prevSourceOpPart, connection.TargetOp, connection.TargetOpPart, connection.Index));
            InsertConnectionAt(connection);
        }

        internal void RemoveConnection(Connection connection)
        {
            if (connection.SourceOp == this) connection.SourceOp = null;
            if (connection.TargetOp == this) connection.TargetOp = null;
            Definition.RemoveConnection(new MetaConnection(connection), connection.Index);
        }

        internal void RemoveConnectionInternal(Connection connection)
        {
            connection.TargetOpPart.RemoveConnectionAt(connection.Index);
            _connectionsChanged = true;
            TriggerConnectionRemoved(new ConnectionChangedEventArgs(connection));
        }

        private bool _connectionsChanged = true;
        private readonly List<Connection> _connections = new List<Connection>();
        public IEnumerable<Connection> Connections
        {
            get
            {
                if (!_connectionsChanged) 
                    return _connections;

                var groupedConnections = from metaCon in Definition.Connections
                                         group metaCon by (metaCon.TargetOpID.ToString() + metaCon.TargetOpPartID.ToString())
                                         into groupedCons
                                         select groupedCons;
                _connections.Clear();
                foreach (var conGroup in groupedConnections)
                {
                    int index = 0;
                    foreach (var con in conGroup)
                    {
                        var sourceOp = InternalOps.SingleOrDefault(op => op.ID == con.SourceOpID);
                        var sourceOpPart = (sourceOp != null)
                                               ? sourceOp.Outputs.Single(part => part.ID == con.SourceOpPartID)
                                               : Inputs.Single(part => part.ID == con.SourceOpPartID);
                        var targetOp = InternalOps.SingleOrDefault(op => op.ID == con.TargetOpID);
                        var targetOpPart = (targetOp != null)
                                               ? targetOp.Inputs.Single(part => part.ID == con.TargetOpPartID)
                                               : Outputs.Single(part => part.ID == con.TargetOpPartID);
                        _connections.Add(new Connection(con.ID, sourceOp, sourceOpPart, targetOp, targetOpPart, index));
                        index += 1;
                    }
                }
                _connectionsChanged = false;
                return _connections;
            }
        }

        #endregion

        #region private stuff

        private void TriggerConnectionAdded(ConnectionChangedEventArgs args)
        {
            EventExt.Raise(ConnectionAddedEvent, this, args);
        }

        private void TriggerConnectionRemoved(ConnectionChangedEventArgs args)
        {
            EventExt.Raise(ConnectionRemovedEvent, this, args);
        }

        internal void TriggerPositionChanged(PositionChangedEventArgs args)
        {
            EventExt.Raise(PositionChangedEvent, this, args);
        }

        internal void TriggerWidthChanged(WidthChangedEventArgs args)
        {
            EventExt.Raise(WidthChangedEvent, this, args);
        }

        internal void TriggerVisibleChanged(VisibleChangedEventArgs args)
        {
            EventExt.Raise(VisibleChangedEvent, this, args);
        }

        protected void TriggerDisabledEvent()
        {
            EventExt.Raise(DisabledEvent, this, EventArgs.Empty);
        }

        internal void TriggerOperatorPartStateChanged(OperatorPartStateChangedEventArgs args)
        {
            EventExt.Raise(OperatorPartStateChangedEvent, this, args);
        }

        internal void DisableOutputs(bool disabled)
        {
            foreach (var output in Outputs)
            {
                output.Disabled = disabled;
            }
            TriggerDisabledEvent();
        }

        private void ConnectTypeChangedHanderWith(IEnumerable<OperatorPart> opParts)
        {
            foreach (var opPart in opParts)
            {
                opPart.TypeChangedEvent += InputTypeChangedHandler;
            }
        }

        private void InputTypeChangedHandler(object sender, OperatorPart.TypeChangedEventArgs args)
        {
            var targetPart = sender as OperatorPart;
            if (Parent != null && targetPart.Type != args.Type)
            {
                int index = 0; //targetPart.Connections.IndexOf(targetPart);
                var sourcePart = targetPart.Connections[index];
                var sourceOp = sourcePart.Parent;
                Parent.RemoveConnection(new Connection(sourceOp, sourcePart, targetPart.Parent, targetPart, index));
            }
        }

        #endregion

        public override string ToString()
        {
            return Name != String.Empty ? (Definition.Name + " '" + Name + "'") : Definition.Name;
        }

        public MetaInput GetMetaInput(OperatorPart input)
        {
            int index = Inputs.IndexOf(input);
            return (index != -1) ? Definition.Inputs[index] : null;
        }

        public MetaOutput GetMetaOutput(OperatorPart input)
        {
            int index = Outputs.IndexOf(input);
            return (index != -1) ? Definition.Outputs[index] : null;
        }

        public MetaOperatorPart GetMetaOperatorPart(OperatorPart opPart)
        {
            int index = InternalParts.IndexOf(opPart);
            return (index != -1) ? Definition.OperatorParts[index].Item2 : null;
        }
    }
}
