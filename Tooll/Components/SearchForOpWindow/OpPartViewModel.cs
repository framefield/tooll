// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using Framefield.Core;

namespace Framefield.Tooll.Components.SearchForOpWindow
{
    public class OpPartViewModel
    {
        private OperatorPart _opPart;

        public enum OpPartUsage
        {
            Input,
            Output,
            Unused
        } 

        public OperatorPart OpPart { get { return _opPart; } }
        public string Value
        {
            get
            {
                if (_opPart.ID == Guid.Empty)
                {
                    return "";
                }
                switch (_opPart.Type)
                {
                    case FunctionType.Float:
                        return _opPart.Eval(new OperatorPartContext()).Value.ToString();
                    case FunctionType.Dynamic:
                        return _opPart.Eval(new OperatorPartContext()).Dynamic.ToString();
                    case FunctionType.Image:
                        return "Image";
                    case FunctionType.Text:
                        return _opPart.Eval(new OperatorPartContext()).Text;
                    case FunctionType.Generic:
                        return "generic";
                    case FunctionType.Scene:
                        return "scene";
                    case FunctionType.Mesh:
                        return "mesh";
                    default:
                        return "";
                }
            }
        }
        public string Name { get { return _opPart.Name; } }
        public bool IsConnected { get; set; }
        public OpPartUsage Usage { get; set; }

        public OpPartViewModel(OperatorPart opPart)
        {
            _opPart = opPart;
        }
    }
}
