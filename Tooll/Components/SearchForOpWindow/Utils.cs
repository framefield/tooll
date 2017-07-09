// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Framefield.Core;

namespace Framefield.Tooll.Components.SearchForOpWindow
{
    class Utils
    {
        internal static List<Operator> GetLowerOps(Operator source)
        {
            var lowers = new List<Operator>();
            if (source.InternalOps.Count <= 0)
                return lowers;
            foreach (Operator op in source.InternalOps)
            {
                lowers.Add(op);
                lowers.AddRange(GetLowerOps(op));
            }
            return lowers;
        }

        internal static string GetOpPath(Operator op)
        {
            var path = String.Empty;
            foreach (Operator par in GetHierachy(op))
            {
                if (par != App.Current.Model.HomeOperator)
                    path += par.Name != String.Empty ? par.Name : par.Definition.Name + "/";
            }
            //path += op.Name != String.Empty ? op.Name : op.Definition.Name;
            return path;
        }

        internal static List<Operator> GetHierachy(Operator op)
        {
            var ret = new List<Operator>();
            if (op.Parent != null)
            {
                ret = GetHierachy(op.Parent);
                ret.Add(op.Parent);
            }
            return ret;
        }

        internal static void JumpTo(Operator op)
        {
            
            if (App.Current.MainWindow.CompositionView.CompositionGraphView.CompositionOperator != op.Parent)
            {
                App.Current.MainWindow.CompositionView.CompositionGraphView.CompositionOperator = op.Parent;
            }
            var widget = GetWidgetOf(op);
            SelectAndCenter(widget);
        }

        internal static OperatorWidget GetWidgetOf(Operator opToSearch)
        {
            return (from opw in App.Current.MainWindow.CompositionView.CompositionGraphView.XOperatorCanvas.Children.OfType<OperatorWidget>()
                    where opw.Operator == opToSearch
                    select opw).SingleOrDefault();
        }

        internal static void SelectAndCenter(OperatorWidget widget)
        {
            if (widget != null)
            {
                App.Current.MainWindow.CompositionView.CompositionGraphView.SelectedElements.Clear();
                var list = new List<ISelectable> { widget };
                App.Current.MainWindow.CompositionView.CompositionGraphView.SelectedElements = list;
                App.Current.MainWindow.CompositionView.CompositionGraphView.CenterAllOrSelectedElements();
                App.Current.MainWindow.CompositionView.XCompositionToolBar.XBreadCrumbs.Clear();
                App.Current.MainWindow.CompositionView.XCompositionToolBar.XBreadCrumbs.Push(GetHierachy(widget.Operator));
            }
        }

        internal static bool IsSearchTextMatchingToMetaOp(MetaOperator metaOp, string searchText)
        {
            var pattern = searchText.Select((t, i) => searchText.Substring(i, 1))
                                    .Where(subString => Regex.Match(subString, "[A-Z0-9_-]", RegexOptions.IgnoreCase) != Match.Empty)
                                    .Aggregate(".*", (current, subString) => current + (subString + ".*"));
            return Regex.IsMatch(metaOp.Namespace + metaOp.Name, pattern, RegexOptions.IgnoreCase);
        }
    }
}
