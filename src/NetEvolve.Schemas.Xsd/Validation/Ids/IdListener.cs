namespace NetEvolve.Schemas.Xsd.Validation.Ids;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Dtd.Validation.Identity;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The listener of the ID, IDREF, and NOTATION checks (XSD-14, #303; design #146 DP-11): ID uniqueness (<c>cvc-id</c>, clause 2),
/// IDREF and IDREFS resolution (<c>cvc-id</c>, clause 1), and NOTATION values (<c>cvc-datatype-valid</c>).
/// </summary>
/// <remarks>
/// <para>
/// A value takes part when its simple type is the built-in <c>ID</c>, <c>IDREF</c>, <c>IDREFS</c>, or <c>NOTATION</c>, or derives
/// from one of them by restriction, or when it is an item of a list of them, or when it is accepted by a union member of them (the
/// first member in declaration order that accepts the value decides). Only valid values count, because the datatype check has
/// reported the others. A duplicate ID is reported at the second and at every further occurrence. A reference to an ID that is
/// already known is resolved at once; a reference to an unknown name waits for the end of the run, because a later ID may match
/// it, and is then reported at its own location, one issue per occurrence in document order. The listener holds the IDs in a
/// pooled table and the waiting references in a pooled buffer, so a document without ID-typed values allocates nothing.
/// </para>
/// <para>
/// Limitations: memory grows with the number of IDs and unresolved references, bounded only by the size of the input. The value of
/// an ID, IDREF, or NOTATION is collapsed (leading and trailing white space is dropped) before it is used. An ID that comes from
/// a default or fixed value is not registered: a default on an ID type is a schema error (#692), and one mistake must not repeat
/// as a duplicate on every element. A NOTATION value is resolved without a namespace context (#604): an unprefixed name is looked
/// up in no namespace, which is where the compatibility rule of the datatypes specification places notations, and a value with a
/// prefix is not assessed. No builder creates notation declaration components yet (#751), so a compiled schema holds no
/// notation and every NOTATION value reports as naming no declared notation until it exists. An element or attribute that the
/// validator does not hand to the listeners (a skipped or rejected subtree) takes no part, and a violation found here does not
/// mark the element invalid (#605). Several ID attributes on one element (XSD 1.1, XSD-71), ENTITY values (XSD-83), and ID typing of
/// <c>xml:id</c> (#79) are not handled.
/// </para>
/// <para>Not thread-safe; one instance serves one run at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class IdListener : ValidationListener
{
    private const int MaxReportedValue = 64;

    private static readonly XsdRule IdRule = XsdRuleLookup.Find(XsdRuleCodes.CvcId);
    private static readonly XsdRule DatatypeRule = XsdRuleLookup.Find(XsdRuleCodes.CvcDatatypeValid);

    private readonly ISimpleValueAssessor _assessor;
    private readonly XsdSchemaSet _schema;
    private readonly DtdIdTable _ids = new();
    private readonly DtdNameStore _pendingNames = new();
    private Pending[] _pending = new Pending[16];
    private int _pendingCount;
    private XsdIssueReporter? _reporter;
    private int _startOrdinal;
    private DocumentLocation _startLocation;

    /// <summary>Initializes a new instance of the <see cref="IdListener"/> class.</summary>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="schema">The schema set that holds the notation declarations.</param>
    internal IdListener(ISimpleValueAssessor assessor, XsdSchemaSet schema)
    {
        ArgumentNullException.ThrowIfNull(assessor);
        ArgumentNullException.ThrowIfNull(schema);

        _assessor = assessor;
        _schema = schema;
    }

    /// <inheritdoc />
    public override ListenerInterest Interest =>
        ListenerInterest.StartElement | ListenerInterest.Attribute | ListenerInterest.SimpleValue;

    /// <inheritdoc />
    public override void BeginRun(in ValidationRunContext context)
    {
        _reporter = context.Reporter;
        _ids.Clear();
        _pendingNames.Clear();
        _pendingCount = 0;
        _startOrdinal = 0;
        _startLocation = default;
    }

    /// <inheritdoc />
    public override void StartElement(in ElementStartInfo info)
    {
        // The simple value of an element carries no location; its element is the last one that started, because the
        // children of an element with simple content are rejected and never reach a listener.
        _startOrdinal = info.Ordinal;
        _startLocation = info.Location;
    }

    /// <inheritdoc />
    public override void Attribute(in AttributeInfo info)
    {
        if (_reporter is not null && info.IsValid && info.Type is not null)
        {
            Visit(info.Type, info.Value, info.Location, info.IsDefaulted);
        }
    }

    /// <inheritdoc />
    public override void SimpleValue(in SimpleValueInfo info)
    {
        if (_reporter is not null && info.IsValid && info.Type is not null)
        {
            var location = info.Ordinal == _startOrdinal ? _startLocation : default;
            Visit(info.Type, info.Value, location, info.IsDefaulted);
        }
    }

    /// <inheritdoc />
    public override void EndRun()
    {
        var count = _pendingCount;
        _pendingCount = 0;
        for (var i = 0; i < count; i++)
        {
            var pending = _pending[i];
            var token = _pendingNames.Get(pending.Start, pending.Length);
            if (!_ids.Contains(token))
            {
                _reporter!.Report(
                    IdRule,
                    pending.Location,
                    "There is no ID with the value '" + Shorten(token) + "' for this reference.",
                    "cvc-id.1"
                );
            }
        }
    }

    // Follows the restriction chain, the items of a list, and the accepting member of a union down to a type of this check.
    private void Visit(IXsdComponent? type, ReadOnlySpan<char> value, DocumentLocation location, bool defaulted)
    {
        while (type is not null)
        {
            switch (type)
            {
                case XsdBuiltInType builtIn:
                    Apply(KindOf(builtIn.Name), value, location, defaulted);
                    return;
                case SimpleTypeDefinition definition:
                    var kind = KindOf(definition.Name);
                    if (kind != IdKind.None)
                    {
                        Apply(kind, value, location, defaulted);
                        return;
                    }

                    switch (definition.Variety)
                    {
                        case DatatypeVariety.List:
                            VisitItems(definition.ItemType, value, location, defaulted);
                            return;
                        case DatatypeVariety.Union:
                            VisitMember(definition, value, location, defaulted);
                            return;
                        default:
                            type = definition.BaseType;
                            break;
                    }

                    break;
                default:
                    return;
            }
        }
    }

    private void VisitItems(
        IXsdComponent? itemType,
        ReadOnlySpan<char> value,
        DocumentLocation location,
        bool defaulted
    )
    {
        var cursor = new XsdListCursor(new TextRange(0, value.Length));
        while (cursor.TryNext(value, out var item))
        {
            Visit(itemType, value.Slice(item.Offset, item.Length), location, defaulted);
        }
    }

    private void VisitMember(
        SimpleTypeDefinition union,
        ReadOnlySpan<char> value,
        DocumentLocation location,
        bool defaulted
    )
    {
        for (var i = 0; i < union.MemberTypes.Count; i++)
        {
            if (_assessor.Assess(union.MemberTypes[i], value, out _).IsValid)
            {
                Visit(union.MemberTypes[i], value, location, defaulted);
                return;
            }
        }
    }

    private void Apply(IdKind kind, ReadOnlySpan<char> value, DocumentLocation location, bool defaulted)
    {
        if (kind == IdKind.None)
        {
            return;
        }

        var cursor = new XsdListCursor(new TextRange(0, value.Length));
        while (cursor.TryNext(value, out var item))
        {
            var token = value.Slice(item.Offset, item.Length);
            switch (kind)
            {
                case IdKind.Id:
                    Register(token, location, defaulted);
                    return;
                case IdKind.Notation:
                    CheckNotation(token, location);
                    return;
                default:
                    // IDREF is one token and IDREFS is a list of them; a valid IDREF value has no second token.
                    Resolve(token, location);
                    break;
            }
        }
    }

    private void Register(ReadOnlySpan<char> token, DocumentLocation location, bool defaulted)
    {
        if (!defaulted && _ids.TryAdd(token, int.MaxValue) == DtdIdAddResult.Duplicate)
        {
            _reporter!.Report(
                IdRule,
                location,
                "The value '" + Shorten(token) + "' is already the ID of another element or attribute.",
                "cvc-id.2"
            );
        }
    }

    // A reference to a known ID is done; any other name may still match a later ID and waits for the end of the run.
    private void Resolve(ReadOnlySpan<char> token, DocumentLocation location)
    {
        if (_ids.Contains(token))
        {
            return;
        }

        if (_pendingCount == _pending.Length)
        {
            Array.Resize(ref _pending, _pending.Length * 2);
        }

        _pending[_pendingCount++] = new Pending(_pendingNames.Append(token), token.Length, location);
    }

    private void CheckNotation(ReadOnlySpan<char> token, DocumentLocation location)
    {
        if (token.IndexOf(':') >= 0)
        {
            return;
        }

        if (!_schema.TryGet(XsdComponentKind.Notation, new XPathQName(string.Empty, token.ToString()), out _))
        {
            _reporter!.Report(
                DatatypeRule,
                location,
                "The value '" + Shorten(token) + "' is not valid: no notation declaration has this name.",
                null
            );
        }
    }

    private static IdKind KindOf(XPathQName name)
    {
        var kind = name.LocalName switch
        {
            "ID" => IdKind.Id,
            "IDREF" or "IDREFS" => IdKind.IdRef,
            "NOTATION" => IdKind.Notation,
            _ => IdKind.None,
        };
        return
            kind != IdKind.None && string.Equals(name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal)
            ? kind
            : IdKind.None;
    }

    private static string Shorten(ReadOnlySpan<char> value) =>
        value.Length <= MaxReportedValue ? value.ToString() : value.Slice(0, MaxReportedValue).ToString() + "...";

    private enum IdKind
    {
        None = 0,
        Id = 1,
        IdRef = 2,
        Notation = 3,
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Pending
    {
        internal Pending(int start, int length, DocumentLocation location)
        {
            Start = start;
            Length = length;
            Location = location;
        }

        internal int Start { get; }

        internal int Length { get; }

        internal DocumentLocation Location { get; }
    }
}
