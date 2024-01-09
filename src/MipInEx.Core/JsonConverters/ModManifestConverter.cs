using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MipInEx.JsonConverters;

/// <summary>
/// A Json Converter for <see cref="ModManifest"/>s.
/// </summary>
public sealed class ModManifestConverter : JsonConverter<ModManifest>
{
    private static readonly string guidProperty = "guid";
    private static readonly string nameProperty = "name";
    private static readonly string descriptionProperty = "description";
    private static readonly string authorProperty = "author";
    private static readonly string versionProperty = "version";
    private static readonly string assetsProperty = "assets";
    private static readonly string dependenciesProperty = "dependencies";
    private static readonly string incompatibilitiesProperty = "incompatibilities";

    /// <inheritdoc/>
    public sealed override ModManifest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return null;
        }
        else if (reader.TokenType is not JsonTokenType.StartObject)
        {
            throw new JsonException($"Unexpected token {reader.TokenType}. Expected '{nameof(JsonTokenType.StartObject)}' token.");
        }

        string guid = null!;
        bool hasGuid = false;

        string name = null!;
        bool hasName = false;

        string description = null!;
        bool hasDescription = false;

        string author = null!;
        bool hasAuthor = false;

        Version version = null!;
        bool hasVersion = false;

        List<IModAssetManifest> assets = new();
        List<ModDependencyInfo> dependencies = new();
        List<ModIncompatibilityInfo> incompatibilities = new();

        bool readingPropertyName = true;
        bool hasEndObject = false;
        string propertyName = string.Empty;

        List<ArgumentException> exceptions = new();

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
                if (propertyName.Equals(ModManifestConverter.guidProperty, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureNullableString(in reader, propertyName);

                    guid = reader.GetString()!;
                    hasGuid = ModPropertyUtil.ValidateGuid(guid, exceptions);
                }
                else if (propertyName.Equals(ModManifestConverter.nameProperty, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureNullableString(in reader, propertyName);

                    name = reader.GetString()?.Trim()!;
                    hasName = ModPropertyUtil.ValidateName(name, exceptions);
                }
                else if (propertyName.Equals(ModManifestConverter.descriptionProperty, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureNullableString(in reader, propertyName);

                    description = reader.GetString()?.Trim()!;
                    hasDescription = ModPropertyUtil.ValidateDescription(description, exceptions);
                }
                else if (propertyName.Equals(ModManifestConverter.authorProperty, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureNullableString(in reader, propertyName);

                    author = reader.GetString()?.Trim()!;
                    hasAuthor = ModPropertyUtil.ValidateAuthor(author, exceptions);
                }
                else if (propertyName.Equals(ModManifestConverter.versionProperty, StringComparison.OrdinalIgnoreCase))
                {
                    JsonConverterUtility.EnsureNullableString(in reader, propertyName);

                    string? versionString = reader.GetString();
                    hasVersion = Version.TryParse(versionString, out version!);
                    if (!hasVersion)
                    {
                        exceptions.Add(new ArgumentException($"Invalid version string '{versionString}'", nameof(version)));
                    }
                }
                else if (propertyName.Equals(ModManifestConverter.assetsProperty, StringComparison.OrdinalIgnoreCase))
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
                                IModAssetManifest? asset = JsonSerializer.Deserialize<IModAssetManifest>(ref reader, options);

                                if (asset is not null)
                                {
                                    assets.Add(asset);
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
                else if (propertyName.Equals(ModManifestConverter.dependenciesProperty, StringComparison.OrdinalIgnoreCase))
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
                                ModDependencyInfo? dependency = JsonSerializer.Deserialize<ModDependencyInfo>(ref reader, options);

                                if (dependency is not null)
                                {
                                    dependencies.Add(dependency);
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
                else if (propertyName.Equals(ModManifestConverter.incompatibilitiesProperty, StringComparison.OrdinalIgnoreCase))
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
                                ModIncompatibilityInfo? incompatibility = JsonSerializer.Deserialize<ModIncompatibilityInfo>(ref reader, options);

                                if (incompatibility is not null)
                                {
                                    incompatibilities.Add(incompatibility);
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
            exceptions.Add(new ArgumentException("A GUID is required", ModManifestConverter.guidProperty));
        }

        if (!hasName)
        {
            exceptions.Add(new ArgumentException("A name is required", ModManifestConverter.nameProperty));
        }

        if (!hasDescription)
        {
            exceptions.Add(new ArgumentException("A description is required", ModManifestConverter.descriptionProperty));
        }

        if (!hasAuthor)
        {
            exceptions.Add(new ArgumentException("An author is required", ModManifestConverter.authorProperty));
        }

        if (!hasVersion)
        {
            exceptions.Add(new ArgumentException("A version is required", ModManifestConverter.versionProperty));
        }

        if (exceptions.Count > 0)
        {
            throw new JsonException("Failed to parse mod manifest", new AggregateException(exceptions));
        }

        return new ModManifest(
            guid, 
            name, 
            description, 
            author,
            version,
            assets.Count == 0 ?
                ImmutableArray<IModAssetManifest>.Empty :
                assets.ToImmutableArray(),
            dependencies.Count == 0 ?
                ImmutableArray<ModDependencyInfo>.Empty :
                dependencies.ToImmutableArray(), 
            incompatibilities.Count == 0 ?
                ImmutableArray<ModIncompatibilityInfo>.Empty :
                incompatibilities.ToImmutableArray());
    }

    /// <inheritdoc/>
    public sealed override void Write(Utf8JsonWriter writer, ModManifest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(ModManifestConverter.guidProperty, value.Guid);
        writer.WriteString(ModManifestConverter.nameProperty, value.Name);
        writer.WriteString(ModManifestConverter.descriptionProperty, value.Description);
        writer.WriteString(ModManifestConverter.versionProperty, value.Version.ToString());
        writer.WritePropertyName(ModManifestConverter.dependenciesProperty);
        writer.WriteStartArray();
        foreach (ModDependencyInfo dependency in value.Dependencies)
        {
            JsonSerializer.Serialize(writer, dependency, options);
        }
        writer.WriteEndArray();
        writer.WritePropertyName(ModManifestConverter.incompatibilitiesProperty);
        writer.WriteStartArray();
        foreach (ModIncompatibilityInfo incompatibility in value.Incompatibilities)
        {
            JsonSerializer.Serialize(writer, incompatibility, options);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
