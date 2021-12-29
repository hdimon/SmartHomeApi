using System;

namespace SmartHomeApi.Core.Interfaces;

public static class PluginsRuntimeSettings
{
    /// <summary>
    /// Indicates which minimal version of SmartHomeApi.Utils is supported by current version of SmartHomeApi.
    /// For example if current version is 1.2.0 and MinimalSupportedVersion is 1.1.0 then plugins which refer to
    /// SmartHomeApi.Utils 1.1.0 will work but plugins which refer to SmartHomeApi.Utils 1.0.0 will not.
    /// </summary>
    public static Version MinimalSupportedVersion { get; } = new("1.4.0.0");
}