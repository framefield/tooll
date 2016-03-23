// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framefield.Core;

namespace Framefield.Tooll
{
    public class CodeSectionViewModel
    {
        public string Id { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string Indentation { get; set; }
        public CodeDefinition CodeDefinition  { get; set; }

    }
}
