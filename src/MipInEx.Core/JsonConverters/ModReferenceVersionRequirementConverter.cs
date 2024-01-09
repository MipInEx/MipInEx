using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MipInEx.JsonConverters;

/// <summary>
/// A Json Converter for converting
/// <see cref="IModReferenceVersionRequirement"/>s.
/// </summary>
public sealed class ModReferenceVersionRequirementConverter : JsonConverter<IModReferenceVersionRequirement>
{
    /// <inheritdoc/>
    public sealed override bool CanConvert(Type typeToConvert)
    {
        return typeof(IModReferenceVersionRequirement).IsAssignableFrom(typeToConvert);
    }

    /// <inheritdoc/>
    public sealed override IModReferenceVersionRequirement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            return ModReferenceVersionRequirement.Parse(reader.GetString());
        }
        else
        {
            throw new JsonException($"Unexpected token of type '{reader.TokenType}'. Expected {nameof(JsonTokenType.String)} or {nameof(JsonTokenType.Null)}.");
        }
    }

    /// <inheritdoc/>
    public sealed override void Write(Utf8JsonWriter writer, IModReferenceVersionRequirement value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
