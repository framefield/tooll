// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace CoreTests
{
    [TestClass]
    public class UndoRedoStackTests
    {
        class TestCommand : ICommand
        {
            public string Name { get { return "TestCommand"; } }
            public bool IsUndoable { get { return true; } }
            public void Do() { }
            public void Undo() { }
        }

        class UndoableCommand : ICommand
        {
            public string Name { get { return "UndoableCommand"; } }
            public bool IsUndoable { get { return false; } }
            public void Do() { }
            public void Undo() { }
        }

        private UndoRedoStack _undoRedoStack;

        [TestInitialize]
        public void Initialize()
        {
            _undoRedoStack = new UndoRedoStack();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _undoRedoStack.Dispose();
            _undoRedoStack = null;
        }


        #region Initial State
        [TestMethod]
        public void InitialState_AfterConstruction_CanUndoIsFalse() {
            Assert.AreEqual(false, _undoRedoStack.CanUndo);
        }

        [TestMethod]
        public void InitialState_AfterConstruction_CanRedoIsFalse() {
            Assert.AreEqual(false, _undoRedoStack.CanRedo);
        }

        [TestMethod]
        public void InitialState_AfterConstruction_UndoListIsEmpty() {
            Assert.AreEqual(0, _undoRedoStack.UndoList.Count());
        }

        [TestMethod]
        public void InitialState_AfterConstruction_RedoListIsEmpty() {
            Assert.AreEqual(0, _undoRedoStack.RedoList.Count());
        }
        #endregion

        #region Add
        [TestMethod]
        public void Add_TestCommand_CanUndoIsTrue() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());

            Assert.AreEqual(true, _undoRedoStack.CanUndo);
        }

        [TestMethod]
        public void Add_TestCommand_UndoListHasCount1() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());

            Assert.AreEqual(1, _undoRedoStack.UndoList.Count());
        }

        [TestMethod]
        public void Add_TestCommand_UndoListEntryIsTestCommand() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());

            Assert.AreEqual((new TestCommand()).Name, _undoRedoStack.UndoList.First());
        }

        [TestMethod]
        public void Add_TestCommand_RedoListIsEmpty() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());

            Assert.AreEqual(0, _undoRedoStack.RedoList.Count());
        }

        [TestMethod]
        public void Add_UndoableCommandToNonEmptyStacks_CanUndoIsFalse() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.AddAndExecute(new UndoableCommand());

            Assert.AreEqual(false, _undoRedoStack.CanUndo);
        }

        [TestMethod]
        public void Add_UndoableCommandToNonEmptyStacks_CanRedoIsFalse() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.AddAndExecute(new UndoableCommand());

            Assert.AreEqual(false, _undoRedoStack.CanRedo);
        }

        [TestMethod]
        public void Add_UndoableCommandToNonEmptyStacks_UndoListIsEmpty() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.AddAndExecute(new UndoableCommand());

            Assert.AreEqual(0, _undoRedoStack.UndoList.Count());
        }

        [TestMethod]
        public void Add_UndoableCommandToNonEmptyStacks_RedoListIsEmpty() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.AddAndExecute(new UndoableCommand());

            Assert.AreEqual(0, _undoRedoStack.RedoList.Count());
        }
        #endregion

        #region Undo
        [TestMethod]
        public void Undo_AddedTestCommand_CanUndoIsFalse() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();

            Assert.AreEqual(false, _undoRedoStack.CanUndo);
        }

        [TestMethod]
        public void Undo_AddedTestCommand_CanRedoIsTrue() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();

            Assert.AreEqual(true, _undoRedoStack.CanRedo);
        }

        [TestMethod]
        public void Undo_AddedTestCommand_UndoListIsEmpty() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();

            Assert.AreEqual(0, _undoRedoStack.UndoList.Count());
        }

        [TestMethod]
        public void Undo_AddedTestCommand_RedoListCountIs1() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();

            Assert.AreEqual(1, _undoRedoStack.RedoList.Count());
        }

        [TestMethod]
        public void Undo_TestCommand_RedoListEntryIsTestCommand() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();

            Assert.AreEqual((new TestCommand()).Name, _undoRedoStack.RedoList.First());
        }
        #endregion

        #region Redo
        public void Redo_UndoedTestCommand_CanUndoIsTrue() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Redo();

            Assert.AreEqual(true, _undoRedoStack.CanUndo);
        }

        public void Redo_UndoedTestCommand_CanRedoIsFalse() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Redo();

            Assert.AreEqual(false, _undoRedoStack.CanRedo);
        }

        public void Redo_UndoedTestCommand_UndoListHasCount1() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Redo();

            Assert.AreEqual(1, _undoRedoStack.UndoList.Count());
        }

        [TestMethod]
        public void Redo_UndoedTestCommand_UndoListEntryIsTestCommand() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Redo();

            Assert.AreEqual((new TestCommand()).Name, _undoRedoStack.UndoList.First());
        }

        [TestMethod]
        public void Redo_UndoedTestCommand_RedoListIsEmpty() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Redo();

            Assert.AreEqual(0, _undoRedoStack.RedoList.Count());
        }
        #endregion

        #region Clear
        [TestMethod]
        public void Clear_WithNonEmptyUndoAndRedoStacks_CanUndoIsFalse() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Clear();

            Assert.AreEqual(false, _undoRedoStack.CanUndo);
        }

        [TestMethod]
        public void Clear_WithNonEmptyUndoAndRedoStacks_CanRedoIsFalse() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Clear();

            Assert.AreEqual(false, _undoRedoStack.CanRedo);
        }

        [TestMethod]
        public void Clear_WithNonEmptyUndoAndRedoStacks_UndoListIsEmpty() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Clear();

            Assert.AreEqual(0, _undoRedoStack.UndoList.Count());
        }

        [TestMethod]
        public void Clear_WithNonEmptyUndoAndRedoStacks_RedoListIsEmpty() 
        {
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.AddAndExecute(new TestCommand());
            _undoRedoStack.Undo();
            _undoRedoStack.Clear();

            Assert.AreEqual(0, _undoRedoStack.RedoList.Count());
        }

        #endregion

        #region Helper
        [TestMethod]
        public void GetNamespaceGuidFromFunctionAssemblyName_CheckIfGuidIsFoundInGivenAssemblyFilename_GuidIsFound()
        {
            var input = "CurveFunc_ID49163ffa-f4e9-406f-8b5c-602c6adc276f_Version757e70ab-555d-42f2-bb0d-1fdbdf77841b";

            var guidGroup = UndoRedoStack.GetNamespaceGuidFromFunctionAssemblyName(input);

            Assert.AreEqual("49163ffa-f4e9-406f-8b5c-602c6adc276f", guidGroup);
        }


        #endregion
    }
}
