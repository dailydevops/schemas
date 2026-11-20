namespace NetEvolve.Schemas.Schematron.Xslt.Keys;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// A compiled <c>xsl:key</c> declaration (XSLT 2.0 section 16.3): the expanded name, the match pattern that the shared pattern
/// matcher compiled, and the key value as a <see langword="use"/> expression or as a sequence constructor. It is immutable and holds
/// no run state, so one component serves every run. Exactly one of <see cref="Use"/> and <see cref="Body"/> is set.
/// </summary>
internal sealed class XsltKeyComponent
{
    private readonly int[] _referencedSlots;

    internal XsltKeyComponent(
        XPathQName name,
        string displayName,
        CompiledPattern match,
        QueryExpression? use,
        XsltBodyExecutable? body,
        DocumentLocation location
    )
    {
        Argument.ThrowIfNull(displayName);
        Argument.ThrowIfNull(match);

        Name = name;
        DisplayName = displayName;
        Match = match;
        Use = use;
        Body = body;
        Location = location;
        _referencedSlots = new SlotSet().Add(match.ReferencedSlots).Add(use).Add(body).ToArray();
    }

    /// <summary>Gets the expanded name; an unprefixed name has no namespace.</summary>
    internal XPathQName Name { get; }

    /// <summary>Gets the name as written, for messages.</summary>
    internal string DisplayName { get; }

    /// <summary>Gets the match pattern.</summary>
    internal CompiledPattern Match { get; }

    /// <summary>Gets the <see langword="use"/> expression, or <see langword="null"/> when the key value is the content.</summary>
    internal QueryExpression? Use { get; }

    /// <summary>Gets the sequence constructor that is the key value, or <see langword="null"/> when there is a <see langword="use"/> expression.</summary>
    internal XsltBodyExecutable? Body { get; }

    /// <summary>Gets the location of the <c>xsl:key</c> element.</summary>
    internal DocumentLocation Location { get; }

    /// <summary>Gets the variable slots that the match pattern and the key value read, which the index build binds first.</summary>
    internal ReadOnlySpan<int> ReferencedSlots => _referencedSlots;
}
