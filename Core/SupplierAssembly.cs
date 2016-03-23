// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;

namespace Framefield.Core
{

    public class SupplierAssembly : OperatorPart.Function
    {
        public string Name { get; set; }

        public sealed override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
        {
            Logger.Debug(this, "Supplier Assembly '{0}' evaluated.", Name);
            return context;
        }

        public static bool IsSupplierAssembly(Type type)
        {
            return type.IsClass && // must be a class
                   type.IsSubclassOf(typeof(SupplierAssembly)) &&
                   type.Assembly.GetName().Name != "Core" && // operators have their own namespace
                   type.Name != "SupplierAssembly"; // must be a derived type
        }
    }
}
