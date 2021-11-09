using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges;
using SmartHomeApi.Core.ItemsLocatorsBridges;
using SmartHomeApi.Core.Services;
using SmartHomeApi.Core.UnitTests.Stubs;
using SmartHomeApi.Core.UnitTests.Stubs.ApiItemsLocatorTestStubs;
using SmartHomeApi.Core.UnitTests.Stubs.StandardItemsLocator1Plugin;
using SmartHomeApi.UnitTestsBase.Stubs;

namespace SmartHomeApi.Core.UnitTests
{
    class ApiItemsLocatorTests
    {
        [Test]
        public async Task OneInitialItemTest()
        {
            string itemId = "TestId1";
            string itemType = nameof(ItemsLocatorStub1);

            var fabric = new SmartHomeApiStubFabric();
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;
            var apiItemsLocator = new ApiItemsLocator(fabric);

            var itemsLocator = new ItemsLocatorStub1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator);
            await itemsLocator.ConfigAdded(new StandardItemsLocator1Config(itemId, itemType));

            pluginsLocator.AddLocator(locatorBridge);

            var items = await apiItemsLocator.GetItems();
            Assert.AreEqual(0, items.Count());

            var tcs = new TaskCompletionSource<bool>();
            apiItemsLocator.ItemAdded += (sender, args) =>
            {
                tcs.TrySetResult(true);
            };

            await apiItemsLocator.Initialize();

            var ct = new CancellationTokenSource(100);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var res = await tcs.Task;

            Assert.IsTrue(res);

            items = await apiItemsLocator.GetItems();
            Assert.AreEqual(1, items.Count());
        }

        [Test]
        public async Task TwoInitialItemsWithDifferentPrioritiesTest()
        {
            string itemId1 = "TestId1";
            string itemId2 = "TestId2";
            string itemType1 = nameof(ItemsLocatorStub1);
            string itemType2 = nameof(ItemsLocatorStub2);

            var fabric = new SmartHomeApiStubFabric();
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;
            var apiItemsLocator = new ApiItemsLocator(fabric);

            var itemsLocator1 = new ItemsLocatorStub1(fabric);
            var locatorBridge1 = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator1);
            await itemsLocator1.ConfigAdded(new StandardItemsLocator1Config(itemId1, itemType1));

            pluginsLocator.AddLocator(locatorBridge1);

            var itemsLocator2 = new ItemsLocatorStub2(fabric);
            var locatorBridge2 = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator2);
            await itemsLocator2.ConfigAdded(new StandardItemsLocator1Config(itemId2, itemType2));

            pluginsLocator.AddLocator(locatorBridge2);

            var items = await apiItemsLocator.GetItems();
            Assert.AreEqual(0, items.Count());

            var tcs1 = new TaskCompletionSource<bool>();
            var tcs2 = new TaskCompletionSource<bool>();
            apiItemsLocator.ItemAdded += (sender, args) =>
            {
                if (args.ItemId == itemId1)
                    tcs1.TrySetResult(true);

                if (args.ItemId == itemId2)
                    tcs2.TrySetResult(true);
            };

            await apiItemsLocator.Initialize();

            var ct1 = new CancellationTokenSource(100);
            ct1.Token.Register(() => tcs1.TrySetResult(false));

            var ct2 = new CancellationTokenSource(100);
            ct2.Token.Register(() => tcs2.TrySetResult(false));

            var res1 = await tcs1.Task;
            var res2 = await tcs2.Task;

            Assert.IsTrue(res1);
            Assert.IsTrue(res2);

            var itemsList = (await apiItemsLocator.GetItems()).ToList();
            Assert.AreEqual(2, itemsList.Count);

            Assert.AreEqual(itemId2, itemsList[0].ItemId);
            Assert.AreEqual(itemId1, itemsList[1].ItemId);
        }

        [Test]
        public async Task AddItemTest()
        {
            string itemId = "TestId1";
            string itemType = nameof(ItemsLocatorStub1);

            var fabric = new SmartHomeApiStubFabric();
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;
            var apiItemsLocator = new ApiItemsLocator(fabric);

            var itemsLocator = new ItemsLocatorStub1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(locatorBridge);

            var tcs = new TaskCompletionSource<bool>();
            apiItemsLocator.ItemAdded += (sender, args) =>
            {
                tcs.TrySetResult(true);
            };

            await apiItemsLocator.Initialize();

            await itemsLocator.ConfigAdded(new StandardItemsLocator1Config(itemId, itemType));

            var ct = new CancellationTokenSource(100);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var res = await tcs.Task;

            Assert.IsTrue(res);

            var items = await apiItemsLocator.GetItems();
            Assert.AreEqual(1, items.Count());
        }

        [Test]
        public async Task DeleteItemTest()
        {
            string itemId = "TestId1";
            string itemType = nameof(ItemsLocatorStub1);

            var fabric = new SmartHomeApiStubFabric();
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;
            var apiItemsLocator = new ApiItemsLocator(fabric);

            var itemsLocator = new ItemsLocatorStub1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(locatorBridge);

            var tcs = new TaskCompletionSource<bool>();
            var dtcs = new TaskCompletionSource<bool>();
            apiItemsLocator.ItemAdded += (sender, args) =>
            {
                tcs.TrySetResult(true);
            };
            apiItemsLocator.ItemDeleted += (sender, args) =>
            {
                dtcs.TrySetResult(true);
            };

            await apiItemsLocator.Initialize();

            await itemsLocator.ConfigAdded(new StandardItemsLocator1Config(itemId, itemType));

            var ct = new CancellationTokenSource(100);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var res = await tcs.Task;

            Assert.IsTrue(res);

            var items = await apiItemsLocator.GetItems();
            Assert.AreEqual(1, items.Count());

            await itemsLocator.ConfigDeleted(itemId);

            ct = new CancellationTokenSource(100);
            ct.Token.Register(() => dtcs.TrySetResult(false));

            res = await dtcs.Task;

            Assert.IsTrue(res);

            items = await apiItemsLocator.GetItems();
            Assert.AreEqual(0, items.Count());
        }

        [Test]
        public async Task AddPluginWithOneItemTest()
        {
            string itemId = "TestId1";
            string itemType = nameof(ItemsLocatorStub1);

            var fabric = new SmartHomeApiStubFabric();
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;
            var apiItemsLocator = new ApiItemsLocator(fabric);

            var tcs = new TaskCompletionSource<bool>();
            apiItemsLocator.ItemAdded += (sender, args) =>
            {
                //Thread.Sleep(5000);
                tcs.TrySetResult(true);
            };

            await apiItemsLocator.Initialize();

            var itemsLocator = new ItemsLocatorStub1(fabric);
            await itemsLocator.ConfigAdded(new StandardItemsLocator1Config(itemId, itemType));
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(locatorBridge);

            var ct = new CancellationTokenSource(100);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var res = await tcs.Task;

            Assert.IsTrue(res);

            var items = await apiItemsLocator.GetItems();
            Assert.AreEqual(1, items.Count());
        }

        [Test]
        public async Task AddPluginAndThenOneItemTest()
        {
            string itemId = "TestId1";
            string itemType = nameof(ItemsLocatorStub1);

            var fabric = new SmartHomeApiStubFabric();
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;
            var apiItemsLocator = new ApiItemsLocator(fabric);

            var tcs = new TaskCompletionSource<bool>();
            var itemTcs = new TaskCompletionSource<bool>();
            var counter = 0;
            apiItemsLocator.ItemAdded += (sender, args) =>
            {
                if (counter == 0)
                    tcs.TrySetResult(true);
                else if (counter == 1)
                    itemTcs.TrySetResult(true);
            };

            await apiItemsLocator.Initialize();

            var itemsLocator = new ItemsLocatorStub1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(locatorBridge);

            var ct = new CancellationTokenSource(100);
            ct.Token.Register(() =>
            {
                counter++;
                tcs.TrySetResult(false);
            });

            var res = await tcs.Task;

            Assert.IsFalse(res);

            var items = await apiItemsLocator.GetItems();
            Assert.AreEqual(0, items.Count());

            await itemsLocator.ConfigAdded(new StandardItemsLocator1Config(itemId, itemType));

            ct = new CancellationTokenSource(100);
            ct.Token.Register(() => itemTcs.TrySetResult(false));

            res = await itemTcs.Task;

            Assert.IsTrue(res);

            items = await apiItemsLocator.GetItems();
            Assert.AreEqual(1, items.Count());
        }

        [Test]
        public async Task DeletePluginTest()
        {
            string itemId = "TestId1";
            string itemType = nameof(ItemsLocatorStub1);

            var fabric = new SmartHomeApiStubFabric();
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;
            var apiItemsLocator = new ApiItemsLocator(fabric);

            var itemsLocator = new ItemsLocatorStub1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator);
            await itemsLocator.ConfigAdded(new StandardItemsLocator1Config(itemId, itemType));

            pluginsLocator.AddLocator(locatorBridge);

            var items = await apiItemsLocator.GetItems();
            Assert.AreEqual(0, items.Count());

            var tcs = new TaskCompletionSource<bool>();
            var dtcs = new TaskCompletionSource<bool>();
            apiItemsLocator.ItemAdded += (sender, args) =>
            {
                tcs.TrySetResult(true);
            };
            apiItemsLocator.ItemDeleted += (sender, args) =>
            {
                dtcs.TrySetResult(true);
            };

            await apiItemsLocator.Initialize();

            var ct = new CancellationTokenSource(100);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var res = await tcs.Task;

            Assert.IsTrue(res);

            items = await apiItemsLocator.GetItems();
            Assert.AreEqual(1, items.Count());

            pluginsLocator.RemoveLocator(locatorBridge);

            ct = new CancellationTokenSource(100);
            ct.Token.Register(() => dtcs.TrySetResult(false));

            res = await dtcs.Task;

            Assert.IsTrue(res);

            items = await apiItemsLocator.GetItems();
            Assert.AreEqual(0, items.Count());
        }

        [Test]
        public async Task UpdatePluginTest()
        {
            string itemId = "TestId1";
            string itemType = nameof(ItemsLocatorStub1);

            var fabric = new SmartHomeApiStubFabric();
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;
            var apiItemsLocator = new ApiItemsLocator(fabric);

            var itemsLocator = new ItemsLocatorStub1(fabric);
            var locatorBridge = (IStandardItemsLocatorBridge)new StandardItemsLocatorBridge(itemsLocator);
            await itemsLocator.ConfigAdded(new StandardItemsLocator1Config(itemId, itemType));

            pluginsLocator.AddLocator(locatorBridge);

            var items = await apiItemsLocator.GetItems();
            Assert.AreEqual(0, items.Count());

            var tcs = new TaskCompletionSource<bool>();
            var dtcs = new TaskCompletionSource<bool>();
            var utcs = new TaskCompletionSource<bool>();
            var counter = 0;
            apiItemsLocator.ItemAdded += (sender, args) =>
            {
                if (counter == 0)
                {
                    counter++;
                    tcs.TrySetResult(true);
                }
                else if (counter == 2)
                {
                    utcs.TrySetResult(true);
                }
                else
                {
                    Assert.Fail();
                }
            };
            apiItemsLocator.ItemDeleted += (sender, args) =>
            {
                if (counter == 1)
                {
                    counter++;
                    dtcs.TrySetResult(true);
                }
                else
                    Assert.Fail();
            };

            await apiItemsLocator.Initialize();

            var ct = new CancellationTokenSource(100);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var res = await tcs.Task;

            Assert.IsTrue(res);

            items = await apiItemsLocator.GetItems();
            Assert.AreEqual(1, items.Count());

            pluginsLocator.AddLocator(locatorBridge);

            ct = new CancellationTokenSource(100);
            ct.Token.Register(() => dtcs.TrySetResult(false));
            var uct = new CancellationTokenSource(100);
            uct.Token.Register(() => utcs.TrySetResult(false));

            res = await dtcs.Task;

            Assert.IsTrue(res);

            res = await utcs.Task;

            Assert.IsTrue(res);

            items = await apiItemsLocator.GetItems();
            Assert.AreEqual(1, items.Count());
        }
    }
}