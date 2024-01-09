using System;

namespace MipInEx;

/// <summary>
/// Information about a mod asset bundle.
/// </summary>
public sealed class ModAssetBundleInfo : IModAssetInfo
{
    private ModInfo mod;
    private ModAssetState state;
    private readonly string name;
    private readonly string assetPath;
    private readonly string longAssetPath;

    internal ModAssetBundleInfo(
        string name,
        string assetPath,
        string longAssetPath,
        ModAssetBundle bundle)
    {
        this.mod = null!;
        this.state = ModAssetState.NotLoaded;

        this.name = name;
        this.assetPath = assetPath;
        this.longAssetPath = longAssetPath;

#if UNITY_ENGINE
        this.Delegate__Contains_String = bundle.Contains;
        this.Delegate__GetAllAssetNames = bundle.GetAllAssetNames;
        this.Delegate__LoadAsset_String = bundle.LoadAsset;
        this.Delegate__LoadAsset_String_Type = bundle.LoadAsset;
        this.Delegate__LoadAllAssets = bundle.LoadAllAssets;
        this.Delegate__LoadAllAssets_Type = bundle.LoadAllAssets;
        this.Delegate__LoadAssetWithSubAssets_String = bundle.LoadAssetWithSubAssets;
        this.Delegate__LoadAssetWithSubAssets_String_Type = bundle.LoadAssetWithSubAssets;
#endif
    }

    /// <inheritdoc cref="ModAssetBundle.Mod"/>
    public ModInfo Mod => this.mod;

    /// <inheritdoc cref="ModAssetBundle.Name"/>
    public string Name => this.name;

    /// <inheritdoc cref="ModAssetBundle.AssetPath"/>
    public string AssetPath => this.assetPath;

    /// <inheritdoc cref="ModAssetBundle.LongAssetPath"/>
    public string LongAssetPath => this.longAssetPath;

    /// <inheritdoc cref="ModAssetBundle.State"/>
    public ModAssetState State => this.state;

    /// <inheritdoc cref="ModAssetBundle.IsLoaded"/>
    public bool IsLoaded => this.state == ModAssetState.Loaded;

    /// <inheritdoc cref="ModAssetBundle.Type"/>
    public ModAssetType Type => ModAssetType.AssetBundle;

    /// <inheritdoc cref="ModAssetBundle.GetDescriptorString()"/>
    public string GetDescriptorString()
    {
        return $"Asset Bundle '{this.name}'";
    }

    /// <inheritdoc cref="ModAssetBundle.ToString()"/>
    public sealed override string ToString()
    {
        return this.GetDescriptorString();
    }

    internal void Initialize(ModInfo mod)
    {
        this.mod = mod;
    }

    internal void SetState(ModAssetState state)
    {
        this.state = state;
    }

#if UNITY_ENGINE
    private readonly Func<string, bool> Delegate__Contains_String;
    private readonly Func<string[]> Delegate__GetAllAssetNames;
    private readonly Func<string, UnityEngine.Object?> Delegate__LoadAsset_String;
    private readonly Func<string, Type, UnityEngine.Object?> Delegate__LoadAsset_String_Type;
    private readonly Func<UnityEngine.Object[]> Delegate__LoadAllAssets;
    private readonly Func<Type, UnityEngine.Object[]> Delegate__LoadAllAssets_Type;
    private readonly Func<string, UnityEngine.Object[]> Delegate__LoadAssetWithSubAssets_String;
    private readonly Func<string, Type, UnityEngine.Object[]> Delegate__LoadAssetWithSubAssets_String_Type;

    /// <inheritdoc cref="ModAssetBundle.Contains(string)"/>
    public bool Contains(string name)
    {
        return this.Delegate__Contains_String(name);
    }

    /// <inheritdoc cref="ModAssetBundle.GetAllAssetNames()"/>
    public string[] GetAllAssetNames()
    {
        return this.Delegate__GetAllAssetNames();
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAsset(string)"/>
    public UnityEngine.Object? LoadAsset(string name)
    {
        return this.Delegate__LoadAsset_String(name);
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAsset(string, Type)"/>
    public UnityEngine.Object? LoadAsset(string name, Type type)
    {
        return this.Delegate__LoadAsset_String_Type(name, type);
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAsset{T}(string)"/>
    public T? LoadAsset<T>(string name)
        where T : UnityEngine.Object
    {
        return (T?)this.Delegate__LoadAsset_String_Type(name, typeof(T));
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAllAssets()"/>
    public UnityEngine.Object[] LoadAllAssets()
    {
        return this.Delegate__LoadAllAssets();
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAllAssets(Type)"/>
    public UnityEngine.Object[] LoadAllAssets(Type type)
    {
        return this.Delegate__LoadAllAssets_Type(type);
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAllAssets{T}()"/>
    public T[] LoadAllAssets<T>()
        where T : UnityEngine.Object
    {
        UnityEngine.Object[] rawObjects = this.Delegate__LoadAllAssets();
        
        T[] array = new T[rawObjects.Length];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = (T)rawObjects[i];
        }

        return array;
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAssetWithSubAssets(string)"/>
    public UnityEngine.Object[] LoadAssetWithSubAssets(string name)
    {
        return this.Delegate__LoadAssetWithSubAssets_String(name);
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAssetWithSubAssets(string, Type)"/>
    public UnityEngine.Object[] LoadAssetWithSubAssets(string name, Type type)
    {
        return this.Delegate__LoadAssetWithSubAssets_String_Type(name, type);
    }

    /// <inheritdoc cref="ModAssetBundle.LoadAssetWithSubAssets{T}(string)"/>
    public T[] LoadAssetWithSubAssets<T>(string name)
        where T : UnityEngine.Object
    {
        UnityEngine.Object[] rawObjects = this.Delegate__LoadAssetWithSubAssets_String_Type(name, typeof(T));

        T[] array = new T[rawObjects.Length];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = (T)rawObjects[i];
        }

        return array;
    }
#endif
}
