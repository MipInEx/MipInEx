using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

/// <summary>
/// The type of mod asset.
/// </summary>
public readonly struct ModAssetType : 
    IEquatable<ModAssetType>,
    IEquatable<string>,
    IEquatable<int>,
    IComparable<ModAssetType>,
    IComparable<string>,
    IComparable<int>,
    IComparable
{
    private readonly int id;
    private readonly string name;

    private ModAssetType(int id, string name)
    {
        this.id = id;
        this.name = name;
    }

    /// <summary>
    /// The ID of the Mod Asset Type.
    /// </summary>
    /// <remarks>
    /// If less than 0, then this represents an unregistered
    /// mod asset type. If equal to 0, then this represents an
    /// unknown mod asset type.
    /// </remarks>
    public int Id => this.id;

    /// <summary>
    /// The Name of the Mod Asset Type.
    /// </summary>
    public string Name => this.name ?? "Unknown";

    /// <summary>
    /// Returns the comparison between this and the specified
    /// object.
    /// </summary>
    /// <param name="obj">The object</param>
    /// <returns>
    /// <list type="table">
    /// <item>
    /// <term>
    /// If <paramref name="obj"/> is <see langword="null"/>
    /// </term>
    /// <description>
    /// <c>1</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If <paramref name="obj"/> is <see cref="ModAssetType"/>
    /// </term>
    /// <description>
    /// The result of calling <see cref="CompareTo(ModAssetType)"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If <paramref name="obj"/> is <see cref="int"/>
    /// </term>
    /// <description>
    /// The result of calling <see cref="CompareTo(int)"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If <paramref name="obj"/> is <see cref="string"/>
    /// </term>
    /// <description>
    /// The result of calling <see cref="CompareTo(string?)"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Otherwise
    /// </term>
    /// <description>
    /// throws an <see cref="ArgumentException"/>.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="obj"/> is not <see langword="null"/>,
    /// <see cref="ModAssetType"/>,
    /// <see cref="int"/>,
    /// or
    /// <see cref="string"/>.
    /// </exception>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        else if (obj is ModAssetType modAssetType) return this.CompareTo(modAssetType);
        else if (obj is int id) return this.CompareTo(id);
        else if (obj is string name) return this.CompareTo(name);
        else throw new ArgumentException($"Cannot compare {nameof(ModAssetType)} to an argument of type {obj.GetType()}. Can only compare 'null', {nameof(ModAssetType)}, 'int', and 'string'!");
    }

    /// <summary>
    /// Returns the result of comparing this mod asset type to
    /// <paramref name="other"/>.
    /// </summary>
    /// <remarks>
    /// Does the following checks in order:
    /// <list type="number">
    /// <item>
    /// <term>
    /// If
    /// <c><see langword="this"/>.<see cref="Id">Id</see></c>
    /// is less than zero and
    /// <c><paramref name="other"/>.<see cref="Id">Id</see></c>
    /// is less than zero
    /// </term>
    /// <description>
    /// Returns the result of comparing
    /// <c><see langword="this"/>.<see cref="Name">Name</see></c>
    /// to
    /// <c><paramref name="other"/>.<see cref="Name">Name</see></c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If
    /// <c><see langword="this"/>.<see cref="Id">Id</see></c>
    /// is less than zero and
    /// <c><paramref name="other"/>.<see cref="Id">Id</see></c>
    /// is greater than or equal to zero
    /// </term>
    /// <description>
    /// Returns <c>1</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If
    /// <c><paramref name="other"/>.<see cref="Id">Id</see></c>
    /// is less than zero
    /// </term>
    /// <description>
    /// Returns <c>-1</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>Otherwise</term>
    /// <description>
    /// Returns the result of comparing
    /// <c><see langword="this"/>.<see cref="Id">Id</see></c>
    /// to
    /// <c><paramref name="other"/>.<see cref="Id">Id</see></c>
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="other">The other mod asset type</param>
    /// <returns>
    /// A 32-bit signed integer that indicates whether this mod
    /// asset type instance precedes, follows, or appears in
    /// the same position in the sort order as
    /// <paramref name="other"/>.
    /// <list type="table">
    /// <item>
    /// <term>
    /// Less than zero
    /// </term>
    /// <description>
    /// This mod asset type preceeds <paramref name="other"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Zero
    /// </term>
    /// <description>
    /// This mod asset type has the same position
    /// in the sort order as <paramref name="other"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Greater than zero
    /// </term>
    /// <description>
    /// This mod asset type follows <paramref name="other"/>.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    public int CompareTo(ModAssetType other)
    {
        if (this.id < 0)
        {
            if (other.id < 0)
            {
                return this.Name.CompareTo(other.Name);
            }

            return 1;
        }
        else if (other.id < 0)
        {
            return -1;
        }

        return this.id.CompareTo(other.id);
    }

    /// <summary>
    /// Returns the result of comparing the id of this mod
    /// asset type to <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id</param>
    /// <returns>
    /// A signed number indicating the relative values of the
    /// id of the mod asset type instance and
    /// <paramref name="id"/>.
    /// <list type="table">
    /// <item>
    /// <term>
    /// Less than zero
    /// </term>
    /// <description>
    /// The id of this mod asset type is less than
    /// <paramref name="id"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Zero
    /// </term>
    /// <description>
    /// The id of this mod asset type is equal to
    /// <paramref name="id"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Greater than zero
    /// </term>
    /// <description>
    /// The id of this mod asset type is greater than
    /// <paramref name="id"/>.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    public int CompareTo(int id)
    {
        return this.id.CompareTo(id);
    }

    /// <summary>
    /// Returns the result of comparing the name of this mod
    /// asset type to <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name</param>
    /// <returns>
    /// A 32-bit signed integer that indicates whether the
    /// name of this mod asset type instance precedes, follows,
    /// or appears in the same position in the sort order as
    /// <paramref name="name"/>.
    /// <list type="table">
    /// <item>
    /// <term>
    /// Less than zero
    /// </term>
    /// <description>
    /// The name of this mod asset type preceeds
    /// <paramref name="name"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Zero
    /// </term>
    /// <description>
    /// The name of this mod asset type has the same position
    /// in the sort order as <paramref name="name"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Greater than zero
    /// </term>
    /// <description>
    /// The name of this mod asset type follows
    /// <paramref name="name"/> OR <paramref name="name"/> is
    /// <see langword="null"/>.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    public int CompareTo(string? name)
    {
        return this.name.CompareTo(name);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.id, this.Name);
    }

    /// <summary>
    /// Returns whether this and the specified object are
    /// equal.
    /// </summary>
    /// <param name="obj">The object</param>
    /// <returns>
    /// <list type="table">
    /// <item>
    /// <term>
    /// If <paramref name="obj"/> is <see langword="null"/>
    /// </term>
    /// <description>
    /// <c><see langword="false"/></c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If <paramref name="obj"/> is <see cref="ModAssetType"/>
    /// </term>
    /// <description>
    /// The result of calling <see cref="Equals(ModAssetType)"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If <paramref name="obj"/> is <see cref="int"/>
    /// </term>
    /// <description>
    /// The result of calling <see cref="Equals(int)"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// If <paramref name="obj"/> is <see cref="string"/>
    /// </term>
    /// <description>
    /// The result of calling <see cref="Equals(string?)"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Otherwise
    /// </term>
    /// <description>
    /// <c><see langword="false"/></c>
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is null) return false;
        else if (obj is ModAssetType modAssetType) return this.Equals(modAssetType);
        else if (obj is int id) return this.Equals(id);
        else if (obj is string name) return this.Equals(name);
        else return false;
    }

    /// <summary>
    /// Returns whether the id of this mod asset type equals
    /// the specified id.
    /// </summary>
    /// <param name="id">The id</param>
    /// <returns>
    /// <see langword="true"/> if this id equals
    /// <paramref name="id"/>; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(int id)
    {
        return this.id == id;
    }

    /// <summary>
    /// Returns whether the name of this mod asset type equals
    /// the specified name.
    /// </summary>
    /// <param name="name">The name</param>
    /// <returns>
    /// <see langword="true"/> if this name equals
    /// <paramref name="name"/>; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals([NotNullWhen(true)] string? name)
    {
        return name is not null && this.Name.Equals(name);
    }

    /// <summary>
    /// Returns whether the name of this mod asset type equals
    /// the specified name.
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="comparisonType">
    /// How the names will be compared.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this name equals
    /// <paramref name="name"/>; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals([NotNullWhen(true)] string? name, StringComparison comparisonType)
    {
        return name is not null && this.Name.Equals(name, comparisonType);
    }

    /// <summary>
    /// Returns whether this mod asset type instance equals
    /// <paramref name="other"/>.
    /// </summary>
    /// <remarks>
    /// Does the following checks in order:
    /// <list type="number">
    /// <item>
    /// <term>
    /// If
    /// <c><see langword="this"/>.<see cref="Id">Id</see></c>
    /// is less than zero
    /// </term>
    /// <description>
    /// Returns whether
    /// <c><paramref name="other"/>.<see cref="Id">Id</see></c>
    /// is less than zero and
    /// <c><see langword="this"/>.<see cref="Name">Name</see></c>
    /// equals
    /// <c><paramref name="other"/>.<see cref="Name">Name</see></c>
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// Otherwise
    /// </term>
    /// <description>
    /// Returns whether
    /// <c><see langword="this"/>.<see cref="Id">Id</see></c>
    /// equals
    /// <c><paramref name="other"/>.<see cref="Id">Id</see></c>
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="other">The other</param>
    /// <returns>
    /// <see langword="true"/> if this equals
    /// <paramref name="other"/>; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(ModAssetType other)
    {
        if (this.id < 0)
        {
            return other.id < 0 &&
                this.Name.Equals(other.Name);
        }

        return this.id == other.id;
    }

    private static readonly List<ModAssetType> idMap;
    private static readonly Dictionary<string, int> nameToIdMap;
    private static int idIncrement;

    /// <summary>
    /// An unknown type of mod asset.
    /// </summary>
    public static readonly ModAssetType Unknown;

    /// <summary>
    /// An assembly mod asset.
    /// </summary>
    public static readonly ModAssetType Assembly;

    /// <summary>
    /// An asset bundle mod asset.
    /// </summary>
    public static readonly ModAssetType AssetBundle;

    static ModAssetType()
    {
        ModAssetType.idMap = new();
        ModAssetType.nameToIdMap = new();
        ModAssetType.idIncrement = 0;

        ModAssetType.Unknown = ModAssetType.CreateImpl("Unknown");
        ModAssetType.Assembly = ModAssetType.CreateImpl("Assembly");
        ModAssetType.AssetBundle = ModAssetType.CreateImpl("Asset Bundle");
    }

    private static ModAssetType CreateImpl(string name)
    {
        int id = ModAssetType.idIncrement;
        ModAssetType.idIncrement++;

        ModAssetType result = new(ModAssetType.idIncrement, name);
        ModAssetType.idMap.Add(result);
        ModAssetType.nameToIdMap.Add(name, id);
        return result;
    }

    /// <summary>
    /// Registers the given name as a mod asset type.
    /// </summary>
    /// <param name="name">
    /// The name of the mod asset type.
    /// </param>
    /// <returns>
    /// The newly registered mod asset type.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    internal static ModAssetType Register(string name)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        if (ModAssetType.nameToIdMap.TryGetValue(name, out int id))
        {
            return ModAssetType.idMap[id];
        }

        return ModAssetType.CreateImpl(name);
    }

    /// <summary>
    /// Attempts to parse a mod asset type from the given name.
    /// </summary>
    /// <param name="name">
    /// The name of the mod asset type.
    /// </param>
    /// <param name="result">
    /// The found mod asset type. If not found, then the value
    /// of this will be <see cref="ModAssetType.Unknown"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the mod asset type was found;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse([NotNullWhen(true)] string? name, out ModAssetType result)
    {
        if (name is not null &&
            ModAssetType.nameToIdMap.TryGetValue(name, out int id))
        {
            result = ModAssetType.idMap[id];
            return true;
        }

        result = ModAssetType.Unknown;
        return false;
    }

    /// <summary>
    /// Attempts to parse a mod asset type from the given name.
    /// </summary>
    /// <param name="name">
    /// The name of the mod asset type.
    /// </param>
    /// <param name="ignoreCase">
    /// Whether or not the case of the mod asset type name
    /// should be ignored.
    /// </param>
    /// <param name="result">
    /// The found mod asset type. If not found, then the value
    /// of this will be <see cref="ModAssetType.Unknown"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the mod asset type was found;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse([NotNullWhen(true)] string? name, bool ignoreCase, out ModAssetType result)
    {
        if (name is null)
        {
            result = ModAssetType.Unknown;
            return false;
        }

        if (ignoreCase)
        {
            foreach (ModAssetType entry in ModAssetType.idMap)
            {
                if (entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    result = entry;
                    return true;
                }
            }
        }
        else if (ModAssetType.nameToIdMap.TryGetValue(name, out int id))
        {
            result = ModAssetType.idMap[id];
            return true;
        }

        result = ModAssetType.Unknown;
        return false;
    }

    /// <summary>
    /// Converts the given mod asset type to an id
    /// <see cref="int"/>.
    /// </summary>
    /// <param name="modAssetType">
    /// The mod asset type.
    /// </param>
    public static explicit operator int(ModAssetType modAssetType)
    {
        return modAssetType.Id;
    }

    /// <summary>
    /// Converts the given mod asset type to it's name.
    /// </summary>
    /// <param name="modAssetType">
    /// The mod asset type.
    /// </param>
    public static explicit operator string(ModAssetType modAssetType)
    {
        return modAssetType.Name;
    }

    /// <summary>
    /// Converts the given id to a <see cref="ModAssetType"/>.
    /// <para>
    /// If not mod asset type with id <paramref name="id"/>
    /// exists, then will return
    /// <see cref="ModAssetType.Unknown"/>.
    /// </para>
    /// </summary>
    /// <param name="id">The id of the mod asset type.</param>
    public static explicit operator ModAssetType(int id)
    {
        if (id < 0 || id >= ModAssetType.idMap.Count)
        {
            return ModAssetType.Unknown;
        }
        else
        {
            return ModAssetType.idMap[id];
        }
    }

    /// <summary>
    /// Converts the given name to a
    /// <see cref="ModAssetType"/>.
    /// <para>
    /// If not mod asset type with name <paramref name="name"/>
    /// exists, then will return a new
    /// <see cref="ModAssetType"/>
    /// with name <paramref name="name"/> and id <c>-1</c>.
    /// </para>
    /// </summary>
    /// <param name="name">The name of the mod asset type.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public static explicit operator ModAssetType(string name)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        if (!ModAssetType.nameToIdMap.TryGetValue(name, out int id))
        {
            return new ModAssetType(-1, name);
        }

        return ModAssetType.idMap[id];
    }

    /// <inheritdoc/>
    public static bool operator ==(ModAssetType left, ModAssetType right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(ModAssetType left, ModAssetType right)
    {
        return !left.Equals(right);
    }
}
