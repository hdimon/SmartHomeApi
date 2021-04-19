using System;
using System.Collections.Generic;

namespace SmartHomeApi.Core
{
    public interface IPluginsFileWatcher : IDisposable
    {
        event EventHandler<PluginEventArgs> PluginAddedOrUpdated;
        event EventHandler<PluginEventArgs> PluginDeleted;

        IList<PluginEventArgs> FindPlugins();
    }
}