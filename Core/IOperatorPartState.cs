// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framefield.Core
{

    public interface IOperatorPartState
    {
        bool Changed { get; set; }
        IOperatorPartState Clone();
    }

}
