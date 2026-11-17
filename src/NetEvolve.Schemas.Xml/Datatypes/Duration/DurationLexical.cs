namespace NetEvolve.Schemas.Xml.Datatypes.Duration;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;

/// <summary>Scans the <c>PnYnMnDTnHnMnS</c> lexical space without allocating.</summary>
internal static class DurationLexical
{
    // Limitation (design #100, AP-9): every integer component is capped at 17 digits, so the month total fits a long.
    private const int MaxComponentDigits = 17;

    [StructLayout(LayoutKind.Auto)]
    internal struct Parts
    {
        public bool Negative;
        public long Years;
        public long Months;
        public long Days;
        public long Hours;
        public long Minutes;
        public long Seconds;
        public int FractionStart;
        public int FractionLength;
    }

    internal static DatatypeError Scan(ReadOnlySpan<char> s, DurationKind kind, out Parts parts, out int errorOffset)
    {
        parts = default;
        errorOffset = 0;
        var i = 0;
        if (s.Length > 0 && s[0] == '-')
        {
            parts.Negative = true;
            i++;
        }

        if (i >= s.Length || s[i] != 'P')
        {
            return Fail(s, i, out errorOffset);
        }

        i++;
        var rank = 0;
        var inTime = false;
        var components = 0;
        while (i < s.Length)
        {
            if (s[i] == 'T')
            {
                if (inTime || kind == DurationKind.YearMonth)
                {
                    return Fail(s, i, out errorOffset);
                }

                inTime = true;
                i++;
                if (i == s.Length)
                {
                    return Fail(s, i, out errorOffset);
                }

                continue;
            }

            var error = Component(s, ref i, kind, inTime, ref rank, ref parts, out errorOffset);
            if (error != DatatypeError.None)
            {
                return error;
            }

            components++;
        }

        return components == 0 ? Fail(s, i, out errorOffset) : DatatypeError.None;
    }

    // One number with its designator; advances the index past the designator.
    private static DatatypeError Component(
        ReadOnlySpan<char> s,
        ref int i,
        DurationKind kind,
        bool inTime,
        ref int rank,
        ref Parts parts,
        out int errorOffset
    )
    {
        errorOffset = 0;
        var start = i;
        i = SkipDigits(s, i);
        if (i == start)
        {
            return Fail(s, i, out errorOffset);
        }

        var numberEnd = i;
        var dot = -1;
        if (i < s.Length && s[i] == '.')
        {
            dot = i++;
            parts.FractionStart = i;
            i = SkipDigits(s, i);
            parts.FractionLength = i - parts.FractionStart;
            if (parts.FractionLength == 0)
            {
                return Fail(s, i, out errorOffset);
            }
        }

        if (i == s.Length)
        {
            return Fail(s, i, out errorOffset);
        }

        var next = inTime ? TimeRank(s[i]) : DateRank(s[i]);
        if (next <= rank)
        {
            return Fail(s, i, out errorOffset);
        }

        // A fraction is only allowed on the seconds.
        if (dot >= 0 && next != 6)
        {
            return Fail(s, dot, out errorOffset);
        }

        if ((kind == DurationKind.YearMonth && next > 2) || (kind == DurationKind.DayTime && next < 3))
        {
            return Fail(s, i, out errorOffset);
        }

        var error = Number(s, start, numberEnd, ref parts, next, out errorOffset);
        if (error != DatatypeError.None)
        {
            return error;
        }

        rank = next;
        i++;
        return DatatypeError.None;
    }

    private static DatatypeError Number(
        ReadOnlySpan<char> s,
        int start,
        int end,
        ref Parts parts,
        int rank,
        out int errorOffset
    )
    {
        errorOffset = start;
        var significant = start;
        while (significant < end - 1 && s[significant] == '0')
        {
            significant++;
        }

        if (end - significant > MaxComponentDigits)
        {
            return DatatypeError.YearOrComponentCap;
        }

        if (rank == 6 && parts.FractionLength > XsdDecimal.DefaultMaxDigits)
        {
            errorOffset = parts.FractionStart + XsdDecimal.DefaultMaxDigits;
            return DatatypeError.DigitCap;
        }

        long value = 0;
        for (var i = significant; i < end; i++)
        {
            value = (value * 10) + (s[i] - '0');
        }

        switch (rank)
        {
            case 1:
                parts.Years = value;
                break;
            case 2:
                parts.Months = value;
                break;
            case 3:
                parts.Days = value;
                break;
            case 4:
                parts.Hours = value;
                break;
            case 5:
                parts.Minutes = value;
                break;
            default:
                parts.Seconds = value;
                break;
        }

        return DatatypeError.None;
    }

    private static int SkipDigits(ReadOnlySpan<char> s, int i)
    {
        while (i < s.Length && s[i] is >= '0' and <= '9')
        {
            i++;
        }

        return i;
    }

    private static int DateRank(char c) =>
        c switch
        {
            'Y' => 1,
            'M' => 2,
            'D' => 3,
            _ => 0,
        };

    private static int TimeRank(char c) =>
        c switch
        {
            'H' => 4,
            'M' => 5,
            'S' => 6,
            _ => 0,
        };

    private static DatatypeError Fail(ReadOnlySpan<char> s, int offset, out int errorOffset)
    {
        errorOffset = offset;
        return offset < s.Length && !IsLexicalCharacter(s[offset])
            ? DatatypeError.InvalidCharacter
            : DatatypeError.InvalidLexicalForm;
    }

    private static bool IsLexicalCharacter(char c) =>
        c is (>= '0' and <= '9') or '.' or '-' or 'P' or 'T' or 'Y' or 'M' or 'D' or 'H' or 'S';
}
