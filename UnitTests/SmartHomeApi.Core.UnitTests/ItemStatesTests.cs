using System;
using NUnit.Framework;
using SmartHomeApi.Core.Services;
using SmartHomeApi.UnitTestsBase.Stubs;

namespace SmartHomeApi.Core.UnitTests
{
    class ItemStatesTests
    {
        [Test]
        public void CreateItemStateCheckEmptyItemTest1()
        {
            var itemType = "Type";
            var processor = new ItemStatesProcessor(new SmartHomeApiStubFabric());

            try
            {
                var state = processor.GetOrCreateItemState("", itemType);
                Assert.Fail();
            }
            catch (ArgumentNullException e)
            {
            }

            try
            {
                var state = processor.GetOrCreateItemState("   ", itemType);
                Assert.Fail();
            }
            catch (ArgumentNullException e)
            {
            }

            try
            {
                var state = processor.GetOrCreateItemState(null, itemType);
                Assert.Fail();
            }
            catch (ArgumentNullException e)
            {
            }

            Assert.Pass();
        }

        [Test]
        public void CreateItemStateTest1()
        {
            var itemId = "TestItemId";
            var itemType = "TestType";
            var processor = new ItemStatesProcessor(new SmartHomeApiStubFabric());

            var state = processor.GetOrCreateItemState(itemId, itemType);
            Assert.IsNotNull(state);

            var secondState = processor.GetOrCreateItemState(itemId, itemType);

            //Should return the same instance
            Assert.AreEqual(state.GetHashCode(), secondState.GetHashCode());
            Assert.AreEqual(state, secondState);
            Assert.AreEqual(itemId, state.ItemId);
        }

        [Test]
        public void SetItemStateTest1()
        {
            var itemId = "TestItemId";
            var itemType = "TestType";
            var processor = new ItemStatesProcessor(new SmartHomeApiStubFabric());

            var state = processor.GetOrCreateItemState(itemId, itemType);

            var states = state.GetStates();
            Assert.AreEqual(0, states.Count);

            //Nothing should happen
            state.RemoveState("Param1");
            states = state.GetStates();
            Assert.AreEqual(0, states.Count);

            var param1 = state.GetState("Param1");
            Assert.IsNull(param1);

            state.SetState("Param1", 5);
            states = state.GetStates();
            Assert.AreEqual(1, states.Count);
            Assert.AreEqual(5, state.GetState("Param1"));

            state.SetState("Param1", 7);
            states = state.GetStates();
            Assert.AreEqual(1, states.Count);
            Assert.AreEqual(7, state.GetState("Param1"));

            state.SetState("Param2", "qwerty");
            states = state.GetStates();
            Assert.AreEqual(2, states.Count);
            Assert.AreEqual("qwerty", state.GetState("Param2"));

            state.RemoveState("Param1");
            states = state.GetStates();
            Assert.AreEqual(1, states.Count);
            Assert.AreEqual("qwerty", state.GetState("Param2"));

            var apiState = processor.GetStatesContainer();
            Assert.AreEqual(1, apiState.States.Count);

            var itemState = apiState.States[itemId];
            Assert.AreEqual(itemId, itemState.ItemId);
            Assert.AreEqual(itemType, itemState.ItemType);
        }
    }
}