namespace NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// The compiled form of an <c>xsl:decimal-format</c> declaration (XSLT 2.0 section 16.4.1): the name and the attributes that the element
/// sets, kept as written. It does nothing when executed, because a declaration produces no output; the table builder merges the
/// declarations of one name.
/// </summary>
internal sealed class XsltDecimalFormatDeclaration : XsltExecutable
{
    /// <summary>The index of the first attribute that is a string; the ones before it are single characters.</summary>
    internal const int FirstStringAttribute = 8;

    /// <summary>Gets the attribute names; <see cref="Values"/> has one entry for each, in this order.</summary>
    internal static IReadOnlyList<string> Attributes { get; } =
    [
        "decimal-separator",
        "grouping-separator",
        "percent",
        "per-mille",
        "zero-digit",
        "digit",
        "pattern-separator",
        "minus-sign",
        "infinity",
        "NaN",
    ];

    internal XsltDecimalFormatDeclaration(
        XPathQName name,
        string displayName,
        IReadOnlyList<string?> values,
        DocumentLocation location
    )
    {
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(values);

        Name = name;
        DisplayName = displayName;
        Values = values;
        Location = location;
    }

    /// <summary>Gets the expanded name; <see langword="default"/> for the unnamed format, an unprefixed name has no namespace.</summary>
    internal XPathQName Name { get; }

    /// <summary>Gets the name as written, for messages; empty for the unnamed format.</summary>
    internal string DisplayName { get; }

    /// <summary>Gets the value of each attribute as written, in the order of <see cref="Attributes"/>; <see langword="null"/> for an attribute the element leaves out.</summary>
    internal IReadOnlyList<string?> Values { get; }

    /// <summary>Gets the location of the <c>xsl:decimal-format</c> element.</summary>
    internal DocumentLocation Location { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(sink);

        return QueryStatus.Ok;
    }
}
