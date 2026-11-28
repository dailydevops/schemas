namespace NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Formatting.Numbers;

/// <summary>
/// Collects the <c>xsl:decimal-format</c> declarations of one schema and merges them (XSLT 2.0 section 16.4.1): the declarations of one
/// name are one decimal format, and each attribute takes its value from the declaration of the highest import precedence that sets it,
/// else the default of the specification. Two values of one attribute at that precedence are the static error XTSE1290. The merged
/// format is checked by the decimal format model of the XPath package, which gives XTSE1295 (a zero digit that is no zero) and XTSE1300
/// (characters that are not distinct); the picture string itself is read by the picture engine of the XPath package at a call.
/// </summary>
internal sealed class XsltDecimalFormatTableBuilder
{
    private readonly List<XPathQName> _order = [];
    private readonly Dictionary<XPathQName, List<Entry>> _byName = [];

    /// <summary>Adds a declaration.</summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="precedence">The import precedence of its module; a higher number wins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="declaration"/> is <see langword="null"/>.</exception>
    internal void Add(XsltDecimalFormatDeclaration declaration, int precedence)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        if (!_byName.TryGetValue(declaration.Name, out var entries))
        {
            entries = [];
            _byName.Add(declaration.Name, entries);
            _order.Add(declaration.Name);
        }

        entries.Add(new Entry(declaration, precedence));
    }

    /// <summary>Merges the declarations added so far and creates the table.</summary>
    /// <param name="problems">The sink that receives NES3200 with <c>XTSE1290</c>, or NES3006 with <c>XTSE1295</c> or <c>XTSE1300</c>.</param>
    /// <param name="table">The table; a format with a problem is left out.</param>
    /// <returns><see langword="false"/> when a problem was reported.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="problems"/> is <see langword="null"/>.</exception>
    internal bool TryBuild(IProblemSink problems, out XsltDecimalFormatTable table)
    {
        ArgumentNullException.ThrowIfNull(problems);

        var formats = new Dictionary<XPathQName, XPathDecimalFormat>(_order.Count);
        var ok = true;
        foreach (var name in _order)
        {
            var entries = _byName[name];
            var format = new XPathDecimalFormat();
            var valid = true;
            for (var attribute = 0; attribute < XsltDecimalFormatDeclaration.Attributes.Count; attribute++)
            {
                valid &= TryMerge(entries, attribute, format, problems);
            }

            if (valid && IsValid(format, entries[0].Declaration, problems))
            {
                formats.Add(name, format);
            }
            else
            {
                ok = false;
            }
        }

        table = new XsltDecimalFormatTable(formats);
        return ok;
    }

    // The value of an attribute is the one of the highest precedence; two values there are XTSE1290, reported once per attribute.
    private static bool TryMerge(List<Entry> entries, int attribute, XPathDecimalFormat format, IProblemSink problems)
    {
        var top = int.MinValue;
        foreach (var entry in entries)
        {
            if (entry.Declaration.Values[attribute] is not null && entry.Precedence > top)
            {
                top = entry.Precedence;
            }
        }

        Entry? first = null;
        foreach (var entry in entries)
        {
            var value = entry.Declaration.Values[attribute];
            if (value is null || entry.Precedence != top)
            {
                continue;
            }

            if (first is not { } winner)
            {
                first = entry;
            }
            else if (!string.Equals(value, winner.Declaration.Values[attribute], StringComparison.Ordinal))
            {
                var problem = new SchematronProblem(
                    SchematronDiagnosticCodes.LetDuplicate,
                    $"The attribute '{XsltDecimalFormatDeclaration.Attributes[attribute]}' of {Describe(entry.Declaration)} has the value '{value}' here and '{winner.Declaration.Values[attribute]}' in the declaration at line {winner.Declaration.Location.Line}, at the same import precedence.",
                    entry.Declaration.Location,
                    "XTSE1290"
                );
                problems.Add(in problem);
                return false;
            }
        }

        if (first is { } source)
        {
            Apply(format, attribute, source.Declaration.Values[attribute]!);
        }

        return true;
    }

    private static void Apply(XPathDecimalFormat format, int attribute, string value)
    {
        switch (attribute)
        {
            case 0:
                format.DecimalSeparator = value[0];
                break;
            case 1:
                format.GroupingSeparator = value[0];
                break;
            case 2:
                format.Percent = value[0];
                break;
            case 3:
                format.PerMille = value[0];
                break;
            case 4:
                format.ZeroDigit = value[0];
                break;
            case 5:
                format.Digit = value[0];
                break;
            case 6:
                format.PatternSeparator = value[0];
                break;
            case 7:
                format.MinusSign = value[0];
                break;
            case 8:
                format.Infinity = value;
                break;
            default:
                format.NaN = value;
                break;
        }
    }

    private static bool IsValid(XPathDecimalFormat format, XsltDecimalFormatDeclaration first, IProblemSink problems)
    {
        if (DecimalFormatModel.TryCreate(format, NumberPictureRules.Xslt20, out _, out var reason))
        {
            return true;
        }

        // The zero digit is the culprit when a format that sets only it fails: the defaults of the others are distinct.
        var zeroDigit = !DecimalFormatModel.TryCreate(
            new XPathDecimalFormat { ZeroDigit = format.ZeroDigit },
            NumberPictureRules.Xslt20,
            out _,
            out _
        );
        var problem = new SchematronProblem(
            SchematronDiagnosticCodes.AttributeInvalid,
            $"{Describe(first)} is not valid: {reason}",
            first.Location,
            zeroDigit ? "XTSE1295" : "XTSE1300"
        );
        problems.Add(in problem);
        return false;
    }

    private static string Describe(XsltDecimalFormatDeclaration declaration) =>
        declaration.DisplayName.Length == 0
            ? "the unnamed decimal format"
            : $"the decimal format '{declaration.DisplayName}'";

    private readonly struct Entry
    {
        internal Entry(XsltDecimalFormatDeclaration declaration, int precedence)
        {
            Declaration = declaration;
            Precedence = precedence;
        }

        internal XsltDecimalFormatDeclaration Declaration { get; }

        internal int Precedence { get; }
    }
}
