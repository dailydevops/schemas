namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;

/// <summary>
/// <c>xsl:namespace</c>: evaluates the prefix (an NCName other than <c>xmlns</c>, or empty; XTDE0920) and the namespace URI (not empty,
/// XTDE0930; not the xmlns namespace, XTDE0905; the xml prefix and the xml namespace only together, XTDE0925), and adds the namespace
/// node to the content of the element (XSLT 2.0 section 11.7).
/// </summary>
internal sealed class XsltNamespaceExecutable : XsltExecutable, IXsltSlotReader, IXsltLocalSlots
{
    private readonly AttributeValueTemplate _name;
    private readonly QueryExpression? _select;
    private readonly XsltBodyExecutable? _body;
    private readonly DocumentLocation _location;
    private readonly int[] _slots;

    internal XsltNamespaceExecutable(
        AttributeValueTemplate name,
        QueryExpression? select,
        XsltBodyExecutable? body,
        DocumentLocation location,
        int slotEnd
    )
    {
        Argument.ThrowIfNull(name);

        _name = name;
        _select = select;
        _body = body;
        _location = location;
        SlotEnd = slotEnd;
        _slots = new SlotSet().Add(name.ReferencedSlots).Add(select).Add(body).ToArray();
    }

    /// <inheritdoc />
    public ReadOnlySpan<int> ReferencedSlots => _slots;

    /// <inheritdoc />
    public int SlotEnd { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        if (_name.TryEvaluate(frame, _location, out var written) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        var prefix = written.Trim();
        if (
            prefix.Length > 0
            && (
                !XsltElementReader.TryParseQName(prefix, out var qualifier, out _)
                || qualifier is not null
                || string.Equals(prefix, "xmlns", StringComparison.Ordinal)
            )
        )
        {
            return ConstructionOutput.Error(
                frame,
                _location,
                "XTDE0920",
                $"The name '{prefix}' of a namespace node is neither empty nor an NCName other than xmlns."
            );
        }

        if (SimpleContent.TryEvaluate(frame, _select, _body, " ", _location, out var uri) == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        var invalid = Validate(prefix, uri);
        return invalid is not null
            ? ConstructionOutput.Error(frame, _location, invalid.Value.Code, invalid.Value.Message)
            : ConstructionOutput.PushNamespace(frame, sink, new NamespaceBinding(prefix, uri), _location);
    }

    private static (string Code, string Message)? Validate(string prefix, string uri)
    {
        if (uri.Length == 0)
        {
            return ("XTDE0930", "The value of a namespace node is the empty string.");
        }

        if (string.Equals(uri, ContentWriter.XmlnsNamespace, StringComparison.Ordinal))
        {
            return ("XTDE0905", "The value of a namespace node is the xmlns namespace.");
        }

        return
            string.Equals(prefix, "xml", StringComparison.Ordinal)
            != string.Equals(uri, ContentWriter.XmlNamespace, StringComparison.Ordinal)
            ? ("XTDE0925", "The xml prefix and the xml namespace belong together and to nothing else.")
            : null;
    }
}
