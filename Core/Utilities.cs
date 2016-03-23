// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using SharpDX;

namespace Framefield.Core
{

    public static class Utilities
    {
        public class ValueFunction : OperatorPart.Function
        {
            public event EventHandler<EventArgs> EvaluatedEvent;

            // a value function has no parent because multiple operatorparts can share one instance
            public override OperatorPart OperatorPart { get { return null; } set { } }

            public IValue Value
            {
                get { return _value; }
                set {
                    _value = value;
                    TriggerChangedEvent(EventArgs.Empty);
                }
            }

            public override OperatorPart.Function Clone()
            {
                var newFunc = CreateValueFunction(Value);
                newFunc.OperatorPart = OperatorPart;
                return newFunc;
            }

            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                if (!Value.Cacheable || Changed)
                {
                    if (inputs.Count > 0)
                    {
                        //regardless what we get as outputidx, we need to pass our evaluationidx for evaluating our input
                        inputs[0].Eval(context, EvaluationIndex);
                        Value.SetValueFromContext(context);
                    }
                    else
                        Value.SetValueInContext(context);

                    Changed = false;
                }
                else
                {
                    Value.SetValueInContext(context);
                }

                if (EvaluatedEvent != null)
                    EvaluatedEvent(this, EventArgs.Empty);

                return context;
            }

            private IValue _value;
        }

        public class DefaultValueFunction : ValueFunction
        {
            public override OperatorPart.Function Clone()
            {
                //only one instance exists of a default value function
                return this;
            }

            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                Value.SetValueInContext(context);
                Changed = false;
                return context;
            }
        }


        public static OperatorPart.Function CreateValueFunction(IValue value)
        {
            var valueFunction = new ValueFunction();
            valueFunction.Value = value.Clone();
            return valueFunction;
        }

        public static DefaultValueFunction CreateDefaultValueFunction(IValue value)
        {
            var valueFunction = new DefaultValueFunction();
            valueFunction.Value = value.Clone();
            return valueFunction;
        }

        public static OperatorPart CreateValueOpPart(Guid id, ValueFunction defaultFunction, bool isMultiInput)
        {
            return new OperatorPart(id, defaultFunction) { Type = defaultFunction.Value.Type, IsMultiInput = isMultiInput };
        }

        public static Operator CreateEmptyOperator()
        {
            var metaOp = new MetaOperator(Guid.NewGuid()) { Name = "Empty" };
            return metaOp.CreateOperator(Guid.NewGuid());
        }

        public static void RemoveAllEventHandlerFrom<T>(T instance) where T : class
        {
            var type = instance.GetType();
            foreach (var eventInfo in type.GetEvents())
            {
                var eventFieldInfo = type.GetField(eventInfo.Name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
                if (eventFieldInfo == null)
                    continue;

                var eventFieldValue = (Delegate) eventFieldInfo.GetValue(instance);
                if (eventFieldValue == null)
                    continue;

                var invocationList = eventFieldValue.GetInvocationList();
                for (int i = 0; i < invocationList.Count(); ++i)
                {
                    eventInfo.RemoveEventHandler(instance, eventFieldValue);
                }
            }
        }

        public static IEnumerable<MethodInfo> GetSubscribedMethods<T>(T instance) where T : class
        {
            return from eventInfo in instance.GetType().GetEvents()
                   let eventFieldInfo = instance.GetType().GetField(eventInfo.Name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField)
                   let eventFieldValue = (Delegate)eventFieldInfo.GetValue(instance)
                   from subscribedDelegate in eventFieldValue.GetInvocationList()
                   select subscribedDelegate.Method;
        }

        public static IEnumerable<Tuple<Assembly, Type[]>> GetAssembliesAndTypesOfCurrentDomain()
        {
            var asmAndTypes = new List<Tuple<Assembly, Type[]>>();

            try
            {
                var currentDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in currentDomainAssemblies)
                {
                    try
                    {
                        asmAndTypes.Add(Tuple.Create(asm, asm.GetTypes()));
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        Logger.Debug("Could not load assembly {0} in order to get its types.", asm.FullName);
                    }
                }
            }
            catch (AppDomainUnloadedException exception)
            {
                Logger.Error("Error getting assemblies of current domain: {0} - {1}", exception.Message, exception.InnerException);
            }

            return asmAndTypes;
        }

        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static bool IsEqual(float lhs, float rhs, float epsilon = 0.001f)
        {
            return Math.Abs(lhs - rhs) < epsilon;
        }

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            return (val.CompareTo(min) < 0) ? min
                                            : (val.CompareTo(max) > 0) ? max
                                                                       : val;
        }

        public static void MemSet<T>(T[] array, T value)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = value;
            }
        }

        public static float SmoothStep(float t)
        {
            return t*t*t*(t*(t*6 - 15) + 10);
        }

        public static double getAngleDifference(double a1, double a2)
        {
            var d = Math.Abs(a1 - a2);
            return d > 180 ? 360 - d : d;
        }

        // t within [0, 1]
        public static float Lerp(float a, float b, float t)
        {
            return a*(1.0f - t) + b*t;
        }

        public static string CharAtToUpper(string text, int index)
        {
            return CharAtTo(text, index, t => t.ToUpper());
        }

        public static string CharAtToLower(string text, int index)
        {
            return CharAtTo(text, index, t => t.ToLower());
        }

        private static string CharAtTo(string text, int index, Func<string, string> charManipulator)
        {
            if ((index < 0) || (index >= text.Length))
                return text;

            var nextChar = text.Substring(index, 1);
            text = text.Remove(index, 1);

            return text.Insert(index, charManipulator(nextChar));
        }

        public static T FromString<T>(string text)
        {
            return (T) TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(text);
        }

        public static double RadToDegree(double rad)
        {
            return rad*180.0/Math.PI;
        }

        public static double DegreeToRad(double degree)
        {
            return degree*Math.PI/180.0;
        }

        public static void DisposeObj<T>(ref T obj) where T : class, IDisposable
        {
            if (obj != null)
            {
                obj.Dispose();
                obj = null;
            }
        }

        public static void CollectAllMetaOperators(this OperatorPart operatorPart, HashSet<MetaOperator> collectedMetaOperators)
        {
            var collectedOperators = new HashSet<Operator>();
            CollectAllOperators(operatorPart, collectedOperators);
            foreach (var op in collectedOperators)
            {
                collectedMetaOperators.Add(op.Definition);
            }
        }

        public static void CollectAllOperators(this OperatorPart operatorPart, HashSet<Operator> collectedOperators)
        {
            var op = operatorPart.Parent;
            if (!collectedOperators.Contains(op))
            {
                // new op, add 
                collectedOperators.Add(op);
                var outputsToTraverse = from internalOp in op.InternalOps
                                        from output in internalOp.Outputs
                                        select output;
                foreach (var output in outputsToTraverse)
                {
                    output.CollectAllOperators(collectedOperators);
                }
            }

            operatorPart.Connections.ForEach(opPart => opPart.CollectAllOperators(collectedOperators));
        }

        public static void CopyDirectory(string sourcePath, string destPath, string searchPattern)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            foreach (string file in Directory.GetFiles(sourcePath, searchPattern))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(file));
                File.Copy(file, dest);
            }

            foreach (string folder in Directory.GetDirectories(sourcePath, searchPattern))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(folder));
                CopyDirectory(folder, dest, searchPattern);
            }
        }

        public static Vector3 ToVector3(this Vector4 vec)
        {
            return new Vector3(vec.X/vec.W, vec.Y/vec.W, vec.Z/vec.W);
        }

        public static Vector2 EvaluateVector2(OperatorPartContext context, List<OperatorPart> inputs, int startIdx)
        {
            return new Vector2(inputs[startIdx].Eval(context).Value,
                               inputs[startIdx + 1].Eval(context).Value);
        }

        public static Vector3 EvaluateVector3(OperatorPartContext context, List<OperatorPart> inputs, int startIdx)
        {
            return new Vector3(inputs[startIdx].Eval(context).Value,
                               inputs[startIdx + 1].Eval(context).Value,
                               inputs[startIdx + 2].Eval(context).Value);
        }

        public static Vector4 EvaluateVector4(OperatorPartContext context, List<OperatorPart> inputs, int startIdx)
        {
            return new Vector4(inputs[startIdx].Eval(context).Value,
                               inputs[startIdx + 1].Eval(context).Value,
                               inputs[startIdx + 2].Eval(context).Value,
                               inputs[startIdx + 3].Eval(context).Value);
        }

        public static string RemoveSpecialCharacters(string str, char replaceChar = ' ')
        {
            var sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') | c == '.' || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(replaceChar);
                }
            }
            return sb.ToString();
        }

        // Returns titles like "Op" -> "Op (2)" -> "Op (3)"
        public static String GetDuplicatedTitle(String name)
        {
            Regex rgx = new Regex(@"(.+) \((\d+)\)");
            MatchCollection matches = rgx.Matches(name);
            if (matches.Count == 1)
            {
                int orgId = Convert.ToInt32(matches[0].Groups[2].Value);
                return String.Format("{0} ({1})", matches[0].Groups[1].Value, orgId + 1);
            }
            else
            {
                return name + " (2)";
            }
        }

        internal static string AdjustOpPartNameForCode(string inputName)
        {
            inputName = inputName.Replace(" ", "");
            int groupSeparatorIndex = inputName.LastIndexOf('.');
            if (groupSeparatorIndex >= 0)
            {
                inputName = inputName.Remove(groupSeparatorIndex, 1);
                inputName = CharAtToUpper(inputName, groupSeparatorIndex);
            }
            return inputName;
        }

        public static string GetCompleteVersionString()
        {
            return String.Format("T2 - {0}.{1} ({2}, {3})", Constants.VersionAsString, BuildProperties.Build,
                                 BuildProperties.Branch, BuildProperties.CommitShort);
        }
    }

    public static class CrashReporter
    {
        public static String GetFormattedStackTrace(Exception ex)
        {
            String s = ex.GetType() + "\n" + new String('-', ex.GetType().ToString().Length) + "\n" + ex.Message + "\n\n";

            foreach (var line in ex.StackTrace.Split('\n'))
            {
                //Original stack trace line looks like this:
                // at Framefield.Tooll.ConnectionLine.CreateLineGeometry() in c:\self.demos\tooll2\Tooll\ConnectionLine.cs:line 303
                Match m = Regex.Match(line, @"\s+at\s+(.*)\s+in\s+(.*):line\s+(\d+).*");
                if (m.Success)
                {
                    String function = m.Groups[1].ToString();
                    String filename = m.Groups[2].ToString().Split('\\').Last().PadLeft(27);
                    String lineNumber = m.Groups[3].ToString().PadLeft(4);
                    s += filename + " " + lineNumber + "  " + function + "\n";
                }
                else
                {
                    s += line.Replace(" at ", "".PadLeft(40 - 8));
                }
            }
            return s;
        }

        public static void WriteCrashReport(System.Exception ex)
        {
            String titleDate = DateTime.Now.ToString("yyyy'-'MM'-'dd HH'-'mm'-'ss");
            String messageTitle = Utilities.RemoveSpecialCharacters(ex.Message);
            Directory.CreateDirectory(@"Log/CrashReports");
            using (var writer = new StreamWriter("Log/CrashReports/Crash " + titleDate + " - " + messageTitle.Substring(0, Math.Min(30, ex.Message.Length)) + ".txt"))
            {
                writer.Write(ComposeCrashReport(ex));
            }
        }

        public static String ComposeCrashReport(System.Exception ex)
        {
            String buffer = "";
            buffer += "Date:".PadRight(15) + DateTime.Now + "\n";
            buffer += "Version:".PadRight(15) + Constants.VersionAsString + '\n';
            buffer += "\n";
            if (ex.InnerException != null)
            {
                buffer += GetFormattedStackTrace(ex.InnerException) + "\n";
            }
            buffer += "\n";
            buffer += GetFormattedStackTrace(ex) + "\n";
            return buffer;
        }


        public static String GetGitLog()
        {
            string output;
            try
            {
                // Start the child process.
                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.Arguments = "log --stat --summary  -3 --no-color";
                p.StartInfo.FileName = "c:\\Program Files (x86)\\Git\\bin\\git.exe";
                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            catch
            {
                output = "Git status not available";
            }
            return output;
        }
    }


    public struct PropertyStasher<T> : IDisposable
    {
        public PropertyStasher(T obj, params string[] args)
        {
            _object = obj;
            _type = typeof(T);
            _propertyStack = new Stack<Tuple<string, Object>>();
            foreach (var propName in args)
            {
                PushProperty(propName);
            }
        }

        public void Dispose()
        {
            int count = _propertyStack.Count;
            for (int i = 0; i < count; ++i)
            {
                PopProperty();
            }
        }

        private T _object;
        private Type _type;
        private Stack<Tuple<string, Object>> _propertyStack;

        private void PushProperty(string propertyName)
        {
            var propInfo = _type.GetProperty(propertyName);
            _propertyStack.Push(Tuple.Create(propertyName, propInfo.GetValue(_object, null)));
        }

        private void PopProperty()
        {
            var nameAndValue = _propertyStack.Pop();
            var propInfo = _type.GetProperty(nameAndValue.Item1);
            propInfo.SetValue(_object, nameAndValue.Item2, null);
        }
    }


    public class SmoothInterpolator
    {
        public SmoothInterpolator(double defaultValue = 0.0, double acceleration = 200.0, double delta = Double.NaN)
        {
            _value = defaultValue;
            Acceleration = acceleration;
            Precision = Double.IsNaN(delta) ? acceleration/100000.0 : delta;
        }

        private const double MAX_TIME_FRAGMENT = 0.1;

        public double Acceleration { get; set; }
        public double Precision { get; set; }
        private double _value = 0;
        private double _valueEnd;
        private bool _running = true;
        private double _time = 0;
        private double _lastTime = 0.0;
        private double _speed = 0.0;
        private double _min = Double.NaN;
        private double _max = Double.NaN;
        private double _maxSpeed = Double.PositiveInfinity;
        private double _borderFriction = 0.5;


        public double GetValue(double time)
        {
            if (_lastTime == time)
                return _value;
            _lastTime = time;


            // ignore first call
            if (_time == 0)
            {
                _time = time;
                return _value;
            }

            double timeFragment = time - _time;

            timeFragment = Math.Max(-MAX_TIME_FRAGMENT, Math.Min(timeFragment, MAX_TIME_FRAGMENT));
            //if (timeFragment > MAX_TIME_FRAGMENT)
            //    timeFragment = MAX_TIME_FRAGMENT;


            _time = time;

            if (!_running)
                return _value;

            // calculate optimal speed
            double distanceBrake = _speed*_speed/Acceleration;
            double distance = _valueEnd - _value;

            if (Math.Abs(distance) < Precision)
            {
                // and abs(_speed) * _acceleration * 2.4  < _delta:
                _speed = 0.0;
                _value = _valueEnd;
                _running = false;
            }
            else if (distance < 0)
            {
                // wrong direction 
                if (_speed > 0)
                {
                    _speed -= Acceleration*timeFragment;
                }
                    // accelerate neg 
                else if (Math.Abs(distance) > distanceBrake)
                {
                    if (Math.Abs(_speed) < _maxSpeed)
                    {
                        _speed = Utilities.Clamp(_speed - Acceleration*timeFragment*0.8, -_maxSpeed, 0.0);
                    }
                    else
                    {
                        _speed *= 0.99;
                    }
                }

                    //
                else
                {
                    if (Math.Abs(distance) < Precision*5.0 && Math.Abs(Acceleration) < Precision)
                    {
                        Acceleration *= 1.0 - (Math.Abs(distance) - Precision*5.0)/50.0;
                    }
                    _speed += Acceleration*timeFragment;
                }
            }
            else if (distance > 0)
            {
                // wrong direction
                if (_speed < 0)
                {
                    _speed += Acceleration*timeFragment;
                }

                    // accelerate neg
                else if (Math.Abs(distance) > distanceBrake)
                {
                    if (Math.Abs(_speed) < _maxSpeed)
                    {
                        _speed = Utilities.Clamp(_speed + Acceleration*timeFragment*0.8, 0.0, _maxSpeed);
                    }
                    else
                    {
                        _speed *= 0.99;
                    }
                }
                    // deccalerate neg
                else
                {
                    if (Math.Abs(distance) < Precision*5.0 && Math.Abs(Acceleration) < Precision)
                        Acceleration *= 1.0 - (Math.Abs(distance) - Precision*5.0)/50.0;

                    _speed -= Acceleration*timeFragment;
                }
            }

            //if _debug:
            //        note("value=%4.3f\tspeed=%4.3f\tdistance=%4.3f\tbrake=%4.3f\tdt=%4.3f\tvalueEnd=%4.3f  maxSpeed=%4.3f" % (_value, _speed, distance, distanceBrake, timeFragment, _valueEnd, _maxSpeed))
            _value += _speed*timeFragment;

            if (!Double.IsNaN(_min) && _value < _min)
            {
                _speed *= -_borderFriction;
                _value = _min + _borderFriction*(_value - _min);
                if (_valueEnd > _min)
                    _valueEnd = _min;
            }

            if (!Double.IsNaN(_max) && _value > _max)
            {
                _speed *= -_borderFriction;
                _value = _max - _borderFriction*(_value - _max);
                if (_valueEnd < _max)
                    _valueEnd = _max;
            }
            _lastTime = time;
            return _value;
        }


        /**
         * set to fixed value and stop animation
         */

        public void SetValue(double value)
        {
            _value = _valueEnd = value;
            _speed = 0.0;
        }

        public void SetAcceleration(double acceleration, double delta = Double.NaN)
        {
            Acceleration = acceleration;
            if (Double.IsNaN(delta))
            {
                Precision = acceleration/5000.0;
            }
            else
            {
                Precision = delta;
            }
        }

        /// <summary>
        /// set to fixed value and stop animation
        /// </summary>
        /// <param name="value"></param>
        /// <param name="time"></param>
        /// <param name="speed"></param>
        /// <param name="maxSpeed"></param>
        public void AnimateTo(double value, double time = Double.NaN, double speed = double.NaN, double maxSpeed = Double.PositiveInfinity)
        {
            _maxSpeed = maxSpeed;

            if (!Double.IsNaN(time))
                _time = time;
            _running = true;
            _valueEnd = value;
            if (!Double.IsNaN(speed))
                _speed = speed;
        }

        /// <summary>
        /// offset the target value (e.g. for scrolling for a fixed distance)
        /// </summary>
        /// <param name="?"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public void OffsetTo(double value)
        {
            _running = true;
            _valueEnd += value;
        }

        /// <summary>
        /// Jumps Value during animation (e.g. for jump-cuts)
        /// </summary>
        /// <param name="offset"></param>
        public void Offset(double offset)
        {
            _value += offset;
            _running = true;
        }

        public bool IsRunning(double time)
        {
            var tmp = GetValue(time);
            return _running;
        }

        /// <summary>
        /// Use None to disable boundary.
        /// </summary>
        /// <param name="?"></param>
        /// <param name="?"></param>
        /// <param name="?"></param>
        public void UseReflectBoundaries(double min, double max)
        {
            _min = min;
            if (!Double.IsNaN(max) && !Double.IsNaN(min) && max < min)
                max = min;
            _max = max;
        }
    }

}
