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
    }
}