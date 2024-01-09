using System;
using System.IO;

namespace MipInEx.Bootstrap;

internal sealed class PluginReference
{
    public PluginReference()
    {
        this.Type = new();
        this.Name = string.Empty;
        this.Guid = string.Empty;
        this.Version = null!;
    }

    public PluginReference(TypeDefinitionReference type)
    {
        this.Type = type;
        this.Name = string.Empty;
        this.Guid = string.Empty;
        this.Version = null!;
    }

    public PluginReference(TypeDefinitionReference type, string name, string guid, Version version)
    {
        this.Type = type;
        this.Name = name;
        this.Guid = guid;
        this.Version = version;
    }

    public TypeDefinitionReference Type { get; }

    public string Name { get; set; }
    public string Guid { get; set; }
    public Version Version { get; set; }

    public void Save(BinaryWriter binaryWriter)
    {
        this.Type.Save(binaryWriter);
        binaryWriter.Write(this.Name);
        binaryWriter.Write(this.Guid);
        binaryWriter.Write(this.Version.ToString());
    }

    public void Load(BinaryReader binaryReader)
    {
        this.Type.Load(binaryReader);
        this.Name = binaryReader.ReadString();
        this.Guid = binaryReader.ReadString();
        this.Version = Version.Parse(binaryReader.ReadString());
    }
}