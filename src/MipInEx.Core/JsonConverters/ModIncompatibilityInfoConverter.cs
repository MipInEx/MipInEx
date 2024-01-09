using System.Collections.Immutable;

namespace MipInEx.JsonConverters;

/// <summary>
/// A Json Converter for converting
/// <see cref="ModIncompatibilityInfo"/>s.
/// </summary>
public sealed class ModIncompatibilityInfoConverter : ModReferenceInfoConverter<ModIncompatibilityInfo, ModIncompatibilityInfoConverter.ReadInterop>
{
    /// <summary>
    /// The Read Interop type.
    /// </summary>
    new public class ReadInterop : ModReferenceInfoConverter<ModIncompatibilityInfo, ReadInterop>.ReadInterop
    {

    }

    /// <inheritdoc/>
    protected sealed override ModIncompatibilityInfo CreateFromInterop(ReadInterop interop, string guid, ImmutableArray<IModReferenceVersionRequirement> versions)
    {
        return new ModIncompatibilityInfo(guid, versions);
    }
}
