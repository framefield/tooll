// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using AvalonDock;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Helper;
using Framefield.Tooll.Components.Bookmark;
using Framefield.Tooll.Components.Console;
using Framefield.Tooll.Components.Dialogs;
using Framefield.Tooll.Components.Helper;
using Framefield.Tooll.Components.SearchForOpWindow;
using Framefield.Tooll.Utils;
using ICSharpCode.AvalonEdit.Editing;
using Framefield.Tooll.Components.SelectionView;
using Newtonsoft.Json;
using NGit.Transport;
using Binding = System.Windows.Data.Binding;
using Clipboard = System.Windows.Clipboard;
using Connection = Framefield.Core.Connection;
using Formatting = System.Xml.Formatting;
using Key = System.Windows.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Logger = Framefield.Core.Logger;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;
using TextDataFormat = System.Windows.TextDataFormat;
using Utilities = Framefield.Core.Utilities;
using Constants = Framefield.Core.Constants;

namespace Framefield.Tooll
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SpaceMouseWPFHandler SpaceMouseHandlerWpf { get; set; }    // will be initialized in MainWindow loaded

        public MainWindow() 
        {
            InitializeComponent();
            _importFbxScene = new ImportFbxScene();

            Title = Utilities.GetCompleteVersionString();

            var binding = new Binding();
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = App.Current.UndoRedoStack;
            binding.Path = new PropertyPath("CanUndo");
            XUndoMenuItem.SetBinding(IsEnabledProperty, binding);

            binding = new Binding();
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            binding.Source = App.Current.UndoRedoStack;
            binding.Path = new PropertyPath("CanRedo");
            XRedoMenuItem.SetBinding(IsEnabledProperty, binding);

            App.Current.Resources["ThemeDictionary"] = new ResourceDictionary();
            string uri = "/AvalonDock.Themes;component/themes/ExpressionDark.xaml";
            ThemeFactory.ChangeTheme(new Uri(uri, UriKind.RelativeOrAbsolute));

            App.Current.MainWindow.XRenderView.TimeLoggingSourceEnabled = true;

            XT2IconImage.ToolTip = Constants.VersionAsString;
            //note: the order of binding to the selection changed event is very important. valueview makes a full evaluation of the operator
            //      and its outputs. parameterview only reads the current values without any evaluation. therefor the valueview must react firstly 
            //      to the event to updated the operator before the parameterview displays the current values.
            CompositionView.CompositionGraphView.SelectionHandler.FirstSelectedChanged += XRenderView.UpdateViewToCurrentSelectionHandler;
            CompositionView.CompositionGraphView.SelectionHandler.FirstSelectedChanged += XRenderView2.UpdateViewToCurrentSelectionHandler;
            CompositionView.CompositionGraphView.SelectionHandler.FirstSelectedChanged += XParameterView.UpdateViewToCurrentSelection;
            CompositionView.CompositionGraphView.SelectionHandler.FirstSelectedChanged += XLibraryView.UpdateViewToCurrentSelectionHandler;
            InitHomeOperator();
            App.Current.Model.ClearEvent += ClearHandler;

            KeyUp += MainWindow_KeyUp;
            KeyDown += MainWindow_KeyDown;
            Closing += MainWindow_ClosingHandler;
            Closed += MainWindow_Closed;

            SetupLayoutHandler();

            LoadLayouts();
            LoadBookmarks();
        }

        private void MainWindow_ClosingHandler(object sender, CancelEventArgs e)
        {
            if (MetaManager.Instance.ChangedMetaOperators.Any() || MetaManager.Instance.HomeOperator.Changed)
            {
                var resultOfSaveChangesDialog = MessageBox.Show("Save changes before closing Tooll?", "Exit", MessageBoxButton.YesNoCancel);
                if (resultOfSaveChangesDialog == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (resultOfSaveChangesDialog == MessageBoxResult.Yes)
                    App.Current.Model.Save();
            }
        }

        void MainWindow_Closed(object sender, EventArgs e) {
            // FIXME: Of course we have to clean up all renderviews, not just the two default ones!
            XRenderView.XShowSceneControl.CleanUp();
            XRenderView2.XShowSceneControl.CleanUp();
        }

        private void SetupLayoutHandler() {
            int functionKeyBase = (int) Key.F1;
            for (int i = 0; i < 12; ++i) {
                var cmd = new RoutedCommand();
                LayoutFilterCommands.Add(cmd);
                cmd.InputGestures.Add(new KeyGesture((Key) (functionKeyBase + i)));
                var loadLayoutBinding = new CommandBinding();
                loadLayoutBinding.Command = cmd;
                loadLayoutBinding.CanExecute += (o, e) => e.CanExecute = true;
                int layoutIdx = i; // make local copy for closure
                loadLayoutBinding.Executed += (o, e) => { SetLayout(layoutIdx); };
                CommandBindings.Add(loadLayoutBinding);
            }
        }

        private ModifierKeys _modifiersWhileKeyDown;

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {            
            var source = e.InputSource;
            var ctrlPressed = (e.KeyboardDevice.Modifiers & ModifierKeys.Control) > 0;
            var altPressed = (e.KeyboardDevice.Modifiers & ModifierKeys.Alt) > 0;
            var shiftPressed = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) > 0;
            var windowsPressed = (e.KeyboardDevice.Modifiers & ModifierKeys.Windows) > 0;
            _modifiersWhileKeyDown = Keyboard.Modifiers;
            
            if (!(e.OriginalSource is TextBox ||
                  e.OriginalSource is TextArea ||
                  e.OriginalSource is MenuItem))
            {
                // handle all key stuff that has no modifier key
                if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
                {
                    switch (e.Key)
                    {
                        case Key.V:
                            CustomCommands.AddMarkerCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.Left:
                            CustomCommands.PreviousFrameCommand.Execute(null, this);
                            e.Handled = true;
                            break;

                        case Key.Right:
                            CustomCommands.NextFrameCommand.Execute(null, this);
                            e.Handled = true;
                            break;

                        case Key.Home:
                            CustomCommands.MoveToBeginningCommand.Execute(null, this);                            
                            e.Handled = true;
                            break;

                    }
                }
            }            
        }

        void MainWindow_KeyUp(object sender, KeyEventArgs e) {
            var source = e.InputSource;
            var ctrlPressed = (_modifiersWhileKeyDown & ModifierKeys.Control) > 0;
            var altPressed = (_modifiersWhileKeyDown & ModifierKeys.Alt) > 0;
            var shiftPressed = (_modifiersWhileKeyDown & ModifierKeys.Shift) > 0;
            var windowsPressed = (_modifiersWhileKeyDown & ModifierKeys.Windows) > 0;

            if (!(e.OriginalSource is TextBox || 
                  e.OriginalSource is TextArea ||
                  e.OriginalSource is MenuItem)) 
            {
                // handle all key stuff that has no modifier key
                if ((_modifiersWhileKeyDown & ModifierKeys.Control) == ModifierKeys.None)
                {
                    switch (e.Key) {
                        case Key.Space:
                            CustomCommands.TogglePlaybackCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.L:
                            CustomCommands.PlayFasterForwardCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.J:
                            CustomCommands.PlayFasterBackwardCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.H:
                            CustomCommands.RewindPlaybackCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.K:
                            CustomCommands.StopPlaybackCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.B:
                            CustomCommands.SetStartTimeCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.N:
                            CustomCommands.SetEndTimeCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.OemPeriod:
                            CustomCommands.JumpToNextKeyCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.OemComma:
                            CustomCommands.JumpToPreviousKeyCommand.Execute(null, this);
                            e.Handled = true;
                            break;




                        case Key.Back:
                            if (!(e.OriginalSource is CurveEditor)) {
                                CustomCommands.DeleteSelectedElementsCommand.Execute(null, this);
                                e.Handled = true;
                            }
                            break;
                        case Key.Delete:
                            if (!(e.OriginalSource is CurveEditor)) {
                                CustomCommands.DeleteSelectedElementsCommand.Execute(null, this);
                                e.Handled = true;
                            }
                            break;
                        case Key.C:
                            CustomCommands.CenterSelectedElementsCommand.Execute(null, this);
                            e.Handled = true;
                            break;
                        case Key.S:
                            if (Keyboard.Modifiers == ModifierKeys.Shift) {
                                CustomCommands.StickySelectedElementCommand.Execute(null, this);
                            }
                            e.Handled = true;
                            break;
                    }
                }
            }
        }


        public void RemoveOperators(IEnumerable<Operator> opsToDelete)
        {
            if (opsToDelete.Count() > 0)
            {
                var cmd = new DeleteOperatorsCommand(CompositionView.CompositionGraphView.CompositionOperator, opsToDelete);
                App.Current.UndoRedoStack.AddAndExecute(cmd);
            }
        }

        public void InsertConnectionAt(Connection connection)
        {
            App.Current.UndoRedoStack.AddAndExecute(new InsertConnectionCommand(CompositionView.CompositionGraphView.CompositionOperator, connection));
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        public void ReplaceConnectionAt(Connection connection)
        {
            App.Current.UndoRedoStack.AddAndExecute(new ReplaceConnectionCommand(CompositionView.CompositionGraphView.CompositionOperator, connection));
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        public void RemoveConnectionAt(Connection connection)
        {
            App.Current.UndoRedoStack.AddAndExecute(new RemoveConnectionCommand(CompositionView.CompositionGraphView.CompositionOperator, connection));
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        public void AddInput(MetaInput input) {
            CompositionView.CompositionGraphView.CompositionOperator.Definition.AddInput(input);
        }

        public void AddOutput(MetaOutput output) {
            CompositionView.CompositionGraphView.CompositionOperator.Definition.AddOutput(output);
        }

        public void RemoveOutput(OperatorPart opPart) {
            CompositionView.CompositionGraphView.CompositionOperator.RemoveOutput(opPart);
        }

        private void NewHandler(object sender, RoutedEventArgs e) {
            App.Current.Model.Clear();
        }

        private void SaveHandler(object sender, RoutedEventArgs e)
        {
            SaveOperators(enterCommitMessage:false);
        }

        private void CloseHandler(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void NewPanelHandler(object sender, RoutedEventArgs e) {
            var documentContent = new DocumentContent();
            documentContent.Title = "MyNewContent";
            var newRenderView = new SelectionView();
            CompositionView.CompositionGraphView.SelectionHandler.FirstSelectedChanged += newRenderView.UpdateViewToCurrentSelectionHandler;
            if (App.Current.MainWindow.CompositionView.CompositionGraphView.SelectedElements.Count > 0) {
                var ow = App.Current.MainWindow.CompositionView.CompositionGraphView.SelectedElements[0] as OperatorWidget;
                newRenderView.SetOperatorWidget(ow);
            }
            documentContent.Content = newRenderView;
            documentContent.Show(dockManager, true);
        }

        private void ShowLibraryHandler(object sender, RoutedEventArgs e) {
            var libraryView = new LibraryView();
            libraryView.Show(dockManager, true);
        }

        private void ClearHandler(object sender, EventArgs args)
        {
            ClearHomeOperator();
            InitHomeOperator();
        }

        private void ClearHomeOperator()
        {
            var homeOp = App.Current.Model.HomeOperator;
            var opsToDelete = homeOp.InternalOps;
            var command = new DeleteOperatorsCommand(homeOp, opsToDelete);
            App.Current.UndoRedoStack.AddAndExecute(command);
        }

        private void InitHomeOperator() 
        {            
            if (!App.Current.UserSettings.Contains("User.Name"))
            {
                var userNameDialog = new UserNameDialog();
                userNameDialog.ShowDialog();

                if (userNameDialog.DialogResult == true) {
                    App.Current.UserSettings["User.Name"] = userNameDialog.XUserName.Text;
                    App.Current.UserSettings["User.Email"] = userNameDialog.XUserEmail.Text;
                }
                else {
                    App.Current.UserSettings["User.Name"] = "Unknown";
                }
            }
            App.Current.Model.HomeOperator.Definition.Namespace = "user." + App.Current.UserSettings["User.Name"];
            CompositionView.CompositionGraphView.CompositionOperator = App.Current.Model.HomeOperator;
        }

        private void UndoHandler(object sender, RoutedEventArgs e) {
            App.Current.UndoRedoStack.Undo();
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void RedoHandler(object sender, RoutedEventArgs e) {
            App.Current.UndoRedoStack.Redo();
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void ShowUndoRedoViewHandler(object sender, RoutedEventArgs e)
        {
            var documentContent = new DocumentContent();
            documentContent.Title = "Undo/Redo Stacks";
            documentContent.Content = new UndoRedoView();
            documentContent.Show(dockManager, true);
        }

        class Layout 
        {
            public string Name { get; set; }
            public string Data { get; set; }
        }

        private List<Layout> _layouts = new List<MainWindow.Layout>();
        private const string _layoutFileName = @"Config/Layouts.xml";
        private void SaveLayoutHandler(object sender, RoutedEventArgs e) {
            var name = "layout" + (_layouts.Count + 1).ToString();

            var popup = new TextInputWindow();
            popup.Title = "Save layout";
            popup.XText.Text = String.Format("Please enter layout title:");
            popup.XTextBox.Text = name;
            popup.XTextBox.SelectAll();
            popup.XTextBox.Focus();
            popup.ShowDialog();

            if (popup.DialogResult == false)
                return;

            name = popup.XTextBox.Text;

            using (var writer = new StringWriter()) {
                dockManager.SaveLayout(writer);
                var layout = new MainWindow.Layout() { Name = name, Data = writer.ToString() };
                AddLayout(layout);
            }

            using (var xmlWriter = new XmlTextWriter(_layoutFileName, Encoding.UTF8)) {
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("Layouts");
                foreach (var layout in _layouts) {
                    xmlWriter.WriteStartElement("Layout");
                    xmlWriter.WriteAttributeString("Name", layout.Name);
                    xmlWriter.WriteRaw(layout.Data);
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
        }

        private void LoadLayouts() {
            if (!File.Exists(_layoutFileName))
                return;

            var doc = XDocument.Load(_layoutFileName);
            foreach (XElement x in doc.Element("Layouts").Elements("Layout")) {
                var name = x.Attribute("Name").Value;
                var data = x.Elements().First();
                var layout = new MainWindow.Layout() { Name = name, Data = data.ToString() };
                AddLayout(layout);
            }
        }

        public static List<RoutedCommand> LayoutFilterCommands = new List<RoutedCommand>();

        private void AddLayout(MainWindow.Layout layout) {
            _layouts.Add(layout);
            var newMenuEntry = new MenuItem() { Header = layout.Name };
            int layoutIdx = _layouts.Count - 1;
            newMenuEntry.Command = LayoutFilterCommands[layoutIdx];
            XLayoutMenuItem.Items.Add(newMenuEntry);
        }

        private void SetLayout(int layoutIndex) {
            if (layoutIndex >= _layouts.Count)
                return;

            using (var reader = new StringReader(_layouts[layoutIndex].Data)) {
                dockManager.RestoreLayout(reader);
            }
        }

        private void DeleteLayoutHandler(object sender, RoutedEventArgs e) {
            XLayoutMenuItem.Items.RemoveAt(XLayoutMenuItem.Items.Count - 1);
            _layouts.RemoveAt(_layouts.Count - 1);
        }

        private void ShowParameterViewHandler(object sender, RoutedEventArgs e) {
            parameterViewDock.Show(dockManager, true);
        }

        private void ShowMeasureViewHandler(object sender, RoutedEventArgs e) {
            var documentContent = new DocumentContent();
            documentContent.Title = "MeasureView";
            var newView = new TimeLogView();
            documentContent.Content = newView;
            documentContent.Show(dockManager, true);
        }

        public static RoutedCommand OpenLastFileCommand = new RoutedCommand();

        #region Command sinks for custom commands
        private void UpdateCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = App.Current.OperatorRepository != null;
        }

        private void UpdateCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                UpdateOperators(enterCommitMessage:false);
            }
            catch
            {
            }
        }

        private static void SaveOperators(bool enterCommitMessage)
        {
            if (!MetaManager.Instance.HomeOperator.Changed &&
                !App.Current.Model.MetaOpManager.ChangedMetaOperators.Any())
            {
                Logger.Info("Nothing to save");
                return;
            }

            var namesOfChangedOps = App.Current.Model.MetaOpManager.ChangedMetaOperators.Select(op => op.Name).ToList();

            if (MetaManager.Instance.HomeOperator.Changed)
            {
                namesOfChangedOps.Add("Home");
            }

            Logger.Info("Saving {0} operators: {1}", namesOfChangedOps.Count(), String.Join(", ",namesOfChangedOps));           

            App.Current.Model.Save();

            var isGitActive = App.Current.OperatorRepository != null;
            if (!isGitActive)
                return;

            SaveToGitReposity(enterCommitMessage);
        }

        private static void SaveToGitReposity(bool enterCommitMessage)
        {
            var repository = App.Current.OperatorRepository;
            if (repository == null)
                return;

            var git = repository.Git;

            // add all new mop files
            var status = repository.Git.Status().Call();
            var newMetaOps = from untrackedFile in status.GetUntracked()
                where untrackedFile.EndsWith(".mop")
                select untrackedFile;
            foreach (var newMetaOp in newMetaOps)
            {
                git.Add()
                    .AddFilepattern(newMetaOp)
                    .Call();
            }

            var modifiedMetaOps = from modifiedFile in status.GetModified()
                where modifiedFile.EndsWith(".mop")
                select modifiedFile;
            foreach (var modifiedMetaOp in modifiedMetaOps)
            {
                git.Add()
                    .AddFilepattern(modifiedMetaOp)
                    .Call();
            }

            status = git.Status().Call();
            var numChanged = status.GetChanged().Count;
            var numAdded = status.GetAdded().Count;

            // check if the sha1 of the last commit exists in remote branch, if that's the case a new commit is created 
            // otherwise the last one is amended
            var localRef = git.GetRepository().GetRef(repository.Branch);
            var localSha1 = localRef.GetObjectId().Name;

            var originRef = git.GetRepository().GetRef("remotes/origin/" + repository.Branch);
            var log = git.Log().Add(originRef.GetObjectId()).Call();
            var amend = true;
            foreach (var logEntry in log)
            {
                if (logEntry.Name == localSha1)
                {
                    amend = false;
                    break;
                }
            }

            if ((numChanged + numAdded) == 0 && !amend)
                return;

            var commitMessage = GetCommitMessage(enterCommitMessage);

            try
            {
                git.Commit()
                    .SetAmend(amend)
                    .SetMessage(commitMessage)
                    .Call();
                Logger.Info("Committed current operator state.");
            }
            catch (Exception exception)
            {
                Logger.Error("Error commiting current operator state:\n{0}.", exception.Message);
            }
        }

        private static string GetCommitMessage(bool enterCommitMessage)
        {
            if (enterCommitMessage)
            {
                var messageDialog = new TextInputWindow
                                        {
                                            XText = { Text = "Please enter change description:" },
                                            XTextBox = { Text = string.Empty }
                                        };
                messageDialog.XTextBox.SelectAll();
                messageDialog.XTextBox.Focus();
                messageDialog.ShowDialog();

                if (messageDialog.DialogResult != true)
                {
                    Logger.Debug("Saving cancelled.");
                    throw new Exception("Canceled save.");
                }
                return messageDialog.XTextBox.Text;
            }

            return "Please publish in order to set commit message";
        }

        private void UpdateOperators(bool enterCommitMessage)
        {
            SaveOperators(enterCommitMessage); // throws if cancelled and enterCommitMessage == true

            try
            {
                var git = App.Current.OperatorRepository.Git;
                git.Pull().Call();
                Logger.Info("Successfully downloaded operators from server. ");
            }
            catch (Exception exception)
            {
                Logger.Error("Downloading operators failed: {0}. Please check in command line.", exception.Message);
                throw;
            }
            App.Current.Model.RebuildMetaOpManager();
            CompositionView.CompositionGraphView.CompositionOperator = App.Current.Model.HomeOperator;
            Logger.Info("Successfully updated operators from server. Please unlock and reselect all locked render-views.");
        }

        private void PublishCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = App.Current.OperatorRepository != null;
        }

        private void PublishCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PublishOperators(enterCommitMessage: true);
        }

        private void PublishOperators(bool enterCommitMessage)
        {
            try
            {
                UpdateOperators(enterCommitMessage);
            }
            catch
            {
                return;
            }

            try
            {
                var repos = App.Current.OperatorRepository;
                var git = App.Current.OperatorRepository.Git;
                var refSpec = new RefSpec().SetSourceDestination(repos.Branch, repos.Branch);
                git.Push()
                   .SetRefSpecs(new List<RefSpec>() { refSpec })
                   .Call();
                Logger.Info("Successfully published your operators to server.");
            }
            catch (Exception exception)
            {
                Logger.Error("Publishing your operators to server failed: {0}.", exception.Message);
            }
        }


        private void ExportSelectedOperatorCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsOnlyOneOperatorSelected;
        }

        private bool IsOnlyOneOperatorSelected
        {
            get
            {
                var currentSelectedGuiElements = CompositionView.CompositionGraphView.SelectedElements;
                return (currentSelectedGuiElements.Count == 1 && currentSelectedGuiElements[0] is OperatorWidget);
            }
        }

        public ImportFbxScene ImportFbxScene
        {
            get { return _importFbxScene; }
        }

        private void ExportSelectedOperatorCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!IsOnlyOneOperatorSelected)
            {
                Logger.Warn("Export is only possible with exactly one operator selected.");
                return;
            }

            var selectedOperatorWidget = CompositionView.CompositionGraphView.SelectedElements[0] as OperatorWidget;
            StandaloneExporter.Export(selectedOperatorWidget.Operator, Directory.GetCurrentDirectory() + @"\" + "Export" + @"\");
        }

        private void ImportFBXCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ImportFBXCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ImportFbxScene.ImportFbxAsOperator();
        }


        private void SearchForOperatorCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new SearchForOpWindow();
            window.Owner = this;
            window.Show();
        }
        
        private void SearchForOperatorCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void ReplaceOperatorCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ReplaceOperatorCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new ReplaceOperatorWindow();
            window.Owner = this;
            window.Show();
        }

        #region Bookmarkstuff
        private static readonly string BOOKMARKS_FILENAME = @"Config/Bookmarks.json";
        private SortedSet<Bookmark> _bookmarks = new SortedSet<Bookmark>(new Bookmark.BmComparer());
        public static List<CommandBinding> BookmarkCommandBindings = new List<CommandBinding>();

        private void SaveBookmarkHandler(object sender, RoutedEventArgs e)
        {
            OperatorWidget[] selectedWidgets = (from opw in App.Current.MainWindow.CompositionView.CompositionGraphView.XOperatorCanvas.Children.OfType<OperatorWidget>()
                                                where opw.IsSelected
                                                select opw).ToArray();

            Guid[] selectedWidgetIDs = (from wid in selectedWidgets
                                        select wid.Operator.ID).ToArray();

            var popup = new SaveBookmarkWindow();
            string bookmarkName = String.Empty;
            if (selectedWidgets.Length > 0)
            {
                bookmarkName = (selectedWidgets[0].Operator.Name == String.Empty ? selectedWidgets[0].Operator.Definition.Name : selectedWidgets[0].Operator.Name)
                               + " in " +
                               (selectedWidgets[0].Operator.Parent.Name == String.Empty ? selectedWidgets[0].Operator.Parent.Definition.Name : selectedWidgets[0].Operator.Parent.Name);
            }
            else
            {
                bookmarkName = CompositionView.CompositionGraphView.CompositionOperator.Name == String.Empty ? CompositionView.CompositionGraphView.CompositionOperator.Definition.Name : CompositionView.CompositionGraphView.CompositionOperator.Name;
            }
            int indexToSelect = 0;
            while ((from bm in _bookmarks select bm.ShortCutValue).Contains(indexToSelect + 1))
            {
                indexToSelect++;
            }
            popup.Title = "Save Bookmark";
            popup.XText.Text = "Please enter Bookmark title:";
            popup.XTextBox.Text = bookmarkName;
            popup.XTextBox.SelectAll();
            popup.XTextBox.Focus();
            popup.XComboBox.SelectedIndex = indexToSelect;
            foreach (var bm in _bookmarks)
            {
                ((ComboBoxItem)popup.XComboBox.Items.GetItemAt(bm.ShortCutValue - 1)).Content += "   (used for " + bm.Name + ")";
            }
            popup.ShowDialog();
            if (popup.DialogResult == false)
                return;

            bookmarkName = popup.XTextBox.Text;
            var shortCut = popup.XComboBox.SelectedIndex + 1;
            if (!BookmarkShortCutAvailable(shortCut))
            {
                MessageBoxResult result = MessageBox.Show("Selected Shortcut is still used. Do you want to override it?", "Shortcut not available", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    DeleteBookmark((from bm in _bookmarks
                                    where bm.ShortCutValue == shortCut
                                    select bm).SingleOrDefault());
                }
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            var bookmark = new Bookmark(CompositionView.CompositionGraphView.CompositionOperator)
                           {
                               Name = bookmarkName,
                               SelectedOps = selectedWidgetIDs.ToList(),
                               ViewMatrix = CompositionView.CompositionGraphView.XViewTransform.Matrix,
                               ShortCutValue = shortCut
                           };
            _bookmarks.Add(bookmark);

            SetBookmarkMenuEntry(bookmark, shortCut);

            SerializeBookmarks();
        }

        private bool BookmarkShortCutAvailable(int shortCut)
        {
            var shortCutList = from bm in _bookmarks
                               select bm.ShortCutValue;
            if (shortCutList.Contains(shortCut))
                return false;

            return true;
        }

        private void SerializeBookmarks()
        {
            SerializeBookmarksAs(BOOKMARKS_FILENAME);
        }

        public void SerializeBookmarksAs(string filePath)
        {
            string json = JsonConvert.SerializeObject(_bookmarks, Newtonsoft.Json.Formatting.Indented);
            using (var sw = new StreamWriter(filePath))
            {
                sw.Write(json);
            }
        }

        private void SetBookmarkMenuEntry(Bookmark bookmark, int orderPosition)
        {
            var indexOfBookmark = _bookmarks.IndexOf(bookmark);
            var newMenuEntry = MakeMenuEntryWithShortcut(bookmark, orderPosition, indexOfBookmark);
            XBookmarkMenuItem.Items.Insert(indexOfBookmark + 2, newMenuEntry);
            UpdateDeleteBookmarkMenu(indexOfBookmark, newMenuEntry);
        }

        private void UpdateDeleteBookmarkMenu(int indexOfBookmark, MenuItem bookmarkMenuItem)
        {
            var deleteItemToAdd = new MenuItem
                                  {
                                      Header = bookmarkMenuItem.Header,
                                      InputGestureText = bookmarkMenuItem.InputGestureText
                                  };
            var bookmarkToDelete = _bookmarks.ElementAt(indexOfBookmark);
            deleteItemToAdd.Click += (o, ev) => DeleteBookmark(bookmarkToDelete);
            XDeleteBookmarkMenu.Items.Insert(indexOfBookmark + 1, deleteItemToAdd);
        }

        private MenuItem MakeMenuEntryWithShortcut(Bookmark bookmark, int orderPosition, int indexOfBookmark)
        {
            var newMenuEntry = new MenuItem { Header = bookmark.Name };
            var cmd = new RoutedCommand();
            var loadBookmarkBinding = new CommandBinding();
            loadBookmarkBinding.Command = cmd;
            cmd.InputGestures.Add(new KeyGesture(GetBookmarkShortcut(orderPosition), ModifierKeys.Control));
            loadBookmarkBinding.CanExecute += BookmarkCommand_CanExecute;
            loadBookmarkBinding.Executed += (o, ev) => ExecuteBookmark(bookmark);
            newMenuEntry.Command = cmd;
            BookmarkCommandBindings.Insert(indexOfBookmark, loadBookmarkBinding);
            CommandBindings.Add(BookmarkCommandBindings[indexOfBookmark]);
            return newMenuEntry;
        }

        private void BookmarkCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private Key GetBookmarkShortcut(int orderPosition)
        {
            switch (orderPosition)
            {
                case 1:
                    return Key.D1;
                case 2:
                    return Key.D2;
                case 3:
                    return Key.D3;
                case 4:
                    return Key.D4;
                case 5:
                    return Key.D5;
                case 6:
                    return Key.D6;
                case 7:
                    return Key.D7;
                case 8:
                    return Key.D8;
                case 9:
                    return Key.D9;
            }
            return Key.D0;
        }

        private void ExecuteBookmark(Bookmark bookmark)
        {
            try
            {
                var newCompOp = GetLastOp(bookmark);
                CompositionView.CompositionGraphView.CompositionOperator = newCompOp;
                CompositionView.CompositionGraphView.SetTransitionTarget(bookmark.ViewMatrix);
                CompositionView.CompositionGraphView.SelectedElements.Clear();
                if (bookmark.SelectedOps != null)
                {
                    List<ISelectable> list = (from widget in App.Current.MainWindow.CompositionView.CompositionGraphView.XOperatorCanvas.Children.OfType<OperatorWidget>()
                                                     where bookmark.SelectedOps.Contains(widget.Operator.ID)
                                                     select widget as ISelectable).ToList();
                    CompositionView.CompositionGraphView.SelectedElements = list;
                }
                CompositionView.XCompositionToolBar.XBreadCrumbsView.Clear();
                CompositionView.XCompositionToolBar.XBreadCrumbsView.Push(bookmark.GetHierarchy(newCompOp));
                CompositionView.XCompositionToolBar.XBreadCrumbsView.Push(newCompOp);
            }
            catch (Exception)
            {
                Logger.Error("Bookmarked Operator can not be found!");
            }
        }

        private static Operator GetLastOp(Bookmark bookmark)
        {
            Operator opById = App.Current.Model.HomeOperator;
            for (int i = 1; i < bookmark.OpIdHierarchy.Count(); i++)
            {
                opById = (from op in opById.InternalOps
                          where op.ID == bookmark.OpIdHierarchy.ElementAt(i)
                          select op).Single();
            }
            return opById;
        }

        private void DeleteAllBookmarksHandler(object sender, RoutedEventArgs e)
        {
            while (_bookmarks.Count > 0)
            {
                DeleteBookmark(_bookmarks.ElementAt(0));
            }
        }

        private void DeleteBookmark(Bookmark bm)
        {
            if (bm == null)
                return;

            if (_bookmarks.Count > 0)
            {
                var index = _bookmarks.IndexOf(bm);
                XDeleteBookmarkMenu.Items.RemoveAt(index + 1);
                XBookmarkMenuItem.Items.RemoveAt(index + 2);
                _bookmarks.Remove(bm);
                CommandBindings.RemoveAt(CommandBindings.IndexOf(BookmarkCommandBindings[index]));
                BookmarkCommandBindings.RemoveAt(index);
                SerializeBookmarks();
            }
        }

        private void LoadBookmarks()
        {
            if (!File.Exists(BOOKMARKS_FILENAME))
                return;

            string json;
            using (var reader = new StreamReader(BOOKMARKS_FILENAME))
            {
                json = reader.ReadToEnd();
            }
            _bookmarks = JsonConvert.DeserializeObject<SortedSet<Bookmark>>(json);
            foreach (var bm in _bookmarks)
            {
                SetBookmarkMenuEntry(bm, bm.ShortCutValue);
            }
        } 
        #endregion

        private void StartPlaybackCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = Math.Abs(App.Current.MainWindow.CompositionView.PlaySpeed) == 0.0;
        }

        private void StartPlaybackCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            StartPlayback();
        }


        private void StopPlaybackCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = Math.Abs(App.Current.MainWindow.CompositionView.PlaySpeed) > 0.0;
        }

        private void StopPlaybackCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            StopPlayback();
        }


        private void TogglePlaybackCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private void TogglePlaybackCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (Math.Abs(App.Current.MainWindow.CompositionView.PlaySpeed) > 0.0) {
                StopPlayback();
            }
            else {
                StartPlayback();
            }
        }


        private void PlayFasterForwardCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void PlayFasterForwardCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            PlayFasterForward();
        }


        private void PlayFasterBackwardCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void PlayFasterBackwardCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            PlayBackward();
        }

        private void RewindPlaybackCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void RewindPlaybackCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RewindPlayback();
        }

        private void ToggleAudioVolumeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private void ToggleAudioVolumeCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            ToggleMutePlayback();
        }

        private void ConnectRemoteClientCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ConnectRemoteClientCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ConnectRemoteClient();
        }

        private void ShowShadowOpsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void ShowShadowOpsCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.CompositionGraphView.ShowShadowOps();
        }


        private void HideShadowOpsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void HideShadowOpsCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.CompositionGraphView.HideShadowOps();
        }



        private void SetStartTimeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void SetStartTimeCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.XTimeView.StartTime= App.Current.Model.GlobalTime;
        }


        private void SetEndTimeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void SetEndTimeCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.XTimeView.EndTime= App.Current.Model.GlobalTime;
        }


        private void FitCurveValueRangeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void FitCurveValueRangeCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.FitValueRange();
        }


        private void JumpToNextKeyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void JumpToNextKeyCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.JumpToNextKey();
        }


        private void JumpToPreviousKeyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void JumpToPreviousKeyCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.JumpToPreviousKey();
        }

        private void MoveToBeginningCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void MoveToBeginningCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            App.Current.Model.GlobalTime = 0;
        }


        private void PreviousFrameCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void PreviousFrameCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var moveFast = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            App.Current.Model.GlobalTime -= moveFast ? 1 : 1 / 30.0;            
        }

        private void NextFrameCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void NextFrameCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {   
            var moveFast = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ;
            App.Current.Model.GlobalTime +=  moveFast  ? 1 :  1 / 30.0;
        }



        private void AddMarkerCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private static Guid MARKER_OP_META_ID = Guid.Parse("{3a680e24-4ac1-4e95-8784-efa048af24a8}");

        private void AddMarkerCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            var currentTime = App.Current.Model.GlobalTime;
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;

            bool isPlaying = App.Current.MainWindow.CompositionView.PlaySpeed != 0;
            
            if(isPlaying)
                cgv.SelectionChangeEnabled = false;

            var command = new AddTimeMarkerCommand(cgv.CompositionOperator, MARKER_OP_META_ID, currentTime, 100, 100, 100, true);
            App.Current.UndoRedoStack.AddAndExecute(command);

            if(isPlaying)
                cgv.SelectionChangeEnabled = true;

            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void DeleteSelectedElementsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void DeleteSelectedElementsCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.CompositionGraphView.RemoveSelectedElements();
        }

        private void CenterSelectedElementsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void CenterSelectedElementsCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.CompositionGraphView.CenterAllOrSelectedElements();
        }

        private void StickySelectedElementCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            //var focusedElement = FocusManager.GetFocusedElement(this);
            //if (!(focusedElement is ShowSceneControl)) {
                e.CanExecute = true;
            //}
        }
        private void StickySelectedElementCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            App.Current.MainWindow.CompositionView.CompositionGraphView.StickySelectedElement();
        }

        private void DuplicatedSelectedElementsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void DuplicatedSelectedElementsCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            var focusedElement = FocusManager.GetFocusedElement(this);
            if (focusedElement is CurveEditor) {
                var editor = focusedElement as CurveEditor;
                editor.DuplicateKeyframes();
            }
            else
                App.Current.MainWindow.CompositionView.CompositionGraphView.CopySelectionToClipboard();
        }

        private void SelectAndAddOperatorAtCursorCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            e.CanExecute = cgv.IsMouseOver;
        }

        private void SelectAndAddOperatorAtCursorCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;

            // We have to capture the mouse position before opening the dialog.
            Point mousePos = Mouse.GetPosition(cgv.XOperatorCanvas);

            FunctionType preferredType = FunctionType.Generic;

            if(cgv.SelectedElements.Count ==1) {
                var opWidget = cgv.SelectedElements[0] as OperatorWidget;
                if (opWidget != null) {
                    var op= opWidget.Operator;
                    if (op != null && op.Outputs.Count > 0) {
                        preferredType = op.Outputs[0].Type;
                    }            
                }
            }

            var createWindow = new QuickCreateWindow(cgv.CompositionOperator)
            {
                SelectedType = preferredType,
                RelavantPositionOnWorkspace = mousePos
            };
            createWindow.Top = App.Current.UserSettings.GetOrSetDefault("UI.QuickCreateWindowPositionTop", 200);
            createWindow.Left = App.Current.UserSettings.GetOrSetDefault("UI.QuickCreateWindowPositionLeft", 200);

            createWindow.ShowDialog();

            App.Current.UserSettings["UI.QuickCreateWindowPositionTop"]= createWindow.Top;
            App.Current.UserSettings["UI.QuickCreateWindowPositionLeft"] = createWindow.Left;

            if (createWindow.SelectedMetaOp != null) {
                cgv.AddOperatorAtPosition(createWindow.SelectedMetaOp, mousePos);
            }
        }

        

        private void FindMissingPaths(Operator op) {
            foreach (var childOp in op.InternalOps) {
                FindMissingPaths(childOp);
            }

            foreach (var parameter in op.Inputs) {
                if (parameter.Name.EndsWith("Path")) {
                    if(parameter.Connections.Count==0) {
                        var context = new OperatorPartContext();
                        string path = parameter.Eval(context).Text;
                        if (!File.Exists(path)) {
                            Logger.Warn("Missing File: {0}", path);
                            string basename = path.Split('/').Last();
                            if (_assetFiles.ContainsKey(basename)) {
                                Logger.Info("  -> fixed with: {0}", _assetFiles[basename]);
                                parameter.Func = Utilities.CreateValueFunction(new Text(_assetFiles[basename]));

                                // Trigger modification
                                string tmpName = op.Name;
                                op.Name = "modified";
                                op.Name = tmpName;
                            }
                        }                        
                    } 
                }
            }
        }

        private Dictionary<String, String> _assetFiles = new Dictionary<string, string>();

        private void ScanAssetDirectory(String path)
        {
            string[] files = new string[0];
            try
            {
                files = Directory.GetFiles(path);
            }
            catch (DirectoryNotFoundException)
            {
                Logger.Warn( String.Format("unable to access directory '{0}'. \nThis could be due to a broken file-link.",path));
                return;
            }

            foreach (var filename in files) {
                var basename = filename.Split('\\').Last();
                if (_assetFiles.ContainsKey(basename)) {
                    Logger.Info("Ignoring reappearing instance of file: {0}", path + "/" + basename);
                }
                else {
                    _assetFiles.Add(basename, path + "/" + basename);
                }                
            }
            foreach (var dir in Directory.GetDirectories(path)) {
                var dirname= dir.Split('\\').Last();
                ScanAssetDirectory(path + "/" + dirname);
            }
        }


        private void FixOperatorFilepathsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            e.CanExecute = cgv.SelectedElements.Count > 0;
        }
        private void FixOperatorFilepathsCommand_Executed(object sender, ExecutedRoutedEventArgs e) {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            _assetFiles.Clear();
            ScanAssetDirectory(".");

            foreach (var c in cgv.SelectedElements) {
                var op = c as OperatorWidget;
                if (op != null) {
                    FindMissingPaths( op.Operator);
                }
            }
        }

        
        #region Find Operator Usages
        private void FindOperatorUsages_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            e.CanExecute = cgv.SelectedElements.Count == 1;
        }

        private void FindOperatorUsages_CanExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            if (cgv.SelectedElements.Count != 1)
            {
                MessageBox.Show("Please select exactly one Operator in Workspace.");
                return;
            }
                

            var opWidget = cgv.SelectedElements.First() as OperatorWidget;
            if (opWidget == null)
            {
                MessageBox.Show("This Object does not appear to be an Operator.");
                return;
            }

            ListMetaOpUsages(opWidget.Operator.Definition);
        }

        public  void ListMetaOpUsages(MetaOperator opDefinition)
        {
            Logger.Info("Usages of {0}.{1} :", opDefinition.Namespace, opDefinition.Name);

            var usages = GetUsagesOfOperator(opDefinition);

            if (!usages.Any())
            {
                Logger.Info("    Operator is never used.");
                return;
            }

            foreach (var metaOp in usages)
            {
                string isUnused = GetUsagesOfOperator(metaOp).Any() ? "" : "(not used further)";
                Logger.Info(String.Format("    {0}.{1}   {2}", metaOp.Namespace, metaOp.Name, isUnused));
            }
        }

        public IEnumerable<MetaOperator> GetUsagesOfOperator(MetaOperator opDefinition)
        {
            var result = new List<MetaOperator>();
            foreach (var metaOp in App.Current.Model.MetaOpManager.MetaOperators)
            {
                var usedHere = false;
                foreach (var internalId in metaOp.Value.InternalOperatorsMetaOpId)
                {
                    if (internalId == opDefinition.ID)
                    {
                        usedHere = true;
                        break;                        
                    }
                }
                if (usedHere)
                {
                    result.Add(metaOp.Value);
                }
            }
            return result.OrderBy(x => x.Namespace);
        } 
        #endregion


        #region Find Operator Dependencies
        private void FindOperatorDependencies_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            e.CanExecute = cgv.SelectedElements.Count == 1;
        }

        private void FindOperatorDependencies_CanExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            if (cgv.SelectedElements.Count != 1)
            {
                MessageBox.Show("Please select exactly one Operator in Workspace.");
                return;
            }


            var opWidget = cgv.SelectedElements.First() as OperatorWidget;
            if (opWidget == null)
            {
                MessageBox.Show("This Object does not appear to be an Operator.");
                return;
            }

            ListMetaOpDependencies(opWidget.Operator.Definition);
        }

        public void ListMetaOpDependencies(MetaOperator opDefinition)
        {
            Logger.Info("Dependencies of {0}.{1} :", opDefinition.Namespace, opDefinition.Name);

            _metaOpDependencyCount = new Dictionary<MetaOperator, int>();
            UpdateMetaOpDependencyCount(opDefinition);

            if (!_metaOpDependencyCount.Any())
            {
                Logger.Info("    Operator does not have any dependencies to other operators.");
                return;
            }

            foreach (var metaOp in _metaOpDependencyCount.Keys.OrderBy(x => x.Namespace + x.Name))
            {
                Logger.Info(String.Format("    {0}.{1}  ({2})", metaOp.Namespace, metaOp.Name, _metaOpDependencyCount[metaOp]));
            }
        }

        private Dictionary<MetaOperator, int> _metaOpDependencyCount;
        private readonly ImportFbxScene _importFbxScene;

        private void UpdateMetaOpDependencyCount(MetaOperator metaOperator)
        {
            if (!_metaOpDependencyCount.ContainsKey(metaOperator))
            {
                _metaOpDependencyCount[metaOperator]=1;
            }
            else
            {
                _metaOpDependencyCount[metaOperator]++;                
            }
                
            foreach (var metaChildOpId in metaOperator.InternalOperatorsMetaOpId)
            {
                UpdateMetaOpDependencyCount(App.Current.Model.MetaOpManager.MetaOperators[metaChildOpId]);
            }
        }
        #endregion




        private void GenerateKeyFramesFromLogfileCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
            e.CanExecute = cgv.SelectedElements.Count > 0;
        }

        // cynic: die implementierung sollte irgendwann anders hin, aber nich nach MainWindow
        private void GenerateKeyFramesFromLogfileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var filepath = UIHelper.PickFileWithDialog(".", ".", "Log-file", "Log Files|*.log");
            var frameratesOverTime = new List<KeyValuePair<double, float>>();

            if (string.IsNullOrEmpty(filepath) || !File.Exists(filepath))
                return;

            // Read the file and display it line by line.
            using (var file = new StreamReader(filepath))
            {
                string line;
                float startTime = 0;

                while ((line = file.ReadLine()) != null)
                {
                    if (startTime == 0)
                    {
                        // Find start time in line that looks like..
                        // 18:41:02.113 (INF): Starting ...
                        var result = Regex.Match(line, @"(\d+):(\d*):(\d*).(\d*) \(INF\): Starting ...");
                        if (result.Success)
                        {
                            Logger.Info("Starttime found: {0}", line);
                            startTime = float.Parse(result.Groups[1].Value)*60*60f +
                                        float.Parse(result.Groups[2].Value)*60f +
                                        float.Parse(result.Groups[3].Value)*1f +
                                        float.Parse(result.Groups[4].Value)/1000.0f;

                        }
                    }
                    else
                    {
                        // Find debug outputs like...
                        // 18:41:02.343 (DBG): fps: 0017,80, mem: 1015444kb
                        var result = Regex.Match(line, @"(\d+):(\d*):(\d*).(\d*) \(DBG\): fps: (\d+)[,.]?(\d*)");
                        if (result.Success)
                        {
                            float localTime = float.Parse(result.Groups[1].Value)*60*60f +
                                              float.Parse(result.Groups[2].Value)*60f +
                                              float.Parse(result.Groups[3].Value)*1f +
                                              float.Parse(result.Groups[4].Value)/1000.0f -
                                              startTime;
                            float framerateFraction = result.Groups[6].Value != String.Empty ? float.Parse(result.Groups[6].Value)/100.0f : 0.0f;
                            float framerate = float.Parse(result.Groups[5].Value) + framerateFraction;
                            frameratesOverTime.Add(new KeyValuePair<double, float>(localTime, framerate));
                            Logger.Info("Framerate {0} -> {1} fps", localTime, framerate);
                        }
                    }
                }
            }

            if (frameratesOverTime.Count > 0)
            {
                var CurveEditor = App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor;
                CurveEditor.AddKeyframesToFirstCurve(frameratesOverTime);                    
            }
        }

        #endregion

        #region view commands
        private void ShowConsoleViewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;            
        }
        private void ShowConsoleViewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var consoleView = new ConsoleView();
            consoleView.Show(dockManager, true);
        }

        private void ShowGeneticVariationsViewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void ShowGeneticVariationsViewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var variationsView = new Components.GeneticVariations.GeneticVariationsPanel();
            variationsView.Show(dockManager, true);
        }

        #endregion

        #region playback control
        const double SLOW_PLAYBACK = 0.3;
        private void StartPlayback() {
            CompositionView cv = App.Current.MainWindow.CompositionView;
            if (cv.PlaySpeed <= 0.0) {
                cv.PlaySpeed = (Keyboard.Modifiers == ModifierKeys.Shift) ? SLOW_PLAYBACK :  1.0;
                App.Current.PlayStream(cv.PlaySpeed);
            }
        }

        private void StopPlayback() {
            CompositionView cv = App.Current.MainWindow.CompositionView;
            cv.PlaySpeed = 0.0;
            App.Current.StopStream();
        }

        private void PlayFasterForward() {
            CompositionView cv = App.Current.MainWindow.CompositionView;
            if (cv.PlaySpeed <= 0.0) {
                cv.PlaySpeed = (Keyboard.Modifiers == ModifierKeys.Shift) ? SLOW_PLAYBACK :  1.0;
            }
            else {
                if (Keyboard.Modifiers == ModifierKeys.Shift) {
                    cv.PlaySpeed *= 0.5;
                }
                else if (cv.PlaySpeed < MAX_PLAYBACK_SPEED)
                {
                    cv.PlaySpeed *= 2.0;                    
                }
            }
            App.Current.PlayStream(cv.PlaySpeed);
        }

        private void PlayBackward() {
            CompositionView cv = App.Current.MainWindow.CompositionView;
            if (cv.PlaySpeed > -1.0)
            {
                cv.PlaySpeed = -1.0;
            }
            else if (cv.PlaySpeed > -MAX_PLAYBACK_SPEED)
            {
                cv.PlaySpeed *= 2.0;
            }                
        }

        const  double MAX_PLAYBACK_SPEED = 8;

        private void RewindPlayback()
        {
            CompositionView cv = App.Current.MainWindow.CompositionView;
            App.Current.Model.GlobalTime = App.Current.Model.GlobalTime -  cv.XTimeView.XBeatMarker.BPM / 16 / 4;
            App.Current.UpdateRequiredAfterUserInteraction = true;
            App.Current.SetStreamToTime(App.Current.Model.GlobalTime);
        }

        private void ToggleMutePlayback() {
            App.Current.ToggleMutePlayback();
        }


        private void ConnectRemoteClient()
        {
#if USE_SOCKETS
            var popup = new TextInputWindow();
            popup.XMainTitle.Text = String.Format("Enter IP Address");
            popup.XTextBox.Text = "127.0.0.1";
            popup.XTextBox.SelectAll();
            popup.XTextBox.Focus();
            var result = popup.ShowDialog();
            if (result.HasValue && result.Value)
            {
                try
                {
                    var ipAddress = System.Net.IPAddress.Parse(popup.XTextBox.Text);
                    App.Current.UndoRedoStack.AddSocketToRemoteReceiver(ipAddress);
                    Logger.Info("Added {0} as client.", popup.XTextBox.Text);
                }
                catch (System.Exception)
                {
                    Logger.Warn("No valid ip address entered!");
                }
            }
#endif
        }
        
        #endregion



        private void OnDragMoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Maximized;
                }                
            }
            else
            {
                DragMove();
            }                 
        }


        private void OnMinimizeWindow(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        private void OnMaximizeWindow(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
        }


        private void OnCloseWindow(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            SpaceMouseHandlerWpf = new SpaceMouseWPFHandler();
        }

        private void CutHandler(object sender, ExecutedRoutedEventArgs e)
        {
            CompositionView.CompositionGraphView.CopySelectionToClipboard();
            CompositionView.CompositionGraphView.RemoveSelectedElements();
        }

        private void CutCanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            var currentSelectedGuiElements = CompositionView.CompositionGraphView.SelectedElements;
            e.CanExecute = currentSelectedGuiElements.Count > 0 && currentSelectedGuiElements[0] is OperatorWidget;
        }

        private void CopyHandler(object sender, ExecutedRoutedEventArgs e)
        {
            CompositionView.CompositionGraphView.CopySelectionToClipboard();
        }

        private void CopyCanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            var currentSelectedGuiElements = CompositionView.CompositionGraphView.SelectedElements;
            e.CanExecute = currentSelectedGuiElements.Count > 0 && currentSelectedGuiElements[0] is OperatorWidget;
        }

        private void PasteHandler(object sender, ExecutedRoutedEventArgs e)
        {
            CompositionView.CompositionGraphView.PasteClipboard();
        }

        private void PasteCanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsText(TextDataFormat.UnicodeText);
        }




    }

}
