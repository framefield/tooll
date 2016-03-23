// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Framefield.Core;

namespace Framefield.Tooll.Components.Bookmark
{
    class Bookmark : IComparable
    {
        public string Name { get; set; }
        public int ShortCutValue { get; set; }
        public Matrix ViewMatrix { get; set; }
        public IEnumerable<Guid> OpIdHierarchy { get { return _opIdHierarchy; } }
        public List<Guid> SelectedOps { get; set; }

        public Bookmark(Operator compOp)
        {
            Name = "Bookmark";
            SetOpIdList(compOp);
        }

        public Bookmark()
        {
            
        }

        public List<Operator> GetHierarchy(Operator op)
        {
            var hierarchicalOrderedOps = new List<Operator>();
            if (op.Parent != null)
            {
                hierarchicalOrderedOps = GetHierarchy(op.Parent);
                hierarchicalOrderedOps.Add(op.Parent);
            }
            return hierarchicalOrderedOps;
        }

        private void SetOpIdList(Operator op)
        {
            foreach (var @operator in GetHierarchy(op))
            {
                _opIdHierarchy.Add(@operator.ID);
            }
            _opIdHierarchy.Add(op.ID);
        }

        private readonly List<Guid> _opIdHierarchy = new List<Guid>();

        public int CompareTo(object obj)
        {
            return ShortCutValue < ((Bookmark) obj).ShortCutValue ? -1 : 1;
        }

        public class BmComparer : IComparer<Bookmark>
        {
            public int Compare(Bookmark x, Bookmark y)
            {
                return x.CompareTo(y);
            }
        }

    }
}
