// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core.UI;

namespace CoreTests.UI
{
    [TestClass]
    public class TransitionHandlerTests
    {
        struct TestStruct {}

        enum States
        {
            State1,
            State2,
            State3,
            State4
        }

        List<TransitionHandler<States>.Transition> _transitions = new List<TransitionHandler<States>.Transition>
                                                                  {
                                                                      new TransitionHandler<States>.Transition { From = States.State1, To = States.State2, StartTime = 0 },
                                                                      new TransitionHandler<States>.Transition { From = States.State2, To = States.State3, StartTime = 1, Duration = 3 },
                                                                      new TransitionHandler<States>.Transition { From = States.State3, To = States.State1, StartTime = 4, Duration = 5 }
                                                                  };

        #region ctor tests
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_InitWithNonEnumGenericType_ThrowsException()
        {
            new TransitionHandler<TestStruct>(null, new TestStruct());
        }

        [TestMethod]
        public void Ctor_InitWithEnumGenericType_NoExceptionIsThrown()
        {
            Assert.IsNotNull(new TransitionHandler<States>(null, States.State1));
        }

        [TestMethod]
        public void Ctor_InitWithTransitions_TransitionsAreStored()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            var privateObject = new PrivateObject(transitionHandler, new PrivateType(typeof(TransitionHandler<States>)));
            var storedTransitions = (IEnumerable<TransitionHandler<States>.Transition>) privateObject.GetField("_transitions");

            Assert.AreEqual(_transitions.Count, storedTransitions.Count());
        }
        #endregion

        [TestMethod]
        public void GetCurrentStateTime_InitWithState1NoChange_Returns0()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            var stateTime = transitionHandler.GetCurrentStateTime(States.State1, 0);
            Assert.AreEqual(0, stateTime);
        }

        #region forward tests
        [TestMethod]
        public void GetCurrentStateTime_SwitchToState2WithGlobalTime1_Returns0()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            var stateTime = transitionHandler.GetCurrentStateTime(States.State2, 1);
            Assert.AreEqual(0, stateTime);
        }

        [TestMethod]
        public void GetCurrentStateTime_SwitchToState2CheckMidOfTransition_Returns0_5()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            transitionHandler.GetCurrentStateTime(States.State2, 3); // switch state at global time 3
            var stateTime = transitionHandler.GetCurrentStateTime(States.State2, 3.5f); // time at 3.5f (half of transition)

            Assert.AreEqual(0.5f, stateTime);
        }

        [TestMethod]
        public void GetCurrentStateTime_SwitchToState2CheckEndOfTransition_Returns1()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            transitionHandler.GetCurrentStateTime(States.State2, 6); // switch state at global time 6
            var stateTime = transitionHandler.GetCurrentStateTime(States.State2, 7); // time at 7 (end of transition)

            Assert.AreEqual(1.0f, stateTime);
        }

        [TestMethod]
        public void GetCurrentStateTime_SwitchToState2CheckBoundsAfterEndOfTransition_Returns1()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            transitionHandler.GetCurrentStateTime(States.State2, 6); 
            var stateTime = transitionHandler.GetCurrentStateTime(States.State2, 8); 

            Assert.AreEqual(1.0f, stateTime);
        }

        #endregion

        #region backward tests
        [TestMethod]
        public void GetCurrentStateTime_SwitchFromState3To2CheckBeginOfTransition_Returns4()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            transitionHandler.GetCurrentStateTime(States.State3, 6);
            var stateTime = transitionHandler.GetCurrentStateTime(States.State2, 10);

            Assert.AreEqual(4.0f, stateTime);
        }

        [TestMethod]
        public void GetCurrentStateTime_SwitchFromState3To2CheckMidOfTransition_Returns2_5()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            var switchTime = 5.0f;
            var duration = 3.0f;
            transitionHandler.GetCurrentStateTime(States.State3, switchTime - 1.0f);
            transitionHandler.GetCurrentStateTime(States.State2, switchTime);
            var stateTime = transitionHandler.GetCurrentStateTime(States.State2, switchTime + duration/2.0f);

            Assert.AreEqual(2.5f, stateTime);
        }

        [TestMethod]
        public void GetCurrentStateTime_SwitchFromState3To2CheckEndOfTransition_Returns1()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            var switchTime = 5.0f;
            var duration = 3.0f;
            transitionHandler.GetCurrentStateTime(States.State3, switchTime - 1.0f);
            transitionHandler.GetCurrentStateTime(States.State2, switchTime);
            var stateTime = transitionHandler.GetCurrentStateTime(States.State2, switchTime + duration);

            Assert.AreEqual(1, stateTime);
        }

        [TestMethod]
        public void GetCurrentStateTime_SwitchFromState3To2CheckBoundsAfterEndOfTransition_Returns1()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            var switchTime = 5.0f;
            var duration = 3.0f;
            transitionHandler.GetCurrentStateTime(States.State3, switchTime - 1.0f);
            transitionHandler.GetCurrentStateTime(States.State2, switchTime);
            var stateTime = transitionHandler.GetCurrentStateTime(States.State2, switchTime + duration + 2.0f);

            Assert.AreEqual(1, stateTime);
        }

        #endregion

        #region jump tests
        [TestMethod]
        public void GetCurrentStateTime_SwitchFromState4To2CheckStartOfTransition_Returns4()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            transitionHandler.GetCurrentStateTime(States.State4, 3); 
            var stateTime = transitionHandler.GetCurrentStateTime(States.State3, 5); 

            Assert.AreEqual(4, stateTime);
        }

        [TestMethod]
        public void GetCurrentStateTime_SwitchFromState4To2CheckBoundsAfterJumpPoint_Returns4()
        {
            var transitionHandler = new TransitionHandler<States>(_transitions, States.State1);

            transitionHandler.GetCurrentStateTime(States.State4, 3); 
            transitionHandler.GetCurrentStateTime(States.State3, 5); 
            var stateTime = transitionHandler.GetCurrentStateTime(States.State3, 9.0f); 

            Assert.AreEqual(4, stateTime);
        }

        #endregion
    }
}
