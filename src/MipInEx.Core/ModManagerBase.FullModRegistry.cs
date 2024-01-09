using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MipInEx;

partial class ModManagerBase
{
    /// <summary>
    /// The full mod registry in a mod manager.
    /// </summary>
    protected internal sealed class FullModRegistry
    {
        private FrozenDictionary<string, Mod> mods;
        private ModRegistry registry;

        /// <summary>
        /// Initializes this full mod registry to be
        /// empty.
        /// </summary>
        public FullModRegistry()
        {
            this.mods = FrozenDictionary<string, Mod>.Empty;
            this.registry = new ModRegistry(this.mods);
        }

        /// <summary>
        /// The <see cref="ModRegistry"/> associated with this
        /// full registry.
        /// </summary>
        public ModRegistry Registry => this.registry;

        /// <inheritdoc cref="ModRegistry.Count"/>
        public int Count => this.mods.Count;

        /// <inheritdoc cref="ModRegistry.this[string]"/>
        public Mod this[string guid]
        {
            get => this.mods[guid];
        }

        /// <inheritdoc cref="ModRegistry.ContainsMod(string?)"/>
        public bool ContainsMod([NotNullWhen(true)] string? guid)
        {
            return guid is not null && this.mods.ContainsKey(guid);
        }

        /// <inheritdoc cref="ModRegistry.TryGetMod(string?, out ModInfo?)"/>
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
            Dictionary<string, Mod> newMods = new();
            foreach (KeyValuePair<string, Mod> entry in this.mods)
            {
                newMods.Add(entry.Key, entry.Value);
            }

            foreach (Mod mod in mods)
            {
                newMods.Add(mod.Guid, mod);
            }

            this.mods = newMods.ToFrozenDictionary();
            this.registry = new ModRegistry(this.mods);
        }
    }
}
