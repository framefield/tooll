// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Framefield.Core
{

    public enum FunctionType
    {
        Generic = 0,
        Float,
        Text,
        Scene,
        Image,
        Dynamic,
        Mesh
    }

    public interface IValue
    {
        FunctionType Type { get; }
        OperatorPartContext SetValueInContext(OperatorPartContext context);
        void SetValueFromContext(OperatorPartContext context);

        [JsonIgnore]
        bool Cacheable { get; }

        IValue Clone();
    }

    public abstract class Value<T>
    {
        public T Val { get; set; }

        public Value(T value)
        {
            Val = value;
        }

        public override string ToString()
        {
            return Val.ToString();
        }
    }

    public class Float : Value<float>, IValue
    {
        public Float(float f) : base(f)
        {
        }

        public FunctionType Type { get { return FunctionType.Float; } }

        public OperatorPartContext SetValueInContext(OperatorPartContext context)
        {
            context.Value = Val;
            return context;
        }

        public void SetValueFromContext(OperatorPartContext context)
        {
            Val = context.Value;
        }

        public bool Cacheable { get { return true; } }

        public IValue Clone()
        {
            return new Float(Val);
        }
    }

    public class Text : Value<string>, IValue
    {
        public Text(string s) : base(s)
        {
        }

        public FunctionType Type { get { return FunctionType.Text; } }

        public OperatorPartContext SetValueInContext(OperatorPartContext context)
        {
            context.Text = Val;
            return context;
        }

        public void SetValueFromContext(OperatorPartContext context)
        {
            Val = context.Text;
        }

        public bool Cacheable { get { return true; } }

        public IValue Clone()
        {
            return new Text(Val);
        }
    }

    public class Image : IValue
    {
        public FunctionType Type { get { return FunctionType.Image; } }

        public OperatorPartContext SetValueInContext(OperatorPartContext context)
        {
            return context;
        }

        public void SetValueFromContext(OperatorPartContext context)
        {
        }

        public bool Cacheable { get { return false; } }

        public IValue Clone()
        {
            return new Image();
        }
    }

    public class Scene : IValue
    {
        public FunctionType Type { get { return FunctionType.Scene; } }

        public OperatorPartContext SetValueInContext(OperatorPartContext context)
        {
            return context;
        }

        public void SetValueFromContext(OperatorPartContext context)
        {
        }

        public bool Cacheable { get { return false; } }

        public IValue Clone()
        {
            return new Scene();
        }
    }

    public class Generic : IValue
    {
        public FunctionType Type { get { return FunctionType.Generic; } }

        public OperatorPartContext SetValueInContext(OperatorPartContext context)
        {
            return context;
        }

        public void SetValueFromContext(OperatorPartContext context)
        {
        }

        public bool Cacheable { get { return false; } }

        public IValue Clone()
        {
            return new Generic();
        }
    }

    public class Dynamic : IValue
    {
        public FunctionType Type { get { return FunctionType.Dynamic; } }

        public OperatorPartContext SetValueInContext(OperatorPartContext context)
        {
            return context;
        }

        public void SetValueFromContext(OperatorPartContext context)
        {
        }

        public bool Cacheable { get { return false; } }

        public IValue Clone()
        {
            return new Dynamic();
        }
    }

    public class MeshValue : IValue
    {
        public FunctionType Type { get { return FunctionType.Mesh; } }

        public OperatorPartContext SetValueInContext(OperatorPartContext context)
        {
            return context;
        }

        public void SetValueFromContext(OperatorPartContext context)
        {
        }

        public bool Cacheable { get { return false; } }
        
        public IValue Clone()
        {
            return new MeshValue();
        }
    }


    public static class ValueUtilities
    {
        public static IValue CreateValue(string type, string value)
        {
            switch (type)
            {
                case "Float":
                    var floatValue = 0.0f;
                    float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue);
                    return new Float(floatValue);
                case "Text":
                    return new Text(value);
                case "Image":
                    return new Image();
                case "Scene":
                    return new Scene();
                case "Generic":
                    return new Generic();
                case "Dynamic":
                    return new Dynamic();
                case "Mesh":
                    return new MeshValue();
            }
            throw new Exception("Unknown value type found");
        }

        public static string GetValueForTypeFromContext(FunctionType type, OperatorPartContext context)
        {
            switch (type)
            {
                case FunctionType.Float:
                    return context.Value.ToString();
                case FunctionType.Text:
                    return context.Text;
                default:
                    return String.Empty;
            }
        }
    }

}
