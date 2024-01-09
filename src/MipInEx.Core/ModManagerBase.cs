using MipInEx.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace MipInEx;

public abstract partial class ModManagerBase
{
    private readonly FullModRegistry registry;
    private readonly Queue<ModLoadBatch> modLoadBatches;

    protected ModManagerBase()
    {
        this.registry = new();
        this.modLoadBatches = new();
    }

    protected bool HasModsToLoad => this.modLoadBatches.Count > 0;

    /// <summary>
    /// Gets the full registry that belongs to this mod manager.
    /// </summary>
    protected internal FullModRegistry FullRegistry => this.registry;

    /// <summary>
    /// The registry associated with this mod manager.
    /// </summary>
    public ModRegistry Registry => this.registry.Registry;

    public abstract ModManagerPaths Paths { get; }

    internal void EnqueueModToLoad(Mod mod)
    {
        this.modLoadBatches.Enqueue(new ModLoadBatch(mod));
    }

    internal void EnqueueModsToLoad(IEnumerable<Mod> mods)
    {
        this.modLoadBatches.Enqueue(new ModLoadBatch(mods));
    }

    protected internal bool TryDequeueModToLoad([NotNullWhen(true)] out Mod? mod)
    {
        mod = null;
        while (this.modLoadBatches.TryPeek(out ModLoadBatch? loadBatch))
        {
            if (loadBatch.TryDequeue(out mod))
            {
                return true;
            }

            this.modLoadBatches.Dequeue();
        }
        return false;
    }

    internal sealed class ModLoadBatch
    {
        private readonly Stack<DependencyStackFrame> dependencyStack;
        private readonly ImmutableArray<Mod> modsToLoad;
        private int queueIndex;

        public ModLoadBatch(Mod mod)
        {
            this.dependencyStack = new();
            this.queueIndex = 0;

            if (mod is null)
            {
                this.modsToLoad = ImmutableArray<Mod>.Empty;
                return;
            }

            this.modsToLoad = ImmutableArray.Create(mod);
        }

        public ModLoadBatch(IEnumerable<Mod> mods)
        {
            this.dependencyStack = new();
            this.queueIndex = 0;

            if (mods is null)
            {
                this.modsToLoad = ImmutableArray<Mod>.Empty;
                return;
            }

            this.modsToLoad = mods.OfType<Mod>().ToImmutableArray();
        }

        public bool TryDequeue([NotNullWhen(true)] out Mod? mod)
        {
            if (this.dependencyStack.Count == 0)
            {
                if (!this.TryGetNextEnqueuedMod(out Mod? enqueuedMod))
                {
                    mod = null;
                    return false;
                }

                this.dependencyStack.Push(new DependencyStackFrame(enqueuedMod));
            }

            while (this.dependencyStack.TryPeek(out DependencyStackFrame? dependencyStackFrame))
            {
                while (dependencyStackFrame.dependencyQueueIndex < dependencyStackFrame.dependencyQueue.Length)
                {
                    Mod dependency = dependencyStackFrame.dependencyQueue[dependencyStackFrame.dependencyQueueIndex++];

                    if (dependency.IsLoaded || !this.ValidateModIsLoadableImpl(dependency))
                    {
                        continue;
                    }

                    this.dependencyStack.Push(new DependencyStackFrame(dependency));
                    break;
                }

                mod = dependencyStackFrame.mod;
                this.dependencyStack.Pop();

                if (mod.IsLoaded)
                {
                    continue;
                }

                if (this.ValidateModIsLoadable(mod))
                {
                    return true;
                }
            }

            mod = null;
            return false;
        }

        private bool TryGetNextEnqueuedMod([NotNullWhen(true)] out Mod? mod)
        {
            while (this.queueIndex < this.modsToLoad.Length)
            {
                mod = this.modsToLoad[this.queueIndex++];
                if (this.ValidateModIsLoadable(mod))
                {
                    return true;
                }
            }
            mod = null;
            return false;
        }

        private bool ValidateModIsLoadable(Mod mod)
        {
            if (mod.IsLoaded)
            {
                return false;
            }

            if (!this.ValidateModIsLoadableImpl(mod))
            {
                this.LogModSkip(mod);
                return false;
            }
            return true;
        }

        private bool ValidateModIsLoadableImpl(Mod mod)
        {
            if (mod.IsCircularDependency ||
                mod.MissingDependencies.Count > 0)
            {
                return false;
            }

            foreach (Mod incompatibility in mod.Incompatibilities)
            {
                if (incompatibility.IsLoaded || this.modsToLoad.Contains(incompatibility))
                {
                    return false;
                }
            }

            foreach (Mod dependency in mod.RequiredDependencies)
            {
                if (!dependency.IsLoaded && !this.ValidateModIsLoadableImpl(dependency))
                {
                    return false;
                }
            }

            return true;
        }

        private void LogModSkip(Mod mod)
        {
            if ((LogLevel.Info & Logger.ListenedLogLevels) == LogLevel.None)
            {
                return;
            }

            StringBuilder textBuilder = new();
            textBuilder.Append("Skip ");
            textBuilder.AppendLine(mod.Guid);

            bool hasInfo = false;

            if (mod.IsCircularDependency)
            {
                hasInfo = true;
                textBuilder.AppendLine("  Circular Dependency");
            }

            bool hasDependencies = false;
            foreach (ModDependencyInfo missingDependency in mod.MissingDependencies)
            {
                hasInfo = true;
                if (!hasDependencies)
                {
                    textBuilder.AppendLine("  Dependencies:");
                    hasDependencies = true;
                }

                textBuilder.Append("    MISSING - ");
                textBuilder.AppendLine(missingDependency.Guid);
            }

            foreach (Mod dependency in mod.Dependencies)
            {
                hasInfo = true;
                if (!hasDependencies)
                {
                    textBuilder.AppendLine("  Dependencies:");
                    hasDependencies = true;
                }

                if (dependency.IsLoaded)
                {
                    textBuilder.Append("    LOADED");
                }
                else if (!this.ValidateModIsLoadableImpl(dependency))
                {
                    textBuilder.Append("    HAS ERROR");
                }
                else
                {
                    textBuilder.Append("    NOT LOADED");
                }
                textBuilder.Append(" - ");
                textBuilder.AppendLine(dependency.Guid);
            }

            if (mod.Incompatibilities.Count > 0)
            {
                hasInfo = true;
                textBuilder.AppendLine("  Incompatibilities:");
            }

            foreach (Mod incompatibility in mod.Incompatibilities)
            {
                if (incompatibility.IsLoaded)
                {
                    textBuilder.Append("    ");
                    textBuilder.Append(incompatibility.Guid);
                    textBuilder.AppendLine(" - LOADED");
                }
                else if (this.modsToLoad.Contains(incompatibility))
                {
                    textBuilder.Append("    ");
                    textBuilder.Append(incompatibility.Guid);
                    textBuilder.AppendLine(" - LOAD QUEUED");
                }
                else
                {
                    textBuilder.Append("    ");
                    textBuilder.Append(incompatibility.Guid);
                    textBuilder.AppendLine(" - NOT LOADED");
                }
            }

            if (!hasInfo)
            {
                textBuilder.AppendLine("  No load information");
            }

            Logger.Log(LogLevel.Info, textBuilder.ToString());

        }

        private sealed class DependencyStackFrame
        {
            public readonly Mod mod;
            public readonly ImmutableArray<Mod> dependencyQueue;
            public int dependencyQueueIndex;

            public DependencyStackFrame(Mod mod)
            {
                this.mod = mod;
                this.dependencyQueueIndex = 0;
                IReadOnlyList<Mod> dependencies = mod.Dependencies;
                if (dependencies is ImmutableArray<Mod> immutableDependencies)
                {
                    this.dependencyQueue = immutableDependencies;
                    return;
                }

                if (dependencies.Count == 0)
                {
                    this.dependencyQueue = ImmutableArray<Mod>.Empty;
                    return;
                }

                this.dependencyQueue = dependencies.ToImmutableArray();
            }
        }
    }
}

/// <summary>
/// The paths of a <see cref="IModManager"/>.
/// </summary>
public sealed class ModManagerPaths
{
    private readonly string gameAssembliesDirectory;
    private readonly string modsDirectory;
    private readonly string configDirectory;
    private readonly string assemblyCachePath;

    /// <summary>
    /// Initializes this paths instance.
    /// </summary>
    /// <param name="gameAssembliesDirectory">
    /// The path to the game assemblies directory.
    /// </param>
    /// <param name="modsDirectory">
    /// The path to the mods directory.
    /// </param>
    /// <param name="configDirectory">
    /// The path the config directory.
    /// </param>
    /// <param name="assemblyCachePath">
    /// The path to the assembly cache file.
    /// </param>
    public ModManagerPaths(
        string gameAssembliesDirectory,
        string modsDirectory,
        string configDirectory,
        string assemblyCachePath)
    {
        this.gameAssembliesDirectory = gameAssembliesDirectory;
        this.modsDirectory = modsDirectory;
        this.configDirectory = configDirectory;
        this.assemblyCachePath = assemblyCachePath;
    }

    /// <summary>
    /// The path to the game's assemblies.
    /// </summary>
    public string GameAssembliesDirectory => this.gameAssembliesDirectory;

    /// <summary>
    /// The path to the mod directory.
    /// </summary>
    public string ModsDirectory => this.modsDirectory;

    /// <summary>
    /// The path to the config directory.
    /// </summary>
    public string ConfigDirectory => this.configDirectory;

    /// <summary>
    /// The path to the assembly cache file.
    /// </summary>
    public string AssemblyCachePath => this.assemblyCachePath;
}