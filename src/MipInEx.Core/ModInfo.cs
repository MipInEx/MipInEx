using MipInEx.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MipInEx;

/// <summary>
/// Represents a mod.
/// </summary>
public sealed class ModInfo
{
    private readonly Mod mod;

    internal ModInfo(Mod mod)
    {
        this.mod = mod;
    }

    public string Guid => this.mod.Guid;
    public Version Version => this.mod.Version;

    /// <summary>
    /// Whether or not this mod is loaded.
    /// </summary>
    public bool IsLoaded => false;
}
