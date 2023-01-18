using System;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Json.Util
{
    // from JetBrains.ReSharper.Psi.JavaScript.Util.StringLiteralUtil
    public static class StringLiteralUtil
    {
        public static string GetDoubleQuotedStringValue(ITokenNode token)
        {
            var text = token.GetText();
            if (text.Length <= 1) return null;

            char firstChar = text[0],
                lastChar = text[text.Length - 1];

            if (firstChar == '\"' || firstChar == '\'')
            {
                var value = (lastChar == firstChar) ? text.Substring(1, text.Length - 2) : text.Substring(1);
                return TryConvertPresentationToValue(value, firstChar);
            }

            if (lastChar == '\"' || lastChar == '\'')
            {
                var value = text.Substring(0, text.Length - 1);
                return TryConvertPresentationToValue(value, lastChar);
            }

            return null;
        }

        [CanBeNull]
        public static string TryConvertPresentationToValue([NotNull] string value, char quote)
        {
            return TryConvertPresentationToValue(value,
                (quote == '\"')
                    ? StringLiteralPresentationForm.DOUBLE_QUOTED
                    : StringLiteralPresentationForm.SINGLE_QUOTED);
        }

        [CanBeNull]
        public static string TryConvertPresentationToValue([NotNull] string value,
            StringLiteralPresentationForm presentationForm)
        {
            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length;)
            {
                var presentationLength = GetCharPresentationLength(value, i);
                if (presentationLength == -1) return null;
                if (presentationLength == 1)
                {
                    builder.Append(value[i++]);
                }
                else
                {
                    var presentation = value.Substring(i, presentationLength);
                    if (!TryConvertPresentationToValue(presentation, presentationForm,
                        out var charResult))
                        return null;

                    builder.Append(charResult);
                    i += presentationLength;
                }
            }

            return builder.ToString();
        }

        public enum StringLiteralPresentationForm
        {
            SINGLE_QUOTED,
            DOUBLE_QUOTED
        }

        public static int GetCharPresentationLength(string presentation, int firstCharInPresentationIndex)
        {
            if (presentation[firstCharInPresentationIndex] != '\\') return 1;
            if (firstCharInPresentationIndex + 1 < presentation.Length)
            {
                int symbolLength;
                switch (presentation[firstCharInPresentationIndex + 1])
                {
                    case 'x':
                        symbolLength = 4;
                        break; // \xFF
                    case 'u':
                        symbolLength = 6;
                        break; // \uFFFF
                    default:
                        symbolLength = 2;
                        break; // \a
                }

                if (firstCharInPresentationIndex + symbolLength <= presentation.Length)
                    return symbolLength;
            }

            return -1;
        }

        public static bool TryConvertPresentationToValue(
            [NotNull] string charPresentation, StringLiteralPresentationForm presentationForm, out string result)
        {
            if (charPresentation.Length == 1)
            {
                var ch = charPresentation[0];
                if (ch == '\\' || ch == '\n') goto Fail;
                if (ch == '\"' && presentationForm == StringLiteralPresentationForm.DOUBLE_QUOTED) goto Fail;
                if (ch == '\'' && presentationForm == StringLiteralPresentationForm.SINGLE_QUOTED) goto Fail;

                result = charPresentation;
                return true;
            }

            switch (charPresentation)
            {
                case @"\'":
                    result = "\'";
                    return true;
                case @"\""":
                    result = "\"";
                    return true;
                case @"\\":
                    result = "\\";
                    return true;
                case @"\0":
                    result = "\0";
                    return true;
                case @"\b":
                    result = "\b";
                    return true;
                case @"\f":
                    result = "\f";
                    return true;
                case @"\n":
                    result = "\n";
                    return true;
                case @"\r":
                    result = "\r";
                    return true;
                case @"\t":
                    result = "\t";
                    return true;
                case @"\v":
                    result = "\v";
                    return true;
            }

            if (charPresentation.StartsWith(@"\x", StringComparison.Ordinal) ||
                charPresentation.StartsWith(@"\u", StringComparison.Ordinal))
            {
                charPresentation = charPresentation.Substring(2);
                if (int.TryParse(charPresentation, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out var utf))
                {
                    if (utf < 0x000000 || utf > 0x10ffff) goto Fail;
                    if (utf >= 0x00d800 && utf <= 0x00dfff)
                    {
                        result = new string(Convert.ToChar(utf), 1);
                        return true;
                    }

                    try
                    {
                        result = Char.ConvertFromUtf32(utf);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        goto Fail;
                    }

                    return true;
                }
            }
            else if (charPresentation.Length == 2)
            {
                // \ NonEscapeCharacter
                result = charPresentation[1].ToString(CultureInfo.InvariantCulture);
                return true;
            }

            Fail:
            result = null;
            return false;
        }
    }
}