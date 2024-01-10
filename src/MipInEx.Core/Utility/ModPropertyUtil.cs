using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MipInEx;

internal static class ModPropertyUtil
{
    // note: when changing these values, be sure to reflect
    //       them inside the xml docs for ModManifest.cs

    public const int MaxGuidLength = 256;
    public const int MaxNameLength = 256;
    public const int MaxDescriptionLength = 512;
    public const int MaxAuthorLength = 64;

    public static bool ValidateGuid([NotNullWhen(true)] string? guid, ICollection<ArgumentException>? exceptions)
    {
        if (guid is null)
        {
            exceptions?.Add(new ArgumentNullException(nameof(guid)));
            return false;
        }

        bool isValid = true;

        int guidLength = guid.Length;
        if (guidLength == 0)
        {
            if (exceptions is null) return false;

            exceptions.Add(new ArgumentException("Guid cannot be empty", nameof(guid)));
            isValid = false;
        }
        else if (guidLength > ModPropertyUtil.MaxGuidLength)
        {
            if (exceptions is null) return false;

            exceptions.Add(new ArgumentException($"Guid cannot exceed {ModPropertyUtil.MaxGuidLength} characters in length! Guid was {guidLength} characters in length.", nameof(guid)));
            guidLength = ModPropertyUtil.MaxGuidLength;
            isValid = false;
        }

        for (int index = 0; index < guidLength; index++)
        {
            char c = guid[index];
            if ((c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                c == '.' ||
                c == '_' ||
                c == '-')
            {
                continue;
            }

            if (exceptions is null) return false;
            else exceptions.Add(new ArgumentException($"Character '{c}' (index: {index}) in guid is not allowed! Only Alpha-Numeric characters, periods, underscores, and dashes are allowed in a GUID.", nameof(guid)));
            isValid = false;
        }

        return isValid;
    }

    public static void ValidateGuid([NotNull] string guid)
    {
        List<ArgumentException> exceptions = new();
        if (!ModPropertyUtil.ValidateGuid(guid, exceptions))
        {
            throw new AggregateException("Failed to validate GUID.", exceptions);
        }
    }

    public static bool TryValidateGuid([NotNullWhen(true)] string? guid)
    {
        return ModPropertyUtil.ValidateGuid(guid, null);
    }

    public static bool ValidateName([NotNullWhen(true)] string? name, ICollection<ArgumentException>? exceptions)
    {
        if (name is null)
        {
            exceptions?.Add(new ArgumentNullException(nameof(name)));
            return false;
        }

        int nameLength = name.Length;

        if (nameLength == 0)
        {
            exceptions?.Add(new ArgumentException("Name cannot be empty", nameof(name)));
            return false;
        }
        else if (nameLength > ModPropertyUtil.MaxNameLength)
        {
            exceptions?.Add(new ArgumentException($"Name cannot exceed {ModPropertyUtil.MaxNameLength} characters in length! Name was {nameLength} characters in length.", nameof(name)));
            return false;
        }

        return true;
    }

    public static void ValidateName([NotNull] string name)
    {
        List<ArgumentException> exceptions = new();
        if (!ModPropertyUtil.ValidateName(name, exceptions))
        {
            throw new AggregateException("Failed to validate Name.", exceptions);
        }
    }

    public static bool TryValidateName([NotNullWhen(true)] string? name)
    {
        return ModPropertyUtil.ValidateName(name, null);
    }

    public static bool ValidateDescription([NotNullWhen(true)] string? description, ICollection<ArgumentException>? exceptions)
    {
        if (description is null)
        {
            exceptions?.Add(new ArgumentNullException(nameof(description)));
            return false;
        }

        int descriptionLength = description.Length;
        if (descriptionLength > ModPropertyUtil.MaxDescriptionLength)
        {
            exceptions?.Add(new ArgumentException($"Description cannot exceed {ModPropertyUtil.MaxDescriptionLength} characters in length! Description was {descriptionLength} characters in length.", nameof(description)));
            return false;
        }

        return false;
    }

    public static void ValidateDescription([NotNull] string description)
    {
        List<ArgumentException> exceptions = new();
        if (!ModPropertyUtil.ValidateDescription(description, exceptions))
        {
            throw new AggregateException("Failed to validate Description.", exceptions);
        }
    }

    public static bool TryValidateDescription([NotNullWhen(true)] string? description)
    {
        return ModPropertyUtil.ValidateDescription(description, null);
    }

    public static bool ValidateAuthor([NotNullWhen(true)] string? author, ICollection<ArgumentException>? exceptions)
    {
        if (author is null)
        {
            exceptions?.Add(new ArgumentNullException(nameof(author)));
            return false;
        }

        int authorLength = author.Length;
        if (authorLength == 0)
        {
            exceptions?.Add(new ArgumentException("Author cannot be empty", nameof(author)));
            return false;
        }
        else if (authorLength > ModPropertyUtil.MaxAuthorLength)
        {
            exceptions?.Add(new ArgumentException($"Author cannot exceed {ModPropertyUtil.MaxAuthorLength} characters in length! Author was {authorLength} characters in length.", nameof(author)));
            return false;
        }

        return true;
    }

    public static void ValidateAuthor([NotNull] string author)
    {
        List<ArgumentException> exceptions = new();
        if (!ModPropertyUtil.ValidateAuthor(author, exceptions))
        {
            throw new AggregateException("Failed to validate Author.", exceptions);
        }
    }

    public static bool TryValidateAuthor([NotNullWhen(true)] string? author)
    {
        return ModPropertyUtil.ValidateAuthor(author, null);
    }

    public static bool ValidateVersion([NotNullWhen(true)] Version? version, ICollection<ArgumentException>? exceptions)
    {
        if (version is null)
        {
            exceptions?.Add(new ArgumentNullException(nameof(version)));
            return false;
        }
        return true;
    }

    public static void ValidateVersion([NotNull] Version version)
    {
        List<ArgumentException> exceptions = new();
        if (!ModPropertyUtil.ValidateVersion(version, exceptions))
        {
            throw new AggregateException("Failed to validate Version.", exceptions);
        }
    }

    public static bool TryValidateVersion([NotNullWhen(true)] Version? version)
    {
        return ModPropertyUtil.ValidateVersion(version, null);
    }
}
