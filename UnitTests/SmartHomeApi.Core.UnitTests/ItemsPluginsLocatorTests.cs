using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Utils;
using NUnit.Framework;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Configuration;
using SmartHomeApi.Core.Services;
using SmartHomeApi.UnitTestsBase.Stubs;

namespace SmartHomeApi.Core.UnitTests
{
    class ItemsPluginsLocatorTests
    {
        private const string InputTestDataFolder = "InputTestData";
        private const string DataFolder = "SmartHomeApiDataTest";
        private const string PluginsFolder = "Plugins";
        private const string StandardTestPlugin1Folder = "StandardTestPlugin1";

        private AppSettings _appSettings;
        private string _inputTestDataFolder;

        [SetUp]
        public void SetUp()
        {
            _inputTestDataFolder = Path.Join(GetDataFolderPath(), InputTestDataFolder);
            _appSettings = new AppSettings();
            _appSettings.DataDirectoryPath = Path.Combine(GetDataFolderPath(), DataFolder);

            CleanDataDirectory(_appSettings.DataDirectoryPath);
        }

        private IItemsPluginsLocator GetPluginLocator(ISmartHomeApiFabric fabric)
        {
            return new ItemsPluginsLocator_New(fabric);
        }

        private string GetDataFolderPath()
        {
            return AppContext.BaseDirectory;
        }

        private void CleanDataDirectory(string path)
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
        public async Task NoPluginsDirectoryTest1()
        {
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());
        }

        [Test]
        public async Task NoInitialPluginsTest1()
        {
            Directory.CreateDirectory(Path.Join(_appSettings.DataDirectoryPath, PluginsFolder));
            
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());
        }

        [Test]
        public async Task OneInitialPluginTest1()
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
        }

        [Test]
        public async Task TwoTheSameInitialPluginsTest1()
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
        }

        [Test]
        public async Task AddOnePluginTest1()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            _appSettings.PluginsLoadingTimeMs = 10;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder),
                Path.Join(pluginsPath, StandardTestPlugin1Folder));

            await Task.Delay(100);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(1, itemLocators.Count());
        }

        [Test]
        public async Task AddOnePluginAndDeleteBeforeProcessingTest1()
        {
            var pluginsPath = Path.Join(_appSettings.DataDirectoryPath, PluginsFolder);
            Directory.CreateDirectory(pluginsPath);

            _appSettings.PluginsLoadingTimeMs = 100;
            var fabric = new SmartHomeApiStubFabric(_appSettings);
            var pluginLocator = GetPluginLocator(fabric);
            await pluginLocator.Initialize();

            var itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());

            var pluginPath = Path.Join(pluginsPath, StandardTestPlugin1Folder);
            FileHelper.Copy(Path.Join(_inputTestDataFolder, StandardTestPlugin1Folder), pluginPath);
            await Task.Delay(10);

            var di = new DirectoryInfo(pluginPath);
            di.Delete(true);

            await Task.Delay(200);

            itemLocators = await pluginLocator.GetItemsLocators();

            Assert.AreEqual(0, itemLocators.Count());
        }

        //Two files in plugin
        //Add event for new locator
    }
}