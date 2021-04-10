using System;
using System.Collections.Generic;
using System.Reflection;

namespace SmartHomeApi.Core
{
    public class LoadingPluginResult
    {
        public bool Success { get; set; }
        public WeakReference Reference { get; set; }
        public CollectibleAssemblyContext AssemblyContext { get; set; }
        public Assembly Assembly { get; set; }
        public List<Type> LocatorTypes { get; set; }
    }
}