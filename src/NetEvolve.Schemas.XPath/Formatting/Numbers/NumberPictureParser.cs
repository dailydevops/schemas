namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using System;
using System.Collections.Generic;

/// <summary>
/// The analysis phase of <c>format-number</c>: it checks a picture string against the rules of one rule set and derives the variables of
/// each sub-picture (XSLT 2.0, 16.4.2 and 16.4.3; Functions and Operators 3.1, 4.7.3 and 4.7.4).
/// </summary>
/// <remarks>
/// <para>
/// The rules differ in these points. XSLT 2.0: only the zero digit is a digit sign, there is no exponent separator, the grouping is regular when
/// the positions are N, 2N, 3N, and so on (one separator counts), a grouping separator at the end of the integer part and two adjacent
/// grouping separators are allowed, and the minimum integer size is one for a picture without a zero digit and a decimal separator. 3.1: every
/// digit of the family is a digit sign, an exponent separator between two active characters starts the exponent, the grouping is regular when
/// every multiple of the size below the number of integer digits carries a separator, the two grouping positions above are errors, and the
/// sizes are adjusted so that a picture without a mandatory digit still shows one.
/// </para>
/// <para>Limitation: a picture with an exponent separator but without a decimal separator and without a mandatory digit in the mantissa (<c>#e0</c>) gets the adjusted fraction size of 4.7.4, so the formatter writes the decimal separator before the one fraction digit although the picture names none; the literal text would fuse the fraction digit to the integer digits.</para>
/// </remarks>
internal static class NumberPictureParser
{
    private const byte Passive = 0;
    private const byte DecimalSign = 1;
    private const byte Grouping = 2;
    private const byte OptionalDigit = 3;
    private const byte MandatoryDigit = 4;
    private const byte ExponentCandidate = 5;
    private const byte ExponentSign = 6;
    private const byte ExponentLiteral = 7;
    private const int StackLimit = 256;

    /// <summary>Analyzes a picture string.</summary>
    /// <param name="picture">The picture string.</param>
    /// <param name="format">The decimal format that gives the meaning of the characters.</param>
    /// <param name="rules">The rule set.</param>
    /// <param name="result">The analyzed picture, <see langword="null"/> when the picture breaks a rule.</param>
    /// <param name="problem">The rule that the picture breaks, <see langword="null"/> when it is valid.</param>
    /// <returns><see langword="true"/> when the picture is valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="picture"/> or <paramref name="format"/> is <see langword="null"/>.</exception>
    public static bool TryParse(
        string picture,
        DecimalFormatModel format,
        NumberPictureRules rules,
        out NumberPicture? result,
        out string? problem
    )
    {
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(format);

        result = null;
        var separator = picture.AsSpan().IndexOf(format.PatternSeparator);
        if (separator < 0)
        {
            if (!TryParseSub(picture.AsSpan(), format, rules, out var only, out problem))
            {
                return false;
            }

            result = new NumberPicture(only!, only!.WithMinusSign(format.MinusSign));
            return true;
        }

        if (picture.AsSpan(separator + 1).IndexOf(format.PatternSeparator) >= 0)
        {
            problem = "A picture string must not contain more than one pattern separator.";
            return false;
        }

        if (
            !TryParseSub(picture.AsSpan(0, separator), format, rules, out var positive, out problem)
            || !TryParseSub(picture.AsSpan(separator + 1), format, rules, out var negative, out problem)
        )
        {
            return false;
        }

        result = new NumberPicture(positive!, negative!);
        return true;
    }

    private static bool TryParseSub(
        ReadOnlySpan<char> text,
        DecimalFormatModel format,
        NumberPictureRules rules,
        out SubPicture? sub,
        out string? problem
    )
    {
        sub = null;
        var xpath31 = rules == NumberPictureRules.Xpath31;
        scoped Span<byte> kinds;
        kinds = text.Length <= StackLimit ? stackalloc byte[text.Length] : new byte[text.Length];
        Classify(text, kinds, format, xpath31);
        var exponentCount = xpath31 ? ResolveExponentSeparators(kinds) : 0;
        if (!TryFindActiveRange(kinds, exponentCount, out var first, out var last, out problem))
        {
            return false;
        }

        // Everything outside the active range is the prefix and the suffix.
        var prefix = text.Slice(0, first).ToString();
        var suffix = text.Slice(last + 1).ToString();
        if (!TryReadScale(prefix, suffix, format, out var multiplier, out problem))
        {
            return false;
        }

        var exponentIndex = exponentCount == 1 ? kinds.IndexOf(ExponentSign) : -1;
        var minimumExponentSize = 0;
        if (
            exponentIndex >= 0
            && !TryReadExponent(kinds, exponentIndex, last, multiplier, out minimumExponentSize, out problem)
        )
        {
            return false;
        }

        var mantissaEnd = exponentIndex >= 0 ? exponentIndex : last + 1;
        if (
            !TryFindDecimalSeparator(kinds, first, mantissaEnd, out var decimalIndex, out problem)
            || !CheckMantissa(kinds, first, mantissaEnd, decimalIndex, xpath31, out problem)
        )
        {
            return false;
        }

        sub = Analyze(
            kinds,
            first,
            mantissaEnd,
            decimalIndex,
            minimumExponentSize,
            prefix,
            suffix,
            multiplier,
            xpath31
        );
        problem = null;
        return true;
    }

    // The first and the last active character; every character between them has to be active too.
    private static bool TryFindActiveRange(
        ReadOnlySpan<byte> kinds,
        int exponentCount,
        out int first,
        out int last,
        out string? problem
    )
    {
        first = -1;
        last = -1;
        problem = null;
        if (exponentCount > 1)
        {
            problem = "A sub-picture must not contain more than one exponent separator.";
            return false;
        }

        for (var i = 0; i < kinds.Length; i++)
        {
            if (IsActive(kinds[i]))
            {
                first = first < 0 ? i : first;
                last = i;
            }
        }

        if (first < 0)
        {
            problem = "A sub-picture must contain at least one digit sign.";
            return false;
        }

        for (var i = first + 1; i < last; i++)
        {
            if (!IsActive(kinds[i]))
            {
                problem = "A sub-picture must not contain a passive character between two active characters.";
                return false;
            }
        }

        return true;
    }

    private static bool TryFindDecimalSeparator(
        ReadOnlySpan<byte> kinds,
        int first,
        int mantissaEnd,
        out int decimalIndex,
        out string? problem
    )
    {
        decimalIndex = -1;
        problem = null;
        for (var i = first; i < mantissaEnd; i++)
        {
            if (kinds[i] != DecimalSign)
            {
                continue;
            }

            if (decimalIndex >= 0)
            {
                problem = "A sub-picture must not contain more than one decimal separator.";
                return false;
            }

            decimalIndex = i;
        }

        return true;
    }

    private static void Classify(ReadOnlySpan<char> text, Span<byte> kinds, DecimalFormatModel format, bool xpath31)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == format.DecimalSeparator)
            {
                kinds[i] = DecimalSign;
            }
            else if (c == format.GroupingSeparator)
            {
                kinds[i] = Grouping;
            }
            else if (c == format.Digit)
            {
                kinds[i] = OptionalDigit;
            }
            else if (xpath31 ? c >= format.ZeroDigit && c < format.ZeroDigit + 10 : c == format.ZeroDigit)
            {
                kinds[i] = MandatoryDigit;
            }
            else if (xpath31 && c == format.ExponentSeparator)
            {
                kinds[i] = ExponentCandidate;
            }
            else
            {
                kinds[i] = Passive;
            }
        }
    }

    // A character of the exponent separator property is the exponent separator when an active character precedes and follows it (4.7.3);
    // otherwise it is passive. A character of that property counts as active for the neighbors, whatever it turns out to be.
    private static int ResolveExponentSeparators(Span<byte> kinds)
    {
        var count = 0;
        for (var i = 0; i < kinds.Length; i++)
        {
            if (kinds[i] != ExponentCandidate)
            {
                continue;
            }

            var separator = i > 0 && kinds[i - 1] != Passive && i + 1 < kinds.Length && kinds[i + 1] != Passive;
            kinds[i] = separator ? ExponentSign : ExponentLiteral;
            count += separator ? 1 : 0;
        }

        return count;
    }

    private static bool IsActive(byte kind) =>
        kind is DecimalSign or Grouping or OptionalDigit or MandatoryDigit or ExponentSign;

    private static bool TryReadScale(
        string prefix,
        string suffix,
        DecimalFormatModel format,
        out int multiplier,
        out string? problem
    )
    {
        var percent = Count(prefix, format.Percent) + Count(suffix, format.Percent);
        var perMille = Count(prefix, format.PerMille) + Count(suffix, format.PerMille);
        multiplier =
            percent > 0 ? 100
            : perMille > 0 ? 1000
            : 1;
        if (percent > 1 || perMille > 1 || (percent > 0 && perMille > 0))
        {
            problem = "A sub-picture must not contain more than one percent or per-mille sign, nor one of each.";
            return false;
        }

        problem = null;
        return true;
    }

    private static int Count(string text, char c)
    {
        var count = 0;
        for (var i = 0; i < text.Length; i++)
        {
            count += text[i] == c ? 1 : 0;
        }

        return count;
    }

    // The exponent part holds digits of the family only, at least one, and a percent or per-mille sign excludes it.
    private static bool TryReadExponent(
        ReadOnlySpan<byte> kinds,
        int exponentIndex,
        int last,
        int multiplier,
        out int minimumExponentSize,
        out string? problem
    )
    {
        minimumExponentSize = last - exponentIndex;
        if (multiplier != 1)
        {
            problem = "A sub-picture with a percent or per-mille sign must not contain an exponent separator.";
            return false;
        }

        if (minimumExponentSize < 1)
        {
            problem = "The exponent separator must be followed by one or more digits.";
            return false;
        }

        for (var i = exponentIndex + 1; i <= last; i++)
        {
            if (kinds[i] != MandatoryDigit)
            {
                problem = "Only digits of the digit family may follow the exponent separator.";
                return false;
            }
        }

        problem = null;
        return true;
    }

    private static bool CheckMantissa(
        ReadOnlySpan<byte> kinds,
        int first,
        int mantissaEnd,
        int decimalIndex,
        bool xpath31,
        out string? problem
    )
    {
        var digits = 0;
        for (var i = first; i < mantissaEnd; i++)
        {
            digits += kinds[i] is OptionalDigit or MandatoryDigit ? 1 : 0;
        }

        if (digits == 0)
        {
            problem = "A sub-picture must contain at least one digit sign.";
            return false;
        }

        if (
            decimalIndex >= 0
            && (
                (decimalIndex > first && kinds[decimalIndex - 1] == Grouping)
                || (decimalIndex + 1 < mantissaEnd && kinds[decimalIndex + 1] == Grouping)
            )
        )
        {
            problem = "A sub-picture must not contain a grouping separator next to the decimal separator.";
            return false;
        }

        if (xpath31)
        {
            if (decimalIndex < 0 && kinds[mantissaEnd - 1] == Grouping)
            {
                problem =
                    "A sub-picture without a decimal separator must not end its integer part with a grouping separator.";
                return false;
            }

            for (var i = first + 1; i < mantissaEnd; i++)
            {
                if (kinds[i] == Grouping && kinds[i - 1] == Grouping)
                {
                    problem = "A sub-picture must not contain two adjacent grouping separators.";
                    return false;
                }
            }
        }

        // The integer part must not have a mandatory digit before an optional one; the fraction part must not have an optional digit before a mandatory one.
        var integerEnd = decimalIndex >= 0 ? decimalIndex : mantissaEnd;
        if (HasOrder(kinds, first, integerEnd, MandatoryDigit, OptionalDigit))
        {
            problem = "The integer part of a sub-picture must not contain a zero digit before an optional digit.";
            return false;
        }

        if (decimalIndex >= 0 && HasOrder(kinds, decimalIndex + 1, mantissaEnd, OptionalDigit, MandatoryDigit))
        {
            problem = "The fractional part of a sub-picture must not contain an optional digit before a zero digit.";
            return false;
        }

        problem = null;
        return true;
    }

    private static bool HasOrder(ReadOnlySpan<byte> kinds, int start, int end, byte earlier, byte later)
    {
        var seen = false;
        for (var i = start; i < end; i++)
        {
            seen |= kinds[i] == earlier;
            if (seen && kinds[i] == later)
            {
                return true;
            }
        }

        return false;
    }

    private static SubPicture Analyze(
        ReadOnlySpan<byte> kinds,
        int first,
        int mantissaEnd,
        int decimalIndex,
        int minimumExponentSize,
        string prefix,
        string suffix,
        int multiplier,
        bool xpath31
    )
    {
        var integerEnd = decimalIndex >= 0 ? decimalIndex : mantissaEnd;

        // The integer part: the digits, the mandatory and optional ones among them, and for each grouping separator the digits to its right.
        var integerDigits = 0;
        var integerMandatory = 0;
        var integerOptional = 0;
        for (var i = first; i < integerEnd; i++)
        {
            integerDigits += kinds[i] is OptionalDigit or MandatoryDigit ? 1 : 0;
            integerMandatory += kinds[i] == MandatoryDigit ? 1 : 0;
            integerOptional += kinds[i] == OptionalDigit ? 1 : 0;
        }

        var positions = IntegerPositions(kinds, first, integerEnd, integerDigits);
        var groupingSize = ResolveGrouping(positions, integerDigits, xpath31);

        // The fraction part: for each grouping separator the digits to its left.
        var fractionStart = decimalIndex < 0 ? mantissaEnd : decimalIndex + 1;
        var fractionPositions = new List<int>();
        var fractionDigits = 0;
        var fractionMandatory = 0;
        for (var i = fractionStart; i < mantissaEnd; i++)
        {
            if (kinds[i] == Grouping)
            {
                fractionPositions.Add(fractionDigits);
            }
            else if (kinds[i] is OptionalDigit or MandatoryDigit)
            {
                fractionDigits++;
                fractionMandatory += kinds[i] == MandatoryDigit ? 1 : 0;
            }
        }

        var (minimumInteger, minimumFraction, maximumFraction) = xpath31
            ? AdjustSizes31(integerMandatory, integerOptional, fractionMandatory, fractionDigits, minimumExponentSize)
            : (
                decimalIndex < 0 && Mandatory(kinds, first, mantissaEnd) == 0 ? 1 : integerMandatory,
                fractionMandatory,
                fractionDigits
            );

        return new SubPicture(
            prefix,
            suffix,
            multiplier,
            minimumInteger,
            minimumFraction,
            maximumFraction,
            integerMandatory,
            minimumExponentSize,
            groupingSize,
            groupingSize > 0 ? [] : positions.FindAll(static p => p > 0).ToArray(),
            fractionPositions.FindAll(static p => p > 0).ToArray()
        );
    }

    // The positions of the grouping separators of the integer part: the number of digits to the right of each.
    private static List<int> IntegerPositions(ReadOnlySpan<byte> kinds, int first, int integerEnd, int integerDigits)
    {
        var positions = new List<int>();
        var digitsSeen = 0;
        for (var i = first; i < integerEnd; i++)
        {
            if (kinds[i] == Grouping)
            {
                positions.Add(integerDigits - digitsSeen);
            }
            else if (kinds[i] is OptionalDigit or MandatoryDigit)
            {
                digitsSeen++;
            }
        }

        return positions;
    }

    // 3.1, 4.7.4: the sizes are adjusted so that a picture without a mandatory digit still shows one.
    private static (int MinimumInteger, int MinimumFraction, int MaximumFraction) AdjustSizes31(
        int integerMandatory,
        int integerOptional,
        int fractionMandatory,
        int fractionDigits,
        int minimumExponentSize
    )
    {
        var minimumInteger = integerMandatory;
        var minimumFraction = fractionMandatory;
        var maximumFraction = fractionDigits;
        if (minimumInteger == 0 && maximumFraction == 0)
        {
            if (minimumExponentSize > 0)
            {
                minimumFraction = 1;
                maximumFraction = 1;
            }
            else
            {
                minimumInteger = 1;
            }
        }

        if (minimumExponentSize > 0 && minimumInteger == 0 && integerOptional > 0)
        {
            minimumInteger = 1;
        }

        if (minimumInteger == 0 && minimumFraction == 0)
        {
            minimumFraction = 1;
        }

        return (minimumInteger, minimumFraction, maximumFraction);
    }

    private static int Mandatory(ReadOnlySpan<byte> kinds, int start, int end)
    {
        var count = 0;
        for (var i = start; i < end; i++)
        {
            count += kinds[i] == MandatoryDigit ? 1 : 0;
        }

        return count;
    }

    // Returns the size of a regular grouping, which extends to every multiple, or zero for explicit positions.
    private static int ResolveGrouping(List<int> positions, int integerDigits, bool xpath31)
    {
        var positive = positions.FindAll(static p => p > 0);
        if (positive.Count == 0)
        {
            return 0;
        }

        positive.Sort();
        if (!xpath31)
        {
            // XSLT 2.0: the positions are N, 2N, 3N, and so on; a single position counts.
            var size = positive[0];
            for (var i = 0; i < positive.Count; i++)
            {
                if (positive[i] != (i + 1) * size)
                {
                    return 0;
                }
            }

            return size;
        }

        // 3.1: the greatest size of which every position is a multiple, and every multiple between two digits of the integer part holds a
        // position: a multiple at the number of digits would be a separator before the first digit, which a regular grouping does not need
        // (the QT3 cases numberformat312 and numberformat320: ##,## and ,##0 are regular).
        var gcd = 0;
        for (var i = 0; i < positive.Count; i++)
        {
            gcd = Gcd(gcd, positive[i]);
        }

        for (var multiple = gcd; multiple < integerDigits; multiple += gcd)
        {
            if (!positive.Contains(multiple))
            {
                return 0;
            }
        }

        return gcd;
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }
}
