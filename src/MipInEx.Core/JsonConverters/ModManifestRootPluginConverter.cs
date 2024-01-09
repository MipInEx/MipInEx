using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MipInEx.JsonConverters;

/// <summary>
/// A Json Converter for converting
/// <see cref="ModRootPluginManifest"/>.
/// </summary>
public sealed class ModManifestRootPluginConverter : JsonConverter<ModRootPluginManifest>
{
    /// <inheritdoc/>
    public sealed override ModRootPluginManifest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        else if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException($"Unexpected token {reader.TokenType}. Expected '{nameof(JsonTokenType.StartObject)}' token.");
        }

        reader.Skip();

        return new ModRootPluginManifest();
    }

    /// <inheritdoc/>
    public sealed override void Write(Utf8JsonWriter writer, ModRootPluginManifest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteEndObject();
    }
}
