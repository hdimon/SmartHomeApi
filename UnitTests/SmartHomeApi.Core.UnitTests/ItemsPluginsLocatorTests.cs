using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Utils;
using NUnit.Framework;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Services;
using SmartHomeApi.UnitTestsBase.Stubs;

namespace SmartHomeApi.Core.UnitTests
{
    /// <summary>
    /// These tests are bad because they rely on Task.Delay (considering that functionality is asynchronous)
    /// which can cause different results on different machines but nevertheless they were extremely helpful in development.
    /// </summary>
    class ItemsPluginsLocatorTests
    {
        private const string InputTestDataFolder = "InputTestData";
        private const string DataFolder = "SmartHomeApiDataTest";
        private const string PluginsFolder = "Plugins";
        private const string StandardTestPlugin1Folder = "StandardTestPlugin1";
        private const string StandardTestPlugin1FolderChanged = "StandardTestPlugin1Changed";
        private const string StandardTestPluginWithDependency1Folder = "StandardTestPluginWithDependency1";
        private const string StandardTestPlugin1DllName = "StandardTestPlugin1.dll";
        private const string StandardTestPluginWithDependency1DllName = "StandardTestPluginWithDependency1.dll";
        private const string PluginWithLocatorConstructorTimeoutFolder = "PluginWithLocatorConstructorTimeout";

        private AppSettings _appSettings;
        private string _inputTestDataFolder;

        [SetUp]
        public async Task SetUp()
        {
            //Wait a bit in order to free files after previous test
            //await Task.Delay(2000);

            _inputTestDataFolder = Path.Join(GetDataFolderPath(), InputTestDataFolder);
            _appSettings = new AppSettings();
            _appSettings.DataDirectoryPath = Path.Combine(GetDataFolderPath(), DataFolder);

            CleanDirectory(_appSettings.DataDirectoryPath);
        }

        private IItemsPluginsLocator GetPluginLocator(ISmartHomeApiFabric fabric)
        {
            return new ItemsPluginsLocator(fabric);
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

        [Test]
        public async Task NoPluginsDirectoryTest()
        {
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task NoInitialPluginsTest()
        {
            Directory.CreateDirectory(Path.Join(_appSettings.DataDirectoryPath, PluginsFolder));
            
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task OneInitialPluginTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task TwoTheSameInitialPluginsTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));
            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, "TheSamePlugin"));

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task AddOnePluginTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            _appSettings.ItemsPluginsLocator.PluginsLoadingTimeMs = 10;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);

            var tcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorAddedOrUpdated += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            var ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task AddOnePluginAndDeleteBeforeProcessingTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            _appSettings.ItemsPluginsLocator.PluginsLoadingTimeMs = 100;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);

            var tcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorAddedOrUpdated += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            var pluginPath = Path.Join(pluginsPath, StandardTestPlugin1Folder);
            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder), pluginPath);
            await Task.Delay(10);

            var di = new DirectoryInfo(pluginPath);
            di.Delete(true);

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(true));

            await tcs.Task;

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task AddOnePluginWithDependencyTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            _appSettings.ItemsPluginsLocator.PluginsLoadingTimeMs = 50;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);

            var tcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorAddedOrUpdated += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPluginWithDependency1Folder),
                Path.Join(pluginsPath, StandardTestPluginWithDependency1Folder));

            var ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task UpdateOnePluginWithAnotherPluginTest()
        {
            int eventsCounter = 0;
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            _appSettings.ItemsPluginsLocator.PluginsLoadingTimeMs = 10;
            _appSettings.ItemsPluginsLocator.PluginsUnloadingAttemptsIntervalMs = 100;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();
            var deletedTcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorAddedOrUpdated += (sender, args) =>
            {
                eventsCounter++;

                if (eventsCounter == 1)
                    tcs.SetResult(true);

                if (eventsCounter == 2)
                    tcs1.SetResult(true);
            };

            pluginLocator.ItemLocatorDeleted += (sender, args) =>
            {
                deletedTcs.SetResult(true);
            };

            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            var ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            Assert.AreEqual(1, eventsCounter);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            //"Update" plugin
            File.Delete(Path.Join(pluginsPath, StandardTestPlugin1Folder, StandardTestPlugin1DllName));
            File.Copy(Path.Join(_inputTestDataFolder, StandardTestPluginWithDependency1Folder, StandardTestPluginWithDependency1DllName),
                Path.Join(pluginsPath, StandardTestPlugin1Folder, StandardTestPluginWithDependency1DllName));

            ct = new CancellationTokenSource(10000);
            ct.Token.Register(() =>
            {
                deletedTcs.TrySetCanceled();
                tcs1.TrySetCanceled();
            });

            await deletedTcs.Task;
            await tcs1.Task;

            ct.Cancel();
            ct.Dispose();

            Assert.AreEqual(2, eventsCounter);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            var locator = itemLocators.First();

            Assert.AreEqual("StandardTestPluginWithDependency1", locator.ItemType);

            pluginLocator.Dispose();
        }

        [Test]
        public async Task UpdateOnePluginTest()
        {
            int eventsCounter = 0;
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            _appSettings.ItemsPluginsLocator.PluginsLoadingTimeMs = 100;
            _appSettings.ItemsPluginsLocator.PluginsUnloadingAttemptsIntervalMs = 100;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);

            var tcs = new TaskCompletionSource<bool>();
            var tcs1 = new TaskCompletionSource<bool>();

            void PluginLocatorOnItemLocatorAddedOrUpdated(object sender, ItemLocatorEventArgs e)
            {
                eventsCounter++;

                if (eventsCounter == 1)
                    tcs.SetResult(true);

                if (eventsCounter == 2)
                    tcs1.SetResult(true);
            }

            pluginLocator.ItemLocatorAddedOrUpdated += PluginLocatorOnItemLocatorAddedOrUpdated;

            pluginLocator.ItemLocatorDeleted += (sender, args) =>
            {
                Assert.Fail();
            };

            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            var ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            ct.Cancel();
            ct.Dispose();

            Assert.AreEqual(1, eventsCounter);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            //"Update" plugin
            File.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1FolderChanged, StandardTestPlugin1DllName),
                Path.Join(pluginsPath, StandardTestPlugin1Folder, StandardTestPlugin1DllName), true);

            ct = new CancellationTokenSource(10000);
            ct.Token.Register(() =>
                {
                    Console.WriteLine("TrySetCanceled " + DateTime.Now);
                    tcs1.TrySetCanceled();
                }
            );

            await tcs1.Task;

            ct.Dispose();

            Assert.AreEqual(2, eventsCounter);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task UpdateOnePluginWithTheSameWithoutChangesTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            _appSettings.ItemsPluginsLocator.PluginsLoadingTimeMs = 500;

            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            //Wait a bit in order not to get ItemLocatorAddedOrUpdated event right after initialization (it's needed only for this test)
            await Task.Delay(200);

            var tcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorAddedOrUpdated += (sender, args) =>
            {
                tcs.SetResult(true);
                Console.WriteLine("SetResult");
            };

            pluginLocator.ItemLocatorDeleted += (sender, args) =>
            {
                Assert.Fail();
            };

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsFalse(result);

            pluginLocator.Dispose();
        }

        [Test]
        public async Task DeleteInitialPluginTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            _appSettings.ItemsPluginsLocator.PluginsUnloadingAttemptsIntervalMs = 100;
            _appSettings.ItemsPluginsLocator.UnloadPluginsTriesIntervalMS = 100;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var tcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorDeleted += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            Directory.Delete(Path.Join(pluginsPath, StandardTestPlugin1Folder), true);

            var ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsTrue(result);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task AddOnePluginAndThenDeleteTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            _appSettings.ItemsPluginsLocator.PluginsLoadingTimeMs = 10;
            _appSettings.ItemsPluginsLocator.PluginsUnloadingAttemptsIntervalMs = 100;
            _appSettings.ItemsPluginsLocator.UnloadPluginsTriesIntervalMS = 100;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);

            var tcs = new TaskCompletionSource<bool>();
            var deletedTcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorAddedOrUpdated += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            pluginLocator.ItemLocatorDeleted += (sender, args) =>
            {
                deletedTcs.SetResult(true);
            };

            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            var ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => tcs.TrySetCanceled());

            await tcs.Task;

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            Directory.Delete(Path.Join(pluginsPath, StandardTestPlugin1Folder), true);

            ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => deletedTcs.TrySetResult(false));

            var result = await deletedTcs.Task;

            Assert.IsTrue(result);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task DeleteDllTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            _appSettings.ItemsPluginsLocator.PluginsUnloadingAttemptsIntervalMs = 100;
            _appSettings.ItemsPluginsLocator.UnloadPluginsTriesIntervalMS = 100;
            _appSettings.ItemsPluginsLocator.UnloadPluginsMaxTries = 10;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var tcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorDeleted += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            File.Delete(Path.Join(pluginsPath, StandardTestPlugin1Folder, StandardTestPlugin1DllName));

            var ct = new CancellationTokenSource(25000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsTrue(result);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task DeleteDllAndAddAgainTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            _appSettings.ItemsPluginsLocator.PluginsUnloadingAttemptsIntervalMs = 100;
            _appSettings.ItemsPluginsLocator.UnloadPluginsTriesIntervalMS = 100;
            _appSettings.ItemsPluginsLocator.UnloadPluginsMaxTries = 10;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var tcs = new TaskCompletionSource<bool>();
            var addedTcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorDeleted += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            pluginLocator.ItemLocatorAddedOrUpdated += (sender, args) =>
            {
                addedTcs.SetResult(true);
            };

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            File.Delete(Path.Join(pluginsPath, StandardTestPlugin1Folder, StandardTestPlugin1DllName));

            var ct = new CancellationTokenSource(25000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsTrue(result);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            File.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder, StandardTestPlugin1DllName),
                Path.Join(pluginsPath, StandardTestPlugin1Folder, StandardTestPlugin1DllName), true);

            ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => addedTcs.TrySetResult(false));

            result = await addedTcs.Task;

            Assert.IsTrue(result);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task DeleteMainDllButLeaveDependencyTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            _appSettings.ItemsPluginsLocator.PluginsUnloadingAttemptsIntervalMs = 100;
            _appSettings.ItemsPluginsLocator.UnloadPluginsTriesIntervalMS = 100;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var tcs = new TaskCompletionSource<bool>();

            pluginLocator.ItemLocatorDeleted += (sender, args) =>
            {
                tcs.SetResult(true);
            };

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());

            File.Delete(Path.Join(pluginsPath, StandardTestPlugin1Folder, StandardTestPlugin1DllName));

            var ct = new CancellationTokenSource(10000);
            ct.Token.Register(() => tcs.TrySetResult(false));

            var result = await tcs.Task;

            Assert.IsTrue(result);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            pluginLocator.Dispose();
        }

        [Test]
        public async Task ItemsLocatorTimeoutTestTest()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            FileHelper.Copy(Path.Join(_inputTestDataFolder, PluginWithLocatorConstructorTimeoutFolder),
                Path.Join(pluginsPath, PluginWithLocatorConstructorTimeoutFolder));

            _appSettings.ItemsPluginsLocator.ItemLocatorConstructorTimeoutMS = 500;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            pluginLocator.Dispose();
        }
    }
}