using System;
using System.Collections.Generic;
using System.IO;

namespace SmartHomeApi.Core
{
    public class PluginEventArgs : EventArgs
    {
        public string PluginDirectoryName { get; set; }
        public DirectoryInfo PluginDirectoryInfo { get; set; }
        public List<FileInfo> Files { get; set; }
        public List<FileInfo> DllFiles { get; set; }
    }
}