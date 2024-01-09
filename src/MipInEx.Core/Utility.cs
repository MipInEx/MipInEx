using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace MipInEx;

internal static class Utility
{
    public static readonly string pluginTypeNamespace = typeof(ModRootPluginBase).Namespace;
    public static readonly string pluginTypeName = nameof(ModRootPluginBase);
    public static readonly string internalPluginTypeName = nameof(ModInternalPluginBase);
    public static readonly string pluginAttributeFullName = typeof(ModPluginAttribute).FullName;
    public static readonly string internalPluginAttributeFullName = typeof(ModPluginInternalAttribute).FullName;

    public static readonly IComparer<IModAsset> AssetPriorityComparer = new ModAssetPriorityComparer();

    /// <summary>
    /// An encoding for UTF-8 which does not emit a byte order
    /// mark (BOM).
    /// </summary>
    public static readonly Encoding UTF8NoBom = new UTF8Encoding(false);

    private static void ValidateAssetPathPart(ReadOnlySpan<char> pathPart, int index, string argumentName)
    {
        bool hasNonPeriod = false;
        for (int partPartCharIndex = 0; partPartCharIndex < pathPart.Length; partPartCharIndex++)
        {
            char pathPartChar = pathPart[partPartCharIndex];
            if (pathPartChar == '.')
                continue;

            hasNonPeriod = true;
            // ensure ascii characters

            // ' ' = 21, '~' = 126
            // everything before 21 is a control char
            // everything after 126 is not supported.
            if (pathPartChar < ' ' || pathPartChar > '~' ||
                pathPartChar == '"' ||
                pathPartChar == '*' ||
                pathPartChar == ':' ||
                pathPartChar == '<' ||
                pathPartChar == '>' ||
                pathPartChar == '?' ||
                pathPartChar == '|')
            {
                throw new ArgumentException($"Unsupported path character '{pathPartChar}' at index {index + partPartCharIndex}.", argumentName);
            }
        }

        if (!hasNonPeriod)
        {
            throw new ArgumentException($"Path part at index {index} cannot be only periods!", argumentName);
        }
    }

    public static string ValidateAssetPath(string assetPath)
    {
        if (assetPath is null)
            throw new ArgumentNullException(nameof(assetPath));
        else if (assetPath.Length == 0)
            throw new ArgumentException("Asset path must have content", nameof(assetPath));

        assetPath = assetPath.Replace('\\', '/');

        ReadOnlySpan<char> assetPathSpan = assetPath.AsSpan();
        int index = 0;
        if (assetPathSpan[0] == '/')
        {
            assetPathSpan = assetPathSpan.Slice(1);
            index++;
        }

        int slashIndex;
        while ((slashIndex = assetPathSpan.IndexOf('/')) > -1)
        {
            if (slashIndex == 0)
            {
                throw new ArgumentException($"Unexpected empty path part at index {index}", nameof(assetPath));
            }

            Utility.ValidateAssetPathPart(assetPathSpan.Slice(0, slashIndex), index, nameof(assetPath));
            assetPathSpan = assetPathSpan.Slice(slashIndex + 1);
            // add one to include '/'
            index += slashIndex + 1;
        }

        if (assetPathSpan.Length == 0)
        {
            throw new ArgumentException($"Unexpected end of asset path at index {index}.", nameof(assetPath));
        }

        Utility.ValidateAssetPathPart(assetPathSpan, index, nameof(assetPath));
        return assetPath;
    }

    public static string ShortenAssetPathWithoutExtension(
        string assetPath,
        ReadOnlySpan<char> basePath,
        ReadOnlySpan<char> extension)
    {
        if (assetPath.Length == 0)
        {
            return string.Empty;
        }

        return Utility.ShortenAssetPathWithoutExtension(assetPath.AsSpan(), basePath, extension).ToString();
    }

    public static string ShortenAssetPathWithoutExtension(
        string assetPath,
        ReadOnlySpan<char> basePath,
        ReadOnlySpan<char> extension,
        StringComparison comparison)
    {
        if (assetPath.Length == 0)
        {
            return string.Empty;
        }

        return Utility.ShortenAssetPathWithoutExtension(assetPath.AsSpan(), basePath, extension, comparison).ToString();
    }

    public static ReadOnlySpan<char> ShortenAssetPathWithoutExtension(
        ReadOnlySpan<char> assetPath,
        ReadOnlySpan<char> basePath,
        ReadOnlySpan<char> extension)
    {
        assetPath = Utility.ShortenAssetPath(assetPath, basePath);

        int dotIndex = assetPath.LastIndexOf('.');
        if (dotIndex == -1 ||
            dotIndex < assetPath.LastIndexOf('/') ||
            !assetPath.Slice(dotIndex + 1).SequenceEqual(extension))
        {
            return assetPath;
        }

        return assetPath.Slice(0, dotIndex);
    }

    public static ReadOnlySpan<char> ShortenAssetPathWithoutExtension(
        ReadOnlySpan<char> assetPath,
        ReadOnlySpan<char> basePath,
        ReadOnlySpan<char> extension,
        StringComparison comparison)
    {
        assetPath = Utility.ShortenAssetPath(assetPath, basePath, comparison);

        int dotIndex = assetPath.LastIndexOf('.');
        if (dotIndex == -1 ||
            dotIndex < assetPath.LastIndexOf('/') ||
            !assetPath.Slice(dotIndex + 1).SequenceEqual(extension))
        {
            return assetPath;
        }

        return assetPath.Slice(0, dotIndex);
    }

    public static string ShortenAssetPath(
        string assetPath,
        ReadOnlySpan<char> basePath)
    {
        if (assetPath.Length == 0)
        {
            return string.Empty;
        }

        return Utility.ShortenAssetPath(assetPath.AsSpan(), basePath).ToString();
    }

    public static string ShortenAssetPath(
        string assetPath,
        ReadOnlySpan<char> basePath,
        StringComparison comparison)
    {
        if (assetPath.Length == 0)
        {
            return string.Empty;
        }

        return Utility.ShortenAssetPath(assetPath.AsSpan(), basePath, comparison).ToString();
    }

    public static ReadOnlySpan<char> ShortenAssetPath(
        ReadOnlySpan<char> assetPath,
        ReadOnlySpan<char> basePath)
    {
        int slashIndex = assetPath.IndexOf('/');
        if (slashIndex < 0 ||
            !assetPath.Slice(0, slashIndex).SequenceEqual(basePath))
        {
            return assetPath;
        }

        return assetPath.Slice(slashIndex);
    }

    public static ReadOnlySpan<char> ShortenAssetPath(
        ReadOnlySpan<char> assetPath,
        ReadOnlySpan<char> basePath,
        StringComparison comparison)
    {
        int slashIndex = assetPath.IndexOf('/');
        if (slashIndex < 0 ||
            !assetPath.Slice(0, slashIndex).Equals(basePath, comparison))
        {
            return assetPath;
        }

        return assetPath.Slice(slashIndex);
    }

    public static bool IncludeAssembly(AssemblyDefinition assembly)
    {
        return true;
    }

    public static List<TNode> TopologicalSort<TNode>(IEnumerable<TNode> nodes, Func<TNode, IEnumerable<TNode>> dependencySelector)
    {
        List<TNode> sortedList = new();
        HashSet<TNode> visited = new();
        HashSet<TNode> sorted = new();

        foreach (TNode input in nodes)
        {
            Stack<TNode> currentStack = new();
            if (!Visit(input, currentStack))
            {
                throw new Exception("Cyclic Dependency:\r\n" + currentStack.Select(x => $" - {x}")
                    .Aggregate((a, b) => $"{a}\r\n{b}"));
            }
        }

        return sortedList;

        bool Visit(TNode node, Stack<TNode> stack)
        {
            if (visited.Contains(node))
            {
                if (!sorted.Contains(node))
                {
                    return false;
                }
            }
            else
            {
                visited.Add(node);
                stack.Push(node);

                foreach (TNode dependency in dependencySelector(node))
                {
                    if (!Visit(dependency, stack))
                    {
                        return false;
                    }
                }

                sorted.Add(node);
                sortedList.Add(node);

                stack.Pop();
            }

            return true;
        }
    }

    internal static CustomAttribute? GetPluginInfoAttribute(TypeDefinition typeDefinition)
    {
        foreach (CustomAttribute attribute in typeDefinition.CustomAttributes)
        {
            TypeDefinition? attributeType = attribute.AttributeType.Resolve();
            if (attributeType != null &&
                attributeType.FullName == Utility.pluginAttributeFullName)
            {
                return attribute;
            }
        }

        return null;
    }

    internal static CustomAttribute? GetInternalPluginInfoAttribute(TypeDefinition typeDefinition)
    {
        foreach (CustomAttribute attribute in typeDefinition.CustomAttributes)
        {
            TypeDefinition? attributeType = attribute.AttributeType.Resolve();
            if (attributeType != null &&
                attributeType.FullName == Utility.internalPluginAttributeFullName)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>
    /// Computes the MD5 hash of the given stream.
    /// </summary>
    /// <param name="stream">Stream to hash.</param>
    /// <returns>MD5 hash as a hex string.</returns>
    public static string HashStream(Stream stream)
    {
        using MD5 md5 = MD5.Create();

        byte[] buffer = new byte[4096];
        int readBytes;
        while ((readBytes = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            md5.TransformBlock(buffer, 0, readBytes, buffer, 0);
        }

        md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return Utility.ByteArrayToString(md5.Hash);
    }

    /// <summary>
    /// Converts the given byte array to a hex string.
    /// </summary>
    /// <param name="data">Bytes to convert.</param>
    /// <returns>Bytes reinterpreted as a hex number.</returns>
    private static string ByteArrayToString(byte[] data)
    {
        StringBuilder builder = new StringBuilder(data.Length * 2);
        foreach (byte b in data)
        {
            builder.AppendFormat("{0:x2}", b);
        }

        return builder.ToString();
    }

    public static string TypeLoadExceptionToString(ReflectionTypeLoadException exception)
    {
        StringBuilder builder = new();

        foreach (Exception subException in exception.LoaderExceptions)
        {
            builder.AppendLine(subException.Message);
            if (subException is FileNotFoundException fileNotFoundEx)
            {
                if (!string.IsNullOrEmpty(fileNotFoundEx.FusionLog))
                {
                    builder.AppendLine("Fusion Log:");
                    builder.AppendLine(fileNotFoundEx.FusionLog);
                }
            }
            else if (subException is FileLoadException fileLoadEx)
            {
                if (!string.IsNullOrEmpty(fileLoadEx.FusionLog))
                {
                    builder.AppendLine("Fusion Log:");
                    builder.AppendLine(fileLoadEx.FusionLog);
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }


    private sealed class ModAssetPriorityComparer : IComparer<IModAsset>
    {
        public int Compare(IModAsset? x, IModAsset? y)
        {
            if (x is null)
            {
                if (y is null) return 0;
                else return -1;
            }
            else if (y is null)
            {
                return 1;
            }

            // remember: higher priority gets loaded first.
            //           thus: we compare y to x instead of x to y.
            int result = y.Manifest.LoadPriority.CompareTo(x.Manifest.LoadPriority);
            if (result != 0)
            {
                return result;
            }

            return y.Type.Id.CompareTo(x.Type.Id);
        }
    }
}
