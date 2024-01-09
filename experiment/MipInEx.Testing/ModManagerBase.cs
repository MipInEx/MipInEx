using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MipInEx;

public sealed class ModManagerBase
{
    private readonly FullModRegistry registry;
    private readonly Queue<ModLoadBatch> modLoadBatches;

    public ModManagerBase()
    {
        this.registry = new();
        this.modLoadBatches = new();
    }

    internal FullModRegistry FullRegistry => this.registry;

    internal void EnqueueModToLoad(Mod mod)
    {
        this.modLoadBatches.Enqueue(new ModLoadBatch(mod));
    }

    internal void EnqueueModsToLoad(IEnumerable<Mod> mods)
    {
        this.modLoadBatches.Enqueue(new ModLoadBatch(mods));
    }

    internal bool TryDequeueModToLoad([NotNullWhen(true)] out Mod? mod)
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
            Console.Write("Skip ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(mod.Guid);
            Console.ResetColor();

            bool hasInfo = false;

            if (mod.IsCircularDependency)
            {
                hasInfo = true;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("  Circular Dependency");
                Console.ResetColor();
            }


            bool hasDependencies = false;
            foreach (ModDependencyInfo missingDependency in mod.MissingDependencies)
            {
                hasInfo = true;
                if (!hasDependencies)
                {
                    Console.WriteLine("  Dependencies:");
                    hasDependencies = true;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("    MISSING");
                Console.ResetColor();
                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(missingDependency.Guid);
                Console.ResetColor();
            }

            foreach (Mod dependency in mod.Dependencies)
            {
                hasInfo = true;
                if (!hasDependencies)
                {
                    Console.WriteLine("  Dependencies:");
                    hasDependencies = true;
                }

                if (dependency.IsLoaded)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("    LOADED");
                    Console.ResetColor();
                }
                else if (!this.ValidateModIsLoadableImpl(dependency))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("    HAS ERROR");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("    NOT LOADED");
                    Console.ResetColor();
                }
                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(dependency.Guid);
                Console.ResetColor();
            }

            if (mod.Incompatibilities.Count > 0)
            {
                hasInfo = true;
                Console.WriteLine("  Incompatibilities:");
            }

            foreach (Mod incompatibility in mod.Incompatibilities)
            {
                if (incompatibility.IsLoaded || this.modsToLoad.Contains(incompatibility))
                {
                    Console.Write("    ");
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(incompatibility.Guid);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write("    ");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(incompatibility.Guid);
                    Console.ResetColor();
                }
            }

            if (!hasInfo)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No load information");
                Console.ResetColor();
            }

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


    /// <summary>
    /// The full mod registry in a mod manager.
    /// </summary>
    internal sealed class FullModRegistry
    {
        private Dictionary<string, Mod> mods;

        /// <summary>
        /// Initializes this full mod registry to be
        /// empty.
        /// </summary>
        public FullModRegistry()
        {
            this.mods = new();
        }

        public int Count => this.mods.Count;

        public Mod this[string guid]
        {
            get => this.mods[guid];
        }

        public bool ContainsMod([NotNullWhen(true)] string? guid)
        {
            return guid is not null && this.mods.ContainsKey(guid);
        }

        public bool TryGetMod([NotNullWhen(true)] string? guid, [NotNullWhen(true)] out Mod? mod)
        {
            if (guid is null)
            {
                mod = null;
                return false;
            }
            else
            {
                return this.mods.TryGetValue(guid, out mod);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// This is a very expensive operation due to the
        /// underlying frozen dictionary needing to be
        /// recalculated.
        /// </remarks>
        /// <param name="mods">
        /// The collection of mods to add.
        /// </param>
        // this is a very expensive operation, as the frozen
        // dictionary needs to be recalculated. 
        internal void AddMods(IEnumerable<Mod> mods)
        {
            foreach (Mod mod in mods)
            {
                this.mods.Add(mod.Guid, mod);
            }
        }
    }
}
