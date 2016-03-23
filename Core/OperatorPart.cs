// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Framefield.Core.OperatorPartTraits;

namespace Framefield.Core
{

    public class OperatorPart : IDisposable
    {
        public static bool EnableEventPropagationByDefault = true;
        public static bool HasValidInvalidationMarksOnOperatorPartsForTraversing = false;

        public abstract class Function : IDisposable
        {
            private bool _changed;
            public event EventHandler<EventArgs> ChangedEvent;

            public virtual OperatorPart OperatorPart { get; set; }
            public bool Changed
            {
                get { return _changed; }
                set
                {
                    _changed = value;
                    if (!value)
                    {
                        ChangedState = ChangedState.Nothing;
                    }
                }
            }

            public ChangedState ChangedState { get; set; }

            protected Function()
            {
                Changed = true;
                ChangedState = ChangedState.Value | ChangedState.Subtree;
            }

            public virtual void Dispose()
            {
                Utilities.RemoveAllEventHandlerFrom(this);
            }

            public virtual Function Clone()
            {
                throw new Exception("should be handled in specific function");
            }

            public int EvaluationIndex { get; set; }

            public abstract OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx);

            protected void TriggerChangedEvent(EventArgs e)
            {
                if (ChangedEvent != null)
                    ChangedEvent(this, e);
            }

            protected static void EvalMultiInput(OperatorPart multiInput, OperatorPartContext context)
            {
                foreach (var input in multiInput.Connections)
                {
                    input.Eval(context);
                }
            }
        }

        #region events

        [Flags]
        public enum ChangedState
        {
            Nothing = 0,
            Value   = 1,
            Subtree = 2
        };

        public class ChangedEventArgs : EventArgs
        {
            public float Time { get; private set; }
            public ChangedState ChangedState { get; private set; }
            public ChangedEventArgs(float time) : this(time, ChangedState.Value)
            {
            }
            public ChangedEventArgs(float time, ChangedState changedState)
            {
                Time = time;
                ChangedState = changedState;
            }
        }
        public delegate void ChangedDelegate(object obj, ChangedEventArgs args);

        public class TypeChangedEventArgs : EventArgs
        {
            public FunctionType Type { get; set; }

            public TypeChangedEventArgs(FunctionType type) {
                Type = type;
            }
        }
        public delegate void TypeChangedDelegate(object obj, TypeChangedEventArgs args);

        public event ChangedDelegate ChangedEvent = (o, a) => {};
        public event TypeChangedDelegate TypeChangedEvent = (o, a) => {};
        public event EventHandler<EventArgs> ManipulatedEvent = (o, a) => {};
        #endregion

        #region properties
        public Guid ID { get; private set; }

        public Function Func {
            get { return _function; }
            set {
                if (_function != null) {
                    _function.ChangedEvent -= FunctionChangedHandler;
                    _function.Dispose();
                }

                _function = value;

                if (_function != null) {
                    _function.Changed = true;
                    _function.ChangedEvent += FunctionChangedHandler;
                    _function.OperatorPart = this;
                }

                ChangedEvent(this, new ChangedEventArgs(_changedTime));
                ManipulatedEvent(this, EventArgs.Empty);
            }
        }

        internal Function DefaultFunc
        {
            get { return _defaultFunc; }
            set
            {
                if (_defaultFunc != null)
                    _defaultFunc.ChangedEvent -= DefaultFunctionChangedHandler;

                _defaultFunc = value;
                _defaultFunc.ChangedEvent += DefaultFunctionChangedHandler;
            }
        }

        private Function _defaultFunc;

        public List<OperatorPart> Connections { get { return _connections; } }

        internal bool Disabled
        {
            get { return _disabled; }
            set
            {
                _disabled = value;
                EmitChangedEvent();
            }
        }

        public FunctionType Type {
            get {
                if (_type == FunctionType.Generic && Connections.Count() > 0)
                    return Connections[0].Type;
                else
                    return _type;
            }
            set { 
                _type = value;
                ManipulatedEvent(this, EventArgs.Empty);
            }
        }

        public float ChangedTime { get { return _changedTime; } }
        public bool IsMultiInput {
            get {
                return _isMultiInput;
            }
            set {
                _isMultiInput = value;
                ManipulatedEvent(this, EventArgs.Empty);
            }
        }
        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
                ManipulatedEvent(this, EventArgs.Empty);
            }
        }
        public Operator Parent {
            get {
                return _parent;
            }
            set {
                _parent = value;
                ManipulatedEvent(this, EventArgs.Empty);
            }
        }
        public delegate void EvaluateCallbackDelegate();
        public static EvaluateCallbackDelegate EvaluateCallback { get; set; }
        #endregion

        public void Dispose() 
        {
            if (_function != null)
                _function.ChangedEvent -= FunctionChangedHandler;
            if (_defaultFunc != null)
                _defaultFunc.ChangedEvent -= DefaultFunctionChangedHandler;
            Utilities.DisposeObj(ref _function);
            Utilities.RemoveAllEventHandlerFrom(this);
        }

        public override string ToString() {
            return Parent + "." + Name;
        }

        public OperatorPart(Guid id, Function defaultFunc) {
            ID = id;
            Func = defaultFunc;
            IsMultiInput = false;
            ChangedPropagationEnabled = EnableEventPropagationByDefault;
            DefaultFunc = defaultFunc;
        }

        public OperatorPartContext Eval(OperatorPartContext context, int outputIdx)
        {
            if (Disabled)
                return context;

            if (EvaluateCallback != null)
                EvaluateCallback();

            return _function.Eval(context, _connections, outputIdx);
        }

        public OperatorPartContext Eval(OperatorPartContext context)
        {
            return Eval(context, Func.EvaluationIndex);
        }

        #region connections
        public void InsertConnectionAt(OperatorPart inputToConnect, int index) {
            if ((_connections.Count > 0) && !IsMultiInput)
                throw new Exception("multiple connections not allowed");

            if ((index < 0) || (index > _connections.Count))
                throw new Exception("index out of range");

            _connections.Insert(index, inputToConnect);

            if (IsDefaultFuncSet)
                ReplaceDefaultWithValueFunc();

            inputToConnect.ChangedEvent += ChangedHandler;
            inputToConnect.TypeChangedEvent += TypeChangedHandler;

            if ((_type == FunctionType.Generic) && (inputToConnect.Type != FunctionType.Generic))
                EmitTypeChangedEvent(Type);

            _function.Changed = true;
            ChangedEvent(this, new ChangedEventArgs(_changedTime, ChangedState.Subtree));
            ManipulatedEvent(this, EventArgs.Empty);
        }

        public void ReplaceConnectionAt(OperatorPart connection, int index) {
            RemoveConnectionAt(index);
            InsertConnectionAt(connection, index);
        }

        public void AppendConnection(OperatorPart connection) {
            InsertConnectionAt(connection, _connections.Count);
        }

        public void RemoveConnectionAt(int index) 
        {
            var prevConnection = _connections[index];
            prevConnection.ChangedEvent -= ChangedHandler;
            prevConnection.TypeChangedEvent -= TypeChangedHandler;
            _connections.RemoveAt(index);
            var prevValueFunction = prevConnection.Func as Utilities.ValueFunction;
            if (prevValueFunction != null)
            {
                // if it was a value function we set that previous input value as the new one
                SetValue(prevValueFunction.Value);
            }
            ChangedEvent(this, new ChangedEventArgs(_changedTime, ChangedState.Subtree));
            ManipulatedEvent(this, EventArgs.Empty);
        }

        public void ClearConnections() {
            _connections.ForEach(c => { 
                c.ChangedEvent -= ChangedHandler;
                c.TypeChangedEvent -= TypeChangedHandler;
            });
            _connections.Clear();
            ManipulatedEvent(this, EventArgs.Empty);
        }
        #endregion

        #region event handling
        public bool ChangedPropagationEnabled { get; set; }

        private void ChangedHandler(object sender, ChangedEventArgs args)
        {
            Func.Changed = true;
            Func.ChangedState |= args.ChangedState;
            if (ChangedPropagationEnabled)
                ChangedEvent(this, args);
        }

        private void TypeChangedHandler(object sender, TypeChangedEventArgs args) {
            if (Type != args.Type) {
                int index = Connections.IndexOf(sender as OperatorPart);
                EmitTypeChangedEvent(args.Type);
            }
            else if (_type == FunctionType.Generic)
                EmitTypeChangedEvent(args.Type); // in case of generic we need to propagate this event further
        }

        private void EmitTypeChangedEvent(FunctionType type) {
            TypeChangedEvent(this, new TypeChangedEventArgs(type));
        }

        private void FunctionChangedHandler(object sender, EventArgs args) {
            ChangedEvent(this, new ChangedEventArgs(_changedTime));
        }

        private void DefaultFunctionChangedHandler(object sender, EventArgs args)
        {
            if (Parent != null)
                Parent.Definition.Changed = true; // set defintion dirty
        }
        #endregion

        public void EmitChangedEvent() {
            ChangedEvent(this, new ChangedEventArgs(_changedTime));
        }

        public void EmitChangedEventForOutput(int outputId)
        {
            OperatorPart output = (from o in Parent.Outputs
                                   where o.Func.EvaluationIndex == outputId
                                   select o).Single();
            output.Func.Changed = true;
            output.EmitChangedEvent();
        }


        public interface IPreTraverseEvaluator
        {
            void PreEvaluate(OperatorPart opPart);
        }

        public interface IPostTraverseEvaluator
        {
            void ChildrenStart();
            void ChildrenEnd();
            void PostEvaluate(OperatorPart opPart);
            void AlreadyVisited(OperatorPart opPart);
        }

        public abstract class Invalidator
        {
            protected Invalidator()
            {
                Clear();
            }

            public void ChildrenStart()
            {
                _changedStates.Push(false);
            }

            public void ChildrenEnd()
            {
                UpdateStackTop(_changedStates.Pop());
            }

            private void UpdateStackTop(bool changed)
            {
                if (changed && !_changedStates.Peek())
                {
                    _changedStates.Pop();
                    _changedStates.Push(changed);
                }
            }

            public void AlreadyVisited(OperatorPart opPart)
            {
                if (opPart.Func != null)
                    UpdateStackTop(opPart.Func.Changed);
            }

            public void Clear()
            {
                _changedStates.Clear();
                _changedStates.Push(false);
            }

            protected Stack<bool> _changedStates = new Stack<bool>();
        }

        public class InvalidateTimeAccessors : Invalidator, IPostTraverseEvaluator
        {
            public void PostEvaluate(OperatorPart opPart)
            {
                if (_changedStates.Peek())
                {
                    if (opPart.Func != null)
                        opPart.Func.Changed = true;
                    return;
                }

                if (opPart.Func is ITimeAccessor)
                {
                    opPart.Func.Changed = true;
                    _changedStates.Pop();
                    _changedStates.Push(true);
                }
            }
        }

        public class InvalidateInvalidatables : Invalidator, IPostTraverseEvaluator
        {
            public void PostEvaluate(OperatorPart opPart)
            {
                if (_changedStates.Peek())
                {
                    if (opPart.Func != null)
                        opPart.Func.Changed = true;
                    return;
                }

                var isTimeAccessor = opPart.Func is ITimeAccessor;
                var asyncDependend = opPart.Func as IAsyncDependend;
                var isVariableAccessor = opPart.Func is IVariableAccessor;
                if (isTimeAccessor || (asyncDependend != null && asyncDependend.AsyncChanged) || isVariableAccessor)
                {
                    opPart.Func.Changed = true;
                    _changedStates.Pop();
                    _changedStates.Push(true);
                }
            }
        }

        public class InvalidateVariableAccessors : Invalidator, IPostTraverseEvaluator
        {
            public InvalidateVariableAccessors(string variableName)
            {
                _variableName = variableName;
            }

            public void PostEvaluate(OperatorPart opPart)
            {
                if (_changedStates.Peek())
                {
                    if (opPart.Func != null)
                        opPart.Func.Changed = true;
                    return;
                }

                var variableAccessor = opPart.Func as IVariableAccessor;
                if (variableAccessor != null && variableAccessor.VariableName == _variableName)
                {
                    opPart.Func.Changed = true;
                    _changedStates.Pop();
                    _changedStates.Push(true);
                }
            }

            private string _variableName;
        }

        public class CycleChecker : IPreTraverseEvaluator
        {
            public bool HasCycle { get; private set; }

            // the input is a list of op parts that should not be found traversing a subtree,
            // normally here are the outputs of a specific op given when trying to add a 
            // connection to it
            public CycleChecker(IEnumerable<OperatorPart> opPartThatWouldBuildACycle)
            {
                HasCycle = false;
                _opPartThatWouldBuildACycle = opPartThatWouldBuildACycle;
            }

            public void PreEvaluate(OperatorPart opPart)
            {
                if (_opPartThatWouldBuildACycle.Contains(opPart))
                {
                    HasCycle = true;
                }
            }

            private readonly IEnumerable<OperatorPart> _opPartThatWouldBuildACycle;
        }

        public class InvalidateAllVariableAccessors : Invalidator, IPostTraverseEvaluator
        {
            public void PostEvaluate(OperatorPart opPart)
            {
                if (_changedStates.Peek())
                {
                    if (opPart.Func != null)
                        opPart.Func.Changed = true;
                    return;
                }

                if (opPart.Func is IVariableAccessor)
                {
                    opPart.Func.Changed = true;
                    _changedStates.Pop();
                    _changedStates.Push(true);
                }
            }
        }

        public void MarkInvalidatables()
        {
            var invalidator = new MarkInvalidatablesInSubTree();
            TraverseWithFunction(null, invalidator);
        }

        private class MarkInvalidatablesInSubTree : IPostTraverseEvaluator
        {
            int intending = 0;
            public MarkInvalidatablesInSubTree()
            {
                intending = 0;
                Clear();
            }

            public void ChildrenStart()
            {
                intending += 2;
                _markedStates.Push(false);
            }

            public void ChildrenEnd()
            {
                intending -= 2;
                UpdateStackTop(_markedStates.Pop());
            }

            private void UpdateStackTop(bool marked)
            {
                if (marked && !_markedStates.Peek())
                {
                    _markedStates.Pop();
                    _markedStates.Push(marked);
                }
            }

            public void PostEvaluate(OperatorPart opPart)
            {
                if (_markedStates.Peek())
                {
                    opPart.SubTreeContainsInvalidatable = true;
                    return;
                }

                var isTimeAccessor = opPart.Func is ITimeAccessor;
                var isAsyncDependend = opPart.Func is IAsyncDependend;
                var isVariableAccessor = opPart.Func is IVariableAccessor;
                opPart.SubTreeContainsInvalidatable = isTimeAccessor || isAsyncDependend || isVariableAccessor;

                if (opPart.SubTreeContainsInvalidatable)
                {
                    _markedStates.Pop();
                    _markedStates.Push(true);
                }
            }

            public void AlreadyVisited(OperatorPart opPart)
            {
                UpdateStackTop(opPart.SubTreeContainsInvalidatable);
            }

            public void Clear()
            {
                _markedStates.Clear();
                _markedStates.Push(false);
            }

            private Stack<bool> _markedStates = new Stack<bool>();
        }

        public class CollectOpPartFunctionsOfType<T> : IPreTraverseEvaluator where T : class
        {
            public HashSet<T> CollectedOpPartFunctions { get; private set; }

            public CollectOpPartFunctionsOfType() {
                CollectedOpPartFunctions = new HashSet<T>();
            }

            public void PreEvaluate(OperatorPart opPart) {
                T func = opPart.Func as T;
                if (func != null)
                    CollectedOpPartFunctions.Add(func);
            }

            public void Clear() {
                CollectedOpPartFunctions.Clear();
            }
        }

        public class CollectAllOperators : IPreTraverseEvaluator
        {
            public HashSet<MetaOperator> CollectedOperators { get; private set; }

            public CollectAllOperators()
            {
                CollectedOperators = new HashSet<MetaOperator>();
            }

            public void PreEvaluate(OperatorPart opPart)
            {
                var metaOperator = opPart.Parent.Definition;
                if (!CollectedOperators.Contains(metaOperator))
                {
                    // new op, add 
                    CollectedOperators.Add(metaOperator);
                }
            }

            public void Clear()
            {
                CollectedOperators.Clear();
            }
        }

        public class WriteChangedStates : IPreTraverseEvaluator
        {
            public void PreEvaluate(OperatorPart opPart)
            {
//                Logger.Info("{0} changed: {1}", opPart.Func.ToString(), opPart.Func.Changed);
            }
        }

        public bool SubTreeContainsInvalidatable { get; private set; }

        [Flags]
        enum TraverseOption
        {
            None = 0,
            UseInvalidationMarks = 1,
        }

        public void TraverseWithFunction(IPreTraverseEvaluator preTraverseEvaluator, IPostTraverseEvaluator postTraverseEvaluator)
        {
            _referenceTraverseID++;
            _traverseOptions = preTraverseEvaluator is Invalidator || postTraverseEvaluator is Invalidator ? TraverseOption.UseInvalidationMarks : TraverseOption.None;
            TraverseWithFunctionInternal(preTraverseEvaluator, postTraverseEvaluator);
        }

        private void TraverseWithFunctionInternal(IPreTraverseEvaluator preTraverseEvaluator, IPostTraverseEvaluator postTraverseEvaluator)
        {
            if ((_traverseOptions.HasFlag(TraverseOption.UseInvalidationMarks) && HasValidInvalidationMarksOnOperatorPartsForTraversing && !SubTreeContainsInvalidatable) ||
                _traverseID == _referenceTraverseID)
            {
                if (postTraverseEvaluator != null)
                    postTraverseEvaluator.AlreadyVisited(this);
                return;
            }
            _traverseID = _referenceTraverseID;

            if (preTraverseEvaluator != null)
                preTraverseEvaluator.PreEvaluate(this);

            foreach (var input in Connections)
            {
                if (postTraverseEvaluator != null)
                    postTraverseEvaluator.ChildrenStart();
                if (!input.Disabled)
                    input.TraverseWithFunctionInternal(preTraverseEvaluator, postTraverseEvaluator);
                if (postTraverseEvaluator != null)
                    postTraverseEvaluator.ChildrenEnd();
            }

            if (postTraverseEvaluator != null)
                postTraverseEvaluator.PostEvaluate(this);
        }

        public void TraverseWithFunctionUseSpecificBehavior(IPreTraverseEvaluator preTraverseEvaluator, IPostTraverseEvaluator postTraverseEvaluator)
        {
            _referenceTraverseID++;
            _traverseOptions = preTraverseEvaluator is Invalidator || postTraverseEvaluator is Invalidator ? TraverseOption.UseInvalidationMarks : TraverseOption.None;
            TraverseWithFunctionUseSpecificBehaviorInternal(preTraverseEvaluator, postTraverseEvaluator);
        }

        private void TraverseWithFunctionUseSpecificBehaviorInternal(IPreTraverseEvaluator preTraverseEvaluator, IPostTraverseEvaluator postTraverseEvaluator)
        {
            if ((_traverseOptions.HasFlag(TraverseOption.UseInvalidationMarks) && HasValidInvalidationMarksOnOperatorPartsForTraversing && !SubTreeContainsInvalidatable) ||
                _traverseID == _referenceTraverseID)
            {
                if (postTraverseEvaluator != null)
                    postTraverseEvaluator.AlreadyVisited(this);
                return;
            }
            _traverseID = _referenceTraverseID;

            if (preTraverseEvaluator != null)
                preTraverseEvaluator.PreEvaluate(this);

            IEnumerable<OperatorPart> connections;
            ITraverseModifier traversModifier = Func as ITraverseModifier;
            if (traversModifier != null)
                connections = traversModifier.GetRelevantConnections();
            else
                connections = Connections;

            foreach (var opPart in connections)
            {
                if (postTraverseEvaluator != null)
                    postTraverseEvaluator.ChildrenStart();
                opPart.TraverseWithFunctionUseSpecificBehaviorInternal(preTraverseEvaluator, postTraverseEvaluator);
                if (postTraverseEvaluator != null)
                    postTraverseEvaluator.ChildrenEnd();
            }

            if (postTraverseEvaluator != null)
                postTraverseEvaluator.PostEvaluate(this);
        }

        internal void SetValueToDefault()
        {
            Func = DefaultFunc;
        }

        public void SetValue(IValue value)
        {
            Func = Utilities.CreateValueFunction(value);
        }

        public void ReplaceDefaultWithValueFunc()
        {
            var func = Func as Utilities.DefaultValueFunction;
            if (func != null)
                SetValue(func.Value);
        }

        public bool IsDefaultFuncSet { get { return Func == DefaultFunc; } }

        #region private members
        private Function _function = null;
        private List<OperatorPart> _connections = new List<OperatorPart>();
        private float _changedTime = 0.0f;
        private FunctionType _type;
        private bool _isMultiInput;
        private Operator _parent;
        private string _name;
        private bool _disabled;
        private int _traverseID;
        private static int _referenceTraverseID = 0;
        private static TraverseOption _traverseOptions = TraverseOption.None;

        #endregion
    }

}
