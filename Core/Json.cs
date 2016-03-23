// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;


namespace Framefield.Core
{
    public static class JsonExtensions
    {
        public static void WriteValue<T>(this JsonTextWriter Writer, string name, T value)
        {
            Writer.WritePropertyName(name);
            Writer.WriteValue(value.ToString());
        }

        public static void WriteValue(this JsonTextWriter Writer, string name, float value)
        {
            Writer.WritePropertyName(name);
            // NOTE: Without invariant culture setting, double values can be written with "," instead of ".",
            // and thus lead to corrupted .top files.
            Writer.WriteValue(value.ToString("G", CultureInfo.InvariantCulture));
        }

        public static void WriteValue(this JsonTextWriter Writer, string name, double value)
        {
            Writer.WritePropertyName(name);
            // NOTE: Without invariant culture setting, double values can be written with "," instead of ".",
            // and thus lead to corrupted .top files.
            Writer.WriteValue(value.ToString("G", CultureInfo.InvariantCulture));
        }

        public static void WriteValue(this JsonTextWriter Writer, string name, IValue value)
        {
            Writer.WritePropertyName(name);
            Writer.WriteStartObject();
            Writer.WriteValue("Type", value.Type.ToString());
            Writer.WriteValue("Value", value.ToString());
            Writer.WriteEndObject();
        }
    }

    internal class Json
    {
        public JsonTextWriter Writer { get; set; }
        public JsonTextReader Reader { get; set; }

        #region write instance stuff

        public void WriteOperator(Operator op)
        {
            Writer.WriteStartObject();

            Writer.WriteValue("Name", op.Name);
            Writer.WriteValue("ID", op.ID);

            Writer.WritePropertyName("Inputs");
            Writer.WriteStartArray();
            op.Inputs.ForEach(opPart => WriteInput(opPart));
            Writer.WriteEndArray();

            Writer.WriteEndObject();
        }

        public void WriteInput(OperatorPart opPart)
        {
            Writer.WriteStartObject();

            Writer.WriteValue("Name", opPart.Name);
            Writer.WriteValue("ID", opPart.ID);
            Writer.WriteValue("Type", opPart.Type);
            Writer.WriteValue("Value", ValueUtilities.GetValueForTypeFromContext(opPart.Type, opPart.Eval(new OperatorPartContext())));

            Writer.WriteEndObject();
        }

        #endregion

        #region write meta stuff

        public void WriteMetaOperator(MetaOperator metaOp)
        {
            Writer.WriteStartObject();

            Writer.WriteValue("Name", metaOp.Name);
            Writer.WriteValue("_id", metaOp.ID);
            if (metaOp.Revision != string.Empty)
                Writer.WriteValue("_rev", metaOp.Revision);
            Writer.WriteValue("Namespace", metaOp.Namespace);
            Writer.WriteValue("Description", metaOp.Description);

            WriteInputs(metaOp.Inputs);
            WriteOutputs(metaOp.Outputs);
            WriteMetaOperatorParts(metaOp.OperatorParts);
            WriteMetaOperators(metaOp, metaOp.Operators);
            WriteMetaConnections(metaOp.Connections);

            Writer.WriteEndObject();
        }

        private void WriteMetaConnections(List<MetaConnection> connections)
        {
            Writer.WritePropertyName("Connections");
            Writer.WriteStartArray();
            connections.ForEach(connection =>
                                {
                                    Writer.WriteStartObject();
                                    Writer.WriteValue("SourceOp", connection.SourceOpID);
                                    Writer.WriteValue("SourceOpPart", connection.SourceOpPartID);
                                    Writer.WriteValue("TargetOp", connection.TargetOpID);
                                    Writer.WriteValue("TargetOpPart", connection.TargetOpPartID);
                                    Writer.WriteEndObject();
                                });
            Writer.WriteEndArray();
        }

        private void WriteMetaOperators(MetaOperator parentOp, Dictionary<Guid, Tuple<MetaOperator, MetaOperator.InstanceProperties>> operators)
        {
            Writer.WritePropertyName("Operators");
            Writer.WriteStartArray();
            foreach (var IdAndMetaOp in operators)
            {
                Writer.WriteStartObject();
                Writer.WriteValue("Name", IdAndMetaOp.Value.Item1.Name);
                Writer.WriteValue("MetaInstanceID", IdAndMetaOp.Key);
                Writer.WriteValue("MetaID", IdAndMetaOp.Value.Item1.ID);
                Writer.WritePropertyName("OperatorProperties");
                WriteOperatorInstanceProperties(parentOp, IdAndMetaOp.Key, IdAndMetaOp.Value.Item1, IdAndMetaOp.Value.Item2);
                Writer.WriteEndObject();
            }
            ;
            Writer.WriteEndArray();
        }

        private void WriteOperatorInstanceProperties(MetaOperator parentOp, Guid metaOpInstanceID, MetaOperator metaOp, MetaOperator.InstanceProperties opProperties)
        {
            Writer.WriteStartObject();

            Writer.WriteValue("Name", opProperties.Name);
            Writer.WriteValue("PosX", opProperties.Position.X);
            Writer.WriteValue("PosY", opProperties.Position.Y);
            Writer.WriteValue("Width", opProperties.Width);
            Writer.WriteValue("Visible", opProperties.Visible);
            Writer.WriteValue("Disabled", opProperties.Disabled);

            Writer.WritePropertyName("Inputs");
            Writer.WriteStartArray();
            foreach (var inputValue in opProperties.InputValues)
            {
                var inputID = inputValue.Key;
                var inputValueFunc = inputValue.Value;

                var metaInput = (from input in metaOp.Inputs
                                 where input.ID == inputValue.Key
                                 select input).SingleOrDefault();

                bool isDefault = ((metaInput != null) && (inputValueFunc == metaInput.DefaultFunc)) ||
                                 parentOp.ExistsConnectionToInput(metaOpInstanceID, metaInput.ID);

                if (!isDefault)
                {
                    Writer.WriteStartObject();
                    Writer.WriteValue("ID", inputID);
                    var inputValueValue = (inputValueFunc as Utilities.ValueFunction).Value;
                    var inputValueType = inputValueValue.Type;

                    Writer.WriteValue("Type", inputValueType);

                    //write value multi lined
                    var value = ValueUtilities.GetValueForTypeFromContext(inputValueType, inputValueValue.SetValueInContext(new OperatorPartContext()));
                    var splittedValue = value.Split(new string[] { "\n" }, StringSplitOptions.None);
                    Writer.WritePropertyName("Value");
                    Writer.WriteStartArray();
                    foreach (var valueLine in splittedValue)
                        Writer.WriteValue(valueLine);
                    Writer.WriteEndArray();
                    Writer.WriteEndObject();
                }
            }
            Writer.WriteEndArray();

            Writer.WritePropertyName("States");
            Writer.WriteStartArray();
            foreach (var stateEntry in opProperties.OperatorPartStates)
            {
                Writer.WriteStartObject();
                Writer.WriteValue("ID", stateEntry.Key);
                Writer.WritePropertyName("State");
                var s = new JsonSerializer();
                s.Serialize(Writer, stateEntry.Value);
                Writer.WriteEndObject();
            }
            Writer.WriteEndArray();

            Writer.WriteEndObject();
        }

        private void WriteMetaOperatorParts(List<Tuple<Guid, MetaOperatorPart>> operatorParts)
        {
            Writer.WritePropertyName("OperatorParts");
            Writer.WriteStartArray();
            operatorParts.ForEach(IdAndMetaOpPart =>
                                  {
                                      Writer.WriteStartObject();
                                      Writer.WriteValue("MetaInstanceID", IdAndMetaOpPart.Item1);
                                      Writer.WriteValue("MetaID", IdAndMetaOpPart.Item2.ID);
                                      Writer.WriteValue("Name", IdAndMetaOpPart.Item2.Name);
                                      Writer.WriteValue("Version", IdAndMetaOpPart.Item2.Version);
                                      Writer.WriteValue("Type", IdAndMetaOpPart.Item2.Type);
                                      Writer.WriteValue("IsMultiInput", IdAndMetaOpPart.Item2.IsMultiInput);

                                      // write script
                                      var code = IdAndMetaOpPart.Item2.Script;

                                      var splittedScript = code.Split(new string[] { "\n" }, StringSplitOptions.None);
                                      Writer.WritePropertyName("Script");
                                      Writer.WriteStartArray();
                                      foreach (var scriptLine in splittedScript)
                                          Writer.WriteValue(scriptLine);
                                      Writer.WriteEndArray();

                                      Writer.WritePropertyName("AdditionalAssemblies");
                                      Writer.WriteStartArray();
                                      foreach (var assembly in IdAndMetaOpPart.Item2.AdditionalAssemblies)
                                          Writer.WriteValue(assembly);
                                      Writer.WriteEndArray();

                                      Writer.WriteEndObject();
                                  });
            Writer.WriteEndArray();
        }

        private void WriteOutputs(List<MetaOutput> outputs)
        {
            Writer.WritePropertyName("Outputs");
            Writer.WriteStartArray();
            outputs.ForEach(metaOutput =>
                            {
                                Writer.WriteStartObject();
                                Writer.WriteValue("Name", metaOutput.Name);
                                Writer.WriteValue("MetaInstanceID", metaOutput.ID);
                                Writer.WriteValue("MetaID", metaOutput.OpPart.ID);
                                Writer.WriteEndObject();
                            });
            Writer.WriteEndArray();
        }

        private void WriteDefaultValue(IValue defaultValue)
        {
            Writer.WritePropertyName("DefaultValue");
            Writer.WriteStartObject();
            Writer.WriteValue("Type", defaultValue.Type.ToString());

            var valueToWrite = defaultValue.ToString();
            var splittedValue = valueToWrite.Split(new string[] { "\n" }, StringSplitOptions.None);
            if (splittedValue.Count() > 1)
            {
                // multi line string -> write as array
                Writer.WritePropertyName("Value");
                Writer.WriteStartArray();
                foreach (var valueLine in splittedValue)
                    Writer.WriteValue(valueLine);
                Writer.WriteEndArray();
            }
            else
            {
                Writer.WriteValue("Value", valueToWrite);
            }

            Writer.WriteEndObject();
        }

        private void WriteInputs(List<MetaInput> inputs)
        {
            Writer.WritePropertyName("Inputs");
            Writer.WriteStartArray();
            inputs.ForEach(metaInput =>
                           {
                               Writer.WriteStartObject();
                               Writer.WriteValue("Name", metaInput.Name);
                               Writer.WriteValue("MetaInstanceID", metaInput.ID);
                               WriteDefaultValue(metaInput.DefaultValue);
                               Writer.WriteValue("MetaID", metaInput.OpPart.ID);
                               Writer.WriteValue("IsMultiInput", metaInput.IsMultiInput);
                               Writer.WriteValue("Relevance", metaInput.Relevance);
                               Writer.WriteValue("Description", metaInput.Description);
                               Writer.WriteValue("Min", metaInput.Min);
                               Writer.WriteValue("Max", metaInput.Max);
                               Writer.WriteValue("Scale", metaInput.Scale);
                               Writer.WriteValue("ScaleType", metaInput.ScaleType);
                               Writer.WritePropertyName("EnumValues");
                               Writer.WriteStartArray();
                               metaInput.EnumValues.ForEach(enumEntry =>
                                                            {
                                                                Writer.WriteStartObject();
                                                                Writer.WriteValue("Name", enumEntry.Name);
                                                                Writer.WriteValue("Value", enumEntry.Value);
                                                                Writer.WriteEndObject();
                                                            });
                               Writer.WriteEndArray();
                               Writer.WriteEndObject();
                           });
            Writer.WriteEndArray();
        }

        #endregion

        #region read instance stuff

        public void ReadAndSetOperatorValues(Operator op)
        {
            var o = JObject.ReadFrom(Reader);
            ReadAndSetOperatorValues(op, o);
        }

        public void ReadAndSetOperatorValues(Operator op, JToken jsonOp)
        {
            op.Name = jsonOp["Name"].Value<string>();

            int i = 0;
            foreach (var jsonInput in jsonOp["Inputs"])
            {
                ReadAndSetInputValues(op.Inputs[i++], jsonInput);
            }
        }

        public void ReadAndSetInputValues(OperatorPart opPart, JToken jsonOpPart)
        {
            opPart.Name = jsonOpPart["Name"].Value<string>();
            if (opPart.ID != Guid.Parse(jsonOpPart["ID"].Value<string>()))
                throw new System.Exception("Wrong op part id in file");

            string type = jsonOpPart["Type"].Value<string>();
            string value = jsonOpPart["Value"].Value<string>();
            opPart.Func = Utilities.CreateValueFunction(ValueUtilities.CreateValue(type, value));
        }

        #endregion

        #region read meta stuff

        public MetaOperator ReadMetaOperator(MetaManager metaManager)
        {
            var o = JObject.ReadFrom(Reader);
            var name = o["Name"].Value<string>();
            var id = Guid.Parse(o["_id"].Value<string>());
            var rev = o.Value<string>("_rev");
            if (rev == null)
                rev = string.Empty;
            var namesp = o["Namespace"].Value<string>();
            var description = o["Description"].Value<string>();
            var inputs = (from i in ((JArray) o["Inputs"])
                          let metaInput = BuildMetaInput(metaManager, i)
                          select metaInput).ToList();
            var outputs = (from output in ((JArray) o["Outputs"])
                           let metaOutput = BuildMetaOutput(metaManager, output)
                           select metaOutput).ToList();
            var operators = (from op in ((JArray) o["Operators"])
                             let metaOp = BuildMetaOperator(metaManager, op)
                             select metaOp).ToDictionary(e => { return e.Item1; }, e => { return e.Item2; });
            var connections = (from c in ((JArray) o["Connections"])
                               let metaConnection = BuildMetaConnection(c)
                               select metaConnection).ToList();
            var newMetaOp = new MetaOperator(id)
                                {
                                    Revision = rev,
                                    Name = name,
                                    Inputs = inputs,
                                    Outputs = outputs,
                                    Operators = operators,
                                    Connections = connections,
                                    Description = description,
                                    Namespace = namesp,
                                };

            // the loading order matters here, operator parts potentially need the parent op (newMetaOp) in 
            // order to determine which supplier assembly need to be loaded
            var opParts = (from opPart in ((JArray) o["OperatorParts"])
                           let metaOpPart = BuildMetaOpPart(metaManager, newMetaOp, opPart)
                           select metaOpPart).ToList();
            newMetaOp.OperatorParts = opParts;

            newMetaOp.CheckForInconsistencyAndFixThem();
            newMetaOp.Changed = false; // must be set after inconsistencies have been fixed!
            return newMetaOp;
        }

        private MetaInput BuildMetaInput(MetaManager metaManager, JToken jsonInput)
        {
            var metaInstanceID = Guid.Parse(jsonInput["MetaInstanceID"].Value<string>());
            var name = jsonInput["Name"].Value<string>();
            var metaIDOfOpPart = Guid.Parse(jsonInput["MetaID"].Value<string>());
            var opPart = metaManager.GetMetaOperatorPart(metaIDOfOpPart);

            // read default value - can be multi line value
            var defaultValueString = string.Empty;
            var defaultValueEntry = jsonInput["DefaultValue"]["Value"];
            if (defaultValueEntry is JValue)
            {
                defaultValueString = defaultValueEntry.Value<string>();
            }
            else
            {
                var lines = new List<String>();
                foreach (var valueLine in defaultValueEntry)
                {
                    lines.Add(valueLine.Value<string>());
                }
                defaultValueString = string.Join("\n", lines);
            }
            var defaultValue = ValueUtilities.CreateValue(jsonInput["DefaultValue"]["Type"].Value<string>(), defaultValueString);

            var isMultiInput = jsonInput["IsMultiInput"].Value<bool>();
            var relevance = (MetaInput.RelevanceType) Enum.Parse(typeof(MetaInput.RelevanceType), jsonInput["Relevance"].Value<string>());
            var description = jsonInput["Description"].Value<string>();

            var min = jsonInput["Min"].Value<float>();
            var max = jsonInput["Max"].Value<float>();
            var scale = jsonInput["Scale"].Value<float>();
            var scaleType = (MetaInput.Scaling) Enum.Parse(typeof(MetaInput.Scaling), jsonInput["ScaleType"].Value<string>());

            var enumEntries = (from enumEntry in jsonInput["EnumValues"]
                               let metaOpPart = BuildEnumEntry(enumEntry)
                               select metaOpPart).ToList();

            return new MetaInput(metaInstanceID, name, opPart, defaultValue, isMultiInput)
                       {
                           Relevance = relevance,
                           Description = description,
                           Min = min,
                           Max = max,
                           Scale = scale,
                           ScaleType = scaleType,
                           EnumValues = enumEntries
                       };
        }

        private MetaInput.EnumEntry BuildEnumEntry(JToken jsonEnumEntry)
        {
            var name = jsonEnumEntry["Name"].Value<string>();
            var value = jsonEnumEntry["Value"].Value<int>();
            return new MetaInput.EnumEntry(name, value);
        }

        private MetaOutput BuildMetaOutput(MetaManager metaManager, JToken jsonOutput)
        {
            var metaInstanceID = Guid.Parse(jsonOutput["MetaInstanceID"].Value<string>());
            var name = jsonOutput["Name"].Value<string>();
            var metaIDOfOpPart = Guid.Parse(jsonOutput["MetaID"].Value<string>());
            var opPart = metaManager.GetMetaOperatorPart(metaIDOfOpPart);

            return new MetaOutput(metaInstanceID, name, opPart);
        }

        private Tuple<Guid, MetaOperatorPart> BuildMetaOpPart(MetaManager metaManager, MetaOperator parent, JToken jsonOpPart)
        {
            var metaInstanceID = Guid.Parse(jsonOpPart["MetaInstanceID"].Value<string>());
            var metaIDOfOpPart = Guid.Parse(jsonOpPart["MetaID"].Value<string>());
            var version = Guid.Parse(jsonOpPart["Version"].Value<string>());

            // build script
            var lines = new List<String>();
            foreach (var scriptLine in jsonOpPart["Script"])
            {
                lines.Add(scriptLine.Value<string>());
            }
            var script = string.Join("\n", lines);

            var additionalAssemblies = new List<string>();
            if (jsonOpPart["AdditionalAssemblies"] != null)
            {
                additionalAssemblies.AddRange(jsonOpPart["AdditionalAssemblies"].Select(assembly => assembly.Value<string>()));
            }

            var isMultiInput = jsonOpPart["IsMultiInput"].Value<bool>();
            var name = jsonOpPart["Name"].Value<string>();
            var type = (FunctionType) Enum.Parse(typeof(FunctionType), jsonOpPart["Type"].Value<string>());

            var metaOpPart = new MetaOperatorPart(metaIDOfOpPart)
                                 {
                                     IsMultiInput = isMultiInput,
                                     Type = type,
                                     Name = name,
                                     AdditionalAssemblies = additionalAssemblies,
                                     Parent = parent,
                                     Version = version // set version at last to prevent version updates of property setters above
                                 };
            foreach (var opEntry in parent.Operators)
            {
                var supplierOpPartDefinition = opEntry.Value.Item1.OperatorParts[0].Item2;
                supplierOpPartDefinition.ScriptChangedEvent += metaOpPart.HandleDependencyOperator_ScriptChange;
            }
            metaOpPart.InitScript(script, version);
            metaManager.AddMetaOperatorPart(metaOpPart);

            return Tuple.Create(metaInstanceID, metaOpPart);
        }

        private Tuple<Guid, Tuple<MetaOperator, MetaOperator.InstanceProperties>> BuildMetaOperator(MetaManager metaManager, JToken jsonOp)
        {
            var metaInstanceID = Guid.Parse(jsonOp["MetaInstanceID"].Value<string>());
            var metaOpID = Guid.Parse(jsonOp["MetaID"].Value<string>());
            var metaOp = metaManager.GetMetaOperator(metaOpID);
            if (metaOp == null)
                return Tuple.Create(metaInstanceID, (Tuple<MetaOperator, MetaOperator.InstanceProperties>) null);

            var opProperties = BuildOperatorInstanceProperties(metaManager, metaOp, jsonOp["OperatorProperties"]);
            return Tuple.Create(metaInstanceID, Tuple.Create(metaOp, opProperties));
        }

        private MetaOperator.InstanceProperties BuildOperatorInstanceProperties(MetaManager metaManager, MetaOperator metaOp, JToken jsonOp)
        {
            var opProperties = new MetaOperator.InstanceProperties();

            opProperties.Name = jsonOp["Name"].Value<string>();
            opProperties.Position = new Point(jsonOp["PosX"].Value<double>(), jsonOp["PosY"].Value<double>());
            opProperties.Width = jsonOp["Width"].Value<double>();
            opProperties.Visible = jsonOp["Visible"].Value<bool>();
            if (jsonOp["Disabled"] != null)
            {
                opProperties.Disabled = jsonOp["Disabled"].Value<bool>();
            }

            var foundMetaInputsList = new List<MetaInput>();
            foreach (var jsonInput in jsonOp["Inputs"])
            {
                var id = Guid.Parse(jsonInput["ID"].Value<string>());
                var metaInput = (from input in metaOp.Inputs
                                 where input.ID == id
                                 select input).SingleOrDefault();
                if (metaInput == null)
                {
                    Logger.Debug("Skipped obsolete parameter value {0} in meta operator {1}, because it no longer exists.", id.ToString(), metaOp.Name);
                    continue;
                }

                var valueToken = jsonInput["Type"];
                if (valueToken != null)
                {
                    var type = valueToken.Value<string>();
                    var lines = new List<String>();
                    foreach (var valueLine in jsonInput["Value"])
                    {
                        lines.Add(valueLine.Value<string>());
                    }
                    var value = string.Join("\n", lines);
                    opProperties.InputValues[id] = Utilities.CreateValueFunction(ValueUtilities.CreateValue(type, value));
                }
                else
                {
                    opProperties.InputValues[id] = metaInput.DefaultFunc;
                }

                foundMetaInputsList.Add(metaInput);
            }

            // extract inputs for which no values have been set, e.g. because the input was added later after creating the parent op
            // for these inputs the default func is set
            var inputsWithoutStoredInstanceValues = foundMetaInputsList.Except(metaOp.Inputs);
            foreach (var input in inputsWithoutStoredInstanceValues)
            {
                opProperties.InputValues[input.ID] = input.DefaultFunc;
                Logger.Debug("Added default value for new parameter {0} in Operator {1}", input.Name, input.ID.ToString());
            }

            foreach (var jsonState in jsonOp["States"])
            {
                var opPartId = Guid.Parse(jsonState.Value<string>("ID"));
                var metaOpPart = metaOp.OperatorParts.First(opPartEntry => opPartEntry.Item1 == opPartId).Item2;
                var stateType = metaOpPart.StateType;
                var serializedState = jsonState.Value<JObject>("State");

                var serializer = new JsonSerializer();
                using (var reader = new StringReader(serializedState.ToString()))
                {
                    var state = serializer.Deserialize(reader, stateType) as IOperatorPartState;
                    if (state != null)
                    {
                        opProperties.OperatorPartStates.Add(opPartId, state);
                    }
                }
            }

            return opProperties;
        }

        private MetaConnection BuildMetaConnection(JToken jsonConnection)
        {
            var sourceOp = Guid.Parse(jsonConnection["SourceOp"].Value<string>());
            var sourceOpPart = Guid.Parse(jsonConnection["SourceOpPart"].Value<string>());
            var targetOp = Guid.Parse(jsonConnection["TargetOp"].Value<string>());
            var targetOpPart = Guid.Parse(jsonConnection["TargetOpPart"].Value<string>());

            return new MetaConnection(sourceOp, sourceOpPart, targetOp, targetOpPart);
        }

        #endregion
    }


    public class JsonIValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken t = JObject.ReadFrom(reader);
            string type = ((FunctionType) t["Type"].Value<int>()).ToString();
            string value = t["Val"].Value<string>();
            return ValueUtilities.CreateValue(type, value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, (IValue) value);
        }
    }

}