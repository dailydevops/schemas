namespace NetEvolve.Schemas.Xsd.Validation.Entities;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Validation.Identity;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The listener of the ENTITY and ENTITIES checks (XSD-83, #309; design #146 DP-11 and DP-21): a value of these types is valid
/// only when each name it contains is declared as an unparsed entity in the document type declaration of the instance
/// (Datatypes 3.3.11 and 3.3.12, reported as <c>cvc-simple-type</c>, String Valid).
/// </summary>
/// <remarks>
/// <para>
/// A value takes part when its simple type is the built-in <c>ENTITY</c> or <c>ENTITIES</c>, or derives from one of them by
/// restriction, or when it is an item of a list of them, or when it is accepted by a union member of them (the first member in
/// declaration order that accepts the value decides). Only valid values count, because the datatype check has reported the
/// others. Each name of an ENTITIES value is checked on its own, and each name that is not an unparsed entity gets its own issue
/// at the location of the value. The document type declaration reaches the listener through <see cref="ReceiveDoctype"/>, which
/// the owner of the run calls on the doctype event of the reader, after <see cref="BeginRun"/> and before the root element. The
/// names of the general entities go into two pooled tables once, so a lookup takes a span and allocates nothing, and a document
/// without ENTITY-typed values allocates nothing. Without a document type declaration, or when it is unusable, every ENTITY
/// value is invalid.
/// </para>
/// <para>
/// Limitations: the declarations come from the internal subset only (DP-21); the external subset and the external parameter
/// entities are not read here, because this listener parses the subset itself with the limits it is given, and entities declared
/// there are reported as undeclared (XML-20, #291; XML-62, #293). The subset is parsed a second time, because the reader keeps its
/// parsed declarations internal and the validator has no doctype hook for listeners yet. The value of an ENTITY is collapsed
/// (leading and trailing white space is dropped) before it is looked up. A default or fixed value is checked like a specified
/// one, because whether it names an unparsed entity depends on the document type of each instance. Parameter entities are never
/// matched. An element or attribute that the validator does not hand to the listeners (a skipped or rejected subtree) takes no
/// part, and a violation found here does not mark the element invalid (#605). Tree-based validation needs the declarations of the
/// source document and is not covered. ID, IDREF, and NOTATION values are checked by the id listener (XSD-14, #303).
/// </para>
/// <para>Not thread-safe; one instance serves one run at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class EntityListener : ValidationListener
{
    private const int MaxReportedValue = 64;

    private static readonly XsdRule Rule = XsdRuleLookup.Find(XsdRuleCodes.CvcSimpleType);

    private readonly ISimpleValueAssessor _assessor;
    private readonly XmlReaderLimits _limits;
    private readonly DtdIdTable _unparsed = new();
    private readonly DtdIdTable _parsed = new();
    private bool _hasDeclarations;
    private XsdIssueReporter? _reporter;
    private int _startOrdinal;
    private DocumentLocation _startLocation;

    /// <summary>Initializes a new instance of the <see cref="EntityListener"/> class.</summary>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="limits">The limits that bound the parse of the document type declaration, <see langword="null"/> for the defaults.</param>
    internal EntityListener(ISimpleValueAssessor assessor, XmlReaderLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(assessor);

        _assessor = assessor;
        _limits = limits ?? XmlReaderLimits.Default;
    }

    /// <inheritdoc />
    public override ListenerInterest Interest =>
        ListenerInterest.StartElement | ListenerInterest.Attribute | ListenerInterest.SimpleValue;

    /// <inheritdoc />
    public override void BeginRun(in ValidationRunContext context)
    {
        _reporter = context.Reporter;
        Forget();
        _startOrdinal = 0;
        _startLocation = default;
    }

    /// <summary>Receives the document type declaration of the run; it must follow <see cref="BeginRun"/> and precede the root element.</summary>
    /// <param name="doctype">The declaration, valid only during the call.</param>
    internal void ReceiveDoctype(in XmlDoctypeInfo doctype)
    {
        Forget();
        // The parser gives no model exactly when it reports an error.
        _ = DtdSubsetParser.Parse(in doctype, _limits, out var declarations);
        if (declarations is null)
        {
            return;
        }

        _hasDeclarations = true;
        foreach (var entity in declarations.GeneralEntities.Values)
        {
            _ = (entity.NotationName is null ? _parsed : _unparsed).TryAdd(entity.Name.AsSpan(), int.MaxValue);
        }
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
            Visit(info.Type, info.Value, info.Location);
        }
    }

    /// <inheritdoc />
    public override void SimpleValue(in SimpleValueInfo info)
    {
        if (_reporter is not null && info.IsValid && info.Type is not null)
        {
            Visit(info.Type, info.Value, info.Ordinal == _startOrdinal ? _startLocation : default);
        }
    }

    private void Forget()
    {
        _unparsed.Clear();
        _parsed.Clear();
        _hasDeclarations = false;
    }

    // Follows the restriction chain, the items of a list, and the accepting member of a union down to a type of this check.
    private void Visit(IXsdComponent? type, ReadOnlySpan<char> value, DocumentLocation location)
    {
        while (type is not null)
        {
            switch (type)
            {
                case XsdBuiltInType builtIn:
                    Apply(builtIn.Name, value, location);
                    return;
                case SimpleTypeDefinition definition:
                    if (IsEntityType(definition.Name))
                    {
                        Apply(definition.Name, value, location);
                        return;
                    }

                    switch (definition.Variety)
                    {
                        case DatatypeVariety.List:
                            // A restriction copies the item type of its base; the built-in IDREFS and ENTITIES have none, so walk to the base.
                            if (definition.ItemType is null)
                            {
                                type = definition.BaseType;
                                break;
                            }

                            VisitItems(definition.ItemType, value, location);
                            return;
                        case DatatypeVariety.Union:
                            VisitMember(definition, value, location);
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

    private void VisitItems(IXsdComponent? itemType, ReadOnlySpan<char> value, DocumentLocation location)
    {
        var cursor = new XsdListCursor(new TextRange(0, value.Length));
        while (cursor.TryNext(value, out var item))
        {
            Visit(itemType, value.Slice(item.Offset, item.Length), location);
        }
    }

    private void VisitMember(SimpleTypeDefinition union, ReadOnlySpan<char> value, DocumentLocation location)
    {
        for (var i = 0; i < union.MemberTypes.Count; i++)
        {
            if (_assessor.Assess(union.MemberTypes[i], value, out _).IsValid)
            {
                Visit(union.MemberTypes[i], value, location);
                return;
            }
        }
    }

    // ENTITY is one token and ENTITIES is a list of them, so one loop serves both.
    private void Apply(XPathQName name, ReadOnlySpan<char> value, DocumentLocation location)
    {
        if (!IsEntityType(name))
        {
            return;
        }

        var cursor = new XsdListCursor(new TextRange(0, value.Length));
        while (cursor.TryNext(value, out var item))
        {
            Check(value.Slice(item.Offset, item.Length), location);
        }
    }

    private void Check(ReadOnlySpan<char> token, DocumentLocation location)
    {
        string reason;
        if (!_hasDeclarations)
        {
            reason = "the instance has no document type declaration that declares unparsed entities.";
        }
        else if (_unparsed.Contains(token))
        {
            return;
        }
        else if (_parsed.Contains(token))
        {
            reason = "it names a parsed entity, but an unparsed entity is required.";
        }
        else
        {
            reason = "no unparsed entity has this name.";
        }

        _reporter!.Report(Rule, location, "The value '" + Shorten(token) + "' is not valid: " + reason);
    }

    private static bool IsEntityType(XPathQName name) =>
        (
            string.Equals(name.LocalName, "ENTITY", StringComparison.Ordinal)
            || string.Equals(name.LocalName, "ENTITIES", StringComparison.Ordinal)
        ) && string.Equals(name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal);

    private static string Shorten(ReadOnlySpan<char> value) =>
        value.Length <= MaxReportedValue ? value.ToString() : value.Slice(0, MaxReportedValue).ToString() + "...";
}
