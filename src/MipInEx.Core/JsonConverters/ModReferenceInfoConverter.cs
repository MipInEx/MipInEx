using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MipInEx.JsonConverters;

/// <summary>
/// A base Json Converter for converting
/// types that inherit <see cref="ModReferenceInfo"/>.
/// </summary>
/// <typeparam name="T">
/// The type this converter handles.
/// </typeparam>
/// <typeparam name="TReadInterop">
/// The interop read type to be converted to
/// <typeparamref name="T"/>.
/// </typeparam>
public abstract class ModReferenceInfoConverter<T, TReadInterop> : JsonConverter<T>
    where T : notnull, ModReferenceInfo
    where TReadInterop : ModReferenceInfoConverter<T, TReadInterop>.ReadInterop
{
    /// <summary>
    /// The base Read Interop type.
    /// </summary>
    public class ReadInterop
    {
        /// <inheritdoc/>
        [JsonInclude]
        public string? guid;
        /// <inheritdoc/>
        [JsonInclude]
        public IModReferenceVersionRequirement? version;
        /// <inheritdoc/>
        [JsonInclude]
        public IModReferenceVersionRequirement?[]? versions;
    }

    /// <inheritdoc/>
    public sealed override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        TReadInterop? interop = JsonSerializer.Deserialize<TReadInterop>(ref reader, options);
        if (interop is null)
        {
            return null;
        }

        if (interop.guid is null)
        {
            throw new JsonException("'guid' is required!");
        }

        ImmutableArray<IModReferenceVersionRequirement> versions;
        if (interop.versions is null)
        {
            if (interop.version is null)
            {
                versions = ImmutableArray<IModReferenceVersionRequirement>.Empty;
            }
            else
            {
                versions = ImmutableArray.Create(interop.version);
            }

        }
        else
        {
            versions = interop.versions.Prepend(interop.version)
                .Where(x => x is not null)
                .ToImmutableArray()!;
        }

        return this.CreateFromInterop(interop, interop.guid, versions);
    }

    /// <summary>
    /// Create <typeparamref name="T"/> from the given interop,
    /// guid, and immutable version requirement array.
    /// </summary>
    /// <param name="interop">
    /// The read interop type.
    /// </param>
    /// <param name="guid">
    /// The guid.
    /// </param>
    /// <param name="versions">
    /// The versions requirements..
    /// </param>
    /// <returns></returns>
    protected abstract T CreateFromInterop(TReadInterop interop, string guid, ImmutableArray<IModReferenceVersionRequirement> versions);

    /// <summary>
    /// Writes additional properties on the derived
    /// <see cref="ModReferenceInfo"/> type
    /// to the specified <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">
    /// The Json Writer to write to.
    /// </param>
    /// <param name="value">
    /// The derived instance of <see cref="ModReferenceInfo"/>.
    /// </param>
    protected virtual void WriteAdditionalProperties(Utf8JsonWriter writer, T value)
    {

    }

    /// <inheritdoc/>
    public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(ReadInterop.guid), value.Guid);
        int versionCount = value.Versions.Count;
        if (versionCount > 0)
        {
            if (versionCount == 1)
            {
                writer.WritePropertyName(nameof(ReadInterop.version));
                JsonSerializer.Serialize(writer, value.Versions[0], options);
            }
            else
            {
                writer.WritePropertyName(nameof(ReadInterop.versions));
                writer.WriteStartArray();
                foreach (IModReferenceVersionRequirement version in value.Versions)
                {
                    JsonSerializer.Serialize(writer, version, options);
                }
                writer.WriteEndArray();
            }
        }
        this.WriteAdditionalProperties(writer, value);
        writer.WriteEndObject();
    }
}