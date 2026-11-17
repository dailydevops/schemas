namespace NetEvolve.Schemas.XPath.Evaluation.Atomization;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Atomization (XPath 2.0, 2.4.2) and effective boolean value (2.4.3): the two operations every operator and function shares.</summary>
/// <remarks>
/// Singletons and the empty sequence take allocation-free fast paths. Limitations: a node yields exactly one typed value, so list
/// types that atomize to several items await schema-aware processing (XSD-16); the effective boolean value covers the
/// families the data model carries today, so a type without a rule (for example a date) raises FORG0006.
/// </remarks>
internal static class XPathAtomization
{
    /// <summary>Atomizes a sequence with the untyped default: nodes yield <c>xs:untypedAtomic</c> or <c>xs:string</c> values.</summary>
    /// <param name="sequence">The sequence.</param>
    /// <returns>The atomic sequence; the input itself when it holds no node.</returns>
    public static XdmSequence Atomize(in XdmSequence sequence) => Atomize(sequence, UntypedAnnotationSource.Instance);

    /// <summary>Atomizes a sequence: atomic values pass through, and nodes yield their typed value, so annotated nodes keep their type.</summary>
    /// <param name="sequence">The sequence.</param>
    /// <param name="source">The annotation source that supplies typed values.</param>
    /// <returns>The atomic sequence; the input itself when it holds no node.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static XdmSequence Atomize(in XdmSequence sequence, IXdmTypeAnnotationSource source)
    {
        Argument.ThrowIfNull(source);

        var count = sequence.Count;
        if (count == 0)
        {
            return sequence;
        }

        if (count == 1)
        {
            var only = sequence[0];
            return only.IsNode ? XdmSequence.Create(AtomizeNode(only, source)) : sequence;
        }

        var first = 0;
        while (first < count && !sequence[first].IsNode)
        {
            first++;
        }

        if (first == count)
        {
            return sequence;
        }

        var items = new XdmItem[count];
        for (var i = 0; i < count; i++)
        {
            var item = sequence[i];
            items[i] = item.IsNode ? AtomizeNode(item, source) : item;
        }

        return XdmSequence.Create(items);
    }

    /// <summary>Computes the effective boolean value of a sequence.</summary>
    /// <param name="sequence">The sequence.</param>
    /// <returns>The value: <see langword="false"/> for the empty sequence, <see langword="true"/> when a node comes first, and the rule of the type for a single atomic value.</returns>
    /// <exception cref="XPathException">The operand has no effective boolean value (FORG0006).</exception>
    public static bool EffectiveBooleanValue(in XdmSequence sequence)
    {
        var count = sequence.Count;
        if (count == 0)
        {
            return false;
        }

        var item = sequence[0];
        if (item.IsNode)
        {
            return true;
        }

        if (count > 1)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FORG0006);
        }

        if (item.TryGetBoolean(out var boolean))
        {
            return boolean;
        }

        return item.Kind switch
        {
            XdmItemKind.XsString => item.TryGetString(out var s) && s!.Length > 0,
            XdmItemKind.UntypedAtomic => item.TryGetUntypedAtomic(out var u) && u!.Length > 0,
            XdmItemKind.AnyUri => item.TryGetAnyUri(out var a) && a!.Length > 0,
            XdmItemKind.XsInteger => item.TryGetInteger(out var i) && i != 0,
            XdmItemKind.XsDouble => item.TryGetDouble(out var d) && d != 0 && !double.IsNaN(d),
            XdmItemKind.XsFloat => item.TryGetFloat(out var f) && f != 0 && !float.IsNaN(f),
            XdmItemKind.XsDecimal => item.TryGetDecimal(out var m) && m.Sign != 0,
            _ => throw XPathErrors.Exception(XPathErrorCodes.FORG0006),
        };
    }

    private static XdmItem AtomizeNode(in XdmItem item, IXdmTypeAnnotationSource source)
    {
        _ = item.TryGetNode(out var node);
        return node.TypedValue(source);
    }
}
