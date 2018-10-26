// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using System;
using System.Windows.Input;

namespace Framefield.Tooll.Utils
{
    public static class OpUtils
    {
        public static MetaOperator FindExampleOperator(MetaOperator metaOp)
        {
            foreach (var potentialExample in App.Current.Model.MetaOpManager.MetaOperators.Values)
            {
                if (potentialExample.Name != metaOp.Name + "Example"
                 && potentialExample.Name != metaOp.Name + "Examples")
                    continue;

                return potentialExample;
            }
            return null;
        }
    }
}