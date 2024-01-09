using MipInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace MipInEx.Bootstrap;

internal abstract class ModImporter : IDisposable
{
    private readonly ModManagerBase modManager;
    private readonly ICollection<string> modGuidsInBatch;
    private readonly ReaderParameters assemblyReaderParameters;
    private readonly Dictionary<string, CachedAssembly>? assemblyCache;
    private readonly JsonSerializerOptions manifestDeserializerOptions;

    protected ModImporter(
        ModManagerBase modManager,
        ICollection<string> modGuidsInBatch,
        JsonSerializerOptions manifestDeserializerOptions,
        ReaderParameters assemblyReaderParameters,
        Dictionary<string, CachedAssembly>? assemblyCache)
    {
        this.modManager = modManager;
        this.modGuidsInBatch = modGuidsInBatch;
        this.manifestDeserializerOptions = manifestDeserializerOptions;
        this.assemblyReaderParameters = assemblyReaderParameters;
        this.assemblyCache = assemblyCache;
    }

    public abstract bool TryImport([NotNullWhen(true)] out Mod? mod);

    protected abstract bool TryImportModManifest([NotNullWhen(true)] out ModManifest? manifest);

    protected abstract bool TryImportREADME([NotNullWhen(true)] out string? readme);
    protected abstract bool TryImportIcon([NotNullWhen(true)] out byte[]? iconData);
    protected abstract bool TryImportCHANGELOG([NotNullWhen(true)] out string? changelog);

    protected abstract IEnumerable<ModAssemblyLoader> GetAssemblyLoaders();
    protected abstract IEnumerable<ModAssetBundleLoader> GetAssetBundleLoaders();

    protected virtual ImmutableArray<AssemblyInfo> ImportAssemblies(ModManifest manifest)
    {
        Dictionary<string, ModAsmLoadInfo> modAssemblies = new();
        Dictionary<string, AssemblyInfo> pluginAssemblies = new();

        foreach (ModAssemblyLoader assemblyLoader in this.GetAssemblyLoaders())
        {
            string fullAssetPath = assemblyLoader.AssetPath;
            string modAssetPath = manifest.Guid + "/" + fullAssetPath;
            string assetPath = Utility.ShortenAssetPathWithoutExtension(
                fullAssetPath,
                "Assemblies",
                ".dll",
                StringComparison.OrdinalIgnoreCase);

            if (!manifest.TryGetAssemblyAsset(assetPath, out ModAssemblyManifest? assemblyManifest))
            {
                assemblyManifest = ModAssemblyManifest.CreateDefault(assetPath);
            }

            try
            {
                using Stream dllStream = assemblyLoader.OpenRead();

                string hash = Utility.HashStream(dllStream);
                dllStream.Position = 0;


                if (this.assemblyCache != null && this.assemblyCache.TryGetValue(modAssetPath, out CachedAssembly? cachedAssembly))
                {
                    if (cachedAssembly.Hash == hash)
                    {
                        bool isPluginAssembly = cachedAssembly.MainPlugin is not null || cachedAssembly.InternalPlugins.Count > 0;

                        modAssemblies[cachedAssembly.Name] = new ModAsmLoadInfo(
                            assemblyLoader,
                            cachedAssembly.Name,
                            isPluginAssembly,
                            cachedAssembly.AssemblyReferences);

                        if (isPluginAssembly)
                        {
                            pluginAssemblies[modAssetPath] = new AssemblyInfo(
                                assemblyLoader,
                                assemblyManifest,
                                cachedAssembly.MainPlugin!,
                                cachedAssembly.InternalPlugins);
                        }
                        continue;
                    }
                }

                using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(dllStream, this.assemblyReaderParameters);

                if (!Utility.IncludeAssembly(assembly))
                {
                    continue;
                }

                cachedAssembly = new()
                {
                    Identifier = modAssetPath,
                    Hash = hash,
                    Name = assembly.Name.Name
                };
                cachedAssembly.AssemblyReferences.AddRange(assembly.MainModule.AssemblyReferences.Select(x => x.Name));

                Logger.Log(LogLevel.Debug, $"Mod '{manifest.Name}': Examining '{modAssetPath}'");

                foreach (TypeDefinition typeDefinition in assembly.MainModule.Types)
                {
                    if (typeDefinition.IsInterface || typeDefinition.IsAbstract)
                        continue;

                    TypeDefinitionReference? resultReference = null;
                    bool isInternalPlugin = false;

                    TypeDefinition? baseTypeDefinition = typeDefinition.BaseType?.Resolve();
                    while (baseTypeDefinition != null)
                    {
                        if (baseTypeDefinition.FullName == "System.Object")
                            break;

                        if (baseTypeDefinition.Namespace == Utility.pluginTypeNamespace)
                        {
                            if (baseTypeDefinition.Name == Utility.pluginTypeName)
                            {
                                if (!baseTypeDefinition.IsGenericInstance)
                                {
                                    resultReference = TypeDefinitionReference.Create(typeDefinition);
                                    isInternalPlugin = false;
                                    break;
                                }
                            }
                            else if (baseTypeDefinition.Name == Utility.internalPluginTypeName)
                            {
                                int genericParameterCount = baseTypeDefinition.GenericParameters.Count;

                                if (genericParameterCount == 0 || genericParameterCount == 1)
                                {
                                    resultReference = TypeDefinitionReference.Create(typeDefinition);
                                    isInternalPlugin = true;
                                    break;
                                }
                            }
                        }

                        baseTypeDefinition = baseTypeDefinition.BaseType?.Resolve();
                    }

                    if (resultReference is null)
                        continue;

                    if (isInternalPlugin)
                    {
                        CustomAttribute? attribute = Utility.GetInternalPluginInfoAttribute(typeDefinition);
                        if (attribute is null)
                            continue;

                        string? guid = (string?)attribute.ConstructorArguments[0].Value;
                        string? name = (string?)attribute.ConstructorArguments[1].Value;
                        string? versionString = (string?)attribute.ConstructorArguments[2].Value;

                        name = name?.Trim();

                        if (!ModPropertyUtil.TryValidateGuid(guid) ||
                            !ModPropertyUtil.TryValidateName(name) ||
                            !Version.TryParse(versionString, out Version? version))
                        {
                            continue;
                        }

                        cachedAssembly.InternalPlugins.Add(new PluginReference(resultReference, name!, guid!, version));
                    }
                    else
                    {
                        CustomAttribute? attribute = Utility.GetPluginInfoAttribute(typeDefinition);
                        if (attribute is null)
                            continue;

                        string guid;
                        string name;
                        Version version;

                        if (attribute.ConstructorArguments.Count == 0)
                        {
                            guid = manifest.Guid;
                            name = manifest.Name;
                            version = manifest.Version;
                        }
                        else
                        {
                            guid = (string?)attribute.ConstructorArguments[0].Value!;
                            name = (string?)attribute.ConstructorArguments[1].Value!;
                            string? versionString = (string?)attribute.ConstructorArguments[2].Value;

                            name = name?.Trim()!;

                            if (!ModPropertyUtil.TryValidateGuid(guid) ||
                                !ModPropertyUtil.TryValidateName(name) ||
                                !Version.TryParse(versionString, out version))
                            {
                                continue;
                            }
                        }

                        cachedAssembly.MainPlugin = new PluginReference(resultReference, name, guid, version);
                    }
                }

                pluginAssemblies[modAssetPath] = new AssemblyInfo(
                    assemblyLoader,
                    assemblyManifest,
                    cachedAssembly.MainPlugin,
                    cachedAssembly.InternalPlugins);

                modAssemblies[cachedAssembly.Name] = new ModAsmLoadInfo(
                    assemblyLoader,
                    cachedAssembly.Name,
                    cachedAssembly.MainPlugin is not null || cachedAssembly.InternalPlugins.Count > 0,
                    cachedAssembly.AssemblyReferences);

                if (this.assemblyCache != null)
                {
                    this.assemblyCache[modAssetPath] = cachedAssembly;
                }
            }
            catch (BadImageFormatException e)
            {
                Logger.Log(LogLevel.Debug, $"Mod '{manifest.Name}': Skipping loading {modAssetPath} because it's not a valid .NET assembly. Full error: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, $"Mod '{manifest.Name}': {e}");
            }
        }

        foreach (KeyValuePair<string, ModAsmLoadInfo> modAssemblyEntry in modAssemblies)
        {
            Stack<(ModAssemblyLoader? loader, Queue<string> queuedDependencies)> assemblyStack = new();
            ModAsmLoadInfo modAssembly = modAssemblyEntry.Value;

            Queue<string> queuedDependencies = new(modAssembly.assemblyReferences);

            if (!modAssembly.isPluginAssembly)
            {
                assemblyStack.Push((null, queuedDependencies));
            }
            else
            {
                assemblyStack.Push((modAssembly.loader, queuedDependencies));
            }

            while (assemblyStack.TryPeek(out (ModAssemblyLoader? loader, Queue<string> queuedDependencies) frame))
            {
                if (frame.queuedDependencies.TryDequeue(out string? dependency))
                {
                    if (modAssemblies.TryGetValue(dependency, out ModAsmLoadInfo? loadInfo))
                    {
                        if (loadInfo.isPluginAssembly)
                        {
                            break;
                        }

                        assemblyStack.Push((loadInfo.loader, new(loadInfo.assemblyReferences)));
                    }
                    continue;
                }

                frame.loader?.Load();
                assemblyStack.Pop();
            }
        }

        ImmutableArray<AssemblyInfo>.Builder assemblyBuilder = ImmutableArray.CreateBuilder<AssemblyInfo>();

        foreach (KeyValuePair<string, AssemblyInfo> asmEntry in pluginAssemblies)
        {
            string asmPath = asmEntry.Key;
            AssemblyInfo asm = asmEntry.Value;

            if (asm.rootPluginReference is null)
            {
                if (asm.internalPluginReferences.Count > 0)
                {
                    Logger.Log(LogLevel.Warning, $"Mod '{manifest.Name}': Skipping loading {asmPath} because even though internal plugins are defined, no main plugin is!");
                }

                continue;
            }

            assemblyBuilder.Add(asm);
        }

        if (assemblyBuilder.Count > 0)
        {
            return assemblyBuilder.ToImmutable();
        }
        else
        {
            return ImmutableArray<AssemblyInfo>.Empty;
        }
    }

    protected virtual ImmutableArray<AssetBundleInfo> ImportAssetBundles(ModManifest manifest)
    {
        ImmutableArray<AssetBundleInfo>.Builder assetBundleBuilder = ImmutableArray.CreateBuilder<AssetBundleInfo>();

        foreach (ModAssetBundleLoader assetBundleLoader in this.GetAssetBundleLoaders())
        {
            string assetPath = Utility.ShortenAssetPathWithoutExtension(
                assetBundleLoader.LongAssetPath,
                "Asset Bundles",
                ".bundle",
                StringComparison.OrdinalIgnoreCase);

            if (!manifest.TryGetAssetBundleAsset(assetPath, out ModAssetBundleManifest? assetBundleManifest))
            {
                assetBundleManifest = ModAssetBundleManifest.CreateDefault(assetPath);
            }

            assetBundleBuilder.Add(
                new AssetBundleInfo(
                    assetBundleLoader, 
                    assetBundleManifest));
        }

        if (assetBundleBuilder.Count > 0)
        {
            return assetBundleBuilder.ToImmutable();
        }
        else
        {
            return ImmutableArray<AssetBundleInfo>.Empty;
        }
    }

    public virtual void Dispose()
    { }

    private sealed class ModAsmLoadInfo
    {
        public readonly ModAssemblyLoader loader;
        public readonly string name;
        public readonly bool isPluginAssembly;
        public readonly List<string> assemblyReferences;

        public ModAsmLoadInfo(
            ModAssemblyLoader loader,
            string name,
            bool isPluginAssembly,
            List<string> assemblyReferences)
        {
            this.loader = loader;
            this.name = name;
            this.isPluginAssembly = isPluginAssembly;
            this.assemblyReferences = assemblyReferences;
        }
    }

    protected internal sealed class AssemblyInfo
    {
        public readonly ModAssemblyLoader loader;
        public readonly ModAssemblyManifest manifest;
        public readonly PluginReference rootPluginReference;
        public readonly List<PluginReference> internalPluginReferences;

        public AssemblyInfo(
            ModAssemblyLoader loader,
            ModAssemblyManifest manifest,
            PluginReference rootPluginReference,
            List<PluginReference> internalPluginReferences)
        {
            this.loader = loader;
            this.manifest = manifest;
            this.rootPluginReference = rootPluginReference;
            this.internalPluginReferences = internalPluginReferences;
        }
    }

    protected internal sealed class AssetBundleInfo
    {
        public readonly ModAssetBundleLoader loader;
        public readonly ModAssetBundleManifest manifest;

        public AssetBundleInfo(
            ModAssetBundleLoader loader,
            ModAssetBundleManifest manifest)
        {
            this.loader = loader;
            this.manifest = manifest;
        }
    }

    public static DirectoryImporter FromDirectory(
        string rootDirectory,
        ModManagerBase modManager,
        ICollection<string> modGuidsInBatch,
        JsonSerializerOptions manifestDeserializerOptions,
        ReaderParameters assemblyReaderParameters,
        Dictionary<string, CachedAssembly>? assemblyCache)
    {
        return new DirectoryImporter(
            rootDirectory,
            modManager,
            modGuidsInBatch,
            manifestDeserializerOptions,
            assemblyReaderParameters,
            assemblyCache);
    }

    public static ZipImporter FromZipFile(
        string filePath,
        ModManagerBase modManager,
        ICollection<string> modGuidsInBatch,
        JsonSerializerOptions manifestDeserializerOptions,
        ReaderParameters assemblyReaderParameters,
        Dictionary<string, CachedAssembly>? assemblyCache)
    {
        ZipArchive zipArchive = new(File.OpenRead(filePath), ZipArchiveMode.Read);

        return new ZipImporter(
            zipArchive,
            Path.GetFileName(filePath),
            modManager,
            modGuidsInBatch,
            manifestDeserializerOptions,
            assemblyReaderParameters,
            assemblyCache);
    }

    public static BinaryImporter FromBinaryReader(
        BinaryReader binaryReader,
        bool keepReaderOpen,
        ModManagerBase modManager,
        ICollection<string> modGuidsInBatch,
        JsonSerializerOptions manifestDeserializerOptions,
        ReaderParameters assemblyReaderParameters,
        Dictionary<string, CachedAssembly>? assemblyCache)
    {
        return new BinaryImporter(
            binaryReader,
            keepReaderOpen,
            modManager,
            modGuidsInBatch,
            manifestDeserializerOptions,
            assemblyReaderParameters,
            assemblyCache);
    }

    public sealed class DirectoryImporter : ModImporter
    {
        private readonly string rootDirectory;

        private string? directoryName;
        private string? manifestFilePath;
        private string? readmeFilePath;
        private string? changelogFilePath;
        private string? iconFilePath;
        private string? assembliesDirectory;
        private string? assetBundlesDirectory;

        public DirectoryImporter(
            string rootDirectory,
            ModManagerBase modManager,
            ICollection<string> modGuidsInBatch,
            JsonSerializerOptions manifestDeserializerOptions,
            ReaderParameters assemblyReaderParameters,
            Dictionary<string, CachedAssembly>? assemblyCache)
            : base(modManager, modGuidsInBatch, manifestDeserializerOptions, assemblyReaderParameters, assemblyCache)
        {
            this.rootDirectory = rootDirectory;

            this.directoryName = null;

            this.manifestFilePath = null;
            this.readmeFilePath = null;
            this.changelogFilePath = null;
            this.iconFilePath = null;

            this.assembliesDirectory = null;
            this.assetBundlesDirectory = null;
        }

        private string DirectoryName
        {
            get
            {
                this.directoryName ??= Path.GetDirectoryName(this.rootDirectory);
                return this.directoryName;
            }
        }

        private string ManifestFilePath
        {
            get
            {
                this.manifestFilePath ??= Path.Combine(this.rootDirectory, "manifest.json");
                return this.manifestFilePath;
            }
        }

        private string ReadmeFilePath
        {
            get
            {
                this.readmeFilePath ??= Path.Combine(this.rootDirectory, "README.md");
                return this.readmeFilePath;
            }
        }

        private string ChangelogFilePath
        {
            get
            {
                this.changelogFilePath ??= Path.Combine(this.rootDirectory, "CHANGELOG.md");
                return this.changelogFilePath;
            }
        }

        private string IconFilePath
        {
            get
            {
                this.iconFilePath ??= Path.Combine(this.rootDirectory, "icon.png");
                return this.iconFilePath;
            }
        }

        private string AssembliesDirectory
        {
            get
            {
                this.assembliesDirectory ??= Path.Combine(this.rootDirectory,  "Assemblies");
                return this.assembliesDirectory;
            }
        }

        private string AssetBundlesDirectory
        {
            get
            {
                this.assetBundlesDirectory ??= Path.Combine(this.rootDirectory, "Asset Bundles");
                return this.assetBundlesDirectory;
            }
        }

        public sealed override bool TryImport([NotNullWhen(true)] out Mod? mod)
        {
            if (!this.TryImportModManifest(out ModManifest? manifest))
            {
                mod = null;
                return false;
            }

            this.TryImportREADME(out string? readme);
            this.TryImportIcon(out byte[]? iconData);
            this.TryImportCHANGELOG(out string? changelog);

            ImmutableArray<AssetBundleInfo> assetBundles = this.ImportAssetBundles(manifest);
            ImmutableArray<AssemblyInfo> assemblies = this.ImportAssemblies(manifest);

            mod = new Mod(
                this.modManager,
                manifest,
                readme,
                changelog,
                iconData,
                assetBundles,
                assemblies);
            return true;
        }

        protected sealed override bool TryImportModManifest([NotNullWhen(true)] out ModManifest? manifest)
        {
            manifest = null;

            if (!File.Exists(this.ManifestFilePath))
            {
                Logger.Log(LogLevel.Debug, $"Skipping directory '{this.DirectoryName}' as there isn't a manifest.json file.");
                return false;
            }

            // manifestPath will be populated

            string manifestContent;
            try
            {
                manifestContent = File.ReadAllText(this.manifestFilePath!);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Failed to read manifest.json for mod in directory '{this.DirectoryName}': {ex}");
                return false;
            }

            try
            {
                manifest = JsonSerializer.Deserialize<ModManifest>(manifestContent, this.manifestDeserializerOptions);
            }
            catch (JsonException ex)
            {
                Logger.Log(LogLevel.Error, $"Failed to load manifest.json for mod in directory '{this.DirectoryName}': {ex}");
                return false;
            }

            if (manifest is null)
            {
                Logger.Log(LogLevel.Error, $"Read manifest.json is null for mod in directory '{this.DirectoryName}'. This should not happen!");
                return false;
            }


            if (this.modManager.FullRegistry.ContainsMod(manifest.Guid) ||
                this.modGuidsInBatch.Contains(manifest.Guid))
            {
                Logger.Log(LogLevel.Error, $"A mod with guid '{manifest.Guid}' already exists!");
                return false;
            }

            return true;
        }

        protected sealed override bool TryImportREADME([NotNullWhen(true)] out string? readme)
        {
            readme = null;

            if (!File.Exists(this.ReadmeFilePath))
            {
                Logger.Log(LogLevel.Warning, $"Mod in directory '{this.DirectoryName}' doesn't include a README.md! Please include one so users of your mod understand what your mod does and how to use it!");
                return false;
            }

            // readmeFilePath is populated.
            try
            {
                readme = File.ReadAllText(this.readmeFilePath!);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Failed to read README.md for mod in directory '{this.DirectoryName}': {ex}");
            }

            return false;
        }

        protected sealed override bool TryImportCHANGELOG([NotNullWhen(true)] out string? changelog)
        {
            changelog = null;
            if (!File.Exists(this.ChangelogFilePath))
            {
                Logger.Log(LogLevel.Debug, $"Mod in directory '{this.DirectoryName}' doesn't include a CHANGELOG.md");
                return false;
            }

            // changelogFilePath is populated.
            try
            {
                changelog = File.ReadAllText(this.changelogFilePath!);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Failed to read CHANGELOG.md for mod in directory '{this.DirectoryName}': {ex}");
            }

            return false;
        }

        protected sealed override bool TryImportIcon([NotNullWhen(true)] out byte[]? iconData)
        {
            iconData = null;
            if (!File.Exists(this.IconFilePath))
            {
                Logger.Log(LogLevel.Warning, $"Mod in directory '{this.DirectoryName}' doesn't include an icon.png! Please include one to distinguish your mod!");
                return false;
            }

            // iconFilePath is populated.
            try
            {
                iconData = File.ReadAllBytes(this.iconFilePath!);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Failed to read icon.png for mod in directory '{this.DirectoryName}': {ex}");
                return false;
            }

            return true;
        }

        protected sealed override IEnumerable<ModAssemblyLoader> GetAssemblyLoaders()
        {
            // assembliesDirectory will be populated
            foreach (string dllPath in Directory.GetFiles(this.assembliesDirectory, "*.dll", SearchOption.AllDirectories))
            {
                string assetPath = Utility.ShortenAssetPath(
                    Path.GetRelativePath(this.rootDirectory, dllPath).Replace('\\', '/'),
                    "Assemblies",
                    StringComparison.OrdinalIgnoreCase);

                yield return ModAssemblyLoader.FromFile(dllPath, assetPath);
            }
        }

        protected sealed override IEnumerable<ModAssetBundleLoader> GetAssetBundleLoaders()
        {
            // assetBundlesDirectory will be populated
            foreach (string bundlePath in Directory.GetFiles(this.assetBundlesDirectory, "*.bundle", SearchOption.AllDirectories))
            {
                string assetPath = Utility.ShortenAssetPath(
                    Path.GetRelativePath(this.rootDirectory, bundlePath).Replace('\\', '/'),
                    "Asset Bundles",
                    StringComparison.OrdinalIgnoreCase);

                yield return ModAssetBundleLoader.FromFile(bundlePath, assetPath);
            }
        }

        protected sealed override ImmutableArray<AssemblyInfo> ImportAssemblies(ModManifest manifest)
        {
            if (!Directory.Exists(this.AssembliesDirectory))
            {
                return ImmutableArray<AssemblyInfo>.Empty;
            }

            return base.ImportAssemblies(manifest);
        }

        protected sealed override ImmutableArray<AssetBundleInfo> ImportAssetBundles(ModManifest manifest)
        {
            if (!Directory.Exists(this.AssetBundlesDirectory))
            {
                return ImmutableArray<AssetBundleInfo>.Empty;
            }

            return base.ImportAssetBundles(manifest);
        }
    }

    public sealed class ZipImporter : ModImporter
    {
        private readonly ZipArchive zipArchive;
        private readonly string fileName;

        private readonly ZipArchiveEntry? manifestEntry;
        private readonly ZipArchiveEntry? readmeEntry;
        private readonly ZipArchiveEntry? iconEntry;
        private readonly ZipArchiveEntry? changelogEntry;

        private readonly ImmutableArray<ZipArchiveEntry> assemblyEntries;
        private readonly ImmutableArray<ZipArchiveEntry> assetBundleEntries;

        public ZipImporter(
            ZipArchive zipArchive,
            string fileName,
            ModManagerBase modManager,
            ICollection<string> modGuidsInBatch,
            JsonSerializerOptions manifestDeserializerOptions,
            ReaderParameters assemblyReaderParameters,
            Dictionary<string, CachedAssembly>? assemblyCache)
            : base(modManager, modGuidsInBatch, manifestDeserializerOptions, assemblyReaderParameters, assemblyCache)
        {
            this.zipArchive = zipArchive;
            this.fileName = fileName;

            this.manifestEntry = null;
            this.readmeEntry = null;
            this.iconEntry = null;
            this.changelogEntry = null;

            ImmutableArray<ZipArchiveEntry>.Builder assembliesBuilder = ImmutableArray.CreateBuilder<ZipArchiveEntry>();
            ImmutableArray<ZipArchiveEntry>.Builder assetBundlesBuilder = ImmutableArray.CreateBuilder<ZipArchiveEntry>();

            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                string fullName = entry.FullName.Replace('\\', '/');
                if (fullName[0] == '/')
                {
                    fullName = fullName.Substring(1);
                }

                if (fullName == "manifest.json")
                {
                    this.manifestEntry = entry;
                    continue;
                }

                if (fullName == "README.md")
                {
                    this.readmeEntry = entry;
                    continue;
                }

                if (fullName == "icon.png")
                {
                    this.iconEntry = entry;
                    continue;
                }

                if (fullName == "CHANGELOG.md")
                {
                    this.changelogEntry = entry;
                    continue;
                }

                if (fullName.StartsWith("Assemblies/") && fullName.EndsWith(".dll"))
                {
                    assembliesBuilder.Add(entry);
                    continue;
                }

                if (fullName.StartsWith("Asset Bundles/") && fullName.EndsWith(".bundle"))
                {
                    assetBundlesBuilder.Add(entry);
                    continue;
                }
            }

            this.assemblyEntries = assembliesBuilder.ToImmutableArray();
            this.assetBundleEntries = assetBundlesBuilder.ToImmutableArray();
        }

        public sealed override bool TryImport([NotNullWhen(true)] out Mod? mod)
        {
            if (!this.TryImportModManifest(out ModManifest? manifest))
            {
                mod = null;
                return false;
            }

            this.TryImportREADME(out string? readme);
            this.TryImportIcon(out byte[]? iconData);
            this.TryImportCHANGELOG(out string? changelog);

            ImmutableArray<AssetBundleInfo> assetBundles = this.ImportAssetBundles(manifest);
            ImmutableArray<AssemblyInfo> assemblies = this.ImportAssemblies(manifest);

            mod = new Mod(
                this.modManager,
                manifest,
                readme,
                changelog,
                iconData,
                assetBundles,
                assemblies);
            return true;
        }

        protected sealed override bool TryImportModManifest([NotNullWhen(true)] out ModManifest? manifest)
        {
            manifest = null;

            if (this.manifestEntry == null)
            {
                Logger.Log(LogLevel.Debug, $"Skipping zip '{this.fileName}' as there isn't a manifest.json file.");
                return false;
            }

            string manifestText;
            try
            {
                using Stream readStream = this.manifestEntry.Open();
                using StreamReader reader = new StreamReader(readStream);

                manifestText = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Failed to read manifest.json for mod in zip '{this.fileName}': {ex}");
                return false;
            }

            try
            {
                manifest = JsonSerializer.Deserialize<ModManifest>(manifestText, this.manifestDeserializerOptions);
            }
            catch (JsonException ex)
            {
                Logger.Log(LogLevel.Error, $"Failed to load manifest.json for mod in zip '{this.fileName}': {ex}");
                return false;
            }

            if (manifest is null)
            {
                Logger.Log(LogLevel.Error, $"Read manifest.json is null for mod in zip '{this.fileName}'. This should not happen!");
                return false;
            }


            if (this.modManager.FullRegistry.ContainsMod(manifest.Guid) ||
                this.modGuidsInBatch.Contains(manifest.Guid))
            {
                Logger.Log(LogLevel.Error, $"A mod with guid '{manifest.Guid}' already exists!");
                return false;
            }

            return true;
        }

        protected sealed override bool TryImportREADME([NotNullWhen(true)] out string? readme)
        {
            readme = null;

            if (this.readmeEntry == null)
            {
                Logger.Log(LogLevel.Warning, $"Mod in zip '{this.fileName}' doesn't include a README.md! Please include one so users of your mod understand what your mod does and how to use it!");
                return false;
            }

            try
            {
                using Stream readStream = this.readmeEntry.Open();
                using StreamReader reader = new StreamReader(readStream);

                readme = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Failed to read README.md for mod in zip '{this.fileName}': {ex}");
                return false;
            }

            return true;
        }

        protected sealed override bool TryImportCHANGELOG([NotNullWhen(true)] out string? changelog)
        {
            changelog = null;

            if (this.changelogEntry == null)
            {
                Logger.Log(LogLevel.Debug, $"Mod in zip '{this.fileName}' doesn't include a CHANGELOG.md");
                return false;
            }

            try
            {
                using Stream readStream = this.changelogEntry.Open();
                using StreamReader reader = new StreamReader(readStream);

                changelog = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Failed to read CHANGELOG.md for mod in zip '{this.fileName}': {ex}");
                return false;
            }

            return true;
        }

        protected sealed override bool TryImportIcon([NotNullWhen(true)] out byte[]? iconData)
        {
            iconData = null;

            if (this.iconEntry == null)
            {
                Logger.Log(LogLevel.Warning, $"Mod in zip '{this.fileName}' doesn't include an icon.png! Please include one to distinguish your mod!");
                iconData = null;
                return false;
            }

            try
            {
                using Stream readStream = this.iconEntry.Open();
                using BinaryReader streamReader = new BinaryReader(readStream);

                iconData = streamReader.ReadBytes((int)(readStream.Length - readStream.Position));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Failed to read icon.png for mod in zip '{this.fileName}': {ex}");
                return false;
            }

            return true;
        }

        protected sealed override IEnumerable<ModAssemblyLoader> GetAssemblyLoaders()
        {
            foreach (ZipArchiveEntry assemblyZipEntry in this.assemblyEntries)
            {
                yield return ModAssemblyLoader.FromZipArchive(assemblyZipEntry);
            }
        }

        protected sealed override IEnumerable<ModAssetBundleLoader> GetAssetBundleLoaders()
        {
            foreach (ZipArchiveEntry assetBundleZipEntry in this.assetBundleEntries)
            {
                yield return ModAssetBundleLoader.FromZipArchive(assetBundleZipEntry);
            }
        }

        protected sealed override ImmutableArray<AssemblyInfo> ImportAssemblies(ModManifest manifest)
        {
            if (this.assemblyEntries.Length == 0)
            {
                return ImmutableArray<AssemblyInfo>.Empty;
            }

            return base.ImportAssemblies(manifest);
        }

        protected sealed override ImmutableArray<AssetBundleInfo> ImportAssetBundles(ModManifest manifest)
        {
            if (this.assetBundleEntries.Length == 0)
            {
                return ImmutableArray<AssetBundleInfo>.Empty;
            }

            return base.ImportAssetBundles(manifest);
        }

        public sealed override void Dispose()
        {
            this.zipArchive.Dispose();
        }
    }

    public sealed class BinaryImporter : ModImporter
    {
        private readonly BinaryReader binaryReader;
        private readonly bool keepReaderOpen;

        public BinaryImporter(
            BinaryReader binaryReader,
            bool keepReaderOpen,
            ModManagerBase modManager,
            ICollection<string> modGuidsInBatch,
            JsonSerializerOptions manifestDeserializerOptions,
            ReaderParameters assemblyReaderParameters,
            Dictionary<string, CachedAssembly>? assemblyCache)
            : base(modManager, modGuidsInBatch, manifestDeserializerOptions, assemblyReaderParameters, assemblyCache)
        {
            this.binaryReader = binaryReader;
            this.keepReaderOpen = keepReaderOpen;
        }

        public sealed override bool TryImport([NotNullWhen(true)] out Mod? mod)
        {
            if (!this.TryImportModManifest(out ModManifest? manifest))
            {
                // we still attempt to load the content to properly
                // consume the binary reader.

                this.TryImportREADME(out _);
                this.TryImportIcon(out _);
                this.TryImportCHANGELOG(out _);

                // this will consume the reader, even though we dont use it.
                this.GetAssetBundleLoaders().ToArray();
                // this will consume the reader, even though we dont use it.
                this.GetAssemblyLoaders().ToArray();

                mod = null;
                return false;
            }

            this.TryImportREADME(out string? readme);
            this.TryImportIcon(out byte[]? iconData);
            this.TryImportCHANGELOG(out string? changelog);

            ImmutableArray<AssetBundleInfo> assetBundles = this.ImportAssetBundles(manifest);
            ImmutableArray<AssemblyInfo> assemblies = this.ImportAssemblies(manifest);

            mod = new Mod(
                this.modManager,
                manifest,
                readme,
                changelog,
                iconData,
                assetBundles,
                assemblies);
            return true;
        }

        protected sealed override bool TryImportModManifest([NotNullWhen(true)] out ModManifest? manifest)
        {
            manifest = null;

            string manifestContent = this.binaryReader.ReadString();

            try
            {
                manifest = JsonSerializer.Deserialize<ModManifest>(manifestContent, this.manifestDeserializerOptions);
            }
            catch (JsonException ex)
            {
                Logger.Log(LogLevel.Error, $"Failed to load manifest.json for mod: {ex}");
                return false;
            }

            if (manifest is null)
            {
                Logger.Log(LogLevel.Error, $"Read manifest.json is null. This should not happen!");
                return false;
            }


            if (this.modManager.FullRegistry.ContainsMod(manifest.Guid) ||
                this.modGuidsInBatch.Contains(manifest.Guid))
            {
                Logger.Log(LogLevel.Error, $"A mod with guid '{manifest.Guid}' already exists!");
                return false;
            }

            return true;
        }

        protected sealed override bool TryImportREADME([NotNullWhen(true)] out string? readme)
        {
            if (!this.binaryReader.ReadBoolean())
            {
                readme = null;
                return false;
            }

            readme = this.binaryReader.ReadString();
            return true;
        }

        protected sealed override bool TryImportIcon([NotNullWhen(true)] out byte[]? iconData)
        {
            if (!this.binaryReader.ReadBoolean())
            {
                iconData = null;
                return false;
            }

            int dataSize = this.binaryReader.ReadInt32();
            iconData = this.binaryReader.ReadBytes(dataSize);
            return true;
        }

        protected sealed override bool TryImportCHANGELOG([NotNullWhen(true)] out string? changelog)
        {
            if (!this.binaryReader.ReadBoolean())
            {
                changelog = null;
                return false;
            }

            changelog = this.binaryReader.ReadString();
            return true;
        }

        protected sealed override IEnumerable<ModAssemblyLoader> GetAssemblyLoaders()
        {
            int assemblyCount = this.binaryReader.ReadInt32();
            while (assemblyCount > 0)
            {
                yield return ModAssemblyLoader.FromBinaryReader(this.binaryReader);
                assemblyCount--;
            }
        }

        protected sealed override IEnumerable<ModAssetBundleLoader> GetAssetBundleLoaders()
        {
            int assetBundleCount = this.binaryReader.ReadInt32();
            while (assetBundleCount > 0)
            {
                yield return ModAssetBundleLoader.FromBinaryReader(this.binaryReader);
                assetBundleCount--;
            }
        }

        public sealed override void Dispose()
        {
            if (this.keepReaderOpen)
            {
                return;
            }

            this.binaryReader.Dispose();
        }
    }
}
