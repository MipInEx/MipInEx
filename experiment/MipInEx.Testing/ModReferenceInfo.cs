using System;

namespace MipInEx;

public abstract class ModReferenceInfo
{
    private readonly string guid;

    public ModReferenceInfo(string guid)
    {
        this.guid = guid;
    }

    public string Guid => this.guid;

    public bool IncludesVersion(Version version)
    {
        return true;
    }
}
