namespace NetEvolve.Schemas.XPath.Types;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A sequence type bound against a static context (XPath 2.0, 2.5.3) with the matching rules of 2.5.4. One matcher serves
/// <c>instance of</c>, <c>treat as</c>, and later casting, function conversion, and typed variables. Immutable and thread-safe;
/// matching does not allocate per item.
/// </summary>
/// <remarks>See <see cref="ItemTypeMatcher"/> for the limitations of the item types.</remarks>
internal sealed class SequenceTypeMatcher
{
    private readonly ItemTypeMatcher _item;
    private readonly Occurrence _occurrence;
    private readonly bool _emptySequence;

    private SequenceTypeMatcher(ItemTypeMatcher item, Occurrence occurrence, bool emptySequence)
    {
        _item = item;
        _occurrence = occurrence;
        _emptySequence = emptySequence;
    }

    /// <summary>Binds a sequence type.</summary>
    /// <param name="syntax">The syntax of the sequence type; no item type stands for <c>empty-sequence()</c>.</param>
    /// <param name="context">The static context that resolves prefixes and the default namespace.</param>
    /// <returns>The matcher.</returns>
    /// <exception cref="XPathException">A prefix is not bound (XPST0081), a type name is not a built-in type (XPST0051), or a schema declaration is named (XPST0008).</exception>
    public static SequenceTypeMatcher Create(SequenceType syntax, XPathStaticContext context)
    {
        Argument.ThrowIfNull(syntax);
        Argument.ThrowIfNull(context);

        return syntax.ItemType is null
            ? new(default, Occurrence.ExactlyOne, emptySequence: true)
            : new(ItemTypeMatcher.Bind(syntax.ItemType, context), syntax.Occurrence, emptySequence: false);
    }

    /// <summary>Determines whether a value matches; nodes are untyped.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when the value matches the sequence type.</returns>
    public bool Matches(in XdmSequence value) => Matches(value, UntypedAnnotationSource.Instance);

    /// <summary>Determines whether a value matches.</summary>
    /// <param name="value">The value.</param>
    /// <param name="annotations">The slot that supplies the type annotation of a node.</param>
    /// <returns><see langword="true"/> when the value matches the sequence type.</returns>
    public bool Matches(in XdmSequence value, IXdmTypeAnnotationSource annotations)
    {
        Argument.ThrowIfNull(annotations);

        var count = value.Count;
        if (_emptySequence)
        {
            return count == 0;
        }

        var countMatches = _occurrence switch
        {
            Occurrence.ExactlyOne => count == 1,
            Occurrence.ZeroOrOne => count <= 1,
            Occurrence.OneOrMore => count >= 1,
            _ => true,
        };
        if (!countMatches)
        {
            return false;
        }

        if (count == 0 || _item.IsAnyItem)
        {
            return true;
        }

        // The name test is bound once per tree, not per item.
        XmlTree? tree = null;
        XPathNodeMatcher bound = default;
        foreach (var item in value)
        {
            if (!_item.Matches(item, annotations, ref tree, ref bound))
            {
                return false;
            }
        }

        return true;
    }
}
