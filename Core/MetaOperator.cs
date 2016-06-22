// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;


namespace Framefield.Core
{
    public class MetaOperator : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        internal class InstanceProperties
        {
            // first item is instance guid of opPart
            public Dictionary<Guid, OperatorPart.Function> InputValues { get; set; }
            public string Name { get; set; }
            public Point Position { get; set; }
            public double Width { get; set; }
            public bool Visible { get; set; }
            public bool Disabled { get; set; }
            public Dictionary<Guid, IOperatorPartState> OperatorPartStates { get; set; }

            public InstanceProperties()
            {
                InputValues = new Dictionary<Guid, OperatorPart.Function>();
                Name = String.Empty;
                Position = new Point(100, 100);
                Width = 100;
                Visible = true;
                OperatorPartStates = new Dictionary<Guid, IOperatorPartState>();
            }

            public InstanceProperties(IEnumerable<MetaInput> inputs) : this()
            {
                foreach (var input in inputs)
                {
                    InputValues[input.ID] = input.DefaultFunc;
                }
            }

            public InstanceProperties Clone()
            {
                var newOpProps = new InstanceProperties()
                                     {
                                         Name = string.Copy(Name),
                                         Position = Position,
                                         Width = Width,
                                         Visible = Visible,
                                         Disabled = Disabled
                                     };
                // clone input values
                foreach (var inputValue in InputValues)
                {
                    newOpProps.InputValues[inputValue.Key] = inputValue.Value.Clone();
                }

                // clone states
                foreach (var state in OperatorPartStates)
                {
                    newOpProps.OperatorPartStates[state.Key] = state.Value.Clone();
                }

                return newOpProps;
            }

            public void ExchangeGuids(Dictionary<Guid, Guid> oldToNewGuid)
            {
                var updatedInputValues = new Dictionary<Guid, OperatorPart.Function>();
                foreach (var inputValue in InputValues)
                {
                    Guid newId = Guid.Empty;
                    if (!oldToNewGuid.TryGetValue(inputValue.Key, out newId))
                        newId = inputValue.Key;

                    updatedInputValues[newId] = inputValue.Value;
                }
                InputValues = updatedInputValues;

                var updatedStates = new Dictionary<Guid, IOperatorPartState>();
                foreach (var state in OperatorPartStates)
                {
                    Guid newInstanceId = Guid.Empty;
                    if (!oldToNewGuid.TryGetValue(state.Key, out newInstanceId))
                        newInstanceId = state.Key;

                    updatedStates[newInstanceId] = state.Value;
                }
                OperatorPartStates = updatedStates;
            }
        }

        #region Properties

        public Guid ID { get; set; }
        public String Revision { get; set; }
        private string _name = string.Empty;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (IsBasic)
                    OperatorParts[0].Item2.Name = _name + "Func";
                RaisePropertyChangedEvent("Name");
                Changed = true;
            }
        }

        public List<MetaInput> Inputs { get; internal set; }
        public List<MetaOutput> Outputs { get; internal set; }
        internal Dictionary<Guid, Tuple<MetaOperator, InstanceProperties>> Operators { get; set; }

        public List<Tuple<Guid, MetaOperatorPart>> OperatorParts
        {
            get { return _operatorParts; }
            set
            {
                foreach (var opPart in value)
                    opPart.Item2.Parent = this;
                _operatorParts = value;
            }
        }

        public List<MetaConnection> Connections { get; set; }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                RaisePropertyChangedEvent("Description");
                Changed = true;
            }
        }

        private string _description = string.Empty;
        private string _namespace = string.Empty;

        public string Namespace
        {
            get { return _namespace; }
            set
            {
                if (value != _namespace)
                {
                    _namespace = value;
                    RaisePropertyChangedEvent("Namespace");
                    Changed = true;
                }
            }
        }

        public bool IsBasic { get { return OperatorParts.Count > 0; } }
        public int InstanceCount { get { return _instances.Count; } }
        private bool _changed = false;

        public bool Changed
        {
            get
            {
                var stateChanged = (from op in Operators
                                    from stateEntry in op.Value.Item2.OperatorPartStates
                                    where stateEntry.Value.Changed
                                    select stateEntry).Any();
                var opPartChanged = (from opPart in OperatorParts
                                     where opPart.Item2.ScriptChanged
                                     select opPart).Any();
                return _changed || stateChanged || opPartChanged;
            }
            internal set
            {
                _changed = value;
                if (value == false)
                {
                    foreach (var op in Operators)
                    {
                        foreach (var stateEntry in op.Value.Item2.OperatorPartStates)
                        {
                            stateEntry.Value.Changed = false;
                        }
                    }
                    foreach (var opPart in OperatorParts)
                    {
                        opPart.Item2.ScriptChanged = false;
                    }
                }
            }
        }

        public IEnumerable<string> SupplierAssemblyNames
        {
            get
            {
                return from opEntry in Operators
                       where IsBasic // additional check
                       let dependencyOpDefinition = opEntry.Value.Item1
                       where dependencyOpDefinition.IsBasic
                       let supplierAssemblyName = dependencyOpDefinition.OperatorParts.First().Item2.SourceName
                       select supplierAssemblyName;
            }
        }

        public IEnumerable<MetaOperator> SupplierDefinitions
        {
            get
            {
                return from opEntry in Operators
                       where IsBasic // additional check
                       let dependencyOpDefinition = opEntry.Value.Item1
                       where dependencyOpDefinition.IsBasic
                       select dependencyOpDefinition;
            }
        }

        // todo: move to cmd
        public void SetSupplierAssemblies(IEnumerable<Assembly> supplierAssemblies)
        {
            foreach (var opEntry in Operators.Reverse())
            {
                var supplierOpPartDefinition = opEntry.Value.Item1.OperatorParts.First().Item2;
                supplierOpPartDefinition.ScriptChangedEvent -= OperatorParts.First().Item2.HandleDependencyOperator_ScriptChange;
                RemoveOperator(opEntry.Key);
            }

            foreach (var supplierAssembly in supplierAssemblies)
            {
                var idSearchPattern = new Regex(@".+Func_ID([0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12})_Version[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}");
                var match = idSearchPattern.Match(supplierAssembly.FullName);
                if (match.Success && match.Groups.Count > 2)
                {
                    var idToLookFor = Guid.Parse(match.Groups[1].Value);
                    //Logger.Debug("{0} at path: {1}", supplierAssembly.GetName(), supplierAssembly.Location);
                    var correspondingOpDefinition = (from opDefintion in MetaManager.Instance.MetaOperators
                                                     where opDefintion.Value.IsBasic
                                                     let opPartDefintion = opDefintion.Value.OperatorParts[0].Item2
                                                     where opPartDefintion.ID == idToLookFor
                                                     select opDefintion.Value).FirstOrDefault();
                    AddOperator(correspondingOpDefinition);
                    var supplierOpPartDefinition = correspondingOpDefinition.OperatorParts.First().Item2;
                    supplierOpPartDefinition.ScriptChangedEvent += OperatorParts.First().Item2.HandleDependencyOperator_ScriptChange;
                }
            }
        }
    #endregion

        #region Constructor
        public MetaOperator(Guid id)
        {
            ID = id;
            Revision = string.Empty;
            Inputs = new List<MetaInput>();
            Outputs = new List<MetaOutput>();
            Operators = new Dictionary<Guid, Tuple<MetaOperator, InstanceProperties>>();
            OperatorParts = new List<Tuple<Guid, MetaOperatorPart>>();
            Connections = new List<MetaConnection>();
            Description = String.Empty;
            Namespace = String.Empty;
            Changed = false;
        }
        #endregion

        public void Dispose()
        {
            var instances = new List<Operator>(_instances);
            instances.ForEach(instance => instance.Dispose());
            _instances.Clear();
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("InstanceCount"));
        }

        public MetaOperator Clone(string newName) 
        {
            var oldToNewGuidDict = new Dictionary<Guid, Guid>();
            oldToNewGuidDict[Guid.Empty] = Guid.Empty;

            var inputs = new List<MetaInput>();
            Inputs.ForEach(i =>
                           {
                               var newInput = i.Clone();
                               inputs.Add(newInput);
                               oldToNewGuidDict[i.ID] = newInput.ID;
                           });

            var outputs = new List<MetaOutput>();
            Outputs.ForEach(o =>
                            {
                                var newOutput = o.Clone();
                                outputs.Add(newOutput);
                                oldToNewGuidDict[o.ID] = newOutput.ID;
                            });

            var opParts = new List<Tuple<Guid, MetaOperatorPart>>();
            OperatorParts.ForEach(e =>
                                  {
                                      var newElement = Tuple.Create(Guid.NewGuid(), e.Item2.Clone(newName + "Func"));
                                      opParts.Add(newElement);
                                      oldToNewGuidDict[e.Item1] = newElement.Item1;
                                      oldToNewGuidDict[e.Item2.ID] = newElement.Item2.ID;
                                  });

            var ops = new Dictionary<Guid, Tuple<MetaOperator, InstanceProperties>>();
            foreach (var o in Operators)
            {
                var newElement = Tuple.Create(o.Value.Item1, o.Value.Item2.Clone());
                var id = Guid.NewGuid();
                ops[id] = newElement;
                oldToNewGuidDict[o.Key] = id;
                oldToNewGuidDict[o.Value.Item1.ID] = newElement.Item1.ID;
            }
            foreach (var o in ops) 
                o.Value.Item2.ExchangeGuids(oldToNewGuidDict);

            var connections = new List<MetaConnection>();
            Connections.ForEach(c =>
                                {
                                    Guid newSourceOpID = Guid.Empty;
                                    if (!oldToNewGuidDict.TryGetValue(c.SourceOpID, out newSourceOpID))
                                        newSourceOpID = c.SourceOpID;
                                    Guid newSourceOpPartID = Guid.Empty;
                                    if (!oldToNewGuidDict.TryGetValue(c.SourceOpPartID, out newSourceOpPartID))
                                        newSourceOpPartID = c.SourceOpPartID;
                                    Guid newTargetOpID = Guid.Empty;
                                    if (!oldToNewGuidDict.TryGetValue(c.TargetOpID, out newTargetOpID))
                                        newTargetOpID = c.TargetOpID;
                                    Guid newTargetOpPartID = Guid.Empty;
                                    if (!oldToNewGuidDict.TryGetValue(c.TargetOpPartID, out newTargetOpPartID))
                                        newTargetOpPartID = c.TargetOpPartID;

                                    connections.Add(new MetaConnection(newSourceOpID, newSourceOpPartID, newTargetOpID, newTargetOpPartID));
                                });

            return new MetaOperator(Guid.NewGuid())
                       {
                           Name = newName,
                           Inputs = inputs,
                           Outputs = outputs,
                           OperatorParts = opParts,
                           Operators = ops,
                           Connections = connections,
                           Description = Description,
                           Namespace = Namespace,
                           Changed = true,
                       };
        }

        #region Consistency Checks
        // checks for inconsistancies and fixes them if found
        // Note: this method modifies directly the meta op, therefore it must be called only at startup at a point where no instances are created
        public void CheckForInconsistencyAndFixThem()
        {
            // check for missing ops
            var opEntriesToRemove = (from opEntry in Operators where opEntry.Value == null select opEntry).ToArray();
            foreach (var opEntry in opEntriesToRemove)
            {
                RemoveOperator(opEntry.Key);
                Logger.Error("Removed reference to missing operator instance {0} in {1}. Be careful now, only save and publish this change, if you know what you're doing.", opEntry.Key, Name);
            }

            // if meta op is basic type (== has a code op part) then the code op part must be a multi input
            if (IsBasic && OperatorParts[0].Item2.IsMultiInput == false)
            {
                Logger.Warn("Fixing OpPart definition of code operator to multi-input: '{0} {1}'. Please report this issue.", Namespace, Name);
                OperatorParts[0].Item2.IsMultiInput = true;
            }

            // check if there are duplicate connections stored, if that's the case check if these duplicates are pointing to a multiinput
            // and are not stored within a basic op
            var uniqueList = Connections.Distinct(new MetaConnectionComparer());
            if (uniqueList.Count() != Connections.Count)
            {
                var duplicates = Connections.Except(uniqueList).ToList();
                // check if output of duplicate connections are pointing to an multiinput
                foreach (var connection in duplicates)
                {
                    bool isMultiInput = true;
                    if (connection.TargetOpID == Guid.Empty)
                    {
                        // find connection target in among outputs and op parts of this meta op
                        var targetOpPart = (from opPart in OperatorParts
                                            where opPart.Item1 == connection.TargetOpPartID
                                            select opPart).SingleOrDefault();
                        if (targetOpPart != null)
                        {
                            isMultiInput = targetOpPart.Item2.IsMultiInput;
                        }
                        else
                        {
                            var targetOutput = (from output in Outputs
                                                where output.ID == connection.TargetOpPartID
                                                select output).Single();
                            isMultiInput = targetOutput.OpPart.IsMultiInput;
                        }
                    }
                    else
                    {
                        // find target among inputs of nested operators
                        var targetInput = (from op in Operators
                                           where op.Key == connection.TargetOpID
                                           from input in op.Value.Item1.Inputs
                                           where input.ID == connection.TargetOpPartID
                                           select input).Single();
                        isMultiInput = targetInput.IsMultiInput;
                    }
                    if (!isMultiInput || IsBasic)
                    {
                        Logger.Error("Fixing double connection in basic operator definition or connectins pointing to a single input'{0} {1}'.", Namespace, Name);
                        Connections.Remove(connection);
                    }
                }
            }

            // check if there are connections that point to/from non existing elements anymore
            var connectionsToRemove = new List<MetaConnection>();
            foreach (var connection in Connections)
            {
                bool sourceFound;
                if (connection.SourceOpID == Guid.Empty)
                {
                    var opPartIDs = from opPart in OperatorParts select opPart.Item1;
                    var inputIDs = from input in Inputs select input.ID;
                    var allIDs = (opPartIDs).Union(inputIDs);
                    sourceFound = allIDs.Count(partID => partID == connection.SourceOpPartID) == 1;
                }
                else
                {
                    sourceFound = (from op in Operators
                                   where op.Key == connection.SourceOpID
                                   from output in op.Value.Item1.Outputs
                                   where output.ID == connection.SourceOpPartID
                                   select output).Count() == 1;
                }

                bool targetFound = false;
                if (sourceFound)
                {
                    if (connection.TargetOpID == Guid.Empty)
                    {
                        var opPartIDs = from opPart in OperatorParts select opPart.Item1;
                        var outputIDs = from output in Outputs select output.ID;
                        var allIDs = (opPartIDs).Union(outputIDs);
                        targetFound = allIDs.Count(partID => partID == connection.TargetOpPartID) == 1;
                    }
                    else
                    {
                        targetFound = (from op in Operators
                                       where op.Key == connection.TargetOpID
                                       from output in op.Value.Item1.Inputs
                                       where output.ID == connection.TargetOpPartID
                                       select output).Count() == 1;
                    }
                }

                if (!targetFound)
                    connectionsToRemove.Add(connection);
            }
            foreach (var connection in connectionsToRemove)
            {
                Logger.Warn("Found connection in '{0}' pointing to non exisiting elements -> removed it.", Name);
                RemoveConnection(connection, 0);
            }
        }


        // specific meta connection comparer that ignores the ID property to find duplicated connections
        private class MetaConnectionComparer : IEqualityComparer<MetaConnection>
        {
            // In this case a meta connections equals if all source/target properties are equal
            public bool Equals(MetaConnection x, MetaConnection y)
            {
                return x.SourceOpID == y.SourceOpID &&
                       x.SourceOpPartID == y.SourceOpPartID &&
                       x.TargetOpID == y.TargetOpID &&
                       x.TargetOpPartID == y.TargetOpPartID;
            }

            // If Equals() returns true for a pair of objects 
            // then GetHashCode() must return the same value for these objects.
            public int GetHashCode(MetaConnection connection)
            {
                // Check whether the object is null
                if (connection == null)
                    return 0;

                //Calculate the hash code
                return connection.SourceOpID.GetHashCode() ^
                       connection.SourceOpPartID.GetHashCode() ^
                       connection.TargetOpID.GetHashCode() ^
                       connection.TargetOpPartID.GetHashCode();
            }
        }
        #endregion

        #region OperatorHandling

        private void MoveOperatorTo(Guid opIdToMove, MetaOperator newParent, Guid newParentId)
        {
            var metaOpEntry = (from o in Operators where o.Key == opIdToMove select o).Single();
            Operators.Remove(opIdToMove);
            newParent.Operators.Add(metaOpEntry.Key, metaOpEntry.Value);

            foreach (var instance in _instances)
            {
                instance.MoveOperatorTo(opIdToMove, newParentId);
            }
        }


        public Guid AddOperator(MetaOperator metaOp, double posX = 100, double posY = 100, double width = 100, bool visible = true)
        {
            return AddOperator(metaOp, Guid.NewGuid(), posX, posY, width, visible);
        }

        public Guid AddOperator(MetaOperator metaOp, Guid id, double posX = 100, double posY = 100, double width = 100, bool visible = true)
        {
            var opProperties = new InstanceProperties() { Position = new Point(posX, posY), Width = width, Visible = visible };
            foreach (var metaInput in metaOp.Inputs)
            {
                opProperties.InputValues.Add(metaInput.ID, metaInput.DefaultFunc);
            }

            foreach (var metaOpPartEntry in metaOp.OperatorParts)
            {
                var stateType = metaOpPartEntry.Item2.StateType;
                if (stateType != null)
                {
                    // check if part contains a state and if so create it
                    var state = Activator.CreateInstance(stateType) as IOperatorPartState;
                    opProperties.OperatorPartStates[metaOpPartEntry.Item1] = state;
                }
            }

            return AddOperator(metaOp, opProperties, id);
        }

        public Operator GetOperatorInstance(Guid opID)
        {
            return _instances.FirstOrDefault(op => op.ID == opID);
        }

        internal IEnumerable<Operator> GetOperatorInstances(Guid opID)
        {
            return _instances.FindAll(op => op.ID == opID);
        }

        internal Guid AddOperator(MetaOperator metaOp, InstanceProperties opProperties, Guid id)
        {
            Operators.Add(id, Tuple.Create(metaOp, opProperties));
            Changed = true;

            foreach (var opInstance in _instances)
            {
                var addedOp = metaOp.CreateOperator(id);
                var opProps = opProperties; // make local copy for closures in lambda functions below!
                addedOp.InputAddedEvent += (o, e) => { AddInputToOperatorProperties(opProps, e.OperatorPart); };
                addedOp.InputRemovedEvent += (o, e) => { RemoveInputFromOperatorProperties(opProps, e.OperatorPart); };

                // bind all inputs to ourself to get informed when these change
                foreach (var input in addedOp.Inputs)
                {
                    if (!opProperties.InputValues.ContainsKey(input.ID))
                    {
                        //Logger.Warn("ignoring missing opProperty:" + input.Name);
                        opProperties.InputValues.Add(input.ID, input.Func);                        
                    }
                    input.Func = opProperties.InputValues[input.ID].Clone();
                    input.ChangedEvent += (o, e) => { UpdateInputValues(o as OperatorPart, opProperties); };
                }

                opInstance.AddOperator(addedOp); // do this as last step as this triggers update events
            }
            return id;
        }

        private void AddInputToOperatorProperties(InstanceProperties opProperties, OperatorPart input)
        {
            opProperties.InputValues[input.ID] = input.Func.Clone();
            input.ChangedEvent += (o, e) => { UpdateInputValues(o as OperatorPart, opProperties); };
        }

        private static void RemoveInputFromOperatorProperties(InstanceProperties opProperties, OperatorPart input)
        {
            opProperties.InputValues.Remove(input.ID);
        }

        internal MetaOperator CombineToNewOp(Guid newMetaID, Guid newOpInstanceId, IEnumerable<Guid> opsToCombine, String name = "CombinedOp", String @namespace = "", String description = "",
                                             double poxX = 100, double posY = 100)
        {
            var newOp = new MetaOperator(newMetaID);
            newOp.Name = name;
            newOp.Namespace = @namespace;
            newOp.Description = description;

            AddOperator(newOp, newOpInstanceId, poxX, posY);

            // find the connections within the grouped op
            var internalConnections = (from con in Connections
                                       from sourceOp in opsToCombine
                                       from targetOp in opsToCombine
                                       where con.SourceOpID == sourceOp
                                       where con.TargetOpID == targetOp
                                       select con).ToList();
            var inputConnections = (from con in Connections
                                    from targetOp in opsToCombine
                                    let opIDs = from o in Operators select o.Key
                                    from op in opIDs.Except(opsToCombine).Union(new List<Guid>() { Guid.Empty })
                                    where con.TargetOpID == targetOp
                                    where con.SourceOpID == op
                                    select con).ToList();
            var outputConnections = (from con in Connections
                                     from sourceOp in opsToCombine
                                     let opIDs = from o in Operators select o.Key
                                     from op in opIDs.Except(opsToCombine).Union(new List<Guid>() { Guid.Empty })
                                     where con.SourceOpID == sourceOp
                                     where con.TargetOpID == op
                                     select con).ToList();

            var allConnectionsUnsorted = inputConnections.Union(internalConnections).Union(outputConnections);
            var allConnectionsSorted = new List<Tuple<MetaConnection, int>>(); // these are later on needed to restore multi input order (item2 == multiIdx)!
            foreach (var con in Connections)
            {
                if (allConnectionsUnsorted.Contains(con))
                {
                    var idxOfCon = Connections.FindIndex(c => c == con); // index of connection
                    var fstIdxOfConTarget = Connections.FindIndex(c => c.TargetOpID == con.TargetOpID && c.TargetOpPartID == con.TargetOpPartID);
                    var multiInputIdx = idxOfCon - fstIdxOfConTarget;
                    allConnectionsSorted.Add(Tuple.Create(con, multiInputIdx));
                }
            }

            // remove all connections to combined ops from parent op
            var consToRemove = new List<Tuple<MetaConnection, int>>(allConnectionsSorted);
            consToRemove.Reverse();
            foreach (var con in consToRemove)
            {
                RemoveConnection(con.Item1, con.Item2);
            }

            foreach (var opId in opsToCombine)
            {
                MoveOperatorTo(opId, newOp, newOpInstanceId);
            }

            // build new inputs and connection to them
            // first group them so that for several connections which have the same source only one input is generated
            var groupedInputs = (from con in inputConnections
                                 group con by (con.SourceOpID.ToString() + con.SourceOpPartID.ToString())
                                 into g
                                 select g).ToList();

            foreach (var inputGroup in groupedInputs)
            {
                MetaConnection prevCon = null;
                MetaInput targetInput = null;
                var targetName = new StringBuilder();
                foreach (var con in inputGroup)
                {
                    prevCon = con;
                    targetInput = (from op in newOp.Operators
                                   where op.Key == con.TargetOpID
                                   from prevTarget in op.Value.Item1.Inputs
                                   where con.TargetOpPartID == prevTarget.ID
                                   select prevTarget).Single();

                    if (targetName.Length > 0)
                        targetName.Append(" | ");
                    targetName.Append(targetInput.Name);
                }
                var sourceName = (prevCon.SourceOpID == Guid.Empty) ? Name : GetName(prevCon.SourceOpID);
                var newInputName = String.IsNullOrEmpty(sourceName) ? targetName.ToString() : sourceName;
                var newInput = new MetaInput(Guid.NewGuid(), newInputName, targetInput.OpPart, targetInput.DefaultValue.Clone(), targetInput.IsMultiInput);
                newInput.Relevance = MetaInput.RelevanceType.Required;
                newOp.AddInput(newInput);

                // create new connection in parent op from prev source to new input of combined op
                var conToNewInput = new MetaConnection(prevCon.SourceOpID, prevCon.SourceOpPartID, newOpInstanceId, newInput.ID);
                InsertConnectionAt(conToNewInput);

                foreach (var con in inputGroup)
                {
                    // set stored connection to new input
                    con.SourceOpID = Guid.Empty;
                    con.SourceOpPartID = newInput.ID;
                }
            }

            // build new outputs and connections to them
            foreach (var con in outputConnections)
            {
                var sourceOutput = (from op in newOp.Operators
                                    where op.Key == con.SourceOpID
                                    from prevSource in op.Value.Item1.Outputs
                                    where con.SourceOpPartID == prevSource.ID
                                    select prevSource).Single();
                var newOutput = new MetaOutput(Guid.NewGuid(), sourceOutput.Name, sourceOutput.OpPart);
                newOp.AddOutput(newOutput);

                var conFromNewOutput = new MetaConnection(newOpInstanceId, newOutput.ID, con.TargetOpID, con.TargetOpPartID);
                var prevConWithMultiIdx = allConnectionsSorted.Find(c => c.Item1.SourceOpID == con.SourceOpID &&
                                                                         c.Item1.SourceOpPartID == con.SourceOpPartID &&
                                                                         c.Item1.TargetOpID == con.TargetOpID &&
                                                                         c.Item1.TargetOpPartID == con.TargetOpPartID);
                InsertConnectionAt(conFromNewOutput, prevConWithMultiIdx.Item2);

                // set stored connection to new output
                con.TargetOpID = Guid.Empty;
                con.TargetOpPartID = newOutput.ID;
            }

            // setup all connections within new op
            // first group all connections to same input in order to get multi input indices
            var groupedConnections = (from con in allConnectionsSorted
                                      group con by (con.Item1.TargetOpID.ToString() + con.Item1.TargetOpPartID.ToString()) into g
                                      select g).ToList();

            // insert the connections to new op
            foreach (var conGroup in groupedConnections)
            {
                var index = 0;
                foreach (var con in conGroup)
                {
                    newOp.InsertConnectionAt(con.Item1, index);
                    index++;
                }
            }

            Changed = true;

            return newOp;
        }

        // returns a dict with mapping of old instance ids to new instance ids
        internal Dictionary<Guid, Guid> UngroupOperator(Guid id, MetaOperator valueMetaOp)
        {
            var opEntryToUngroup = (from op in Operators where op.Key == id select op).First();
            var metaOpToUngroup = opEntryToUngroup.Value.Item1;
            // get position of ungrouped op
            var ungroupOpPosition = GetPosition(id);
            var opIDsToUngroup = (from op in metaOpToUngroup.Operators select op.Key);
            var topLeftPoint = new Point(double.MaxValue, double.MaxValue);
            foreach (var opIdToUngroup in opIDsToUngroup)
            {
                if (metaOpToUngroup.GetVisible(opIdToUngroup))
                {
                    var pos = metaOpToUngroup.GetPosition(opIdToUngroup);
                    if (pos.X < topLeftPoint.X)
                        topLeftPoint.X = pos.X;
                    if (pos.Y < topLeftPoint.Y)
                        topLeftPoint.Y = pos.Y;
                }
            }
            var offsetToUngroupOpPos = ungroupOpPosition - topLeftPoint;

            // for each internal op create instantiation here
            var opsToUngroup = new Dictionary<Guid, Tuple<MetaOperator, InstanceProperties>>(metaOpToUngroup.Operators); // make copy as we're modifying original list during move
            var originalToCopyMap = new Dictionary<Guid, Guid>();
            foreach (var opEntry in opsToUngroup)
            {
                var opID = opEntry.Key;
                var op = opEntry.Value.Item1;
                var newOpId = Guid.NewGuid();
                AddOperator(op, opEntry.Value.Item2.Clone(), newOpId);
                var pos = GetPosition(newOpId);
                SetPosition(newOpId, pos + offsetToUngroupOpPos);
                originalToCopyMap[opID] = newOpId;
            }

            // find the connections within the op
            var internalConnections = (from con in metaOpToUngroup.Connections
                                       from sourceOp in opIDsToUngroup
                                       from targetOp in opIDsToUngroup
                                       where con.SourceOpID == sourceOp
                                       where con.TargetOpID == targetOp
                                       select new MetaConnection(originalToCopyMap[con.SourceOpID], con.SourceOpPartID, originalToCopyMap[con.TargetOpID], con.TargetOpPartID)).ToList();
            var newConnectionsToInputsTmp = (from conToOp in Connections
                                             where conToOp.TargetOpID == opEntryToUngroup.Key
                                             from conFromInput in metaOpToUngroup.Connections
                                             from targetOp in opIDsToUngroup
                                             where conFromInput.SourceOpID == Guid.Empty
                                             where conFromInput.TargetOpID == targetOp
                                             where conToOp.TargetOpPartID == conFromInput.SourceOpPartID
                                             let newCon = new MetaConnection(conToOp.SourceOpID, conToOp.SourceOpPartID,
                                                                             originalToCopyMap[conFromInput.TargetOpID], conFromInput.TargetOpPartID)
                                             select new { NewCon = newCon, PrevInputID = conFromInput.SourceOpPartID }).ToList();
            var newConnectionsToInputs = (from con in newConnectionsToInputsTmp select con.NewCon).ToList();
            // connections that have as input a hidden op (e.g. when a 'ungrouped' input was animated) get
            // an additional value op inserted to visualize this connection
            var connectionsWhichNeedAdditionalValueOp = (from conToInput in newConnectionsToInputsTmp
                                                         from opEntry in Operators
                                                         where opEntry.Key == conToInput.NewCon.SourceOpID
                                                         where opEntry.Value.Item2.Visible == false
                                                         select conToInput).ToList();
            foreach (var con in connectionsWhichNeedAdditionalValueOp)
            {
                var newValueOpID = Guid.NewGuid();
                AddOperator(valueMetaOp, new InstanceProperties(valueMetaOp.Inputs), newValueOpID);
                var inputThatIsReplaced = (from input in metaOpToUngroup.Inputs where input.ID == con.PrevInputID select input).Single();
                foreach (var instance in _instances)
                {
                    var valueInstance = (from op in instance.InternalOps where op.ID == newValueOpID select op).Single();
                    valueInstance.Name = inputThatIsReplaced.Name;
                }
                var newCon = new MetaConnection(con.NewCon.SourceOpID, con.NewCon.SourceOpPartID, newValueOpID, valueMetaOp.Inputs[0].ID);
                newConnectionsToInputs.Add(newCon);
                con.NewCon.SourceOpID = newValueOpID;
                con.NewCon.SourceOpPartID = valueMetaOp.Outputs[0].ID;
            }

            var newConnectionsFromOutputs = (from conFromOp in Connections
                                             where conFromOp.SourceOpID == opEntryToUngroup.Key
                                             from conToOutput in metaOpToUngroup.Connections
                                             from sourceOp in opIDsToUngroup
                                             where conToOutput.SourceOpID == sourceOp
                                             where conToOutput.TargetOpID == Guid.Empty
                                             where conToOutput.TargetOpPartID == conFromOp.SourceOpPartID
                                             let newCon = new MetaConnection(originalToCopyMap[conToOutput.SourceOpID], conToOutput.SourceOpPartID,
                                                                             conFromOp.TargetOpID, conFromOp.TargetOpPartID)
                                             let index = GetConnectionIndexAtTargetInput(conFromOp)
                                             select new { Connection = newCon, Index = index }).ToList();

            var allInputConnectionsUnsorted = newConnectionsToInputs.Union(internalConnections).ToList();

            RemoveOperator(id);

            // setup all connections within new op
            // first group all connections to same input in order to get multi input indices
            var groupedInputConnections = (from con in allInputConnectionsUnsorted
                                           group con by (con.TargetOpID.ToString() + con.TargetOpPartID.ToString())
                                           into @group
                                           select @group).ToList();

            // insert the connections to new op
            foreach (var conGroup in groupedInputConnections)
            {
                var index = 0;
                foreach (var con in conGroup)
                {
                    InsertConnectionAt(con, index);
                    index++;
                }
            }

            // add connections from outputs
            foreach (var c in newConnectionsFromOutputs)
            {
                InsertConnectionAt(c.Connection, c.Index);
            }

            Changed = true;

            return originalToCopyMap;
        }

        internal void RemoveOperator(Guid opId)
        {
            // first remove connections to and from op
            RemoveAllConnectionsToOp(opId);
            RemoveAllConnectionsFromOp(opId);

            // then remove op itself
            Operators.Remove(opId);

            foreach (var opInstance in _instances)
            {
                RemoveOperatorFromInstance(opId, opInstance);
            }
            Changed = true;
        }

        internal void RemoveInstance(Operator op)
        {
            _instances.Remove(op);
            RaisePropertyChangedEvent("InstanceCount");
            //Changed = true;
        }

        private void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private InstanceProperties GetOperatorProperties(Guid opId)
        {
            return Operators[opId].Item2;
        }

        internal string GetName(Guid opId)
        {
            return GetOperatorProperties(opId).Name;
        }

        internal void SetName(Guid opId, string name)
        {
            GetOperatorProperties(opId).Name = name;
            Changed = true;
        }

        internal Point GetPosition(Guid opId)
        {
            return GetOperatorProperties(opId).Position;
        }

        internal void SetPosition(Guid opId, Point position)
        {
            GetOperatorProperties(opId).Position = position;
            foreach (var opParentInstance in _instances)
            {
                var op = opParentInstance.InternalOps.Find(o => o.ID == opId);
                op.TriggerPositionChanged(new PositionChangedEventArgs(position));
            }
            Changed = true;
        }

        internal double GetWidth(Guid opId)
        {
            return GetOperatorProperties(opId).Width;
        }

        internal void SetWidth(Guid opId, double width)
        {
            GetOperatorProperties(opId).Width = width;
            foreach (var opParentInstance in _instances)
            {
                var op = opParentInstance.InternalOps.Find(o => o.ID == opId);
                op.TriggerWidthChanged(new WidthChangedEventArgs(width));
            }
            Changed = true;
        }

        internal bool GetVisible(Guid opId)
        {
            return GetOperatorProperties(opId).Visible;
        }

        internal void SetVisible(Guid opId, bool visible)
        {
            GetOperatorProperties(opId).Visible = visible;
            foreach (var opParentInstance in _instances)
            {
                var op = opParentInstance.InternalOps.Find(o => o.ID == opId);
                op.TriggerVisibleChanged(new VisibleChangedEventArgs(visible));
            }
            Changed = true;
        }

        internal bool GetDisabled(Guid opId)
        {
            return GetOperatorProperties(opId).Disabled;
        }

        internal void SetDisabled(Guid opId, bool disabled)
        {
            GetOperatorProperties(opId).Disabled = disabled;
            foreach (var opParentInstance in _instances)
            {
                var op = opParentInstance.InternalOps.Find(o => o.ID == opId);
                op.DisableOutputs(disabled);
            }
            Changed = true;
        }

        public IOperatorPartState GetOperatorPartState(Guid opId, Guid opPartId)
        {
            IOperatorPartState element = null;
            if (GetOperatorProperties(opId).OperatorPartStates.TryGetValue(opPartId, out element))
                return element;
            return null;
        }

        public void SetOperatorPartState(Guid opId, Guid opPartId, IOperatorPartState state)
        {
            GetOperatorProperties(opId).OperatorPartStates[opPartId] = state;
            foreach (var opParentInstance in _instances)
            {
                var op = opParentInstance.InternalOps.Find(o => o.ID == opId);
                op.TriggerOperatorPartStateChanged(new OperatorPartStateChangedEventArgs(state));
            }
            Changed = true;
        }

        public IEnumerable<Guid> InternalOperatorsMetaOpId
        {
            get { return Operators.Select(item => item.Value.Item1.ID); }
        }
        #endregion

        #region Input-Output Handling

        public void InsertInput(int index, MetaInput input)
        {
            if ((index < 0) || (index > Inputs.Count))
                throw new IndexOutOfRangeException();

            Inputs.Insert(index, input);

            foreach (var instance in _instances)
            {
                instance.InsertInput(index, input.CreateInstance());
            }

            if (IsBasic)
                InsertConnectionAt(new MetaConnection(Guid.Empty, input.ID, Guid.Empty, OperatorParts.First().Item1), index);

            Changed = true;
        }

        public void AddInput(MetaInput input)
        {
            InsertInput(Inputs.Count, input);
        }

        public void RemoveInput(Guid inputId)
        {
            Inputs.RemoveAll(input => input.ID == inputId);
            RemoveAllConnectionsFromInput(inputId);

            foreach (var instance in _instances)
            {
                var input = instance.FindOpPart(Guid.Empty, inputId);
                instance.RemoveInputInternal(input.Item2);
            }
            Changed = true;
        }

        public void RemoveAllInputs()
        {
            foreach (var instance in _instances)
            {
                foreach (var metaInput in Inputs)
                {
                    var input = instance.FindOpPart(Guid.Empty, metaInput.ID);
                    instance.RemoveInputInternal(input.Item2);
                }
            }
            Inputs.Clear();
            Changed = true;
        }

        public void InsertOutput(int index, MetaOutput output)
        {
            if ((index < 0) || (index > Outputs.Count))
                throw new IndexOutOfRangeException();

            Outputs.Insert(index, output);

            foreach (var instance in _instances)
            {
                instance.InsertOutput(index, output.CreateInstance());
            }

            if (IsBasic)
                InsertConnectionAt(new MetaConnection(Guid.Empty, OperatorParts.First().Item1, Guid.Empty, output.ID), 0);

            Changed = true;
        }

        public void AddOutput(MetaOutput output)
        {
            InsertOutput(Outputs.Count, output);
        }

        public void RemoveOutput(Guid outputId)
        {
            Outputs.RemoveAll(output => output.ID == outputId);
            RemoveAllConnectionsToOutput(outputId);

            foreach (var instance in _instances)
            {
                var output = instance.FindOpPart(Guid.Empty, outputId);
                instance.RemoveOutputInternal(output.Item2);
            }
            Changed = true;
        }

        public void RemoveAllOutputs()
        {
            foreach (var instance in _instances)
            {
                foreach (var metaOutput in Outputs)
                {
                    var output = instance.FindOpPart(Guid.Empty, metaOutput.ID);
                    instance.RemoveOutputInternal(output.Item2);
                }
            }
            Outputs.Clear();
            Changed = true;
        }

        // Returns the outputs ordered by their output idx 
        public IEnumerable<MetaOutput> GetOrderedOutputs()
        {
            // makes only sense for basic ops
            if (!IsBasic)
                return new List<MetaOutput>();

            // get id of op part and collect all connections to this id
            var metaOpPartID = OperatorParts.First().Item1;
            var outputsToOpPart = from c in Connections
                                  where c.SourceOpID == Guid.Empty &&
                                        c.SourceOpPartID == metaOpPartID
                                  select new { MetaOpID = c.TargetOpID, MetaOpPartID = c.TargetOpPartID };

            //create a list of outputs that are connected to the metaop with respect to the order defined within the connection list
            var outputOpParts = (from o in outputsToOpPart
                                 join output in Outputs on o.MetaOpPartID equals output.ID
                                 select output).ToList();
            return outputOpParts;
        }

        // Returns the inputs ordered by their input idx 
        public IEnumerable<MetaInput> GetOrderedInputs()
        {
            // makes only sense for basic ops
            if (!IsBasic)
                return new List<MetaInput>();

            // get id of op part and collect all connections to this id
            var metaOpPartID = OperatorParts.First().Item1;
            var inputsToOpPart = from c in Connections
                                 where c.TargetOpID == Guid.Empty &&
                                       c.TargetOpPartID == metaOpPartID
                                 select new { MetaOpID = c.SourceOpID, MetaOpPartID = c.SourceOpPartID };

            //create a list of inputs that are connected to the metaop with respect to the order defined within the connection list
            var inputOpParts = (from i in inputsToOpPart
                                join input in Inputs on i.MetaOpPartID equals input.ID
                                select input).ToList();
            return inputOpParts;
        }

        #endregion

        #region ConnectionHandling

        internal int GetConnectionIndexAtTargetInput(MetaConnection connectionToLookup)
        {
            var firstOccuranceOfTargetOpID = Connections.FindIndex(con => (con.TargetOpID == connectionToLookup.TargetOpID) &&
                                                                          (con.TargetOpPartID == connectionToLookup.TargetOpPartID));
            var occurance = Connections.FindIndex(con => (con.SourceOpID == connectionToLookup.SourceOpID) &&
                                                         (con.SourceOpPartID == connectionToLookup.SourceOpPartID) &&
                                                         (con.TargetOpID == connectionToLookup.TargetOpID) &&
                                                         (con.TargetOpPartID == connectionToLookup.TargetOpPartID));

            return occurance - firstOccuranceOfTargetOpID;
        }

        internal void InsertConnectionAt(MetaConnection newConnection, int connectionIndex = 0)
        {
            //todo: add some checks to prevent inconsistencies
            //-find targetOpPartID within op parts of targetOpID
            //-if foundOpPart.IsMultiInput = false and connection already found failwith "connection already exists"
            var firstOccuranceOfTargetOpID = Connections.FindIndex(con => (con.TargetOpID == newConnection.TargetOpID) &&
                                                                          (con.TargetOpPartID == newConnection.TargetOpPartID));
            if (firstOccuranceOfTargetOpID == -1)
                firstOccuranceOfTargetOpID = Connections.Count;

            var lastOccuranceOfTargetOpID = Connections.FindLastIndex(con => (con.TargetOpID == newConnection.TargetOpID) &&
                                                                             (con.TargetOpPartID == newConnection.TargetOpPartID));
            if (lastOccuranceOfTargetOpID == -1)
                lastOccuranceOfTargetOpID = Connections.Count;

            var indexToInsert = firstOccuranceOfTargetOpID + connectionIndex;
            if ((indexToInsert < firstOccuranceOfTargetOpID) || (indexToInsert > lastOccuranceOfTargetOpID + 1))
                throw new IndexOutOfRangeException();

            Connections.Insert(indexToInsert, newConnection);

            foreach (var instance in _instances)
            {
                var source = instance.FindOpPart(newConnection.SourceOpID, newConnection.SourceOpPartID);
                if (source.Item1 == instance)
                    source = new Tuple<Operator, OperatorPart>(null, source.Item2);
                var target = instance.FindOpPart(newConnection.TargetOpID, newConnection.TargetOpPartID);
                if (target.Item1 == instance)
                    target = new Tuple<Operator, OperatorPart>(null, target.Item2);
                instance.InsertConnectionAtInternal(new Connection(newConnection.ID, source.Item1, source.Item2, target.Item1, target.Item2, connectionIndex));
            }
            Changed = true;
        }

        private void RemoveConnectionInInstances(MetaConnection connection, int connectionIdx)
        {
            foreach (var instance in _instances)
            {
                var source = instance.FindOpPart(connection.SourceOpID, connection.SourceOpPartID);
                if (source.Item1 == instance)
                    source = new Tuple<Operator, OperatorPart>(null, source.Item2);
                var target = instance.FindOpPart(connection.TargetOpID, connection.TargetOpPartID);
                if (target.Item1 == instance)
                    target = new Tuple<Operator, OperatorPart>(null, target.Item2);
                instance.RemoveConnectionInternal(new Connection(connection.ID, source.Item1, source.Item2, target.Item1, target.Item2, connectionIdx));
            }
        }


        internal void RemoveConnection(MetaConnection connection, int connectionIndex)
        {
            var firstOccuranceOfTargetOpID = Connections.FindIndex(con => (con.TargetOpID == connection.TargetOpID) &&
                                                                          (con.TargetOpPartID == connection.TargetOpPartID));
            var lastOccuranceOfTargetOpID = Connections.FindLastIndex(con => (con.TargetOpID == connection.TargetOpID) &&
                                                                             (con.TargetOpPartID == connection.TargetOpPartID));
            if ((firstOccuranceOfTargetOpID == -1) || (lastOccuranceOfTargetOpID == -1))
                throw new System.Exception("connection not available");

            var indexToRemove = firstOccuranceOfTargetOpID + connectionIndex;
            if ((indexToRemove < firstOccuranceOfTargetOpID) || (indexToRemove > lastOccuranceOfTargetOpID))
                throw new System.Exception("index out of range");

            var connectionID = Connections[indexToRemove].ID;
            Connections.RemoveAt(indexToRemove);
            connection.ID = connectionID;
            RemoveConnectionInInstances(connection, connectionIndex);
            Changed = true;
        }

        public bool ExistsConnectionToInput(Guid targetOpID, Guid targetInputID)
        {
            var numConnectionsToInput = (from con in Connections
                                         where con.TargetOpID == targetOpID
                                         where con.TargetOpPartID == targetInputID
                                         select con).Count();
            return numConnectionsToInput > 0;
        }
        #endregion

        private List<Operator> _alreadyUpdatedInputs = new List<Operator>();
        private int _inputUpdateRecursionLevel = 0;


        // Attention, this method should be treated like a static method and NO
        // members should be used. The only exceptions is the recursion guard stuff.
        // As this method can be called in an object context (meta parent) that is
        // not the current meta parent of the opPart parent anymore (eg through
        // a combine).
        private void UpdateInputValues(OperatorPart opPart, InstanceProperties opProperties)
        {
            if (opPart.Connections.Count > 0)
                return; // if oppart gets value via connection there's nothing to do here!

            var inputParent = opPart.Parent;
            var thisInstance = inputParent.Parent; // instance of this metaop incarnation

            // setup recursion guard
            _inputUpdateRecursionLevel += 1;
            _alreadyUpdatedInputs.Add(thisInstance);

            // update other input instances, but only in other this metaop instances!
            var newFunc = opPart.Func.Clone();
            opProperties.InputValues[opPart.ID] = newFunc;
            foreach (var instance in thisInstance.Definition._instances)
            {
                if (!_alreadyUpdatedInputs.Exists((i) => i == instance))
                {
                    _alreadyUpdatedInputs.Add(instance);
                    var inputInstance = instance.FindOpPart(inputParent.ID, opPart.ID);
                    inputInstance.Item2.Func = newFunc;
                }
            }

            // clean up recursion guard
            _inputUpdateRecursionLevel -= 1;
            if (_inputUpdateRecursionLevel == 0)
                _alreadyUpdatedInputs.Clear();
        }

        public Operator CreateOperator(Guid id)
        {
            var creationInputs = (from input in Inputs select new { ID = Guid.Empty, MetaID = input.ID, OpPart = input.CreateInstance() }).ToList();
            var creationOutputs = (from output in Outputs select new { ID = Guid.Empty, MetaID = output.ID, OpPart = output.CreateInstance() }).ToList();
            var creationOpParts = (from p in OperatorParts select new { ID = Guid.Empty, MetaID = p.Item1, OpPart = p.Item2.CreateOpPart(p.Item1) }).ToList();
            var creationOperators = (from op in Operators select new { ID = op.Key, Operator = op.Value.Item1.CreateOperator(op.Key) }).ToList();

            // setup properties management for operators
            var instancePropertiesForAllOps = (from op in Operators select op.Value.Item2).ToList();
            var createdOpsAndProperties = (creationOperators.Zip(instancePropertiesForAllOps, (co, p) => new { CreatedOp = co, Properties = p })).ToList();
            foreach (var opPair in createdOpsAndProperties)
            {
                var createdOp = opPair.CreatedOp.Operator;
                var propertiesForCreatedOp = opPair.Properties; // make local copy for closures in lambda functions below!
                createdOp.InputAddedEvent += (o, e) => { AddInputToOperatorProperties(propertiesForCreatedOp, e.OperatorPart); };
                createdOp.InputRemovedEvent += (o, e) => { RemoveInputFromOperatorProperties(propertiesForCreatedOp, e.OperatorPart); };
                foreach (var input in createdOp.Inputs)
                {
                    if (propertiesForCreatedOp.InputValues.ContainsKey(input.ID))
                    {
                        input.Func = propertiesForCreatedOp.InputValues[input.ID]; //set to default
                    }
                    else
                    {
                        // Since we don't save params with default-values, they will contantly added on create time 
                        //Logger.Warn("Added missing input {0} to operator {1} '{2}' of Type {3}.", input.ID, createdOp.Definition.Name, createdOp.Name, createdOp.Definition.ID);
                        propertiesForCreatedOp.InputValues.Add(input.ID, input.Func);
                        //Changed = true;
                    }
                    input.ChangedEvent += (o, e) => UpdateInputValues(o as OperatorPart, propertiesForCreatedOp);
                }
                foreach (var output in createdOp.Outputs)
                {
                    output.Disabled = propertiesForCreatedOp.Disabled;
                }
            }

            var zippedMetaOpWithInstances = Operators.Zip(creationOperators, (metaOp, op) => { return new { MetaOp = metaOp, Op = op }; });
            var creationInputsOfMetaOps = (from e in zippedMetaOpWithInstances
                                           from input in
                                               e.MetaOp.Value.Item1.Inputs.Zip(e.Op.Operator.Inputs, (metaInput, opInput) => { return new { MetaInput = metaInput, OpInput = opInput }; })
                                           select new { ID = e.MetaOp.Key, MetaID = input.MetaInput.ID, OpPart = input.OpInput }).ToList();
            var creationOutputsOfMetaOps = (from e in zippedMetaOpWithInstances
                                            from output in
                                                e.MetaOp.Value.Item1.Outputs.Zip(e.Op.Operator.Outputs,
                                                                                 (metaOutput, opOutput) => { return new { MetaOutput = metaOutput, OpOutput = opOutput }; })
                                            select new { ID = e.MetaOp.Key, MetaID = output.MetaOutput.ID, OpPart = output.OpOutput }).ToList();

            var allOpParts = creationInputs.Union(creationOutputs).Union(creationOpParts).Union(creationInputsOfMetaOps).Union(creationOutputsOfMetaOps).ToList();

            Connections.ForEach(c =>
                                {
                                    var targetOp = (from opPart in allOpParts where (opPart.ID == c.TargetOpID) && (opPart.MetaID == c.TargetOpPartID) select opPart.OpPart).Single();
                                    var inputOp = (from opPart in allOpParts where (opPart.ID == c.SourceOpID) && (opPart.MetaID == c.SourceOpPartID) select opPart.OpPart).Single();
                                    targetOp.AppendConnection(inputOp);
                                });

            var inputs = (from input in creationInputs select input.OpPart).ToList();
            var outputs = (from output in creationOutputs select output.OpPart).ToList();
            var opParts = (from opPart in creationOpParts select opPart.OpPart).ToList();
            var operators = (from op in creationOperators select op.Operator).ToList();

            var createdOperator = new Operator(id, this, inputs, outputs, operators, opParts);
            AddInstance(createdOperator);

            OperatorParts.ForEach(e => e.Item2.ScriptChangedEvent += HandleScriptChange);

            return createdOperator;
        }

        private void AddInstance(Operator instance)
        {
            _instances.Add(instance);
            RaisePropertyChangedEvent("InstanceCount");
        }

        #region PrivateStuff

        private static void RemoveOperatorFromInstance(Guid opIdToRemove, Operator opInstance)
        {
            var opToRemove = opInstance.InternalOps.Single(op => op.ID == opIdToRemove);
            opToRemove.Definition.RemoveInstance(opToRemove);
            opInstance.RemoveOperatorInternal(opToRemove, disposeOpResources: true);
        }

        private void RemoveAllConnectionsFromOp(Guid opId)
        {
            Func<MetaConnection, bool> connectionPredicate = (c) => { return c.SourceOpID == opId; };
            RemoveConnections(connectionPredicate);
        }

        private void RemoveAllConnectionsToOp(Guid opId)
        {
            Func<MetaConnection, bool> connectionPredicate = (c) => { return c.TargetOpID == opId; };
            RemoveConnections(connectionPredicate);
        }

        private void RemoveAllConnectionsFromInput(Guid inputId)
        {
            Func<MetaConnection, bool> connectionPredicate = (c) => { return c.SourceOpID == Guid.Empty && c.SourceOpPartID == inputId; };
            RemoveConnections(connectionPredicate);
        }

        private void RemoveAllConnectionsToOutput(Guid outputId)
        {
            Func<MetaConnection, bool> connectionPredicate = (c) => { return c.TargetOpID == Guid.Empty && c.TargetOpPartID == outputId; };
            RemoveConnections(connectionPredicate);
        }

        private void RemoveConnections(Func<MetaConnection, bool> connectionPredicate)
        {
            int multiInputIdx = 0;
            Guid currentOpId = Guid.Empty;
            Guid currentOpPartId = Guid.Empty;
            var connectionsToRemove = new List<Tuple<MetaConnection, int>>();
            foreach (var connection in Connections)
            {
                if (currentOpId == connection.TargetOpID && currentOpPartId == connection.TargetOpPartID)
                    multiInputIdx += 1;
                else
                {
                    currentOpId = connection.TargetOpID;
                    currentOpPartId = connection.TargetOpPartID;
                    multiInputIdx = 0;
                }

                if (connectionPredicate(connection))
                {
                    connectionsToRemove.Add(Tuple.Create(connection, multiInputIdx));
                }
            }

            connectionsToRemove.Reverse(); // delete back to front
            foreach (var connectionEntry in connectionsToRemove)
            {
                RemoveConnection(connectionEntry.Item1, connectionEntry.Item2);
            }
        }

        private void HandleScriptChange(object sender, EventArgs e)
        {
            var metaOpPart = (MetaOperatorPart) sender;
            foreach (var instance in _instances)
            {
                var opParts = from mopPart in OperatorParts
                              where mopPart.Item2 == metaOpPart
                              from opPart in instance.InternalParts
                              where opPart.ID == mopPart.Item1
                              select opPart;
                foreach (var opPartInstance in opParts)
                    opPartInstance.Func = metaOpPart.FunctionCreator();
            }
        }

        private List<Operator> _instances = new List<Operator>();
        private List<Tuple<Guid, MetaOperatorPart>> _operatorParts;
        #endregion

    }

}
