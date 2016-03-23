// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;

namespace Framefield.Core
{
    public class MetaOutput
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public MetaOperatorPart OpPart { get; set; }

        public MetaOutput(Guid id, string name, MetaOperatorPart opPart)
        {
            ID = id;
            Name = name;
            OpPart = opPart;
        }

        public MetaOutput Clone()
        {
            var output = new MetaOutput(Guid.NewGuid(), Name, OpPart);

            if (Name != null)
                output.Name = string.Copy(Name);
            return output;
        }

        public OperatorPart CreateInstance()
        {
            return OpPart.CreateFunc(ID, Utilities.CreateValueFunction(ValueUtilities.CreateValue(OpPart.Type.ToString(), String.Empty)), false, Name);
        }
    }
}
