using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MipInEx.JsonConverters;

/// <summary>
/// A Json Converter for converting
/// <see cref="ModInternalPluginManifest"/>.
/// </summary>
public sealed class ModManifestInternalPluginConverter : JsonConverter<ModInternalPluginManifest>
{
    private static readonly string guidProperty = "guid";
    private static readonly string loadPriorityProperty = "load_priority";
    private static readonly string loadPriorityPropertyAlt1 = "loadpriority";
    private static readonly string loadManuallyProperty = "load_manually";
    private static readonly string loadManuallyPropertyAlt1 = "loadmanually";

    /// <inheritdoc/>
    public sealed override ModInternalPluginManifest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        else if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException($"Unexpected token {reader.TokenType}. Expected '{nameof(JsonTokenType.StartObject)}' token.");
        }

        bool hasGuid = false;
        string? guid = null;

        long loadPriority = 0;
        bool loadManually = false;

        bool readingPropertyName = true;
        bool hasEndObject = false;
        string propertyName = string.Empty;

        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject)
            {
                hasEndObject = true;
                break;
            }

            if (readingPropertyName)
            {
                propertyName = reader.GetString()!;
            }
            else
            {
                if (propertyName.Equals(ModManifestInternalPluginConverter.guidProperty, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureNullableString(in reader, propertyName);

                    guid = reader.GetString();
                    hasGuid = guid is not null;
                }
                else if (propertyName.Equals(ModManifestInternalPluginConverter.loadPriorityPropertyAlt1, StringComparison.OrdinalIgnoreCase) ||
                    propertyName.Equals(ModManifestInternalPluginConverter.loadPriorityProperty, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureNumber(in reader, propertyName);

                    try
                    {
                        loadPriority = reader.GetInt64();
                    }
                    catch (FormatException ex)
                    {
                        throw new JsonException($"Error whilst reading property '{propertyName}'", ex);
                    }
                }
                else if (propertyName.Equals(ModManifestInternalPluginConverter.loadManuallyProperty, StringComparison.OrdinalIgnoreCase) ||
                    propertyName.EndsWith(ModManifestInternalPluginConverter.loadManuallyPropertyAlt1, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureBoolean(in reader, propertyName);

                    loadManually = reader.GetBoolean();
                }
                else
                {
                    reader.Skip();
                }
            }

            readingPropertyName = !readingPropertyName;
        }

        if (!hasEndObject)
        {
            throw new JsonException("Unexpected end of input!");
        }

        if (!hasGuid)
        {
            throw new JsonException($"Property '{ModManifestInternalPluginConverter.guidProperty}' is required!");
        }

        return new ModInternalPluginManifest(guid!, loadPriority, loadManually);
    }

    /// <inheritdoc/>
    public sealed override void Write(Utf8JsonWriter writer, ModInternalPluginManifest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(ModManifestInternalPluginConverter.guidProperty, value.Guid);
        writer.WriteNumber(ModManifestInternalPluginConverter.loadPriorityProperty, value.LoadPriority);
        writer.WriteBoolean(ModManifestInternalPluginConverter.loadManuallyProperty, value.LoadManually);
        writer.WriteEndObject();
    }
}
