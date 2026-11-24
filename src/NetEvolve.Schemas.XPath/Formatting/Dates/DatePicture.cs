namespace NetEvolve.Schemas.XPath.Formatting.Dates;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.XPath.Formatting.Integers;

/// <summary>
/// A parsed picture string of <c>format-date</c>, <c>format-dateTime</c>, and <c>format-time</c> (Functions and Operators 3.1, 9.8.4):
/// literal text with <c>[[</c> and <c>]]</c> escapes, and variable markers made of a component specifier, a presentation modifier, and a
/// width modifier. White space inside a marker is ignored. The integer presentation is read by <see cref="IntegerPicture"/>, so the date
/// formatting and <c>format-integer</c> share one numbering component.
/// </summary>
/// <remarks>
/// Limitations: a component that has a digit pattern with at least two digit signs and no width modifier is limited to that many digits
/// for the year and the fractional seconds only; a name is abbreviated to three letters when it is longer than the maximum width; the
/// minimum width does not pad names and the maximum width does not shorten non-decimal numbers or time zones.
/// </remarks>
internal sealed class DatePicture
{
    private const string Components = "YMDdFWwHhPmsfZzCE";

    private DatePicture(IReadOnlyList<object> parts) => Parts = parts;

    /// <summary>Gets the parts in order: a <see cref="string"/> for literal text, a <see cref="Marker"/> for a variable marker.</summary>
    public IReadOnlyList<object> Parts { get; }

    /// <summary>Parses a picture string.</summary>
    /// <param name="picture">The picture string.</param>
    /// <param name="result">The picture, <see langword="null"/> when the string is not valid.</param>
    /// <param name="problem">The rule that is broken, <see langword="null"/> when the string is valid.</param>
    /// <returns><see langword="true"/> when the string is valid.</returns>
    public static bool TryParse(string picture, out DatePicture? result, out string? problem)
    {
        result = null;
        var parts = new List<object>();
        var literal = new StringBuilder();
        var i = 0;
        while (i < picture.Length)
        {
            var c = picture[i];
            if (c == '[' && i + 1 < picture.Length && picture[i + 1] == '[')
            {
                _ = literal.Append('[');
                i += 2;
            }
            else if (c == ']' && i + 1 < picture.Length && picture[i + 1] == ']')
            {
                _ = literal.Append(']');
                i += 2;
            }
            else if (c == ']')
            {
                problem = "A closing bracket in the picture string is not doubled.";
                return false;
            }
            else if (c == '[')
            {
                var end = picture.IndexOfAny(['[', ']'], i + 1);
                if (end < 0 || picture[end] == '[')
                {
                    problem = "A variable marker of the picture string is not closed.";
                    return false;
                }

                if (!TryParseMarker(Strip(picture.Substring(i + 1, end - i - 1)), out var marker, out problem))
                {
                    return false;
                }

                if (literal.Length > 0)
                {
                    parts.Add(literal.ToString());
                    _ = literal.Clear();
                }

                parts.Add(marker!);
                i = end + 1;
            }
            else
            {
                _ = literal.Append(c);
                i++;
            }
        }

        if (literal.Length > 0)
        {
            parts.Add(literal.ToString());
        }

        problem = null;
        result = new DatePicture(parts);
        return true;
    }

    private static string Strip(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (!char.IsWhiteSpace(c))
            {
                _ = builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static bool TryParseMarker(string text, out Marker? marker, out string? problem)
    {
        marker = null;
        if (text.Length == 0 || Components.AsSpan().IndexOf(text[0]) < 0)
        {
            problem = "A variable marker of the picture string has no valid component specifier.";
            return false;
        }

        var component = text[0];
        var rest = text.Substring(1);
        int min = 0,
            max = 0;
        var hasWidth = false;
        var comma = rest.AsSpan().LastIndexOf(',');
        if (comma >= 0 && TryParseWidth(rest.Substring(comma + 1), out min, out max, out var widthProblem))
        {
            if (widthProblem is not null)
            {
                problem = widthProblem;
                return false;
            }

            hasWidth = true;
            rest = rest.Substring(0, comma);
        }

        SplitModifiers(rest, out var first, out var second);
        var created = new Marker
        {
            Component = component,
            Min = min,
            Max = max,
            HasWidth = hasWidth,
            Traditional = second.AsSpan().IndexOf('t') >= 0,
        };
        if (!TryParsePresentation(created, first, second, out problem))
        {
            return false;
        }

        marker = created;
        return true;
    }

    // The width modifier is the text after the last comma when it reads min or min-max, each a positive integer or an asterisk.
    private static bool TryParseWidth(string text, out int min, out int max, out string? problem)
    {
        min = 0;
        max = 0;
        problem = null;
        var dash = text.AsSpan().IndexOf('-');
        var minText = dash < 0 ? text : text.Substring(0, dash);
        var maxText = dash < 0 ? null : text.Substring(dash + 1);
        if (!IsWidthPart(minText) || (maxText is not null && !IsWidthPart(maxText)))
        {
            return false;
        }

        if (!ReadWidth(minText, out min) || (maxText is not null && !ReadWidth(maxText, out max)))
        {
            problem = "A width modifier of the picture string is not a positive integer.";
        }
        else if (max > 0 && min > max)
        {
            problem = "The maximum width of a variable marker is smaller than its minimum width.";
        }

        return true;
    }

    private static bool IsWidthPart(string text)
    {
        if (text == "*")
        {
            return true;
        }

        if (text.Length == 0)
        {
            return false;
        }

        foreach (var c in text)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool ReadWidth(string text, out int value)
    {
        value = 0;
        return text == "*"
            || (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0);
    }

    // The presentation modifier is a first modifier (the format token) and an optional second one: c or o with an optional variation
    // in parentheses, and a or t. A lone a or t is a format token.
    private static void SplitModifiers(string text, out string first, out string second)
    {
        var tail = string.Empty;
        if (text.Length > 1 && text[text.Length - 1] is 'a' or 't')
        {
            tail = text.Substring(text.Length - 1);
            text = text.Substring(0, text.Length - 1);
        }

        var ordinal = string.Empty;
        if (text.Length > 0 && text[text.Length - 1] == ')')
        {
            var open = text.AsSpan().LastIndexOf('(');
            if (open > 0 && text[open - 1] is 'c' or 'o')
            {
                ordinal = text.Substring(open - 1);
                text = text.Substring(0, open - 1);
            }
        }
        else if (text.Length > 0 && text[text.Length - 1] is 'c' or 'o')
        {
            ordinal = text.Substring(text.Length - 1);
            text = text.Substring(0, text.Length - 1);
        }

        first = text;
        second = ordinal + tail;
    }

    private static string DefaultToken(char component) =>
        component switch
        {
            'F' => "Nn",
            'P' or 'E' or 'C' => "n",
            'm' or 's' => "01",
            'Z' or 'z' => "01:01",
            _ => "1",
        };

    private static bool TryParsePresentation(Marker marker, string first, string second, out string? problem)
    {
        problem = null;
        var component = marker.Component;
        var token = first.Length == 0 ? DefaultToken(component) : first;
        var name = token switch
        {
            "N" => NameStyle.Upper,
            "n" => NameStyle.Lower,
            "Nn" => NameStyle.Title,
            _ => NameStyle.None,
        };

        if (component is 'E' or 'C' or 'P')
        {
            marker.Name = name == NameStyle.None ? NameStyle.Lower : name;
            return true;
        }

        if (name != NameStyle.None && component is 'M' or 'F' or 'Z')
        {
            marker.Name = name;
            return true;
        }

        if (name != NameStyle.None)
        {
            token = DefaultToken(component);
        }

        if (component is 'Z' or 'z')
        {
            marker.Military = component == 'Z' && token == "Z";
            marker.Zone = ZonePattern.Parse(token);
            return true;
        }

        if (!IntegerPicture.TryParse(token + ";" + second, out var picture, out problem))
        {
            return false;
        }

        if (picture!.Sequence == IntegerSequence.Decimal && marker.Min > picture.MinimumDigits)
        {
            picture.MinimumDigits = marker.Min;
        }

        marker.Integer = picture;
        return true;
    }

    /// <summary>How a name is cased: not a name, all capitals, all lower case, or as the table has it.</summary>
    internal enum NameStyle
    {
        /// <summary>The component is not written as a name.</summary>
        None,

        /// <summary>All capitals.</summary>
        Upper,

        /// <summary>All lower case.</summary>
        Lower,

        /// <summary>The first letter in capitals, as the table has it.</summary>
        Title,
    }

    /// <summary>One variable marker of the picture string.</summary>
    internal sealed class Marker
    {
        /// <summary>Gets the component specifier.</summary>
        public char Component { get; internal set; }

        /// <summary>Gets the minimum width, 0 when it is not given.</summary>
        public int Min { get; internal set; }

        /// <summary>Gets the maximum width, 0 when it is not given.</summary>
        public int Max { get; internal set; }

        /// <summary>Gets a value indicating whether the marker has a width modifier.</summary>
        public bool HasWidth { get; internal set; }

        /// <summary>Gets a value indicating whether the second modifier asks for the traditional form (<c>t</c>).</summary>
        public bool Traditional { get; internal set; }

        /// <summary>Gets the style of a name presentation, or <see cref="NameStyle.None"/> for a number.</summary>
        public NameStyle Name { get; internal set; }

        /// <summary>Gets a value indicating whether the time zone is written as a military letter.</summary>
        public bool Military { get; internal set; }

        /// <summary>Gets the numbering of a numeric component.</summary>
        public IntegerPicture? Integer { get; internal set; }

        /// <summary>Gets the pattern of a time zone component.</summary>
        public ZonePattern? Zone { get; internal set; }
    }

    /// <summary>The digit pattern of a time zone offset.</summary>
    internal sealed class ZonePattern
    {
        private ZonePattern(int zero, int hours, string separator, bool minutesRequired)
        {
            Zero = zero;
            HoursWidth = hours;
            Separator = separator;
            MinutesRequired = minutesRequired;
        }

        /// <summary>Gets the code point of the zero digit.</summary>
        public int Zero { get; }

        /// <summary>Gets the minimum number of digits of the hours.</summary>
        public int HoursWidth { get; }

        /// <summary>Gets the text between the hours and the minutes.</summary>
        public string Separator { get; }

        /// <summary>Gets a value indicating whether the minutes are written when they are zero.</summary>
        public bool MinutesRequired { get; }

        /// <summary>Reads a digit pattern such as <c>01:01</c>, <c>0</c>, <c>0000</c>, or <c>00~00</c>; a pattern without digits is the default.</summary>
        /// <param name="token">The format token.</param>
        /// <returns>The pattern.</returns>
        public static ZonePattern Parse(string token)
        {
            var zero = (int)'0';
            int before = 0,
                after = 0;
            string? separator = null;
            for (var i = 0; i < token.Length; i += char.IsHighSurrogate(token, i) ? 2 : 1)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(token, i) == UnicodeCategory.DecimalDigitNumber)
                {
                    zero = char.ConvertToUtf32(token, i) - (int)CharUnicodeInfo.GetDecimalDigitValue(token, i);
                    if (separator is null)
                    {
                        before++;
                    }
                    else
                    {
                        after++;
                    }
                }
                else if (token[i] != '#' && before > 0 && separator is null)
                {
                    separator = token.Substring(i, char.IsHighSurrogate(token, i) ? 2 : 1);
                }
            }

            if (before == 0)
            {
                return new ZonePattern('0', 2, ":", true);
            }

            if (separator is not null)
            {
                return new ZonePattern(zero, before, separator, true);
            }

            return before <= 2
                ? new ZonePattern(zero, before, ":", false)
                : new ZonePattern(zero, before - 2, string.Empty, true);
        }
    }
}
