// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using Framefield.Core.UI;

namespace CoreTests.UI
{
    [TestClass]
    public class EventSubcriptionManagerTests
    {
        class EmitterArgs : EventArgs
        {
        }

        class Emitter : IEventEmitter
        {
            public event EventHandler<EmitterArgs> Event1;
            public void TriggerEvent1()
            {
                if (Event1 != null)
                    Event1(this, new EmitterArgs());
            }
            public event EventHandler<EmitterArgs> Event2;
            public void TriggerEvent2()
            {
                if (Event2 != null)
                    Event2(this, new EmitterArgs());
            }
            public void TriggerAllEvents()
            {
                TriggerEvent1();
                TriggerEvent2();
            }

            public Matrix ObjectToWorld { get; set; }
            public Size2F Size { get; set; }
            public bool HandleEvent(Event e) { return false; }
            public string ID { get; private set; }
        }

        class Receiver
        {
            public void Handler1(object obj, EmitterArgs args)
            {
                Handler1CallCount++;
            }
            public int Handler1CallCount = 0;
        }

        [TestMethod]
        public void SubscribeTo_AddLambdaHandlerAndTrigger_HandlerGotCalled()
        {
            var emitter = new Emitter();
            var eventManager = new EventSubscriptionManager();

            int lambdaCount = 0;

            eventManager.Subscribe<EmitterArgs>(emitter, "Event1", (obj, args) => { lambdaCount++; });
            emitter.TriggerEvent1();

            Assert.AreEqual(1, lambdaCount);
        }

        [TestMethod]
        public void SubscribeTo_AddMethodHandlerAndTrigger_HandlerGotCalled()
        {
            var emitter = new Emitter();
            var receiver = new Receiver();
            var eventManager = new EventSubscriptionManager();

            eventManager.Subscribe<EmitterArgs>(emitter, "Event1", receiver.Handler1);
            emitter.TriggerEvent1();

            Assert.AreEqual(1, receiver.Handler1CallCount);
        }

        [TestMethod]
        public void UnsubscribeAll_AddTwoHandlerAndUnsubscribeAll_AllHandlerAreRemoved()
        {
            var emitter = new Emitter();
            var receiver = new Receiver();
            var eventManager = new EventSubscriptionManager();
            int lambdaCount = 0;
            eventManager.Subscribe<EmitterArgs>(emitter, "Event1", receiver.Handler1);
            eventManager.Subscribe<EmitterArgs>(emitter, "Event2", (obj, args) => { lambdaCount++; });

            eventManager.UnsubscribeAll();
            emitter.TriggerAllEvents();
            emitter.TriggerAllEvents();

            Assert.AreEqual(0, receiver.Handler1CallCount);
            Assert.AreEqual(0, lambdaCount);
        }

        [TestMethod]
        public void Subscribe_AddTwoHandlerToOneEvent_BothHandlerAreAdded()
        {
            var emitter = new Emitter();
            var eventManager = new EventSubscriptionManager();
            int lambdaCount1 = 0;
            int lambdaCount2 = 0;
            eventManager.Subscribe<EmitterArgs>(emitter, "Event1", (obj, args) => { lambdaCount1++; });
            eventManager.Subscribe<EmitterArgs>(emitter, "Event1", (obj, args) => { lambdaCount2++; });

            emitter.TriggerAllEvents();

            Assert.AreEqual(1, lambdaCount1);
            Assert.AreEqual(1, lambdaCount2);
        }

        [TestMethod]
        public void Subscribe_AddHandlerToEmitterWhichEventsHasBeenRemoved_SecondHandlerIsAddedFirstIsNotCalledAnymore()
        {
            var emitter = new Emitter();
            var eventManager = new EventSubscriptionManager();
            int lambdaCount1 = 0;
            int lambdaCount2 = 0;
            eventManager.Subscribe<EmitterArgs>(emitter, "Event1", (obj, args) => { lambdaCount1++; });
            Framefield.Core.Utilities.RemoveAllEventHandlerFrom(emitter);
            eventManager.Subscribe<EmitterArgs>(emitter, "Event1", (obj, args) => { lambdaCount2++; });

            emitter.TriggerAllEvents();

            Assert.AreEqual(0, lambdaCount1);
            Assert.AreEqual(1, lambdaCount2);
        }

        [TestMethod]
        public void Dispose_AddTwoHandlerAndDispose_AllHandlerAreRemoved()
        {
            var emitter = new Emitter();
            var receiver = new Receiver();
            var eventManager = new EventSubscriptionManager();
            int lambdaCount = 0;
            eventManager.Subscribe<EmitterArgs>(emitter, "Event1", receiver.Handler1);
            eventManager.Subscribe<EmitterArgs>(emitter, "Event2", (obj, args) => { lambdaCount++; });

            eventManager.Dispose();
            emitter.TriggerAllEvents();
            emitter.TriggerAllEvents();

            Assert.AreEqual(0, receiver.Handler1CallCount);
            Assert.AreEqual(0, lambdaCount);
        }
    }
}
