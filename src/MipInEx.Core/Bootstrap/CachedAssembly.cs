using System.Collections.Generic;
using System.IO;

namespace MipInEx.Bootstrap;

internal sealed class CachedAssembly : ICacheable
{
    private string identifier = string.Empty;
    private string hash = string.Empty;
    private string name = string.Empty;
    private PluginReference? mainPlugin;
    private readonly List<PluginReference> internalPlugins = new();
    private readonly List<string> assemblyReferences = new();

    public string Identifier
    {
        get => this.identifier;
        set => this.identifier = value;
    }
    public string Hash
    {
        get => this.hash;
        set => this.hash = value;
    }
    public string Name
    {
        get => this.name;
        set => this.name = value;
    }
    public PluginReference? MainPlugin
    {
        get => this.mainPlugin;
        set => this.mainPlugin = value;
    }
    public List<PluginReference> InternalPlugins => this.internalPlugins;
    public List<string> AssemblyReferences => this.assemblyReferences;

    public void Load(BinaryReader binaryReader)
    {
        this.identifier = binaryReader.ReadString();
        this.hash = binaryReader.ReadString();
        this.name = binaryReader.ReadString();
        bool hasMainPlugin = binaryReader.ReadBoolean();
        if (hasMainPlugin)
        {
            this.mainPlugin ??= new();
            this.mainPlugin.Load(binaryReader);
        }
        else
        {
            this.mainPlugin = null;
        }

        int internalPluginsCount = this.internalPlugins.Count;
        if (this.internalPlugins.Count > internalPluginsCount)
        {
            int removeCount = this.internalPlugins.Count - internalPluginsCount;
            this.internalPlugins.RemoveRange(this.internalPlugins.Count - removeCount, removeCount);
        }
        
        while (this.internalPlugins.Count < internalPluginsCount)
        {
            this.internalPlugins.Add(new PluginReference());
        }

        this.internalPlugins.TrimExcess();

        for (int index = 0; index < this.internalPlugins.Count; index++)
        {
            this.internalPlugins[index].Load(binaryReader);
        }

        this.assemblyReferences.Clear();
        int assemblyReferenceCount = binaryReader.ReadInt32();
        while (assemblyReferenceCount > 0)
        {
            this.assemblyReferences.Add(binaryReader.ReadString());
            assemblyReferenceCount--;
        }
        this.assemblyReferences.TrimExcess();
    }

    public void Save(BinaryWriter binaryWriter)
    {
        binaryWriter.Write(this.identifier);
        binaryWriter.Write(this.hash);
        binaryWriter.Write(this.name);

        if (this.mainPlugin != null)
        {
            binaryWriter.Write(true);
            this.mainPlugin.Save(binaryWriter);
        }
        else
        {
            binaryWriter.Write(false);
        }

        int internalPluginsCount = this.internalPlugins.Count;
        binaryWriter.Write(internalPluginsCount);
        for (int index = 0; index < internalPluginsCount; index++)
        {
            this.internalPlugins[index].Save(binaryWriter);
        }

        int assemblyReferenceCount = this.assemblyReferences.Count;
        binaryWriter.Write(assemblyReferenceCount);
        for (int index = 0; index < assemblyReferenceCount; index++)
        {
            binaryWriter.Write(this.assemblyReferences[index]);
        }
    }
}
