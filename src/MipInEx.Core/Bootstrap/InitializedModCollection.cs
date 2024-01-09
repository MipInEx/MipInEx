//using MipInEx.Logging;
//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Text;

//namespace MipInEx.Bootstrap;

//internal sealed class InitializedModCollection
//{
//    private readonly ImmutableArray<ModInfo> mods;

//    internal static List<ModDependencyInfo> GetMissingDependencies(ModInfo mod)
//    {
//        List<ModDependencyInfo> missingDependencies = new();

//        foreach (ModDependencyInfo dependency in mod.Manifest.Dependencies)
//        {
//            if (!dependency.Required)
//            {
//                continue;
//            }

//            if (!mod.Registry.TryGetMod(dependency.Guid, out ModInfo? dependencyMod) ||
//                !dependencyMod.IsLoaded)
//            {
//                missingDependencies.Add(dependency);
//            }
//        }

//        return missingDependencies;
//    }

//    internal static List<ModIncompatibilityInfo> GetIncompatibleMods(ModInfo mod)
//    {
//        List<ModIncompatibilityInfo> incompatibleMods = new();

//        foreach (ModIncompatibilityInfo incompatibility in mod.Manifest.Incompatibilities)
//        {
//            if (!mod.Registry.ContainsMod(incompatibility.Guid))
//            {
//                incompatibleMods.Add(incompatibility);
//            }
//        }

//        return incompatibleMods;
//    }

//    public void Load(Func<ModInfo, bool>? enabledFilter = null)
//    {
//        Logger.Log(LogLevel.Info, "Loading mods...");

//        Dictionary<string, Mod> allMods = new();

//        foreach (ModInfo modInfo in this.mods)
//        {
//            Mod mod = new(modInfo, enabledFilter == null || enabledFilter.Invoke(modInfo));
//            allMods.Add(modInfo.Manifest.Guid, mod);
//        }

//        foreach (Mod mod in allMods.Values)
//        {
//            mod.Load(allMods, new Stack<string>());
//        }
//    }

//    private sealed class ModIncompatibility
//    {
//        private readonly ModInfo mod;
//        private readonly ModIncompatibilityInfo incompatibilityInfo;

//        public ModIncompatibility(ModInfo mod, ModIncompatibilityInfo incompatibilityInfo)
//        {
//            this.mod = mod;
//            this.incompatibilityInfo = incompatibilityInfo;
//        }

//        public ModInfo Mod => this.mod;
//        public ModIncompatibilityInfo IncompatibilityInfo => this.incompatibilityInfo;

//        public sealed override string ToString()
//        {
//            return $"{this.incompatibilityInfo.Guid} ({this.incompatibilityInfo.GetVersionString()}) [Currently Installed {mod}]";
//        }
//    }

//    private abstract class MissingModDependency
//    {
//        private readonly ModDependencyInfo dependencyInfo;

//        protected MissingModDependency(ModDependencyInfo dependencyInfo)
//        {
//            this.dependencyInfo = dependencyInfo;
//        }

//        public ModDependencyInfo DependencyInfo => this.dependencyInfo;
//        public abstract string InfoString { get; }

//        public sealed override string ToString()
//        {
//            return $"Missing Dependency '{this.DependencyInfo.Guid}': {this.InfoString}";
//        }

//        public static NotFound CreateNotFound(ModDependencyInfo dependencyInfo)
//            => new(dependencyInfo);

//        public static VersionNotMatching CreateVersionNotMatching(ModInfo mod, ModDependencyInfo dependencyInfo)
//            => new(mod, dependencyInfo);

//        public static Disabled CreateDisabled(ModInfo mod, ModDependencyInfo dependencyInfo)
//            => new(mod, dependencyInfo);

//        public static LoadError CreateLoadError(ModInfo mod, ModDependencyInfo dependencyInfo)
//            => new(mod, dependencyInfo);

//        public sealed class NotFound : MissingModDependency
//        {
//            private string? infoString;

//            public NotFound(ModDependencyInfo dependencyInfo)
//                : base(dependencyInfo)
//            { }

//            public sealed override string InfoString
//            {
//                get
//                {
//                    this.infoString ??= $"Could not find mod with guid '{this.dependencyInfo.Guid}'";
//                    return this.infoString;
//                }
//            }
//        }

//        public sealed class VersionNotMatching : MissingModDependency
//        {
//            private readonly ModInfo mod;
//            private string? infoString;

//            public VersionNotMatching(ModInfo mod, ModDependencyInfo dependencyInfo)
//                : base(dependencyInfo)
//            {
//                this.mod = mod;
//            }

//            public Version Version => this.mod.Manifest.Version;

//            public sealed override string InfoString
//            {
//                get
//                {
//                    this.infoString ??= $"Version {this.Version} does not match any of the required versions ({this.dependencyInfo.GetVersionString()})";
//                    return this.infoString;
//                }
//            }
//        }

//        public sealed class Disabled : MissingModDependency
//        {
//            private readonly ModInfo mod;

//            public Disabled(ModInfo mod, ModDependencyInfo dependencyInfo)
//                : base(dependencyInfo)
//            {
//                this.mod = mod;
//            }

//            public ModInfo Mod => this.mod;

//            public sealed override string InfoString => Disabled.InfoStringInstance;
//            private static readonly string InfoStringInstance = "The dependency is disabled";
//        }

//        public sealed class LoadError : MissingModDependency
//        {
//            private readonly ModInfo mod;

//            public LoadError(ModInfo mod, ModDependencyInfo dependencyInfo)
//                : base(dependencyInfo)
//            {
//                this.mod = mod;
//            }

//            public ModInfo Mod => this.mod;

//            public sealed override string InfoString => LoadError.InfoStringInstance;
//            private static readonly string InfoStringInstance = "One or more errors occured whilst loading the dependency.";
//        }
//    }


//    private static class ModStatuses
//    {
//        public static readonly ModStatus.LoadDisabled LoadDisabled = default;
//        public static readonly ModStatus.Loaded Loaded = default;
//        public static readonly ModStatus.Loaded NotLoaded = default;
//        public static readonly ModStatus.Error Error = default;

//        public static ModStatus.CircularDependencies CreateCircularDependencies(IReadOnlyList<ModInfo> dependencyStack)
//        {
//            return new ModStatus.CircularDependencies(dependencyStack);
//        }

//        public static ModStatus.HasIncompatibilities CreateHasIncompatibilities(IReadOnlyList<ModIncompatibility> incompatibilities)
//        {
//            return new ModStatus.HasIncompatibilities(incompatibilities);
//        }

//        public static ModStatus.MissingDependencies CreateMissingDependencies(IReadOnlyList<MissingModDependency> dependencies)
//        {
//            return new ModStatus.MissingDependencies(dependencies);
//        }
//    }

//    private sealed class Mod
//    {
//        public readonly ModInfo info;
//        public IModStatus status;

//        public Mod(ModInfo modInfo, bool enabled)
//        {
//            this.info = modInfo;
//            this.status = enabled ? ModStatuses.NotLoaded : ModStatuses.LoadDisabled;
//        }

//        public bool Load(Dictionary<string, Mod> allMods, Stack<string> dependencyStack)
//        {
//            if (!this.status.CanLoad)
//            {
//                return false;
//            }
//            else if (this.status.IsLoaded)
//            {
//                return true;
//            }

//            if (this.info.IsLoaded)
//            {
//                this.status = ModStatuses.Loaded;
//                return true;
//            }

//            string modGuid = this.info.Manifest.Guid;

//            // handle circular dependencies
//            if (dependencyStack.Contains(modGuid))
//            {
//                ImmutableArray<ModInfo>.Builder dependencyStackBuilder = ImmutableArray.CreateBuilder<ModInfo>(dependencyStack.Count + 1);
//                dependencyStackBuilder.Add(this.info);

//                foreach (string dependencyGuid in dependencyStack)
//                {
//                    if (!allMods.TryGetValue(dependencyGuid, out Mod? dependencyModInfo))
//                    {
//                        continue;
//                    }

//                    dependencyStackBuilder.Add(dependencyModInfo.info);
//                }

//                this.status = ModStatuses.CreateCircularDependencies(dependencyStackBuilder.ToImmutable());
//                goto HANDLE_ERROR;
//            }

//            dependencyStack.Push(modGuid);

//            // check for missing dependencies and load dependencies
//            List<MissingModDependency> missingDependencies = new();

//            foreach (ModDependencyInfo dependencyInfo in this.info.Manifest.Dependencies)
//            {
//                if (!allMods.TryGetValue(dependencyInfo.Guid, out Mod? dependency))
//                {
//                    if (dependencyInfo.Required)
//                    {
//                        missingDependencies.Add(MissingModDependency.CreateNotFound(dependencyInfo));
//                    }

//                    continue;
//                }

//                if (dependencyInfo.Required && !dependencyInfo.IncludesVersion(dependency.info.Manifest.Version))
//                {
//                    missingDependencies.Add(MissingModDependency.CreateVersionNotMatching(dependency.info, dependencyInfo));
//                    continue;
//                }

//                if (dependency.Load(allMods, dependencyStack) || !dependencyInfo.Required)
//                {
//                    continue;
//                }

//                IModStatus dependencyStatus = dependency.status;
//                if (dependencyStatus is ModStatus.LoadDisabled)
//                {
//                    missingDependencies.Add(MissingModDependency.CreateDisabled(dependency.info, dependencyInfo));
//                }
//                else
//                {
//                    missingDependencies.Add(MissingModDependency.CreateLoadError(this.info, dependencyInfo));
//                }
//            }

//            dependencyStack.Pop();

//            if (missingDependencies.Count > 0)
//            {
//                this.status = ModStatuses.CreateMissingDependencies(missingDependencies.ToImmutableArray());
//                goto HANDLE_ERROR;
//            }

//            // check for incompatibilities.
//            List<ModIncompatibility> incompatibleMods = new();

//            foreach (ModIncompatibilityInfo incompatibilityInfo in this.info.Manifest.Incompatibilities)
//            {
//                if (!allMods.TryGetValue(incompatibilityInfo.Guid, out Mod? incompatibileMod))
//                {
//                    continue;
//                }

//                if (!incompatibilityInfo.IncludesVersion(incompatibileMod.info.Manifest.Version))
//                {
//                    continue;
//                }

//                IModStatus incompatibileModStatus = incompatibileMod.status;

//                if (incompatibileModStatus.IsLoaded || incompatibileModStatus.CanLoad)
//                {
//                    incompatibleMods.Add(new ModIncompatibility(incompatibileMod.info, incompatibilityInfo));
//                }
//            }

//            if (incompatibleMods.Count > 0)
//            {
//                this.status = ModStatuses.CreateHasIncompatibilities(incompatibleMods.ToImmutableArray());
//                goto HANDLE_ERROR;
//            }

//            Logger.Log(LogLevel.Info, $"Loading [{this.info}]");
//            if (this.info.Load())
//            {
//                this.status = ModStatuses.Loaded;
//                return true;
//            }

//            this.status = ModStatuses.Error;

//            HANDLE_ERROR:
//            Logger.Log(LogLevel.Error, $"An error occurred whilst loading {this.info}: {this.status.GetErrorMessage()}");
//            return false;
//        }
//    }
//}