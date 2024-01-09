using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MipInEx;

public sealed class Mod
{
    private readonly ModManagerBase modManager;
    private readonly string guid;
    private readonly Version version;
    private readonly ImmutableArray<ModDependencyInfo> rawDependencies;
    private readonly ImmutableArray<ModIncompatibilityInfo> rawIncompatibilities;

    private ImmutableArray<Mod> incompatibilities;
    private ImmutableArray<ModDependencyInfo> missingDependencies;
    private ImmutableArray<Mod> requiredDependencies;
    private ImmutableArray<Mod> dependencies;
    private bool isCircularDependency;
    private bool isLoaded;

    public Mod(ModManagerBase modManager, string guid, ImmutableArray<ModDependencyInfo> rawDependencies, ImmutableArray<ModIncompatibilityInfo> rawIncompatibilities)
    {
        this.modManager = modManager;
        this.guid = guid;
        this.version = new Version(1, 0, 0);
        this.rawDependencies = rawDependencies;
        this.rawIncompatibilities = rawIncompatibilities;
        this.isLoaded = false;
        this.incompatibilities = ImmutableArray<Mod>.Empty;
        this.missingDependencies = ImmutableArray<ModDependencyInfo>.Empty;
        this.requiredDependencies = ImmutableArray<Mod>.Empty;
        this.dependencies = ImmutableArray<Mod>.Empty;
    }

    public string Guid => this.guid;
    public Version Version => this.version;

    public IReadOnlyList<Mod> Incompatibilities => this.incompatibilities;
    public IReadOnlyList<ModDependencyInfo> MissingDependencies => this.missingDependencies;
    public IReadOnlyList<Mod> RequiredDependencies => this.requiredDependencies;
    public IReadOnlyList<Mod> Dependencies => this.dependencies;

    internal bool IsCircularDependency => this.isCircularDependency;

    public bool IsLoaded
    {
        get => this.isLoaded;
    }
    
    public void RefreshIncompatibilitiesAndDependencies(List<string> refreshedModGuids)
    {
        this.RefreshIncompatibilitiesAndDependencies(refreshedModGuids, new List<string>());
    }

    private void RefreshIncompatibilitiesAndDependencies(List<string> refreshedModGuids, List<string> dependencyStack)
    {
        int existingDependencyIndex = dependencyStack.IndexOf(this.Guid);

        if (existingDependencyIndex > -1)
        {
            this.isCircularDependency = true;

            for (int index = dependencyStack.Count - 1; index > existingDependencyIndex; index--)
            {
                if (this.modManager.FullRegistry.TryGetMod(dependencyStack[index], out Mod? mod))
                {
                    mod.isCircularDependency = true;
                }
            }
            return;
        }
        if (refreshedModGuids.Contains(this.Guid))
            return;

        ImmutableArray<Mod>.Builder incompatibilities = ImmutableArray.CreateBuilder<Mod>();

        ImmutableArray<Mod>.Builder requiredDependencies = ImmutableArray.CreateBuilder<Mod>();
        ImmutableArray<Mod>.Builder allDependencies = ImmutableArray.CreateBuilder<Mod>();
        ImmutableArray<ModDependencyInfo>.Builder missingDependencies = ImmutableArray.CreateBuilder<ModDependencyInfo>();

        foreach (ModIncompatibilityInfo incompatibility in this.rawIncompatibilities)
        {
            if (this.modManager.FullRegistry.TryGetMod(incompatibility.Guid, out Mod? mod) &&
                incompatibility.IncludesVersion(mod.Version))
            {
                incompatibilities.Add(mod);
            }
        }

        dependencyStack.Add(this.Guid);
        foreach (ModDependencyInfo dependency in this.rawDependencies)
        {
            if (this.modManager.FullRegistry.TryGetMod(dependency.Guid, out Mod? mod) &&
                dependency.IncludesVersion(mod.Version))
            {
                mod.RefreshIncompatibilitiesAndDependencies(refreshedModGuids, dependencyStack);
                allDependencies.Add(mod);
                if (dependency.Required)
                {
                    requiredDependencies.Add(mod);
                }
                continue;
            }

            if (!dependency.Required)
                continue;

            missingDependencies.Add(dependency);
        }
        dependencyStack.RemoveAt(dependencyStack.Count - 1);
        refreshedModGuids.Add(this.Guid);

        this.incompatibilities = incompatibilities.ToImmutable();
        this.requiredDependencies = requiredDependencies.ToImmutable();
        this.missingDependencies = missingDependencies.ToImmutable();
        this.dependencies = allDependencies.ToImmutable();
    }

    public void Load()
    {
        Console.WriteLine("Loading " + this.Guid);
        this.isLoaded = true;
    }

    public void LogInfo()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Info - Mod ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(this.Guid);
        Console.ResetColor();

        if (this.rawDependencies.Length > 0)
        {
            Console.WriteLine("  Depends on:");
            foreach (ModDependencyInfo dependency in this.rawDependencies)
            {
                Console.Write("    ");
                if (dependency.Required)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("HARD");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("SOFT");
                    Console.ResetColor();
                }
                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(dependency.Guid);
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  No dependencies");
            Console.ResetColor();
        }

        if (this.rawIncompatibilities.Length > 0)
        {
            Console.WriteLine("  Incompatible with:");
            foreach (ModIncompatibilityInfo incompatibility in this.rawIncompatibilities)
            {
                Console.Write("    ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(incompatibility.Guid);
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  No incompatibilities");
            Console.ResetColor();
        }
    }
}