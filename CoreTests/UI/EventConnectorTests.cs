// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using Framefield.Core;
using Framefield.Core.UI;

namespace CoreTests.UI
{

    [TestClass]
    public class EventConnectorTests
    {
        #region HelperClasses
        class EmitterFunc : OperatorPart.Function, IButton
        {
            public void TriggerClicked()
            {
                if (Clicked != null)
                    Clicked(this, ButtonEventArgs.Empty);
            }

            public int EvalCount = 0;
            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                ++EvalCount;
                return context;
            }

            public Matrix ObjectToWorld { get; set; }
            public Size2F Size { get; set; }
            public bool HandleEvent(Event e)
            {
                return false;
            }

            public string ID { get { return "EmitterID"; } }
            public event EventHandler<ButtonEventArgs> Clicked;
            #pragma warning disable 0067 // disable warning for non used events
            public event EventHandler<ButtonEventArgs> Pressed;
            public event EventHandler<ButtonEventArgs> Released;
            public event EventHandler<ButtonEventArgs> Dragged;
            #pragma warning restore 0067
        }

        class ReceiverFunc : OperatorPart.Function, IUIElement
        {
            public int EvalCount = 0;
            public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
            {
                ++EvalCount;
                return context;
            }

            public Matrix ObjectToWorld { get; set; }
            public Size2F Size { get; set; }
            public bool HandleEvent(Event e)
            {
                return false;
            }

            public string ID { get { return "ReceiverID"; } }
        }
        #endregion


        [TestMethod]
        public void Initialize_SetUIElementAsEvaluationInput_UIElementIsEvaluated()
        {
            var receiverFunc = new ReceiverFunc();
            var receiverOpPart = new OperatorPart(Guid.NewGuid(), receiverFunc);

            var eventConnector = new EventConnector();
            eventConnector.Initialize(receiverOpPart, new OperatorPartContext());

            Assert.AreEqual(1, receiverFunc.EvalCount);
        }


        [TestMethod]
        public void Initialize_SetButtonInSubtreeAsEvaluationInput_ButtonIsEvaluated()
        {
            var receiverFunc = new ReceiverFunc();
            var receiverOpPart = new OperatorPart(Guid.NewGuid(), receiverFunc);
            var emitterFunc = new EmitterFunc();
            var emitterOpPart = new OperatorPart(Guid.NewGuid(), emitterFunc);
            receiverOpPart.AppendConnection(emitterOpPart);

            var eventConnector = new EventConnector();
            eventConnector.Initialize(receiverOpPart, new OperatorPartContext());

            Assert.AreEqual(1, emitterFunc.EvalCount);
        }


//        [TestMethod]
//        public void AddEventHandler_AndInterfaceWhichIsNotPresentInSubtree_NoEventIsSubscribed()
//        {
//            var receiverFunc = new ReceiverFunc();
//            var receiverOpPart = new OperatorPart(Guid.NewGuid(), receiverFunc);
//            var emitterFunc = new EmitterFunc();
//            var emitterOpPart = new OperatorPart(Guid.NewGuid(), emitterFunc);
//            receiverOpPart.AppendConnection(emitterOpPart);
//
//            var eventConnector = new EventConnector();
//            eventConnector.Initialize(receiverOpPart, new OperatorPartContext());
//            eventConnector.AddEventHandler<IGestureTouchable, ButtonEventArgs>("EmitterID", "Clicked", (o, args) => { });
//
//            var eventConnectorPrivateObject = new PrivateObject(eventConnector, new PrivateType(typeof(EventConnector)));
//            var esm = (EventSubscriptionManager) eventConnectorPrivateObject.GetField("_eventSubscriptionManager");
//
//            Assert.AreEqual(0, esm.NumSubscriptions);
//        }


        [TestMethod]
        public void AddEventHandler_AndHandlerForButtonClickedInSubtree_EventHandlerIsAdded()
        {
            var receiverFunc = new ReceiverFunc();
            var receiverOpPart = new OperatorPart(Guid.NewGuid(), receiverFunc);
            var emitterFunc = new EmitterFunc();
            var emitterOpPart = new OperatorPart(Guid.NewGuid(), emitterFunc);
            receiverOpPart.AppendConnection(emitterOpPart);
            int lambdaCount = 0;

            var eventConnector = new EventConnector();
            eventConnector.Initialize(receiverOpPart, new OperatorPartContext());
            eventConnector.AddEventHandler<IButton, ButtonEventArgs>("EmitterID", "Clicked", (o, args) => { ++lambdaCount; });
            emitterFunc.TriggerClicked();

            Assert.AreEqual(1, lambdaCount);
        }


        [TestMethod]
        public void AddButtonHandler_AndHandlerForButtonClickedInSubtree_EventHandlerIsAdded()
        {
            var receiverFunc = new ReceiverFunc();
            var receiverOpPart = new OperatorPart(Guid.NewGuid(), receiverFunc);
            var emitterFunc = new EmitterFunc();
            var emitterOpPart = new OperatorPart(Guid.NewGuid(), emitterFunc);
            receiverOpPart.AppendConnection(emitterOpPart);
            int lambdaCount = 0;

            var eventConnector = new EventConnector();
            eventConnector.Initialize(receiverOpPart, new OperatorPartContext());
            eventConnector.AddButtonHandler("EmitterID", "Clicked", (o, args) => { ++lambdaCount; });
            emitterFunc.TriggerClicked();

            Assert.AreEqual(1, lambdaCount);
        }


        [TestMethod]
        public void Dispose_AndHandlerForButtonClickedInSubtreeAndDispose_NoHandlerIsSubscripedAnymore()
        {
            var receiverFunc = new ReceiverFunc();
            var receiverOpPart = new OperatorPart(Guid.NewGuid(), receiverFunc);
            var emitterFunc = new EmitterFunc();
            var emitterOpPart = new OperatorPart(Guid.NewGuid(), emitterFunc);
            receiverOpPart.AppendConnection(emitterOpPart);

            var eventConnector = new EventConnector();
            eventConnector.Initialize(receiverOpPart, new OperatorPartContext());
            eventConnector.AddButtonHandler("EmitterID", "Clicked", (o, args) => { });
            eventConnector.Dispose();

            var eventConnectorPrivateObject = new PrivateObject(eventConnector, new PrivateType(typeof(EventConnector)));
            var esm = (EventSubscriptionManager) eventConnectorPrivateObject.GetField("_eventSubscriptionManager");

            Assert.AreEqual(0, esm.NumSubscriptions);
        }

    }
}
