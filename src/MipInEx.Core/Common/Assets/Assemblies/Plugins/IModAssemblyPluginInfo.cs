namespace MipInEx;

/// <summary>
/// Contains general information about a mod's plugin.
/// </summary>
public interface IModAssemblyPluginInfo
{
    /// <inheritdoc cref="IModAssemblyPlugin.IsInternalPlugin"/>
    bool IsInternalPlugin { get; }

    /// <inheritdoc cref="IModAssemblyPlugin.Mod"/>
    ModInfo Mod { get; }

    /// <inheritdoc cref="IModAssemblyPlugin.Assembly"/>
    ModAssemblyInfo Assembly { get; }

    /// <inheritdoc cref="IModAssemblyPlugin.Metadata"/>
    ModPluginMetadata Metadata { get; }

    /// <inheritdoc cref="IModAssemblyPlugin.IsLoaded"/>
    bool IsLoaded { get; }

    /// <inheritdoc cref="IModAssemblyPlugin.GetDescriptorString()"/>
    string GetDescriptorString();
}
