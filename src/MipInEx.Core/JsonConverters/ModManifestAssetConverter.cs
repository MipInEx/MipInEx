using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MipInEx.JsonConverters;

/// <summary>
/// A Json Converter for converting
/// <see cref="IModAssetManifest"/>s.
/// </summary>
public sealed class ModManifestAssetConverter : JsonConverter<IModAssetManifest>
{
    private static readonly string assetPathProperty = "asset_path";
    private static readonly string assetPathPropertyAlt1 = "assetpath";
    private static readonly string loadPriorityProperty = "load_priority";
    private static readonly string loadPriorityPropertyAlt1 = "loadpriority";
    private static readonly string loadManuallyProperty = "load_manually";
    private static readonly string loadManuallyPropertyAlt1 = "loadmanually";
    private static readonly string pluginProperty = "plugin";
    private static readonly string internalPluginsProperty = "internal_plugins";
    private static readonly string internalPluginsPropertyAlt1 = "internalplugins";
    private static readonly string typeProperty = "type";

    /// <inheritdoc/>
    public sealed override bool CanConvert(Type typeToConvert)
    {
        return typeof(IModAssetManifest).IsAssignableFrom(typeToConvert);
    }

    /// <inheritdoc/>
    public sealed override IModAssetManifest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        else if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException($"Unexpected token {reader.TokenType}. Expected '{nameof(JsonTokenType.StartObject)}' token.");
        }

        bool hasAssetPath = false;
        string? assetPath = null;

        long loadPriority = 0;
        bool loadManually = false;
        ModRootPluginManifest? plugin = null;
        List<ModInternalPluginManifest> internalPlugins = new();

        bool hasType = false;
        ModAssetType type = ModAssetType.Unknown;

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
                if (propertyName.Equals(ModManifestAssetConverter.assetPathProperty, StringComparison.OrdinalIgnoreCase) ||
                    propertyName.Equals(ModManifestAssetConverter.assetPathPropertyAlt1, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureNullableString(in reader, propertyName);

                    assetPath = reader.GetString();
                    hasAssetPath = assetPath is not null;
                }
                else if (propertyName.Equals(ModManifestAssetConverter.loadPriorityProperty, StringComparison.OrdinalIgnoreCase) ||
                    propertyName.Equals(ModManifestAssetConverter.loadPriorityPropertyAlt1, StringComparison.OrdinalIgnoreCase))
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
                else if (propertyName.Equals(ModManifestAssetConverter.loadManuallyProperty, StringComparison.OrdinalIgnoreCase) ||
                    propertyName.EndsWith(ModManifestAssetConverter.loadManuallyPropertyAlt1, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureBoolean(in reader, propertyName);

                    loadManually = reader.GetBoolean();
                }
                else if (propertyName.Equals(ModManifestAssetConverter.pluginProperty, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        plugin = JsonSerializer.Deserialize<ModRootPluginManifest>(ref reader, options);
                    }
                    catch (Exception ex)
                    {
                        throw new JsonException($"Error whilst reading property '{propertyName}'", ex);
                    }
                }
                else if (propertyName.Equals(ModManifestAssetConverter.internalPluginsProperty, StringComparison.OrdinalIgnoreCase) ||
                    propertyName.EndsWith(ModManifestAssetConverter.internalPluginsPropertyAlt1))
                {
                    if (reader.TokenType is JsonTokenType.Null)
                    { }
                    else
                    {
                        JsonConverterUtility.EnsureArray(in reader, propertyName);

                        bool unexpectedEndOfInput = true;
                        int arrayIndex = 0;
                        while (reader.Read())
                        {
                            if (reader.TokenType is JsonTokenType.EndArray)
                            {
                                unexpectedEndOfInput = false;
                                break;
                            }

                            try
                            {
                                ModInternalPluginManifest? internalPlugin = JsonSerializer.Deserialize<ModInternalPluginManifest>(ref reader, options);

                                if (internalPlugin is not null)
                                {
                                    internalPlugins.Add(internalPlugin);
                                }
                                arrayIndex++;
                            }
                            catch (Exception ex)
                            {
                                throw new JsonException($"Error whilst reading property '{propertyName}' (array index {arrayIndex})", ex);
                            }
                        }

                        if (unexpectedEndOfInput)
                        {
                            throw new JsonException($"Unexpected end of input whilst parsing '{propertyName}'.");
                        }
                    }
                }
                else if (propertyName.Equals(ModManifestAssetConverter.typeProperty, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureString(in reader, propertyName);

                    string typeString = reader.GetString()!;
                    if (!ModAssetType.TryParse(assetPath, true, out type))
                    {
                        throw new JsonException($"Error whilst reading property '{propertyName}'", new ArgumentException($"Mod asset type '{typeString}' is not a registered/recognized mod asset type!", ModManifestAssetConverter.assetPathProperty));
                    }
                    hasType = true;
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

        if (!hasType)
        {
            throw new JsonException($"Property '{ModManifestAssetConverter.typeProperty}' is required!");
        }

        if (!hasAssetPath)
        {
            throw new JsonException($"Property '{ModManifestAssetConverter.assetPathProperty}' is required!");
        }

        if (type == ModAssetType.AssetBundle)
        {
            return new ModAssetBundleManifest(assetPath!, loadPriority, loadManually);
        }
        else if (type == ModAssetType.Assembly)
        {
            if (internalPlugins.Count > 0)
            {
                return new ModAssemblyManifest(assetPath!, loadPriority, loadManually, plugin);
            }
            else
            {
                return new ModAssemblyManifest(assetPath!, loadPriority, loadManually, plugin, internalPlugins);
            }
        }
        else
        {
            throw new JsonException($"Unknown asset type '{type}'");
        }
    }

    /// <inheritdoc/>
    public sealed override void Write(Utf8JsonWriter writer, IModAssetManifest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value is ModAssetBundleManifest assetBundle)
        {
            writer.WriteString(ModManifestAssetConverter.assetPathProperty, assetBundle.AssetPath);
            writer.WriteNumber(ModManifestAssetConverter.loadPriorityProperty, assetBundle.LoadPriority);
            writer.WriteBoolean(ModManifestAssetConverter.loadManuallyProperty, assetBundle.LoadManually);
        }
        else if (value is ModAssemblyManifest assembly)
        {
            writer.WriteString(ModManifestAssetConverter.assetPathProperty, assembly.AssetPath);
            writer.WriteNumber(ModManifestAssetConverter.loadPriorityProperty, assembly.LoadPriority);
            writer.WriteBoolean(ModManifestAssetConverter.loadManuallyProperty, assembly.LoadManually);
            // note: we skip 'plugin' property as it's empty.
            writer.WritePropertyName(ModManifestAssetConverter.internalPluginsProperty);
            writer.WriteStartArray();
            foreach (ModInternalPluginManifest internalPlugin in assembly.InternalPlugins)
            {
                JsonSerializer.Serialize(writer, internalPlugin, options);
            }
            writer.WriteEndArray();
        }
        else
        {
            writer.WriteString(ModManifestAssetConverter.assetPathProperty, value.AssetPath);
            writer.WriteNumber(ModManifestAssetConverter.loadPriorityProperty, value.LoadPriority);
            writer.WriteBoolean(ModManifestAssetConverter.loadManuallyProperty, value.LoadManually);
        }

        writer.WriteString(ModManifestAssetConverter.typeProperty, value.Type.Name);
        writer.WriteEndObject();
    }
}
