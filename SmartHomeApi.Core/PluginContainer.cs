using System.Collections.Generic;
using System.IO;
using SmartHomeApi.Core.Interfaces;

namespace SmartHomeApi.Core
{
    public class PluginContainer
    {
        public string PluginDirectoryName { get; set; }
        public DirectoryInfo PluginDirectoryInfo { get; set; }
        public List<FileInfo> Files { get; set; }
        public List<FileInfo> DllFiles { get; set; }

        public List<FileInfo> TempFiles { get; set; }
        public List<FileInfo> TempDllFiles { get; set; }

        public CollectibleAssemblyContext AssemblyContext { get; set; }
        public List<IItemsLocator> Locators { get; set; } = new List<IItemsLocator>();

        public override string ToString()
        {
            return PluginDirectoryName;
        }
    }
}