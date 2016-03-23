// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Linq;
using Framefield.Core;

namespace Framefield.Tooll.Components.SearchForOpWindow.ResultFinders
{
    public class PathFinder : ResultFinder
    {
        private readonly Operator _operatorToBrowse;

        public PathFinder(ReplaceOperatorWindow window, Operator operatorToBrowse)
            : base(window)
        {
            _operatorToBrowse = operatorToBrowse;
        }

        public override void FindResults()
        {
            var selectedPopupItem = Window.XSearchPopupList.SelectedItem as AutoCompleteEntry;
            var searchText = selectedPopupItem != null ? selectedPopupItem.Content : Window.XSearchTextBox.Text;
            var matchingInternalOps = Utils.GetLowerOps(_operatorToBrowse).Where(internalOp => Utils.IsSearchTextMatchingToMetaOp(internalOp.Definition, searchText));
            foreach (var internalOp in matchingInternalOps)
            {
                Window.Results.Add(new ReplaceOperatorViewModel(internalOp));
            }
        }
    }
}
