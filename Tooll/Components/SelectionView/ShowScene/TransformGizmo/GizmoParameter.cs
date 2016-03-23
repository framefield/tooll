// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framefield.Core;

namespace Framefield.Tooll.Components.SelectionView.ShowScene.TransformGizmo
{
    public enum GizmoParameterIds
    {
        TranslateX,
        TranslateY,
        TranslateZ,
        RotateX,
        RotateY,
        RotateZ,
        ScaleX,
        ScaleY,
        ScaleZ,
        PivotX,
        PivotY,
        PivatZ
    }

    public class GizmoParameter
    {
        public GizmoParameterIds Identifier;
        public OperatorPart Input;
        public float Value;
    }

}
