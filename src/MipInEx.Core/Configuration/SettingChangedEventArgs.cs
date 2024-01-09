using System;

namespace MipInEx.Configuration;

/// <summary>
/// Arguments for events concerning a change of a setting.
/// </summary>
public sealed class SettingChangedEventArgs : EventArgs
{
    private readonly ConfigEntryBase changedSetting;

    /// <inheritdoc/>
    public SettingChangedEventArgs(ConfigEntryBase changedSetting)
    {
        this.changedSetting = changedSetting;
    }

    /// <summary>
    /// Setting that was changed.
    /// </summary>
    public ConfigEntryBase ChangedSetting => this.changedSetting;
}
