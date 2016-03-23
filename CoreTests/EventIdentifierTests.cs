// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core;

namespace CoreTests
{
    [TestClass]
    public class EventIdentifierTests
    {
        private const string EventName = "my fancy event";
        private const string EventID = "5B8DF7F4-6790-45D2-A268-39E6E9C02700";

        private class EventIdentifierTestable
        {
            [EventIdentifier(EventName, EventID)]
            public event EventHandler<EventArgs> TestEvent;
        }


        private const string HandlerName = "my fancy event handler";
        private const string HandlerID = "4827D25B-73C5-437F-92BC-54757F2C18B0";

        private class EventHandlerIdentifierTestable
        {
            [EventHandlerIdentifier(HandlerName, HandlerID)]
            public void Handler(object o, EventArgs e)
            {
            }
        }


        [TestMethod]
        public void EventIdentifier_testClassWithOneEvent_AttributeCanBeFound()
        {
            var testClass = new EventIdentifierTestable();

            var events = testClass.GetType().GetEvents();
            Assert.AreEqual(1, events.Count());

            var @event = events.First();
            var eventAttributes = @event.GetCustomAttributes(true);
            Assert.AreEqual(1, eventAttributes.Count());

            var attr = eventAttributes.First();
            Assert.IsInstanceOfType(attr, typeof(EventIdentifier));

            var eventIdentifier = attr as EventIdentifier;
            Assert.AreEqual( Guid.Parse(EventID), eventIdentifier.id);
            Assert.AreEqual(EventName, eventIdentifier.name);
        }

        [TestMethod]
        public void EventIdentifier_GetEventsIn_TestClassWithOneEvent_OneEventIdentified()
        {
            var testClass = new EventIdentifierTestable();
            var events = EventIdentifier.GetEventsIn(testClass.GetType());

            Assert.AreEqual(1, events.Length);
            var eventIdentifier = events.First().Item1;
            Assert.AreEqual(Guid.Parse(EventID), eventIdentifier.id);
            Assert.AreEqual(EventName, eventIdentifier.name);
        }

        [TestMethod]
        public void EventHandlerIdentifier_testClassWithOneHandler_AttributeCanBeFound()
        {
            var testClass = new EventHandlerIdentifierTestable();

            var methods = testClass.GetType().GetMethods();
            Assert.AreEqual(5, methods.Count());

            var handler = (from m in methods where m.Name == "Handler" select m).First();
            var handlerAttributes = handler.GetCustomAttributes(true);
            Assert.AreEqual(1, handlerAttributes.Count());

            var attr = handlerAttributes.First();
            Assert.IsInstanceOfType(attr, typeof(EventHandlerIdentifier));

            var eventHandlerIdentifier = attr as EventHandlerIdentifier;
            Assert.AreEqual(Guid.Parse(HandlerID), eventHandlerIdentifier.id);
            Assert.AreEqual(HandlerName, eventHandlerIdentifier.name);
        }

        [TestMethod]
        public void EventHandlerIdentifier_GetHandlerIn_TestClassWithOneHandler_OneHandlerIdentified()
        {
            var testClass = new EventHandlerIdentifierTestable();
            var handler = EventHandlerIdentifier.GetHandlerIn(testClass.GetType());

            Assert.AreEqual(1, handler.Length);
            var eventHandlerIdentifier = handler.First().Item1;
            Assert.AreEqual(Guid.Parse(HandlerID), eventHandlerIdentifier.id);
            Assert.AreEqual(HandlerName, eventHandlerIdentifier.name);
        }
    }
}
