// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framefield.Tooll
{
    public class OperatorTypeTree
    {
        public string Name { get { return _name; } }
        public List<OperatorTypeTree> Children { get { return _children; } }
        public List<Core.MetaOperator> Operators { get { return _operators; } }

        public OperatorTypeTree(String name)
        {
            _name = name;
        }

        public void Clear()
        {
            _children.Clear();
            _operators.Clear();
        }

        public void SortInOperator(Core.MetaOperator metaOp)
        {
            if (metaOp == null || metaOp.Namespace==null)
            {
                return;

            }
            var spaces = metaOp.Namespace.Split(new[] { ' ', ',', '.', ':', '\t' });

            var currentNode = this;
            var expandingSubTree = false;

            foreach (var spaceName in spaces)
            {
                if (spaceName == "")
                    continue;

                if (!expandingSubTree)
                {
                    var node= currentNode.FindNodeDataByName(spaceName);
                    if (node != null)
                    {
                        currentNode = node;
                    }
                    else
                    {
                        expandingSubTree = true;
                    }
                }

                if (expandingSubTree)
                {
                    var newNode = new OperatorTypeTree(spaceName);
                    currentNode._children.Add(newNode);
                    currentNode = newNode;
                }
            }

            currentNode._operators.Add(metaOp);
        }

        private OperatorTypeTree FindNodeDataByName(String name)
        {
            return _children.FirstOrDefault(n => n._name == name);
        }

        private readonly String _name = "";
        private readonly List<OperatorTypeTree> _children = new List<OperatorTypeTree>();
        private readonly List<Core.MetaOperator> _operators = new List<Core.MetaOperator>();
    }
}
