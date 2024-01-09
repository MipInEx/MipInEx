using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MipInEx.JsonConverters;

internal static class JsonConverterUtility
{
    public static void EnsureNullableString(in Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType is JsonTokenType.String or JsonTokenType.Null)
        {
            return;
        }

        throw new JsonException($"Unexpected {JsonConverterUtility.GetCategoryName(reader.TokenType)} when reading '{propertyName}' property. Expected string or null.");
    }

    public static void EnsureString(in Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            return;
        }

        throw new JsonException($"Unexpected {JsonConverterUtility.GetCategoryName(reader.TokenType)} when reading '{propertyName}' property. Expected string.");
    }

    public static void EnsureBoolean(in Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType is JsonTokenType.True or JsonTokenType.False)
        {
            return;
        }

        throw new JsonException($"Unexpected {JsonConverterUtility.GetCategoryName(reader.TokenType)} when reading '{propertyName}' property. Expected 'true' or 'false'.");
    }

    public static void EnsureArray(in Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType is JsonTokenType.StartArray)
        {
            return;
        }

        throw new JsonException($"Unexpected {JsonConverterUtility.GetCategoryName(reader.TokenType)} when reading '{propertyName}' property. Expected array.");
    }

    public static void EnsureNumber(in Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType is JsonTokenType.Number)
        {
            return;
        }

        throw new JsonException($"Unexpected {JsonConverterUtility.GetCategoryName(reader.TokenType)} when reading '{propertyName}' property. Expected number.");
    }

    public static string GetCategoryName(JsonTokenType tokenType)
    {
        return tokenType switch
        {
            JsonTokenType.None => "Unknown",
            JsonTokenType.StartObject or JsonTokenType.EndObject => "Object",
            JsonTokenType.StartArray or JsonTokenType.EndArray => "Array",
            JsonTokenType.PropertyName => "Property",
            JsonTokenType.Comment => "Comment",
            JsonTokenType.String => "String",
            JsonTokenType.Number => "Number",
            JsonTokenType.True => "True Boolean",
            JsonTokenType.False => "False Boolean",
            JsonTokenType.Null => "Null",
            _ => "Unknown"
        };
    }
}
