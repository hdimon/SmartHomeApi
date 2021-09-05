using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Services;
using SmartHomeApi.Core.UnitTests.Stubs.TestItem1Plugin;
using SmartHomeApi.UnitTestsBase.Stubs;

namespace SmartHomeApi.Core.UnitTests
{
    public class NotificationsProcessorTests
    {
        private INotificationsProcessor GetProcessor(ISmartHomeApiFabric fabric)
        {
            return new NotificationsProcessor(fabric);
        }

        [Test]
        public async Task NotifySubscribersTest()
        {
            for (int i = 0; i < 30; i++)
            {
                await NotifySubscribers();
            }
        }

        private async Task NotifySubscribers()
        {
            var itemsCount = 1000;

            var fabric = new SmartHomeApiStubFabric();

            var processor = GetProcessor(fabric);

            var taskCompletionSources = new List<Task<bool>>(itemsCount);

            for (int i = 1; i <= itemsCount; i++)
            {
                var itemId = $"ItemId{i}";
                var itemType = $"ItemType{i}";
                var helpersFabric = new ItemHelpersStubFabric(itemId, itemType, fabric);
                var config = new TestItem1Config(itemId, itemType);
                var item = new TestItem1(fabric.GetApiManager(), helpersFabric, config);
                var tcs = new TaskCompletionSource<bool>();

                item.OnProcessNotification = async args =>
                {
                    await Task.Delay(10);
                    tcs.SetResult(true);
                };

                var ct = new CancellationTokenSource(5000);
                ct.Token.Register(() => tcs.TrySetResult(false));

                processor.RegisterSubscriber(item);

                taskCompletionSources.Add(tcs.Task);
            }

            var ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", 5, 10);
            processor.NotifySubscribers(ev);

            var results = await Task.WhenAll(taskCompletionSources);

            Assert.IsTrue(results.All(r => r));
        }

        [Test]
        public async Task CheckExceptionCatchingTest()
        {
            var fabric = new SmartHomeApiStubFabric();
            var logger = (ApiStubLogger)fabric.GetApiLogger();

            var processor = GetProcessor(fabric);

            var itemId = "ItemId1";
            var itemType = "ItemType1";
            var helpersFabric = new ItemHelpersStubFabric(itemId, itemType, fabric);
            var config = new TestItem1Config(itemId, itemType);
            var item = new TestItem1(fabric.GetApiManager(), helpersFabric, config);
            var tcs = new TaskCompletionSource<bool>();

            item.OnProcessNotification = async args =>
            {
                throw new Exception();
            };

            var ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs.TrySetResult(true));

            processor.RegisterSubscriber(item);

            Assert.AreEqual(0, logger.Logs.Count);

            var ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", 5, 10);
            processor.NotifySubscribers(ev);

            var result = await tcs.Task;

            Assert.IsTrue(result);

            Assert.AreEqual(1, logger.Logs.Count);
        }

        [Test]
        public async Task CheckBooleanEquality()
        {
            var counter = 0;
            var fabric = new SmartHomeApiStubFabric();

            var processor = GetProcessor(fabric);

            var itemId = "ItemId";
            var itemType = "ItemType";
            var helpersFabric = new ItemHelpersStubFabric(itemId, itemType, fabric);
            var config = new TestItem1Config(itemId, itemType);
            var item = new TestItem1(fabric.GetApiManager(), helpersFabric, config);
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            var tcs3 = new TaskCompletionSource<bool>();
            var tcs4 = new TaskCompletionSource<bool>();
            var tcs5 = new TaskCompletionSource<bool>();
            var tcs6 = new TaskCompletionSource<bool>();

            item.OnProcessNotification = async args =>
            {
                if (counter == 0)
                {
                    tcs1.SetResult(true);
                    counter++;
                }
                else if (counter == 1)
                {
                    tcs2.SetResult(true);
                    counter++;
                }
                else if (counter == 2)
                {
                    tcs3.SetResult(true);
                    counter++;
                }
                else if (counter == 3)
                {
                    tcs4.SetResult(true);
                    counter++;
                }
                else if (counter == 4)
                {
                    tcs5.SetResult(true);
                    counter++;
                }
                else if (counter == 5)
                {
                    tcs6.SetResult(true);
                    counter++;
                }
            };

            processor.RegisterSubscriber(item);

            var ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs1.TrySetResult(false));

            var ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", null, null);
            processor.NotifySubscribers(ev);

            var result = await tcs1.Task;
            Assert.IsFalse(result);


            ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs2.TrySetResult(false));

            ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", null, false);
            processor.NotifySubscribers(ev);

            result = await tcs2.Task;
            Assert.IsFalse(result);


            ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs3.TrySetResult(false));

            ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", null, true);
            processor.NotifySubscribers(ev);

            result = await tcs3.Task;
            Assert.IsFalse(result);


            ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs4.TrySetResult(false));

            ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", true, false);
            processor.NotifySubscribers(ev);

            result = await tcs4.Task;
            Assert.IsFalse(result);


            ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs5.TrySetResult(false));

            ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", true, true);
            processor.NotifySubscribers(ev);

            result = await tcs5.Task;
            Assert.IsFalse(result);


            ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs6.TrySetResult(false));

            ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", false, false);
            processor.NotifySubscribers(ev);

            result = await tcs6.Task;
            Assert.IsFalse(result);
        }

        [Test]
        public async Task CheckIntegerEquality()
        {
            var counter = 0;
            var fabric = new SmartHomeApiStubFabric();

            var processor = GetProcessor(fabric);

            var itemId = "ItemId";
            var itemType = "ItemType";
            var helpersFabric = new ItemHelpersStubFabric(itemId, itemType, fabric);
            var config = new TestItem1Config(itemId, itemType);
            var item = new TestItem1(fabric.GetApiManager(), helpersFabric, config);
            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();

            item.OnProcessNotification = async args =>
            {
                if (counter == 0)
                {
                    tcs1.SetResult(true);
                    counter++;
                }
                else if (counter == 1)
                {
                    tcs2.SetResult(true);
                    counter++;
                }
            };

            processor.RegisterSubscriber(item);

            var ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs1.TrySetResult(false));

            var ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", null, 0);
            processor.NotifySubscribers(ev);

            var result = await tcs1.Task;
            Assert.IsTrue(result);


            ct = new CancellationTokenSource(500);
            ct.Token.Register(() => tcs2.TrySetResult(false));

            ev = new StateChangedEvent(StateChangedEventType.ValueUpdated, "Test", "Id", "Parameter", null, 5);
            processor.NotifySubscribers(ev);

            result = await tcs2.Task;
            Assert.IsTrue(result);
        }
    }
}