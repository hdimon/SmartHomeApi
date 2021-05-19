using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Interfaces.ItemsLocatorsBridges;
using SmartHomeApi.Core.ItemsLocatorsBridges;
using SmartHomeApi.Core.Services;
using SmartHomeApi.Core.UnitTests.Stubs;
using SmartHomeApi.Core.UnitTests.Stubs.TestItem1Plugin;
using SmartHomeApi.Core.UnitTests.Stubs.TestItem2Plugin;
using SmartHomeApi.UnitTestsBase.Stubs;

namespace SmartHomeApi.Core.UnitTests
{
    public class ItemsConfigLocatorTests
    {
        private const string InputTestDataFolder = "InputTestData";
        private const string DataFolder = "SmartHomeApiDataTest";
        private const string ConfigsFolder = "Configs";
        private const string TestConfigsFolder = "TestConfigs";
        private const string TestItem1Type = "TestItem1";
        private const string TestItem1ConfigName = "TestItem1.json";
        private const string TestItem2ConfigName = "TestItem2.json";
        private const string TestItem1ChangedConfigName = "TestItem1Changed.json";
        private const string TestItem1CopyConfigName = "TestItem1Copy.json";
        private const string InvalidConfigName = "InvalidConfig.json";
        private const string InvalidConfigWithoutItemIdName = "InvalidConfigWithoutItemId.json";
        private const string InvalidConfigWithoutItemTypeName = "InvalidConfigWithoutItemType.json";

        private AppSettings _appSettings;
        private string _inputTestDataFolder;

        [SetUp]
        public async Task SetUp()
        {
            //Wait a bit in order to free files after previous test
            await Task.Delay(2000);

            _inputTestDataFolder = Path.Join(GetDataFolderPath(), InputTestDataFolder, TestConfigsFolder);
            _appSettings = new AppSettings();
            _appSettings.DataDirectoryPath = Path.Combine(GetDataFolderPath(), DataFolder);

            CleanDirectory(_appSettings.DataDirectoryPath);
        }

        private string GetDataFolderPath()
        {
            return AppContext.BaseDirectory;
        }

        private void CleanDirectory(string path)
        {
            var di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private IItemsConfigLocator GetConfigLocator(ISmartHomeApiFabric fabric)
        {
            return new ItemsConfigLocator(fabric);
        }

        private IStandardItemsLocatorBridge GetStandardItemsLocatorBridge(IItemsLocator itemsLocator)
        {
            return new StandardItemsLocatorBridge(itemsLocator);
        }

        [Test]
        public async Task NoConfigsDirectoryTest()
        {
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var configs = await configLocator.GetItemsConfigs("");

            Assert.AreEqual(0, configs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task NoInitialConfigsTest()
        {
            Directory.CreateDirectory(Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var configs = await configLocator.GetItemsConfigs("");

            Assert.AreEqual(0, configs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task OneInitialConfigTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config => { tcs.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task TwoTheSameInitialConfigsTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));
            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1CopyConfigName));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            int counter = 0;
            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded = async config =>
            {
                counter++;
                tcs.SetResult(true);
            };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);
            Assert.AreEqual(1, counter);

            configLocator.Dispose();
        }

        [Test]
        public async Task AddOneConfigTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded = async config => { tcs.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task AddOneConfigAndDeleteBeforeProcessingTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded = async config => { tcs.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            await Task.Delay(10);

            File.Delete(Path.Join(configsPath, TestItem1ConfigName));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsFalse(result);

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(0, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task UpdateOneConfigTest()
        {
            int eventsCounter = 0;

            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 1)
                    tcs.SetResult(true);

                if (eventsCounter == 2)
                    Assert.Fail();
            };
            itemsLocator.OnConfigUpdated += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 2)
                    tcs1.SetResult(true);
            };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            var result = await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            var itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.IsNull(itemConfig.TestString);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ChangedConfigName), Path.Join(configsPath, TestItem1ConfigName),
                true);

            ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs1.TrySetCanceled());

            result = await tcs1.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.AreEqual("Test", itemConfig.TestString);
        }

        [Test]
        public async Task UpdateOneConfigWithTheSameWithoutChangesTest()
        {
            int eventsCounter = 0;

            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 1)
                    tcs.SetResult(true);

                if (eventsCounter == 2)
                    Assert.Fail();
            };
            itemsLocator.OnConfigUpdated += async config => { Assert.Fail(); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            var result = await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            var itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.IsNull(itemConfig.TestString);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName), true);

            ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs1.TrySetResult(false));

            result = await tcs1.Task;
            Assert.IsFalse(result);

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.IsNull(itemConfig.TestString);
        }

        [Test]
        public async Task DeleteInitialConfigTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();

            itemsLocator.OnConfigAdded += async config => { };
            itemsLocator.OnConfigDeleted += async config => { tcs.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            File.Delete(Path.Join(configsPath, TestItem1ConfigName));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(0, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task AddOneConfigAndThenDeleteTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded = async config => { tcs.SetResult(true); };
            itemsLocator.OnConfigDeleted += async config => { tcs1.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            File.Delete(Path.Join(configsPath, TestItem1ConfigName));

            ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs1.TrySetCanceled());

            await tcs1.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(0, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task OneInitialConfigAndThenAddItemsLocatorTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var itemConfigs = await configLocator.GetItemsConfigs(TestItem1Type);

            Assert.AreEqual(0, itemConfigs.Count);

            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config => { tcs.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(TestItem1Type);

            Assert.AreEqual(1, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task OneInitialConfigAndThenAddItemsLocatorAndThenUpdateConfigTest()
        {
            int eventsCounter = 0;

            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var itemConfigs = await configLocator.GetItemsConfigs(TestItem1Type);

            Assert.AreEqual(0, itemConfigs.Count);

            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 1)
                    tcs.SetResult(true);

                if (eventsCounter == 2)
                    Assert.Fail();
            };
            itemsLocator.OnConfigUpdated += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 2)
                    tcs1.SetResult(true);
            };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(TestItem1Type);

            Assert.AreEqual(1, itemConfigs.Count);

            var itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.IsNull(itemConfig.TestString);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ChangedConfigName), Path.Join(configsPath, TestItem1ConfigName),
                true);

            ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs1.TrySetCanceled());

            await tcs1.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.AreEqual("Test", itemConfig.TestString);

            configLocator.Dispose();
        }

        [Test]
        public async Task OneInitialConfigAndThenAddItemsLocatorAndThenUpdateTheSameConfigTest()
        {
            int eventsCounter = 0;

            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var itemConfigs = await configLocator.GetItemsConfigs(TestItem1Type);

            Assert.AreEqual(0, itemConfigs.Count);

            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 1)
                    tcs.SetResult(true);

                if (eventsCounter == 2)
                    Assert.Fail();
            };
            itemsLocator.OnConfigUpdated += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 2)
                    Assert.Fail();
            };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(TestItem1Type);

            Assert.AreEqual(1, itemConfigs.Count);

            var itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.IsNull(itemConfig.TestString);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName),
                true);

            ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs1.SetResult(true));

            await tcs1.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.IsNull(itemConfig.TestString);

            configLocator.Dispose();
        }

        [Test]
        public async Task OneInitialConfigWithInvalidJsonTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, InvalidConfigName), Path.Join(configsPath, InvalidConfigName));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config => { tcs.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsFalse(result);

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(0, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task OneInitialConfigUpdateWithInvalidJsonTest()
        {
            int eventsCounter = 0;

            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 1)
                    tcs.SetResult(true);
                else
                    Assert.Fail();
            };
            itemsLocator.OnConfigUpdated += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 2)
                    Assert.Fail();
            };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            File.Copy(Path.Join(_inputTestDataFolder, InvalidConfigName), Path.Join(configsPath, TestItem1ConfigName), true);

            await Task.Delay(1000);

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task OneInitialInvalidConfigWithoutItemIdTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, InvalidConfigWithoutItemIdName),
                Path.Join(configsPath, InvalidConfigWithoutItemIdName));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config => { tcs.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsFalse(result);

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(0, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task OneInitialInvalidConfigWithoutItemTypeTest()
        {
            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, InvalidConfigWithoutItemTypeName),
                Path.Join(configsPath, InvalidConfigWithoutItemTypeName));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config => { tcs.SetResult(true); };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsFalse(result);

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(0, itemConfigs.Count);

            configLocator.Dispose();
        }

        [Test]
        public async Task UpdateOneConfigWithConfigForAnotherItemTypeTest()
        {
            int eventsCounter = 0;

            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();

            //Create the first locator
            var itemsLocator1 = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();

            itemsLocator1.OnConfigAdded += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 1)
                {
                    tcs.SetResult(true);
                    return;
                }

                Assert.Fail();
            };
            itemsLocator1.OnConfigUpdated += async config => { Assert.Fail(); };
            itemsLocator1.OnConfigDeleted += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 2)
                {
                    tcs1.SetResult(true);
                    return;
                }

                Assert.Fail();
            };

            var itemsLocatorWrapper1 = GetStandardItemsLocatorBridge(itemsLocator1);
            pluginsLocator.AddLocator(itemsLocatorWrapper1);

            //Create the second locator
            var itemsLocator2 = new TestItem2ItemLocator();
            var tcs2 = new TaskCompletionSource<bool>();

            itemsLocator2.OnConfigAdded += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 3)
                {
                    tcs2.SetResult(true);
                    return;
                }

                Assert.Fail();
            };
            itemsLocator2.OnConfigUpdated += async config => { Assert.Fail(); };
            itemsLocator2.OnConfigDeleted += async config => { Assert.Fail(); };

            var itemsLocatorWrapper2 = GetStandardItemsLocatorBridge(itemsLocator2);

            pluginsLocator.AddLocator(itemsLocatorWrapper2);

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ChangedConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            var result = await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator1.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            var itemConfig = (TestItem1Config)itemConfigs.First();
            Assert.AreEqual("Test", itemConfig.TestString);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem2ConfigName), Path.Join(configsPath, TestItem1ConfigName), true);

            ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs1.TrySetCanceled());

            result = await tcs1.Task;

            ct.Cancel();
            ct.Dispose();

            var ct1 = new CancellationTokenSource(2000);
            ct1.Token.Register(() => tcs2.TrySetCanceled());

            result = await tcs2.Task;

            ct1.Cancel();
            ct1.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator1.ItemType);

            Assert.AreEqual(0, itemConfigs.Count);

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator2.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            var item2Config = (TestItem2Config)itemConfigs.First();
            Assert.AreEqual("TestTest", item2Config.TestString1);
        }

        [Test]
        public async Task RemoveInitialItemsLocatorAndThenAddAgainTest()
        {
            int eventsCounter = 0;

            var configsPath = Path.Join(_appSettings.DataDirectoryPath, ConfigsFolder);
            Directory.CreateDirectory(configsPath);

            File.Copy(Path.Join(_inputTestDataFolder, TestItem1ConfigName), Path.Join(configsPath, TestItem1ConfigName));

            var pluginsLocator = new ItemsPluginsLocatorForConfigTests();
            var itemsLocator = new TestItem1ItemLocator();

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();
            itemsLocator.OnConfigAdded += async config =>
            {
                eventsCounter++;

                if (eventsCounter == 1)
                {
                    tcs.SetResult(true);
                    return;
                }

                if (eventsCounter == 2)
                {
                    tcs1.SetResult(true);
                    return;
                }

                Assert.Fail();
            };

            var itemsLocatorWrapper = GetStandardItemsLocatorBridge(itemsLocator);

            pluginsLocator.AddLocator(itemsLocatorWrapper);
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            fabric.ItemsPluginsLocator = pluginsLocator;

            var configLocator = GetConfigLocator(fabric);
            await configLocator.Initialize();

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            var itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            //Remove items locator and make sure that configs are also removed
            pluginsLocator.RemoveLocator(itemsLocatorWrapper);

            //It's a bad thing of course but need to be sure that ItemLocatorDeleted event is processed by ItemsConfigLocator
            await Task.Delay(1000);

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(0, itemConfigs.Count);

            //Add items locator back
            pluginsLocator.AddLocator(itemsLocatorWrapper);

            ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs1.TrySetCanceled());

            await tcs1.Task;

            ct.Cancel();
            ct.Dispose();

            itemConfigs = await configLocator.GetItemsConfigs(itemsLocator.ItemType);

            Assert.AreEqual(1, itemConfigs.Count);

            configLocator.Dispose();
        }
    }
}