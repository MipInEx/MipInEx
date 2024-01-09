using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MipInEx.Configuration.Converters;

/// <summary>
/// A <see cref="string"/> Converter.
/// </summary>
public sealed class StringTomlConverter : SpecializedTomlTypeConverter<string>
{
    /// <inheritdoc/>
    public sealed override string Serialize(string? value, Type targetType)
    {
        if (value is not string strValue) return string.Empty;
        else return StringTomlConverter.Escape(strValue);
    }

    /// <inheritdoc/>
    public sealed override string? Deserialize(string value, Type targetType)
    {
        if (Regex.IsMatch(value, @"^""?\w:\\(?!\\)(?!.+\\\\)"))
        {
            return value;
        }
        else
        {
            return StringTomlConverter.Unescape(value);
        }
    }

    private static string Unescape(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;


        StringBuilder stringBuilder = new(text.Length);
        int currentIndex = 0;
        while (currentIndex < text.Length)
        {
            int escapeCharIndex = text.IndexOf('\\', currentIndex);
            if (escapeCharIndex < 0 || escapeCharIndex == text.Length - 1)
            {
                escapeCharIndex = text.Length;
            }
            stringBuilder.Append(text, currentIndex, escapeCharIndex - currentIndex);

            if (escapeCharIndex >= text.Length)
                break;

            char c = text[escapeCharIndex + 1];

            switch (c)
            {
                case '0':
                    stringBuilder.Append('\0');
                    break;
                case 'a':
                    stringBuilder.Append('\a');
                    break;
                case 'b':
                    stringBuilder.Append('\b');
                    break;
                case 't':
                    stringBuilder.Append('\t');
                    break;
                case 'n':
                    stringBuilder.Append('\n');
                    break;
                case 'v':
                    stringBuilder.Append('\v');
                    break;
                case 'f':
                    stringBuilder.Append('\f');
                    break;
                case 'r':
                    stringBuilder.Append('\r');
                    break;
                case '\'':
                    stringBuilder.Append('\'');
                    break;
                case '\"':
                    stringBuilder.Append('\"');
                    break;
                case '\\':
                    stringBuilder.Append('\\');
                    break;
                default:
                    stringBuilder.Append('\\').Append(c);
                    break;
            }

            currentIndex = escapeCharIndex + 2;
        }

        return stringBuilder.ToString();
    }

    private static string Escape(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        StringBuilder stringBuilder = new(text.Length + 2);

        foreach (char c in text)
        {
            switch (c)
            {
                case '\0':
                    stringBuilder.Append(@"\0");
                    break;
                case '\a':
                    stringBuilder.Append(@"\a");
                    break;
                case '\b':
                    stringBuilder.Append(@"\b");
                    break;
                case '\t':
                    stringBuilder.Append(@"\t");
                    break;
                case '\n':
                    stringBuilder.Append(@"\n");
                    break;
                case '\v':
                    stringBuilder.Append(@"\v");
                    break;
                case '\f':
                    stringBuilder.Append(@"\f");
                    break;
                case '\r':
                    stringBuilder.Append(@"\r");
                    break;
                case '\'':
                    stringBuilder.Append(@"\'");
                    break;
                case '\\':
                    stringBuilder.Append(@"\");
                    break;
                case '"':
                    stringBuilder.Append(@"\""");
                    break;
                default:
                    stringBuilder.Append(c);
                    break;
            }
        }

        return stringBuilder.ToString();
    }
}
