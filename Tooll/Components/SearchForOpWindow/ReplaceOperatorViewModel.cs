// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;
using Framefield.Core;

namespace Framefield.Tooll.Components.SearchForOpWindow
{
    public class ReplaceOperatorViewModel
    {
        public Operator Operator { get; private set; }
        public string Name { get; set; }
        public string InstanceName { get; set; }
        public string Path { get; set; }
        public string Namespace { get; set; }
        public string NamespaceAndName { get { return Namespace + Name; } }
        public List<OperatorPart> Inputs { get { return Operator.Inputs; } }
        public bool IsReplaced { get; set; }

        public ReplaceOperatorViewModel(Operator op)
        {
            Operator = op;
            Name = Operator.Definition.Name;
            InstanceName = Operator.Name == string.Empty ? string.Empty : "   \"" + Operator.Name + "\""; 
            Namespace = Operator.Definition.Namespace;
            Path = @"~/" + Utils.GetOpPath(Operator);
        }
    }
}
