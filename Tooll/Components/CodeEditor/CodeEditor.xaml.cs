// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Framefield.Core;
using Framefield.Core.Rendering;
using Framefield.Tooll.Components;
using Framefield.Tooll.Components.CodeEditor;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace Framefield.Tooll
{
    /**
     * This UserControl links an AvalonEdit, CodeSectionManager, CodeSectionTabControl to edit and compile code operators.
     * It is used in the Composition graph view
     */
    public partial class CodeEditor : UserControl
    {

        public CodeEditor() {
            InitializeComponent();
            XCodeTextBox.Options.ConvertTabsToSpaces = true;
            XCodeTextBox.Options.ShowTabs = true;
            XCodeTextBox.Options.ShowSpaces = true;
            XCodeTextBox.Options.IndentationSize = 4;
            XCodeTextBox.TextArea.TextView.NonPrintableCharacterBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#30000000"));
            XCodeTextBox.TextArea.TextView.NonPrintableCharacterBrush.Freeze();

            // Highlight current line
            XCodeTextBox.TextArea.TextView.BackgroundRenderers.Add(new HighlightCurrentLineBackgroundRenderer(XCodeTextBox));

            // Highlight errors in code
            _errorHighlightRenderer = new HighlightErrorLineBackgroundRenderer(XCodeTextBox);
            XCodeTextBox.TextArea.TextView.BackgroundRenderers.Add(_errorHighlightRenderer);
            XCodeTextBox.TextArea.Caret.PositionChanged += (sender, e) => XCodeTextBox.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

            XCodeTextBox.TextChanged += CodeTextBox_TextChanged;
            XCodeSectionTabControl.SectionChangedEvent += XCodeSectionTabControl_SectionChangedHandler;

            XCodeTextBox.KeyDown += XCodeTextBox_KeyDownHandler;
            XCodeTextBox.KeyUp += XCodeTextBox_KeyUpHandler;
            XCodeTextBox.MouseDoubleClick += XCodeTextBox_MouseDoubleClick;

            bool showExternalEditorButton = App.Current.UserSettings.TryGet("Tooll.ShowExternalEditorOption", false);
            XUseExternalEditorButton.Visibility = showExternalEditorButton ? Visibility.Visible : Visibility.Hidden;
        }

        void XCodeTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var CGV = App.Current.MainWindow.CompositionView.XCompositionGraphView;
            CGV.SelectionHandler.SetElement(CGV);
        }

        void XCodeTextBox_KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) 
                return;

            if (e.Key == Key.F) 
            {
                FindReplaceDialog.Show(XCodeTextBox, FindReplaceDialog.SearchType.Find);
                e.Handled = true;
            }

            if (e.Key == Key.H)
            {
                FindReplaceDialog.Show(XCodeTextBox, FindReplaceDialog.SearchType.Replace);
                e.Handled = true;
            }
            if (e.Key == Key.OemPlus)
            {
                XCodeTextBox.FontSize++;
                e.Handled = true;
            }

            if (e.Key == Key.OemMinus)
            {
                if(XCodeTextBox.FontSize > MIN_FONT_SIZE)
                    XCodeTextBox.FontSize--;
                e.Handled = true;
            }
        }

        private const int MIN_FONT_SIZE = 6;

        void XCodeTextBox_KeyUpHandler(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Compile();
            }
            else if(e.Key == Key.Escape)
            {
                TryToUpdateAndCompileFXSourceCode();
            }
        }

        private HighlightErrorLineBackgroundRenderer _errorHighlightRenderer;

        /**
         * This is set by CompositionGraph view when selecting a code op
         */
        private Operator _operator;
        public Operator Operator {
            get {
                return _operator;
            }
            set {
                _operator = value;
                _metaOperator = _operator != null ? _operator.Definition : null;

                XUseExternalEditorButton.IsChecked = false;

                if (_operator != null) {
                    var newCsm = new List<CodeSectionManager>();

                    _scriptCodeSectionManager = new CodeSectionManager
                                                    {
                                                        CompleteCode = GetOperatorScriptCode(),
                                                        CodeDefinition = new ScriptCodeDefinition()
                                                    };
                    newCsm.Add(_scriptCodeSectionManager);

                    var csmToBeActive = _scriptCodeSectionManager;

                    var fxSource = _operator.InternalParts[0].Func as IFXSourceCode;
                    if (fxSource != null) 
                    {
                        for (int i = 0; i < fxSource.NumCodes(); ++i) 
                        {
                            var fxCodeSectionManager = new CodeSectionManager()
                                                           {
                                                               CompleteCode = fxSource.GetCode(i),
                                                               CodeDefinition = new ShaderCodeDefinition(),
                                                               CodeIndex = i
                                                           };
                            newCsm.Add(fxCodeSectionManager);

                            if (i == 0)
                                csmToBeActive = fxCodeSectionManager;
                        }
                    }

                    XCodeSectionTabControl.CodeSectionManagers = newCsm;

                    string sectionIdToBeActive;
                    if (csmToBeActive.GetSectionCode(csmToBeActive.CodeDefinition.InitialSelectedSectionId) == null)
                        sectionIdToBeActive = "*";
                    else
                        sectionIdToBeActive = csmToBeActive.CodeDefinition.InitialSelectedSectionId;

                    XCodeSectionTabControl.SetActiveSectionId(sectionIdToBeActive);
                }
            }
        }


        #region event handlers
        private void XCodeSectionTabControl_SectionChangedHandler(object o, CodeSectionTabControl.SectionChangedEventArgs e) {
            if (o is CodeSectionTabControl) {
                ActiveCodeSectionManager = e.CodeSectionManager;
                _activeSectionId = e.CodeSection.Id;
                XCodeTextBox.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(ActiveCodeSectionManager.CodeDefinition.LangageAbbrev);
                _enableTextChangeEvent = false;
                XCodeTextBox.Text = ActiveCodeSectionManager.GetSectionCode(_activeSectionId);
                _errorHighlightRenderer.CurrentCodeSectionStartLine = ActiveCodeSectionManager.GetCodeSectionStartLine(_activeSectionId);
                _enableTextChangeEvent = true;
                XUseExternalEditorButton.IsEnabled = ActiveCodeSectionManager.CodeDefinition.LangageAbbrev == "HLSL";
            }
        }

        private void CodeTextBox_TextChanged(object sender, EventArgs e) {
            if (_enableTextChangeEvent) {
                ActiveCodeSectionManager.ReplaceCodeInsideSection(_activeSectionId, XCodeTextBox.Text);
                if ((bool)XAutoCompileButton.IsChecked) {
                    TryToUpdateAndCompileFXSourceCode();
                }
            }
        }

        private void AdditionalAssembliesButton_Clicked(object sender, EventArgs e)
        {
            var operatorPartDefinition = Operator.Definition.OperatorParts.First().Item2;

            var assemblyDialog = new AdditionalAssembliesWindow(operatorPartDefinition.AdditionalAssemblies, Operator.Definition.SupplierAssemblyNames);
            assemblyDialog.ShowDialog();

            if (assemblyDialog.DialogResult == true)
            {
                operatorPartDefinition.AdditionalAssemblies = assemblyDialog.AdditionalAssemblies;
                Operator.Definition.SetSupplierAssemblies(assemblyDialog.SupplierAssemblies);
                Compile();
            }
        }

        private void UseExternalEditorButton_Checked(object sender, RoutedEventArgs e)
        {
            XCodeTextBox.IsEnabled = false;
            StartWatchingEditedFile();
        }

        private void StartWatchingEditedFile()
        {
            var fileinfo = new FileInfo(@".\Temp\" + Guid.NewGuid() + ".hlsl");

            using (var writer = new StreamWriter(fileinfo.FullName))
            {
                writer.Write(XCodeTextBox.Text);
            }
            Process.Start("explorer.exe", "/select," + fileinfo.FullName);

            try
            {
                _filewatcher = new FileSystemWatcher(fileinfo.DirectoryName, fileinfo.Name)
                                   {
                                       NotifyFilter = NotifyFilters.LastWrite
                                   };
                Logger.Info("watching {0}\\{1}", fileinfo.DirectoryName, fileinfo.Name);
                _filewatcher.Changed += Filewatcher_Changed;
                _filewatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error setting up filewatcher for {0}. Problem: {1}", fileinfo.FullName, ex.Message);
            }
        }

        private void UseExternalEditorButton_Unchecked(object sender, RoutedEventArgs e)
        {
            XCodeTextBox.IsEnabled = true;
            StopWatchingEditedFile();
        }

        private void StopWatchingEditedFile()
        {
            if (_filewatcher != null)
            {
                _filewatcher.EnableRaisingEvents = false;
                Utilities.DisposeObj(ref _filewatcher);
            }
        }

        private void Filewatcher_Changed(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            Logger.Info("changed: {0}", fileSystemEventArgs.Name);
            var text = string.Empty;
            try
            {
                using (var reader = new StreamReader(fileSystemEventArgs.FullPath))
                {
                    text = reader.ReadToEnd();
                }
                Action action = () =>
                                {
                                    XCodeTextBox.Text = text;
                                    Compile();
                                };
                App.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
            }
            catch
            {
                // other app locked file
            }
        }

        private void OnCompiledClicked(object sender, RoutedEventArgs e) {
            Compile();
        }

        #endregion

        public bool Compile() {
            if (_metaOperator == null)
                return false;

            int keepCurrentLineNumer = XCodeTextBox.CaretOffset;

            TryToUpdateAndCompileFXSourceCode();
            UpdateAndCompileScriptSourceCode();

            App.Current.UpdateRequiredAfterUserInteraction = true;

            // Update text editor
            XCodeTextBox.Text = ActiveCodeSectionManager.GetSectionCode(_activeSectionId);
            XCodeTextBox.CaretOffset = Math.Min(keepCurrentLineNumer, XCodeTextBox.Text.Count() - 1);
            return true;
        }

        private void TryToUpdateAndCompileFXSourceCode() {
            var fxSourceCodeOp = _operator.InternalParts[0].Func as IFXSourceCode;
            if (fxSourceCodeOp != null && ActiveCodeSectionManager != _scriptCodeSectionManager) {
                UpdateSourceCode(ActiveCodeSectionManager);
                fxSourceCodeOp.SetCode(ActiveCodeSectionManager.CodeIndex, ActiveCodeSectionManager.CompleteCode);
                var compileErrors = fxSourceCodeOp.Compile(ActiveCodeSectionManager.CodeIndex);
                ActiveCodeSectionManager.CompilerErrorCollection = compileErrors;
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
        }

        private void UpdateAndCompileScriptSourceCode() {
            UpdateSourceCode(_scriptCodeSectionManager);
            GetCodeOperatorPart().Script = _scriptCodeSectionManager.CompleteCode;
            var results = GetCodeOperatorPart().Compile();
            _scriptCodeSectionManager.CompilerErrorCollection = results;
        }

        private void UpdateSourceCode(CodeSectionManager codeSectionManager) {
            var codeDefinition = codeSectionManager.CodeDefinition;
            foreach (var sectionId in codeDefinition.AutoGeneratedSectionIds) {
                var code = codeDefinition.GenerateSectionCode(sectionId, _metaOperator, GetCodeOperatorPart());
                if (!codeSectionManager.ReplaceCodeInsideSection(sectionId, code)) {
                    Logger.Debug("Missing smart comment for source code section {0}.", sectionId);
                }
            }
            codeSectionManager.CompleteCode = codeDefinition.UpdateScript(_metaOperator, codeSectionManager.CompleteCode);
        }

        private MetaOperatorPart GetCodeOperatorPart() {
            return _metaOperator.OperatorParts[0].Item2;
        }

        private String GetOperatorScriptCode() {
            return GetCodeOperatorPart().Script.Replace("\r\n","\n").Replace("\r","\n");
        }

        /**
         * Highlight current line of editor
         */
        private class HighlightCurrentLineBackgroundRenderer : IBackgroundRenderer
        {
            public HighlightCurrentLineBackgroundRenderer(ICSharpCode.AvalonEdit.TextEditor editor) {
                _editor = editor;
                _brush = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
                _brush.Freeze();
            }

            public KnownLayer Layer {
                get { return KnownLayer.Background; }
            }

            public void Draw(TextView textView, DrawingContext drawingContext) {
                if (_editor.Document == null)
                    return;

                textView.EnsureVisualLines();
                var currentLine = _editor.Document.GetLineByOffset(_editor.CaretOffset);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, currentLine))
                {
                    var rect2 = new Rect(rect.Location, new Size(textView.ActualWidth, rect.Height));
                    if (rect2.Height > 1 && rect2.Width > 1)
                    {
                        drawingContext.DrawRectangle(_brush, null, rect2);
                    }
                }
            }

            private ICSharpCode.AvalonEdit.TextEditor _editor;
            private SolidColorBrush _brush;
        }

        /**
         * Highlight current line of editor
         */
        private class HighlightErrorLineBackgroundRenderer : IBackgroundRenderer
        {

            public CodeSectionManager CurrentCodeSectionManager { private get; set; }
            public int CurrentCodeSectionStartLine { private get; set; }
            public string CurrentCodeSectionId { get; set; }

            public HighlightErrorLineBackgroundRenderer(ICSharpCode.AvalonEdit.TextEditor editor)
            {
                _editor = editor;
                _brush = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
                _brush.Freeze();
                CurrentCodeSectionId = string.Empty;
            }

            public KnownLayer Layer
            {
                get { return KnownLayer.Background; }
            }

            public void Draw(TextView textView, DrawingContext drawingContext)
            {
                if (_editor.Document == null || CurrentCodeSectionManager == null || CurrentCodeSectionManager.CompilerErrorCollection == null)
                    return;

                textView.EnsureVisualLines();
                for (int i = 0; i < CurrentCodeSectionManager.CompilerErrorCollection.Count; ++i )
                {
                    var error = CurrentCodeSectionManager.CompilerErrorCollection[i];
                    int lineNumber = error.Line - CurrentCodeSectionStartLine;
                    if (lineNumber > 0 && lineNumber < _editor.LineCount)
                    {
                        var line = _editor.Document.GetLineByNumber(lineNumber);
                        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
                        {
                            var rect2 = new Rect(rect.Location, new Size(textView.ActualWidth, rect.Height));
                            if (rect2.Height > 1 && rect2.Width > 1)
                            {
                                drawingContext.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(0x20, 0xff, 0, 0)), new Pen(new SolidColorBrush(Color.FromArgb(0xa0, 0xff, 0, 0)), 1), rect2, 3, 3);
                            }
                            var column = error.Column - CurrentCodeSectionManager.GetSectionIndendationSize(CurrentCodeSectionId);

                            DrawColumnIndicator(drawingContext, new Point(rect2.Left + column * 7+3, rect2.Top-1));
                        }
                    }
                }
            }

            private void DrawColumnIndicator( DrawingContext dc, Point position) {
                const double HEIGHT = 4;
                Point start = new Point(position.X + HEIGHT, position.Y);
                LineSegment[] segments = new LineSegment[] { 
                    new LineSegment(new Point(position.X, position.Y + HEIGHT ), true), 
                    new LineSegment(new Point( position.X - HEIGHT, position.Y), true) 
                };
                PathFigure figure = new PathFigure(start, segments, false);
                PathGeometry geo = new PathGeometry(new PathFigure[] { figure });
                dc.DrawGeometry(Brushes.Red, new Pen(Brushes.Black, 0.5), geo);
            }

            private ICSharpCode.AvalonEdit.TextEditor _editor;
            private SolidColorBrush _brush;
        }


        bool _enableTextChangeEvent = true;
        public string _activeSectionId {get;set;}
        public CodeSectionManager ActiveCodeSectionManager {
            get {
                return _activeCodeSectionManager;
            }
            set {
                _activeCodeSectionManager = value;
                _errorHighlightRenderer.CurrentCodeSectionManager = value;
            }
        }
        private CodeSectionManager _activeCodeSectionManager = new CodeSectionManager();
        private CodeSectionManager _scriptCodeSectionManager = new CodeSectionManager();
        private MetaOperator _metaOperator;
        private FileSystemWatcher _filewatcher;
        private DispatcherISyncInvoke _dispi = new DispatcherISyncInvoke(App.Current.Dispatcher);
    }

    internal class DispatcherISyncInvoke : ISynchronizeInvoke
    {
        #region Internal IAsync Class

        private class DispatcherOperationAsync : IAsyncResult, IDisposable
        {
            private readonly DispatcherOperation _dop;
            private ManualResetEvent _handle = new ManualResetEvent(false);

            #region Implementation of IAsyncResult

            public DispatcherOperationAsync(DispatcherOperation dispatcherOperation)
            {
                _dop = dispatcherOperation;
                _dop.Aborted += DopAborted;
                _dop.Completed += DopCompleted;
            }

            public object Result
            {
                get
                {
                    if (!IsCompleted)
                        throw new InvalidAsynchronousStateException("Not Completed");
                    return _dop.Result;
                }
            }

            private void DopCompleted(object sender, EventArgs e)
            {
                _handle.Set();
            }

            private void DopAborted(object sender, EventArgs e)
            {
                _handle.Set();
            }

            public bool IsCompleted { get { return _dop.Status == DispatcherOperationStatus.Completed; } }

            public WaitHandle AsyncWaitHandle { get { return _handle; } }

            public object AsyncState
            {
                get
                {
                    //Not Implementted
                    return null;
                }
            }

            public bool CompletedSynchronously { get { return false; } }

            #endregion

            #region Implementation of IDisposable

            public void Dispose()
            {
                if (_handle == null) return;
                _handle.Dispose();
                _handle = null;
            }

            #endregion
        }

        #endregion

        private readonly Dispatcher _dispatcher;

        #region Implementation of ISynchronizeInvoke

        public DispatcherISyncInvoke(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            return new DispatcherOperationAsync(_dispatcher.BeginInvoke(method, args));
        }

        public object EndInvoke(IAsyncResult result)
        {
            result.AsyncWaitHandle.WaitOne();
            if (result is DispatcherOperationAsync)
                return ((DispatcherOperationAsync) result).Result;
            return null;
        }

        public object Invoke(Delegate method, object[] args)
        {
            return InvokeRequired ? EndInvoke(BeginInvoke(method, args)) : method.DynamicInvoke(args);
        }

        public bool InvokeRequired { get { return _dispatcher.CheckAccess(); } }

        #endregion
    }
}
