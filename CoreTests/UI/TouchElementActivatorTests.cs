// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core;
using Framefield.Core.UI;

namespace CoreTests.UI
{
    [TestClass]
    public class TouchElementActivatorTests
    {
        struct TestStruct {}

        enum States
        {
            State1,
            State2,
            State3,
            State4,
            State5
        }

        class TouchElement : IDisengageable
        {
            public TouchElement(string id)
            {
                ID = id;
            }

            public string ID { get; private set; }
            public bool Enabled { get; set; }
        }

        private static string ELEMENT_1_ID = "element1";
        private static string ELEMENT_2_ID = "element2";
        private static string ELEMENT_3_ID = "element3";
        private static string ELEMENT_4_ID = "element4";
        TouchElement _element1 = new TouchElement(ELEMENT_1_ID);
        TouchElement _element2 = new TouchElement(ELEMENT_2_ID);
        TouchElement _element3 = new TouchElement(ELEMENT_3_ID);
        TouchElement _element4 = new TouchElement(ELEMENT_4_ID);
        private List<TouchElement> _elements;

        private TouchElementActivator<States>.Entry[] _entries = new []
                                                                     {
                                                                         new TouchElementActivator<States>.Entry
                                                                             {
                                                                                 EnabledStates = new[] { States.State1, States.State2 },
                                                                                 TouchElementIDs = new[] { ELEMENT_1_ID, ELEMENT_2_ID }
                                                                             },
                                                                         new TouchElementActivator<States>.Entry
                                                                             {
                                                                                 EnabledStates = new[] { States.State3 },
                                                                                 TouchElementIDs = new[] { ELEMENT_1_ID, ELEMENT_2_ID, ELEMENT_3_ID }
                                                                             },
                                                                         new TouchElementActivator<States>.Entry
                                                                             {
                                                                                 EnabledStates = new[] { States.State4 },
                                                                                 TouchElementIDs = new[] { ELEMENT_4_ID }
                                                                             }
                                                                     };

        [TestInitialize]
        public void Setup()
        {
            _elements = new List<TouchElement>() { _element1, _element2, _element3, _element4 };
        }

        #region ctor tests
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_InitWithNonEnumGenericType_ThrowsException()
        {
            new TouchElementActivator<TestStruct>(null);
        }

        [TestMethod]
        public void Ctor_InitWithEnumGenericType_NoExceptionIsThrown()
        {
            Assert.IsNotNull(new TouchElementActivator<States>(new TouchElementActivator<States>.Entry[] {}));
        }

        [TestMethod]
        public void Ctor_Init_TouchElementsIsEmptyArray()
        {
            var activator = new TouchElementActivator<States>(new TouchElementActivator<States>.Entry[] { });

            Assert.AreEqual(0, activator.TouchElements.Count());
        }

        [TestMethod]
        public void Ctor_InitWithNoEntries_ForEachStateADictEntryIsGenerated()
        {
            var activator = new TouchElementActivator<States>(new TouchElementActivator<States>.Entry[] {});

            var privateObject = new PrivateObject(activator, new PrivateType(typeof(TouchElementActivator<States>)));
            var enabledElementPerStatesDict = (Dictionary<States, List<string>>) privateObject.GetField("_enabledElementsPerState");

            Assert.AreEqual(Utilities.GetValues<States>().Count(), enabledElementPerStatesDict.Count());
        }

        [TestMethod]
        public void Ctor_InitWithEntries_ForEachStateADictEntryIsGenerated()
        {
            var activator = new TouchElementActivator<States>(_entries);

            var privateObject = new PrivateObject(activator, new PrivateType(typeof(TouchElementActivator<States>)));
            var enabledElementPerStatesDict = (Dictionary<States, List<string>>) privateObject.GetField("_enabledElementsPerState");

            Assert.AreEqual(Utilities.GetValues<States>().Count(), enabledElementPerStatesDict.Count());
        }

        [TestMethod]
        public void Ctor_InitWithEntries_EachStateHasStoredTheRightElementIDs()
        {
            var activator = new TouchElementActivator<States>(_entries);

            var privateObject = new PrivateObject(activator, new PrivateType(typeof(TouchElementActivator<States>)));
            var enabledElementPerStatesDict = (Dictionary<States, List<string>>) privateObject.GetField("_enabledElementsPerState");

            Assert.AreEqual(2, enabledElementPerStatesDict[States.State1].Count());
            Assert.AreEqual(2, enabledElementPerStatesDict[States.State2].Count());
            Assert.AreEqual(3, enabledElementPerStatesDict[States.State3].Count());
            Assert.AreEqual(1, enabledElementPerStatesDict[States.State4].Count());
            Assert.AreEqual(0, enabledElementPerStatesDict[States.State5].Count());
        }

        #endregion


        #region EnableTouchElements

        [TestMethod]
        public void EnableTouchElements_CheckState1_Element1And2AreEnabled()
        {
            var activator = new TouchElementActivator<States>(_entries) { TouchElements = _elements };

            activator.EnableTouchElements(States.State1);

            Assert.AreEqual(true, _element1.Enabled);
            Assert.AreEqual(true, _element2.Enabled);
            Assert.AreEqual(false, _element3.Enabled);
            Assert.AreEqual(false, _element4.Enabled);
        }

        [TestMethod]
        public void EnableTouchElements_CheckState2_Element1And2AreEnabled()
        {
            var activator = new TouchElementActivator<States>(_entries) { TouchElements = _elements };

            activator.EnableTouchElements(States.State2);

            Assert.AreEqual(true, _element1.Enabled);
            Assert.AreEqual(true, _element2.Enabled);
            Assert.AreEqual(false, _element3.Enabled);
            Assert.AreEqual(false, _element4.Enabled);
        }

        [TestMethod]
        public void EnableTouchElements_CheckState3_Element1To3AreEnabled()
        {
            var activator = new TouchElementActivator<States>(_entries) { TouchElements = _elements };

            activator.EnableTouchElements(States.State3);

            Assert.AreEqual(true, _element1.Enabled);
            Assert.AreEqual(true, _element2.Enabled);
            Assert.AreEqual(true, _element3.Enabled);
            Assert.AreEqual(false, _element4.Enabled);
        }

        [TestMethod]
        public void EnableTouchElements_CheckState4_OnlyElement4IsEnabled()
        {
            var activator = new TouchElementActivator<States>(_entries) { TouchElements = _elements };

            activator.EnableTouchElements(States.State4);

            Assert.AreEqual(false, _element1.Enabled);
            Assert.AreEqual(false, _element2.Enabled);
            Assert.AreEqual(false, _element3.Enabled);
            Assert.AreEqual(true, _element4.Enabled);
        }

        [TestMethod]
        public void EnableTouchElements_CheckState5_NoElementIsEnabled()
        {
            var activator = new TouchElementActivator<States>(_entries) { TouchElements = _elements };

            activator.EnableTouchElements(States.State5);

            Assert.AreEqual(false, _element1.Enabled);
            Assert.AreEqual(false, _element2.Enabled);
            Assert.AreEqual(false, _element3.Enabled);
            Assert.AreEqual(false, _element4.Enabled);
        }

        #endregion

    }
}
