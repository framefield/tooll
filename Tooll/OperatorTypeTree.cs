// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framefield.Tooll
{
    /** A nested container that can contain further instances of OperatorTypeTree */
    public class OperatorTypeTree
    {
        public string Name { get { return _name; } }
        public List<OperatorTypeTree> Children { get { return _children; } }
        public List<Core.MetaOperator> Operators { get { return _operators; } }
        public List<OperatorTypeTree> Parents { get { return _parents; } }

        public OperatorTypeTree(String name, List<OperatorTypeTree> parents)
        {
            _name = name;
            _parents = parents;
        }

        public void Clear()
        {
            _children.Clear();
            _parents.Clear();
            _operators.Clear();
        }

        public void SortInOperator(Core.MetaOperator metaOp)
        {
            if (metaOp == null || metaOp.Namespace == null)
            {
                return;
            }

            var spaces = metaOp.Namespace.Split(new[] { '.' });

            var currentNode = this;
            var expandingSubTree = false;
            var parentSpaces = new List<OperatorTypeTree>();

            foreach (var spaceName in spaces)
            {
                if (spaceName == "")
                    continue;

                if (!expandingSubTree)
                {
                    var node = currentNode.FindNodeDataByName(spaceName);
                    if (node != null)
                    {
                        currentNode = node;
                        parentSpaces.Add(node);
                    }
                    else
                    {
                        expandingSubTree = true;
                    }
                }

                if (expandingSubTree)
                {
                    var newNode = new OperatorTypeTree(spaceName, new List<OperatorTypeTree>(parentSpaces));
                    currentNode._children.Add(newNode);
                    parentSpaces.Add(newNode);
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
        private readonly List<OperatorTypeTree> _parents = new List<OperatorTypeTree>();
        private readonly List<Core.MetaOperator> _operators = new List<Core.MetaOperator>();
    }
}
