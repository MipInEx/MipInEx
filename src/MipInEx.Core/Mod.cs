using MipInEx.Bootstrap;
using MipInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace MipInEx;

// assume we have 4 mods that are like this:
// ModA
// - dependencies:
//   - ModB
//
// ModB
// - incompatibilities:
//   - ModC
//
// ModC
// - incompatibilities: 
//   - ModD
//
// ModD
//
// Assume we will auto-load:
// - ModA
// - ModB
// - ModC
//
// The following sort order should be achieved:
// - ModB
// - ModA
// - ModC
//
// 1. Loading ModB should fail, as ModC will also be loaded and
//    will result in an incompatibility.
// 2. Loading ModA should fail, as ModB wasn't loaded.
// 3. ModC should be loaded.
//
// Now assume we want to load ModD
// 1. We check all incompatibilities, and notice that ModC is
//    incompatible with ModD
// 2. We prompt the user:
//      Do you want to load ModD? Doing so will unload ModC!
// 3. If the user selects yes,
//   a. ModC should be unloaded
//   b. ModD should be loaded.
// 4. Otherwise if the user selects no,
//   Nothing should happen.

public sealed class Mod
{
    private readonly ModManagerBase modManager;
    private readonly ModManifest manifest;
    private readonly string? readme;
    private readonly string? changelog;
    private readonly byte[]? iconData;

    private readonly ModInfo info;

    private readonly ImmutableArray<ModAssetBundle> assetBundles;
    private readonly ImmutableArray<ModAssembly> assemblies;
    private readonly ImmutableArray<IModAsset> allAssets;

    private ImmutableArray<Mod> incompatibilities;
    private ImmutableArray<ModDependencyInfo> missingDependencies;
    private ImmutableArray<Mod> requiredDependencies;
    private ImmutableArray<Mod> dependencies;

    private bool isEnabled;
    private bool isCircularDependency;


    internal Mod(
        ModManagerBase modManager,
        ModManifest manifest,
        string? readme,
        string? changelog,
        byte[]? iconData,
        ImmutableArray<ModImporter.AssetBundleInfo> assetBundles,
        ImmutableArray<ModImporter.AssemblyInfo> assemblies)
    {
        this.readme = readme;
        this.changelog = changelog;
        this.iconData = iconData;
        this.manifest = manifest;
        this.modManager = modManager;

        this.assetBundles = assetBundles
            .Select(x => new ModAssetBundle(
                this,
                x.manifest,
                x.loader))
            .ToImmutableArray();
        this.assemblies = assemblies
            .Select(x => new ModAssembly(
                this,
                x.manifest,
                x.loader,
                x.rootPluginReference,
                x.internalPluginReferences))
            .ToImmutableArray();
        this.allAssets = this.assetBundles.OfType<IModAsset>()
            .Concat(this.assemblies)
            .OrderBy(x => x, Utility.AssetPriorityComparer)
            .ToImmutableArray();

        this.incompatibilities = ImmutableArray<Mod>.Empty;

        this.missingDependencies = ImmutableArray<ModDependencyInfo>.Empty;
        this.requiredDependencies = ImmutableArray<Mod>.Empty;
        this.dependencies = ImmutableArray<Mod>.Empty;

        this.isCircularDependency = false;
        this.info = new(this);

        foreach (ModAssembly assembly in this.assemblies)
        {
            assembly.Info.Initialize(this.info);
        }

        foreach (ModAssetBundle assetBundle in this.assetBundles)
        {
            assetBundle.Info.Initialize(this.info);
        }
    }

    /// <summary>
    /// The mod manager that manages this mod.
    /// </summary>
    public ModManagerBase ModManager => this.modManager;

    public IReadOnlyList<Mod> Incompatibilities => this.incompatibilities;

    public IReadOnlyList<ModDependencyInfo> MissingDependencies => this.missingDependencies;

    public IReadOnlyList<Mod> RequiredDependencies => this.requiredDependencies;

    public IReadOnlyList<Mod> Dependencies => this.dependencies;

    internal bool IsCircularDependency => this.isCircularDependency;

    public string Name => this.manifest.Name;
    public string Guid => this.manifest.Guid;
    public Version Version => this.manifest.Version;
    public ModManifest Manifest => this.manifest;

    public bool IsEnabled => this.isEnabled;

    public ModInfo Info => this.info;

    public bool IsLoaded => this.info.IsLoaded;

    public sealed override string ToString()
    {
        return this.manifest.ToString();
    }

    internal void RefreshIncompatibilitiesAndDependencies(ICollection<string> processedModGuids)
    {
        this.RefreshIncompatibilitiesAndDependencies(processedModGuids, new List<string>());
    }

    private void RefreshIncompatibilitiesAndDependencies(ICollection<string> processedModGuids, List<string> dependencyStack)
    {
        int existingDependencyIndex = dependencyStack.IndexOf(this.Guid);

        if (existingDependencyIndex > -1)
        {
            this.isCircularDependency = true;

            // this pass is here to mark the other dependencies
            // that requires this dependency as a circular
            // dependency too if it exists.
            //
            // This fixes the following bug.
            //
            // ModD
            //   Depends on ModE
            // ModE
            //   Depends on ModD
            //
            // In the load order:
            // - ModD
            // - ModE
            //
            // ModD would be skipped due to being marked a
            // circular dependency, but ModE wouldn't be marked
            // as a circular dependency and would be invalidly
            // loaded.

            for (int index = dependencyStack.Count - 1; index > existingDependencyIndex; index--)
            {
                if (this.modManager.FullRegistry.TryGetMod(dependencyStack[index], out Mod? mod))
                {
                    mod.isCircularDependency = true;
                }
            }
            return;
        }

        if (processedModGuids.Contains(this.Guid))
            return;

        ImmutableArray<Mod>.Builder incompatibilities = ImmutableArray.CreateBuilder<Mod>();

        ImmutableArray<Mod>.Builder requiredDependencies = ImmutableArray.CreateBuilder<Mod>();
        ImmutableArray<Mod>.Builder dependencies = ImmutableArray.CreateBuilder<Mod>();
        ImmutableArray<ModDependencyInfo>.Builder missingDependencies = ImmutableArray.CreateBuilder<ModDependencyInfo>();

        foreach (ModIncompatibilityInfo incompatibility in this.manifest.Incompatibilities)
        {
            if (this.modManager.FullRegistry.TryGetMod(incompatibility.Guid, out Mod? mod) &&
                incompatibility.IncludesVersion(mod.Version))
            {
                incompatibilities.Add(mod);
            }
        }

        dependencyStack.Add(this.Guid);
        foreach (ModDependencyInfo dependency in this.manifest.Dependencies)
        {
            if (this.modManager.FullRegistry.TryGetMod(dependency.Guid, out Mod? mod) &&
                dependency.IncludesVersion(mod.Version))
            {
                mod.RefreshIncompatibilitiesAndDependencies(processedModGuids, dependencyStack);
                dependencies.Add(mod);

                if (dependency.Required)
                    requiredDependencies.Add(mod);

                continue;
            }

            if (!dependency.Required)
                continue;

            missingDependencies.Add(dependency);
        }
        dependencyStack.RemoveAt(dependencyStack.Count - 1);
        processedModGuids.Add(this.Guid);

        this.incompatibilities = incompatibilities.ToImmutable();
        this.missingDependencies = missingDependencies.ToImmutable();
        this.requiredDependencies = requiredDependencies.ToImmutable();
        this.dependencies = dependencies.ToImmutable();
    }
}
