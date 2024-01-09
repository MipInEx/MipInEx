using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MipInEx.JsonConverters;

/// <summary>
/// A Json Converter for converting
/// <see cref="ModDependencyInfo"/>s.
/// </summary>
public sealed class ModDependencyInfoConverter : ModReferenceInfoConverter<ModDependencyInfo, ModDependencyInfoConverter.ReadInterop>
{
    /// <summary>
    /// The Read Interop type.
    /// </summary>
    new public class ReadInterop : ModReferenceInfoConverter<ModDependencyInfo, ReadInterop>.ReadInterop
    {
        /// <inheritdoc/>
        [JsonInclude]
        public bool required;
    }

    /// <inheritdoc/>
    protected sealed override ModDependencyInfo CreateFromInterop(ReadInterop interop, string guid, ImmutableArray<IModReferenceVersionRequirement> versions)
    {
        return new ModDependencyInfo(guid, interop.required, versions);
    }

    /// <inheritdoc/>
    protected sealed override void WriteAdditionalProperties(Utf8JsonWriter writer, ModDependencyInfo value)
    {
        writer.WriteBoolean(nameof(ReadInterop.required), value.Required);
    }
}
