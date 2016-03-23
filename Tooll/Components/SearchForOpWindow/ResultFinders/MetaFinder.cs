// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)


using System.Collections.Generic;
using Framefield.Core;

namespace Framefield.Tooll.Components.SearchForOpWindow.ResultFinders
{
    public class MetaFinder : ResultFinder
    {
        public MetaFinder(ReplaceOperatorWindow window) : base(window)
        {
        }

        public override void FindResults()
        {
            throw new System.NotImplementedException();
        }

        private void FindOccurrencesOf(MetaOperator metaOpToFind)
        {
            foreach (var metaOp in App.Current.Model.MetaOpManager.MetaOperators)
            {
            }
        }
    }
}
