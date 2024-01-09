//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace MipInEx.Bootstrap;

///// <summary>
///// The status of the mod.
///// </summary>
//internal interface IModStatus
//{
//    /// <summary>
//    /// Whether or not the mod can be loaded.
//    /// </summary>
//    bool CanLoad { get; }

//    /// <summary>
//    /// Whether or not this status is an error-like status.
//    /// </summary>
//    bool IsError { get; }

//    /// <summary>
//    /// Gets the error message of the error-like status.
//    /// </summary>
//    /// <returns>
//    /// <see cref="string.Empty"/> if this status not not an
//    /// error-like status, otherwise will be the message of the
//    /// error-like status.
//    /// </returns>
//    string GetErrorMessage();
//}

///// <summary>
///// A class containing implementations of
///// <see cref="IModStatus"/>.
///// </summary>
//internal static class ModStatus
//{
//    /// <summary>
//    /// The mod is not loaded.
//    /// </summary>
//    public readonly struct NotLoaded : IModStatus
//    {
//        bool IModStatus.CanLoad => true;
//        bool IModStatus.IsError => false;

//        string IModStatus.GetErrorMessage()
//            => string.Empty;
//    }

//    /// <summary>
//    /// The mod is loaded.
//    /// </summary>
//    public readonly struct Loaded : IModStatus
//    {
//        bool IModStatus.CanLoad => true;
//        bool IModStatus.IsError => false;

//        string IModStatus.GetErrorMessage()
//            => string.Empty;
//    }

//    /// <summary>
//    /// The mod has loading disabled.
//    /// </summary>
//    public readonly struct LoadDisabled : IModStatus
//    {
//        bool IModStatus.CanLoad => false;
//        bool IModStatus.IsError => false;

//        string IModStatus.GetErrorMessage()
//            => string.Empty;
//    }

//    /// <summary>
//    /// The mod is errored due to circular dependencies.
//    /// </summary>
//    public sealed class CircularDependenciesError : IModStatus
//    {
//        private readonly IReadOnlyList<ModInfo> dependencyStack;

//        /// <summary>
//        /// Initializes this circular dependencies error with
//        /// the specified dependency stack.
//        /// </summary>
//        /// <param name="dependencyStack">
//        /// The dependency stack.
//        /// </param>
//        public CircularDependenciesError(IReadOnlyList<ModInfo> dependencyStack)
//        {
//            this.dependencyStack = dependencyStack;
//        }

//        bool IModStatus.CanLoad => false;
//        bool IModStatus.IsError => true;

//        /// <summary>
//        /// The stack of dependencies.
//        /// </summary>
//        public IReadOnlyList<ModInfo> DependencyStack => this.dependencyStack;

//        /// <inheritdoc/>
//        public string GetErrorMessage()
//        {
//            StringBuilder builder = new("Mod has circular dependencies. Dependency Stack:");
//            foreach (ModInfo mod in this.dependencyStack)
//            {
//                builder.Append("\n - ");
//                builder.Append(mod);
//            }
//            return builder.ToString();
//        }
//    }

//    /// <summary>
//    /// The mod is errored due to circular dependencies.
//    /// </summary>
//    public sealed class HasIncompatibilitiesError : IModStatus
//    {
//        private readonly IReadOnlyList<ModIncompatibility> incompatibilities;

//        public HasIncompatibilitiesError(IReadOnlyList<ModIncompatibility> incompatibilities)
//        {
//            this.incompatibilities = incompatibilities;
//        }

//        bool IModStatus.CanLoad => false;
//        bool IModStatus.IsError => true;

//        public IReadOnlyList<ModIncompatibility> Incompatibilities => this.incompatibilities;

//        public string GetErrorMessage()
//        {
//            StringBuilder builder = new("Mod is incompatible with:");
//            foreach (ModIncompatibility incompatibility in this.incompatibilities)
//            {
//                builder.Append("\n - ");
//                builder.Append(incompatibility);
//            }
//            return builder.ToString();
//        }
//    }

//    public sealed class ErrorMissingDependencies : IModStatus
//    {
//        private readonly IReadOnlyList<MissingModDependency> dependencies;

//        public ErrorMissingDependencies(IReadOnlyList<MissingModDependency> dependencies)
//        {
//            this.dependencies = dependencies;
//        }

//        bool IModStatus.CanLoad => false;
//        bool IModStatus.IsError => true;

//        public IReadOnlyList<MissingModDependency> Dependencies => this.dependencies;

//        public string GetErrorMessage()
//        {
//            StringBuilder builder = new("Mod is missing dependencies:");
//            foreach (MissingModDependency dependency in this.dependencies)
//            {
//                builder.Append("\n - ");
//                builder.Append(dependency);
//            }
//            return builder.ToString();
//        }
//    }

//    public readonly struct Error : IModStatus
//    {
//        bool IModStatus.CanLoad => false;
//        bool IModStatus.IsError => true;

//        public string GetErrorMessage()
//        {
//            return "Mod failed to load due to an error. Check logs for details.";
//        }
//    }
//}
