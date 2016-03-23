// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;

namespace Framefield.Tooll.Components.SearchForOpWindow
{
    public class AutoCompleteEntry
    {
        private MetaOperator _metaOp;

        public MetaOperator MetaOperator { get { return _metaOp; } }
        public string Content { get { return _metaOp.Namespace + _metaOp.Name; } }

        public AutoCompleteEntry(MetaOperator metaOp)
        {
            _metaOp = metaOp;
        }
    }
}
