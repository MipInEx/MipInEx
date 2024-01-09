namespace MipInEx;

public sealed class ModDependencyInfo : ModReferenceInfo
{
    private readonly bool required;

    public ModDependencyInfo(string guid, bool required)
        : base(guid)
    {
        this.required = required;
    }

    public bool Required => this.required;
}
