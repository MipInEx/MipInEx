using Mono.Cecil;
using MipInEx.Logging;
using MipInEx.JsonConverters;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace MipInEx.Bootstrap;

internal sealed class InitializeContext
{
    private readonly ModManagerBase modManager;
    private readonly string rootDirectory;
    private readonly bool useAssemblyCache;
    private Dictionary<string, CachedAssembly>? assemblyCache;
    private readonly DefaultAssemblyResolver cecilResolver;
    private readonly ReaderParameters readerParameters;
    private readonly JsonSerializerOptions serializerOptions;
    
    public InitializeContext(ModManagerBase modManager, string rootDirectory, bool useAssemblyCache)
    {
        this.modManager = modManager;
        this.rootDirectory = rootDirectory;
        this.assemblyCache = null;
        this.useAssemblyCache = useAssemblyCache;

        this.cecilResolver = new DefaultAssemblyResolver();
        this.readerParameters = new() { AssemblyResolver = this.cecilResolver };
        this.cecilResolver.ResolveFailure += this.CecilResolveOnFailure;

        this.serializerOptions = new JsonSerializerOptions()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters =
            {
                new ModDependencyInfoConverter(),
                new ModIncompatibilityInfoConverter(),
                new ModReferenceVersionRequirementConverter(),
                new ModManifestConverter(),
                new ModManifestAssetConverter(),
                new ModManifestInternalPluginConverter(),
                new ModManifestRootPluginConverter()
            }
        };
    }

    public event AssemblyResolveEventHandler AssemblyResolve = null!;
    public event Action<ModInfo> ModInitialized = null!;
    public event Action Finished = null!;

    private AssemblyDefinition? CecilResolveOnFailure(object sender, AssemblyNameReference reference)
    {
        AssemblyName assemblyName;
        try
        {
            assemblyName = new AssemblyName(reference.FullName);
        }
        catch
        {
            return null;
        }

        string fileName = $"{assemblyName.Name}.dll";

        string path = Path.Combine(this.modManager.Paths.GameAssembliesDirectory, fileName);
        try
        {
            if (File.Exists(path))
            {
                return AssemblyDefinition.ReadAssembly(path);
            }
        }
        catch { }


        foreach (string subDirectory in Directory.GetDirectories(this.modManager.Paths.ModsDirectory, "*", SearchOption.AllDirectories))
        {
            path = Path.Combine(subDirectory, fileName);
            try
            {
                if (File.Exists(path))
                {
                    return AssemblyDefinition.ReadAssembly(path);
                }
            }
            catch
            {
                continue;
            }
        }

        return this.AssemblyResolve?.Invoke(sender, reference);
    }

    private void LoadAssemblyCache()
    {
        if (!this.useAssemblyCache)
            return;

        string cacheFilePath = this.modManager.Paths.AssemblyCachePath;
        if (!Directory.Exists(Path.GetDirectoryName(cacheFilePath)))
        {
            this.assemblyCache = null;
            return;
        }

        Dictionary<string, CachedAssembly> result = new();

        try
        {
            if (!File.Exists(cacheFilePath))
            {
                this.assemblyCache = null;
                return;
            }

            using BinaryReader binaryReader = new BinaryReader(File.OpenRead(cacheFilePath));

            int entryCount = binaryReader.ReadInt32();
            while (entryCount > 0)
            {
                CachedAssembly asm = new();
                asm.Load(binaryReader);

                result[asm.Identifier] = asm;
                entryCount--;
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Warning, $"Failed to load assembly cache; skipping loading cache. Reason: {e.Message}");
        }

        this.assemblyCache = result;
    }

    private void SaveAssemblyCache()
    {
        if (!this.useAssemblyCache)
            return;

        string cacheFilePath = this.modManager.Paths.AssemblyCachePath;
        try
        {
            string cacheFileDirectory = Path.GetDirectoryName(cacheFilePath);
            if (!Directory.Exists(cacheFileDirectory))
                Directory.CreateDirectory(cacheFileDirectory);

            using BinaryWriter binaryWriter = new BinaryWriter(File.OpenWrite(cacheFilePath));
            if (this.assemblyCache is null)
            {
                binaryWriter.Write(0);
            }
            else
            {
                binaryWriter.Write(this.assemblyCache.Count);

                foreach (CachedAssembly asm in this.assemblyCache.Values)
                {
                    asm.Save(binaryWriter);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Warning, $"Failed to save assembly cache; skipping saving cache. Reason: {e.Message}");
        }

    }

    public void Initialize()
    {
        try
        {
            this.LoadAssemblyCache();

            List<ModImporter> modImporters = new();
            List<Mod> importedMods = new();
            HashSet<string> modGuids = new();

            foreach (string directory in Directory.GetDirectories(this.rootDirectory))
            {
                modImporters.Add(ModImporter.FromDirectory(
                    directory,
                    this.modManager,
                    modGuids,
                    this.serializerOptions,
                    this.readerParameters,
                    this.assemblyCache));
            }

            foreach (string zipFile in Directory.GetFiles(this.rootDirectory, "*.zip"))
            {
                modImporters.Add(ModImporter.FromZipFile(
                    zipFile,
                    this.modManager,
                    modGuids,
                    this.serializerOptions,
                    this.readerParameters,
                    this.assemblyCache));
            }

            foreach (ModImporter importer in modImporters)
            {
                if (importer.TryImport(out Mod? mod))
                {
                    importedMods.Add(mod);
                    modGuids.Add(mod.Guid);
                }
            }

            this.SaveAssemblyCache();

            Logger.Log(LogLevel.Info, $"Found {importedMods.Count} mod{(importedMods.Count == 1 ? string.Empty : "s")} to import");
            this.modManager.FullRegistry.AddMods(importedMods);

            HashSet<string> processedModGuids = new();
            foreach (Mod mod in importedMods)
            {
                mod.RefreshIncompatibilitiesAndDependencies(processedModGuids);
            }

            this.modManager.EnqueueModsToLoad(importedMods);
            this.Finished?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error occurred whilst initializing mods: {ex}");
        }

        /*
        foreach (ModInfo mod in modsToLoad)
        {
            if (!this.modManager.IsModEnabled(mod.Manifest.Guid))
            {
                continue;
            }

            if (!mod.AreRequiredDependenciesLoaded)
            {
                Logger.Log(LogLevel.Info, $"Skipping [{mod}] as one or more dependencies aren't loaded.");
            }

            Logger.Log(LogLevel.Info, $"Loading [{mod}]");

            try
            {
                mod.Load();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error occurred whilst loading mod: {ex}");
            }
        }*/
    }

    /*
    private void ModifyLoadOrder(List<ModInfo> mods, List<string> dependencyErrors)
    {
        // We use a sorted dictionary to ensure consistent load order
        SortedDictionary<string, IEnumerable<string>> dependencyDictionary = new(StringComparer.InvariantCultureIgnoreCase);

        Dictionary<string, ModInfo> modsByGuid = new();

        foreach (IGrouping<string, ModInfo> modInfoGroup in mods.GroupBy(mod => mod.Manifest.Guid))
        {
            if (this.registry.TryGetMod(modInfoGroup.Key, out ModInfo? loadedMod))
            {
                Logger.Log(LogLevel.Warning, $"Skipping [{modInfoGroup.Key}] because a mod with a similar GUID ([{loadedMod}]) has been already loaded.");
                continue;
            }

            ModInfo? loadedVersion = null;
            foreach (ModInfo modInfo in modInfoGroup.OrderByDescending(x => x.Manifest.Version))
            {
                if (loadedVersion != null)
                {
                    Logger.Log(LogLevel.Warning, $"Skipping [{modInfo}] because a newer version exists ({loadedVersion}).");
                }

                loadedVersion = modInfo;
                dependencyDictionary[modInfo.Manifest.Guid] = modInfo.Manifest.Dependencies.Select(x => x.Guid);
                modsByGuid[modInfo.Manifest.Guid] = modInfo;
            }
        }

        foreach (ModInfo modInfo in modsByGuid.Values.ToList())
        {
            List<ModInfo> incompatibleMods = new();

            foreach (ModIncompatibilityInfo incompatibility in modInfo.Manifest.Incompatibilities)
            {
                ModInfo? incompatibleMod;
                if (!modsByGuid.TryGetValue(incompatibility.Guid, out incompatibleMod) &&
                    !this.registry.TryGetMod(incompatibility.Guid, out incompatibleMod))
                {
                    continue;
                }

                if (incompatibility.Versions.Count == 0)
                {
                    incompatibleMods.Add(modInfo);
                }

                foreach (IModReferenceVersionRequirement incompatibilityRequirement in incompatibility.Versions)
                {
                    if (incompatibilityRequirement.MeetsRequirements(incompatibleMod.Manifest.Version))
                    {
                        incompatibleMods.Add(modInfo);
                        break;
                    }
                }
            }

            if (incompatibleMods.Count > 0)
            {
                string dependencyErrorMessage = $"Could not load [{modInfo}] because it is incompatible with: {string.Join(", ", incompatibleMods.Select(x => x.Manifest.Guid + '@' + x.Manifest.Version))}";
                Logger.Log(LogLevel.Error, dependencyErrorMessage);
                dependencyErrors.Add(dependencyErrorMessage);
                modsByGuid.Remove(modInfo.Manifest.Guid);
                dependencyDictionary.Remove(modInfo.Manifest.Guid);
            }

            // todo: we might want add support for limiting game versions in the future.

        }

        // We don't add already loaded mods to the dependency graph as they are already loaded

        string[] emptyDependencies = Array.Empty<string>();

        // Sort mods by their dependencies.
        // Give missing dependencies no dependencies of its own, which will cause missing mods to be first in the resulting list.

        IEnumerable<string> sortedModGuids = Utility.TopologicalSort(
            dependencyDictionary.Keys,
            x =>
            {
                if (dependencyDictionary.TryGetValue(x, out IEnumerable<string>? dependencies))
                {
                    return dependencies;
                }
                else
                {
                    return emptyDependencies;
                }
            });

        mods.Clear();
        foreach (string sortedModGuid in sortedModGuids)
        {
            if (modsByGuid.TryGetValue(sortedModGuid, out ModInfo? modInfo))
            {
                mods.Add(modInfo);
            }
        }
        mods.TrimExcess();
    }

    private List<ModInfo> InitializeMods(List<ModInfo> mods)
    {
        List<string> dependencyErrors = new();
        this.ModifyLoadOrder(mods, dependencyErrors);

        HashSet<string> invalidMods = new();
        Dictionary<string, Version> processedMods = new();
        Dictionary<string, Assembly> loadedAssemblies = new();
        List<ModInfo> initializedMods = new();

        foreach (ModInfo mod in mods)
        {
            bool dependsOnInvalidMod = false;
            List<ModDependencyInfo> missingDependencies = new();

            foreach (ModDependencyInfo dependency in mod.Manifest.Dependencies)
            {
                // If the dependency wasn't already processed, it's missing altogether
                bool dependencyExists = processedMods.TryGetValue(dependency.Guid, out Version? modVersion);

                // Alternatively, if the dependency hasn't been loaded before, it's missing too
                if (!dependencyExists)
                {
                    dependencyExists = this.registry.TryGetMod(dependency.Guid, out ModInfo? modInfo);
                    modVersion = modInfo?.Manifest.Version;
                }

                if (dependencyExists && dependency.Versions.Count > 0)
                {
                    dependencyExists = false;
                    foreach (IModReferenceVersionRequirement versionRequirement in dependency.Versions)
                    {
                        if (versionRequirement.MeetsRequirements(modVersion))
                        {
                            dependencyExists = true;
                            break;
                        }
                    }
                }

                if (!dependencyExists)
                {
                    // If the dependency is hard, collect it into a list to show
                    if (dependency.Required)
                        missingDependencies.Add(dependency);

                    continue;
                }

                // If the dependency is a hard and is invalid (e.g. has missing dependencies), report that to the user
                if (invalidMods.Contains(dependency.Guid) && dependency.Required)
                {
                    dependsOnInvalidMod = true;
                    break;
                }
            }

            processedMods.Add(mod.Manifest.Guid, mod.Manifest.Version);

            if (dependsOnInvalidMod)
            {
                string message = $"Skipping [{mod}] because it has a dependency that was not loaded. See previous errors for details.";
                dependencyErrors.Add(message);
                Logger.Log(LogLevel.Error, message);
                continue;
            }

            if (missingDependencies.Count != 0)
            {
                string missingDependenciesString = string.Join(", ", missingDependencies.Select(x =>
                {
                    if (x.Versions.Count == 0)
                        return x.Guid;
                    else
                        return $"{x.Guid} ({string.Join(", ", x.Versions)})";
                }));

                string message = $"Could not load [{mod}] because it has missing dependencies: {missingDependenciesString}";
                dependencyErrors.Add(message);
                Logger.Log(LogLevel.Error, message);

                invalidMods.Add(mod.Manifest.Guid);
            }

            try
            {
                Logger.Log(LogLevel.Info, $"Initializing [{mod}]");

                foreach (ModRootPluginInfo plugin in mod.Plugins)
                {
                    plugin.Initialize(loadedAssemblies);
                }

                this.registry.AddMod(mod);
                initializedMods.Add(mod);

                this.ModInitialized?.Invoke(mod);
            }
            catch (Exception ex)
            {
                invalidMods.Add(mod.Manifest.Guid);
                this.registry.RemoveMod(mod.Manifest.Guid);

                Logger.Log(LogLevel.Error, $"Error loading [{mod}]: {(ex is ReflectionTypeLoadException re ? Utility.TypeLoadExceptionToString(re) : ex.ToString())}");
            }
        }

        return initializedMods;
    }
    */
}
