using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges;
using SmartHomeApi.Core.ItemsLocatorsBridges;
using SmartHomeApi.Core.UnitTests.Stubs.StandardItemsLocator1Plugin;
using SmartHomeApi.UnitTestsBase.Stubs;

namespace SmartHomeApi.Core.UnitTests
{
    class StandardItemsLocatorTests
    {
        private const string ItemId = "TestId1";
        private const string ItemType = "StandardItemsLocator1Plugin";

        [Test]
        public async Task NotInitializedLocatorDoesNotProcessConfigEventsTest()
        {
            var fabric = new SmartHomeApiStubFabric();

            var locator = new StandardItemsLocator1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(locator);

            var tcs = new TaskCompletionSource<bool>();
            var bridgeTcs = new TaskCompletionSource<bool>();
            locator.ItemAdded += (sender, args) =>
            {
                tcs.SetResult(true);
            };
            locatorBridge.ItemAdded += (sender, args) =>
            {
                bridgeTcs.SetResult(true);
            };

            await locator.ConfigAdded(new StandardItemsLocator1Config(ItemId, ItemType));

            var ct = new CancellationTokenSource(1000);
            ct.Token.Register(() => tcs.TrySetResult(false));
            var bridgeCt = new CancellationTokenSource(1000);
            bridgeCt.Token.Register(() => bridgeTcs.TrySetResult(false));

            var res = await tcs.Task;
            var bridgeRes = await bridgeTcs.Task;

            Assert.IsFalse(res);
            Assert.IsFalse(bridgeRes);

            ct.Cancel();
            ct.Dispose();
            bridgeCt.Cancel();
            bridgeCt.Dispose();

            var items = await locator.GetItems();

            Assert.AreEqual(0, items.Count());
        }

        [Test]
        public async Task AddItemTest()
        {
            var fabric = new SmartHomeApiStubFabric();

            var locator = new StandardItemsLocator1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(locator);

            await locatorBridge.Initialize();

            var tcs = new TaskCompletionSource<bool>();
            var bridgeTcs = new TaskCompletionSource<bool>();
            locator.ItemAdded += (sender, args) =>
            {
                tcs.SetResult(true);
            };
            locatorBridge.ItemAdded += (sender, args) =>
            {
                bridgeTcs.SetResult(true);
            };

            await locator.ConfigAdded(new StandardItemsLocator1Config(ItemId, ItemType));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());
            var bridgeCt = new CancellationTokenSource(1000);
            bridgeCt.Token.Register(() => bridgeTcs.TrySetCanceled());

            var res = await tcs.Task;
            var bridgeRes = await bridgeTcs.Task;

            Assert.IsTrue(res);
            Assert.IsTrue(bridgeRes);

            ct.Cancel();
            ct.Dispose();
            bridgeCt.Cancel();
            bridgeCt.Dispose();

            var items = await locator.GetItems();

            Assert.AreEqual(1, items.Count());
        }

        [Test]
        public async Task AddItemsOnInitializeTest()
        {
            var fabric = new SmartHomeApiStubFabric();
            var configLocator = new ItemsConfigsLocatorStub();
            configLocator.Configs.Add(new StandardItemsLocator1Config(ItemId, ItemType));
            fabric.ItemsConfigLocator = configLocator;

            var locator = new StandardItemsLocator1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(locator);

            var tcs = new TaskCompletionSource<bool>();
            var bridgeTcs = new TaskCompletionSource<bool>();
            locator.ItemAdded += (sender, args) =>
            {
                tcs.SetResult(true);
            };
            locatorBridge.ItemAdded += (sender, args) =>
            {
                bridgeTcs.SetResult(true);
            };

            await locator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());
            var bridgeCt = new CancellationTokenSource(1000);
            bridgeCt.Token.Register(() => bridgeTcs.TrySetCanceled());

            var res = await tcs.Task;
            var bridgeRes = await bridgeTcs.Task;

            Assert.IsTrue(res);
            Assert.IsTrue(bridgeRes);

            ct.Cancel();
            ct.Dispose();
            bridgeCt.Cancel();
            bridgeCt.Dispose();

            var items = await locator.GetItems();

            Assert.AreEqual(1, items.Count());
        }

        [Test]
        public async Task UpdateItemTest()
        {
            int counter = 0;
            int bridgeCounter = 0;

            var fabric = new SmartHomeApiStubFabric();
            var configLocator = new ItemsConfigsLocatorStub();

            var config = new StandardItemsLocator1Config(ItemId, ItemType);
            configLocator.Configs.Add(config);
            fabric.ItemsConfigLocator = configLocator;

            var locator = new StandardItemsLocator1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(locator);

            var tcs = new TaskCompletionSource<bool>();
            var bridgeTcs = new TaskCompletionSource<bool>();
            locator.ItemAdded += (sender, args) =>
            {
                counter++;

                if (counter == 1)
                    tcs.SetResult(true);
                else
                {
                    Assert.Fail();
                }
            };
            locatorBridge.ItemAdded += (sender, args) =>
            {
                bridgeCounter++;

                if (bridgeCounter == 1)
                    bridgeTcs.SetResult(true);
                else
                {
                    Assert.Fail();
                }
            };

            await locator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(false));
            var bridgeCt = new CancellationTokenSource(2000);
            bridgeCt.Token.Register(() => bridgeTcs.TrySetResult(false));

            var res = await tcs.Task;
            var bridgeRes = await bridgeTcs.Task;

            Assert.IsTrue(res);
            Assert.IsTrue(bridgeRes);

            ct.Cancel();
            ct.Dispose();
            bridgeCt.Cancel();
            bridgeCt.Dispose();

            var items = await locator.GetItems();

            Assert.AreEqual(1, items.Count());

            var item = (IConfigurable)items.First();
            Assert.IsNull(((StandardItemsLocator1Config)item.Config).TestField);

            config.TestField = "Test";

            await locator.ConfigUpdated(config);

            items = await locator.GetItems();

            Assert.AreEqual(1, items.Count());

            item = (IConfigurable)items.First();

            Assert.AreEqual("Test", ((StandardItemsLocator1Config)item.Config).TestField);
        }

        [Test]
        public async Task UpdateItemWithUnchangedConfigTest()
        {
            int counter = 0;
            int bridgeCounter = 0;

            var fabric = new SmartHomeApiStubFabric();
            var configLocator = new ItemsConfigsLocatorStub();

            var config = new StandardItemsLocator1Config(ItemId, ItemType);
            configLocator.Configs.Add(config);
            fabric.ItemsConfigLocator = configLocator;

            var locator = new StandardItemsLocator1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(locator);

            var tcs = new TaskCompletionSource<bool>();
            var bridgeTcs = new TaskCompletionSource<bool>();
            locator.ItemAdded += (sender, args) =>
            {
                counter++;

                if (counter == 1)
                    tcs.SetResult(true);
                else
                {
                    Assert.Fail();
                }
            };
            locatorBridge.ItemAdded += (sender, args) =>
            {
                bridgeCounter++;

                if (bridgeCounter == 1)
                    bridgeTcs.SetResult(true);
                else
                {
                    Assert.Fail();
                }
            };

            await locator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(false));
            var bridgeCt = new CancellationTokenSource(2000);
            bridgeCt.Token.Register(() => bridgeTcs.TrySetResult(false));

            var res = await tcs.Task;
            var bridgeRes = await bridgeTcs.Task;

            Assert.IsTrue(res);
            Assert.IsTrue(bridgeRes);

            ct.Cancel();
            ct.Dispose();
            bridgeCt.Cancel();
            bridgeCt.Dispose();

            var items = await locator.GetItems();

            Assert.AreEqual(1, items.Count());

            var item = (IConfigurable)items.First();
            Assert.IsNull(((StandardItemsLocator1Config)item.Config).TestField);

            await locator.ConfigUpdated(config);

            items = await locator.GetItems();

            Assert.AreEqual(1, items.Count());

            item = (IConfigurable)items.First();

            Assert.IsNull(((StandardItemsLocator1Config)item.Config).TestField);
        }

        [Test]
        public async Task DeleteItemTest()
        {
            int counter = 0;
            int bridgeCounter = 0;

            var fabric = new SmartHomeApiStubFabric();
            var configLocator = new ItemsConfigsLocatorStub();

            var config = new StandardItemsLocator1Config(ItemId, ItemType);
            configLocator.Configs.Add(config);
            fabric.ItemsConfigLocator = configLocator;

            var locator = new StandardItemsLocator1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(locator);

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();
            var bridgeTcs = new TaskCompletionSource<bool>();
            var bridgeTcs1 = new TaskCompletionSource<bool>();
            locator.ItemAdded += (sender, args) =>
            {
                counter++;

                if (counter == 1)
                    tcs.SetResult(true);
                else
                {
                    Assert.Fail();
                }
            };
            locatorBridge.ItemAdded += (sender, args) =>
            {
                bridgeCounter++;

                if (bridgeCounter == 1)
                    bridgeTcs.SetResult(true);
                else
                {
                    Assert.Fail();
                }
            };
            locator.ItemDeleted += (sender, args) =>
            {
                counter++;

                if (counter == 2)
                    tcs1.SetResult(true);
                else
                {
                    Assert.Fail();
                }
            };
            locatorBridge.ItemDeleted += (sender, args) =>
            {
                bridgeCounter++;

                if (bridgeCounter == 2)
                    bridgeTcs1.SetResult(true);
                else
                {
                    Assert.Fail();
                }
            };

            await locator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(false));
            var bridgeCt = new CancellationTokenSource(2000);
            bridgeCt.Token.Register(() => bridgeTcs.TrySetResult(false));

            var res = await tcs.Task;
            var bridgeRes = await bridgeTcs.Task;

            Assert.IsTrue(res);
            Assert.IsTrue(bridgeRes);

            ct.Cancel();
            ct.Dispose();
            bridgeCt.Cancel();
            bridgeCt.Dispose();

            var items = await locator.GetItems();

            Assert.AreEqual(1, items.Count());

            await locator.ConfigDeleted(config.ItemId);

            ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs1.TrySetResult(false));
            bridgeCt = new CancellationTokenSource(2000);
            bridgeCt.Token.Register(() => bridgeTcs1.TrySetResult(false));

            res = await tcs1.Task;
            bridgeRes = await bridgeTcs1.Task;

            Assert.IsTrue(res);
            Assert.IsTrue(bridgeRes);

            ct.Cancel();
            ct.Dispose();
            bridgeCt.Cancel();
            bridgeCt.Dispose();

            items = await locator.GetItems();

            Assert.AreEqual(0, items.Count());
        }
    }
}