// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input; //for the 

namespace Framefield.Tooll
{

    /// <summary>
    /// Declare a new <see cref="RoutedCommand">RoutedCommand</see> that
    /// is used by the <see cref="Window1">Window1</see> class where the 
    /// Command bindings and Command Sink events are declared. The Actual
    /// Comman is used on a Button within the <see cref="UserControlThatUsesCustomCommand">
    /// UserControlThatUsesCustomCommand</see> UserControl
    /// </summary>
    public class CustomCommands
    {
        #region Instance Fields
        public static readonly RoutedCommand UpdateCommand = new RoutedCommand("UpdateCommand", typeof(CustomCommands));
        public static readonly RoutedCommand PublishCommand = new RoutedCommand("PublishCommand", typeof(CustomCommands));
        public static readonly RoutedCommand ExportSelectedOperatorCommand = new RoutedCommand("ExportSelectedOperatorCommand", typeof(CustomCommands));
        public static readonly RoutedCommand ImportFBXCommand = new RoutedCommand("ImportFBXCommand", typeof(CustomCommands));
        public static readonly RoutedCommand SearchForOperatorCommand = new RoutedCommand("SearchForOperatorCommand", typeof(CustomCommands));
        public static readonly RoutedCommand ReplaceOperatorCommand = new RoutedCommand("ReplaceOperatorCommand", typeof(CustomCommands));
        public static readonly RoutedCommand BookmarkCommand = new RoutedCommand("BookmarkCommand", typeof(CustomCommands));

        public static readonly RoutedCommand StartPlaybackCommand = new RoutedCommand("StartPlaybackCommand", typeof(CustomCommands));
        public static readonly RoutedCommand TogglePlaybackCommand = new RoutedCommand("TogglePlaybackCommand", typeof(CustomCommands));
        public static readonly RoutedCommand StopPlaybackCommand = new RoutedCommand("StopPlaybackCommand", typeof(CustomCommands));
        public static readonly RoutedCommand PlayFasterForwardCommand = new RoutedCommand("PlayFasterForwardCommand", typeof(CustomCommands));
        public static readonly RoutedCommand PlayFasterBackwardCommand = new RoutedCommand("PlayFasterBackwardCommand", typeof(CustomCommands));
        public static readonly RoutedCommand RewindPlaybackCommand = new RoutedCommand("RewindPlaybackCommand", typeof(CustomCommands));
        public static readonly RoutedCommand ToggleAudioVolumeCommand = new RoutedCommand("ToggleAudioVolumeCommand", typeof(CustomCommands));
        public static readonly RoutedCommand ConnectRemoteClientCommand = new RoutedCommand("ConnectRemoteClientCommand", typeof(CustomCommands));

        public static readonly RoutedCommand ShowShadowOpsCommand = new RoutedCommand("ShowShadowOpsCommand", typeof(CustomCommands));
        public static readonly RoutedCommand HideShadowOpsCommand = new RoutedCommand("HideShadowOpsCommand", typeof(CustomCommands));

        public static readonly RoutedCommand SetStartTimeCommand = new RoutedCommand("SetStartTimeCommand", typeof(CustomCommands));
        public static readonly RoutedCommand SetEndTimeCommand = new RoutedCommand("SetEndTimeCommand", typeof(CustomCommands));

        // Curve Editor
        public static readonly RoutedCommand FitCurveValueRangeCommand = new RoutedCommand("FitCurveValueRangeCommand", typeof(CustomCommands));

        public static readonly RoutedCommand JumpToPreviousKeyCommand = new RoutedCommand("JumpToPreviousKeyCommand", typeof(CustomCommands));
        public static readonly RoutedCommand JumpToNextKeyCommand = new RoutedCommand("JumpToNextKeyCommand", typeof(CustomCommands));

        public static readonly RoutedCommand MoveToBeginningCommand = new RoutedCommand("MoveToBeginningCommand", typeof(CustomCommands));
        public static readonly RoutedCommand PreviousFrameCommand = new RoutedCommand("PreviousCommand", typeof(CustomCommands));
        public static readonly RoutedCommand NextFrameCommand = new RoutedCommand("NextFrameCommand", typeof(CustomCommands));

        // time line
        public static readonly RoutedCommand AddMarkerCommand = new RoutedCommand("AddMarkerCommand", typeof(CustomCommands));

        // composition graph view
        public static readonly RoutedCommand DeleteSelectedElementsCommand = new RoutedCommand("DeleteSelectedElementsCommand", typeof(CustomCommands));
        public static readonly RoutedCommand CenterSelectedElementsCommand = new RoutedCommand("CenterSelectedElementsCommand", typeof(CustomCommands));
        public static readonly RoutedCommand StickySelectedElementCommand = new RoutedCommand("StickySelectedElementCommand", typeof(CustomCommands));

        public static readonly RoutedCommand DuplicatedSelectedElementsCommand = new RoutedCommand("DuplicatedSelectedElementCommand", typeof(CustomCommands));

        public static readonly RoutedCommand SelectAndAddOperatorAtCursorCommand = new RoutedCommand("SelectAndAddOperatorAtCursorCommand", typeof(CustomCommands));

        public static readonly RoutedCommand FixOperatorFilepathsCommand = new RoutedCommand("FixOperatorFilepathsCommand", typeof(CustomCommands));
        public static readonly RoutedCommand FindOperatorUsagesCommand = new RoutedCommand("FindOperatorUsagesCommand", typeof(CustomCommands));
        public static readonly RoutedCommand FindOperatorDependenciesCommand = new RoutedCommand("FindOperatorDependenciesCommand", typeof(CustomCommands));
        public static readonly RoutedCommand GenerateKeyFramesFromLogfileCommand = new RoutedCommand("GenerateKeyFramesFromLogfileCommand", typeof(CustomCommands));

        // panels and views
        public static readonly RoutedCommand ShowConsoleViewCommand = new RoutedCommand("ShowConsoleViewCommand", typeof(CustomCommands));
        public static readonly RoutedCommand ShowGeneticVariationsViewCommand = new RoutedCommand("ShowGeneticVariationsViewCommand", typeof(CustomCommands));

        #endregion
    }
}
