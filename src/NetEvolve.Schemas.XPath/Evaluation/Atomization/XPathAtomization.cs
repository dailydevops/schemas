namespace NetEvolve.Schemas.XPath.Evaluation.Atomization;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Atomization (XPath 2.0, 2.4.2) and effective boolean value (2.4.3): the two operations every operator and function shares.</summary>
/// <remarks>
/// A function item has no typed value (FOTY0013) and no effective boolean value (FORG0006). Singletons and the empty sequence take allocation-free fast paths. Limitations: a node yields exactly one typed value, so list
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
        ArgumentNullException.ThrowIfNull(source);

        var count = sequence.Count;
        if (count == 0)
        {
            return sequence;
        }

        if (count == 1)
        {
            var only = sequence[0];
            ThrowIfFunction(only);
            return only.IsNode ? XdmSequence.Create(AtomizeNode(only, source)) : sequence;
        }

        // Every item is checked, because a function item has no typed value wherever it stands in the sequence.
        var first = count;
        for (var i = 0; i < count; i++)
        {
            var item = sequence[i];
            ThrowIfFunction(item);
            if (first == count && item.IsNode)
            {
                first = i;
            }
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

        // Each family is tried in turn; a failed accessor falls through to the next one.
        if (item.TryGetBoolean(out var boolean))
        {
            return boolean;
        }

        if (item.TryGetString(out var text))
        {
            return text!.Length > 0;
        }

        if (item.TryGetUntypedAtomic(out var untyped))
        {
            return untyped!.Length > 0;
        }

        if (item.TryGetAnyUri(out var uri))
        {
            return uri!.Length > 0;
        }

        if (item.TryGetInteger(out var integer))
        {
            return integer != 0;
        }

        if (item.TryGetDouble(out var number))
        {
            return number != 0 && !double.IsNaN(number);
        }

        if (item.TryGetFloat(out var single))
        {
            return single != 0 && !float.IsNaN(single);
        }

        if (item.TryGetDecimal(out var dec))
        {
            return dec.Sign != 0;
        }

        throw XPathErrors.Exception(XPathErrorCodes.FORG0006);
    }

    // XPath 3.0, 2.4.2: a function item has no typed value, so atomizing one is an error.
    private static void ThrowIfFunction(in XdmItem item)
    {
        if (item.Kind == XdmItemKind.Function)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FOTY0013);
        }
    }

    private static XdmItem AtomizeNode(in XdmItem item, IXdmTypeAnnotationSource source)
    {
        _ = item.TryGetNode(out var node);
        return node.TypedValue(source);
    }
}
