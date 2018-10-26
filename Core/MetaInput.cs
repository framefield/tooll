// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;

namespace Framefield.Core
{
    public class MetaInput
    {
        public enum RelevanceType
        {
            Required = 0,
            Relevant,
            Optional
        }

        public enum Scaling
        {
            Linear = 0,
            Quadratic,
            Logarithmic
        }

        public class EnumEntry
        {
            public EnumEntry(string name, int value)
            {
                ID = Guid.NewGuid();
                Name = name;
                Value = value;
            }

            public Guid ID { get; private set; }
            public string Name { get; set; }
            public int Value { get; set; }
        }

        public Guid ID { get; set; }
        public string Name { get; set; }
        public MetaOperatorPart OpPart { get; set; }
        public IValue DefaultValue
        {
            get { return ((Utilities.ValueFunction) _defaultFunc).Value; } 
            set { ((Utilities.ValueFunction) _defaultFunc).Value = value.Clone(); }
        }
        private OperatorPart.Function _defaultFunc = null;
        public OperatorPart.Function DefaultFunc { get { return _defaultFunc; } }

        public bool IsMultiInput { get; set; }
        public RelevanceType Relevance { get; set; }
        public string Description { get; set; }

        // float stuff
        public float Min { get; set; }
        public float Max { get; set; }
        public float Scale { get; set; }
        public Scaling ScaleType { get; set; }
        public List<EnumEntry> EnumValues { get; set; }
        public bool IsEnum { get { return EnumValues.Count > 0; } }

        public MetaInput(Guid id, string name, MetaOperatorPart opPart, IValue defaultValue, bool isMultiInput)
        {
            ID = id;
            Name = name;
            OpPart = opPart;
            _defaultFunc = Utilities.CreateDefaultValueFunction(defaultValue);
            IsMultiInput = isMultiInput;
            Relevance = RelevanceType.Optional;
            Description = string.Empty;

            // float stuff
            Min = -100000;
            Max = 100000;
            Scale = 0.1f;
            ScaleType = Scaling.Linear;
            EnumValues = new List<EnumEntry>();
        }

        public MetaInput Clone()
        {
            var input = new MetaInput(Guid.NewGuid(), Name, OpPart, DefaultValue.Clone(), IsMultiInput)
                            {
                                Relevance = Relevance,
                                Description = Description,

                                // float stuff
                                Min = Min,
                                Max = Max,
                                Scale = Scale,
                                ScaleType = ScaleType
                            };

            foreach (var entry in EnumValues)
            {
                input.EnumValues.Add(new EnumEntry(entry.Name, entry.Value));
            }

            if (Name != null)
                input.Name = string.Copy(Name);
            return input;
        }

        public OperatorPart CreateInstance()
        {
            return OpPart.CreateFunc(ID, _defaultFunc, IsMultiInput, Name);
        }
    }

}
