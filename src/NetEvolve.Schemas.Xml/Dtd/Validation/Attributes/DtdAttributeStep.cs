namespace NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Attributes;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// The validation step for the attribute constraints of XML 1.0 (design #122, XML-64): Attribute Value Type (an attribute is
/// declared), Name Token (an NMTOKEN value is a name token and an NMTOKENS value is name tokens), Enumeration (the value is
/// one of the declared tokens), Required Attribute, Fixed Attribute Default (section 3.3.2), No Duplicate Tokens in an
/// enumeration or a notation type, and Attribute Default Value Syntactically Correct. It also reports the warning for
/// attributes declared for an element type that has no declaration (<see cref="DtdValidityCodes.XML5902"/>).
/// </summary>
/// <remarks>
/// <para>
/// Everything that is a property of the declarations is decided once at <see cref="BeginDocument"/>, over the complete model:
/// the duplicate tokens, the lexical correctness of each default value (so a default that is never used is reported once, at
/// the declaration), and the expanded default of each fixed attribute. The attributes of a start tag are then checked when the tag
/// arrives, on the normalized value that the reader hands over (section 3.3.3), so a value with surrounding spaces in an
/// NMTOKEN attribute is valid. A required attribute that is absent is reported at the start tag; every other violation at the
/// attribute, and for an NMTOKENS value only the offending token. A defaulted attribute (not specified by the document) is
/// not checked against its type or its fixed value, because its declaration was checked. The element types and attribute
/// names are looked up in tables that <see cref="BeginDocument"/> builds, so a valid document allocates nothing per element.
/// </para>
/// <para>
/// Which step owns which type: Attribute Value Type is reported for an undeclared attribute only; a value of the wrong
/// type is reported under the constraint of the type (Name Token, Enumeration), and the ID, IDREF, ENTITY, and NOTATION
/// values by the identity and notation steps. A default value of any type is checked here as one constraint, whatever the
/// type, so a default of an ID, IDREF, ENTITY, or NOTATION attribute that is not a legal value is reported at the
/// declaration as well; the identity and notation steps still report the same value where an element receives it.
/// </para>
/// <para>
/// Limitations: (1) The attributes <c>xml:*</c> and <c>xmlns*</c> are declared like any other attribute; XML 1.0 has no
/// exemption. (2) A document without a document type declaration (the empty name of the model) reports nothing here; the
/// document step reports it once. (3) The second definition of an attribute is dropped by the declaration model, so the
/// optional warning for it (<see cref="DtdValidityCodes.XML5903"/>) is not reported until the dropped definitions are
/// recorded (#909, #608). (4) A default value that the reader refuses (an undeclared, external, or unparsed entity, a
/// <c>&lt;</c>, a recursion, or a value beyond the limits of the reader) does not expand and is neither checked for its
/// syntax nor compared; the reader reports it as a well-formedness error where the default is applied. (5) The declaration-time
/// reports use <see cref="DtdValidationContext.DocumentTypeLocation"/> until per-declaration locations exist (#608). (6) An
/// entity of the external subset that a default value uses is expanded although <c>standalone="yes"</c> would refuse
/// it; the reader reports that case.
/// </para>
/// <para>Not thread-safe; one instance validates one document at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class DtdAttributeStep : IDtdValidationStep
{
    private readonly XmlReaderLimits _limits;
    private DtdValidationContext? _context;
    private SpanInterningNameTable _elementNames = new(1);
    private ElementRules?[] _rules = [];

    /// <summary>Initializes a new instance of the <see cref="DtdAttributeStep"/> class.</summary>
    /// <param name="limits">The reader limits that bound the expansion of a default value, <see langword="null"/> for <see cref="XmlReaderLimits.Default"/>.</param>
    internal DtdAttributeStep(XmlReaderLimits? limits = null) => _limits = limits ?? XmlReaderLimits.Default;

    /// <inheritdoc />
    public void BeginDocument(DtdValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var declarations = context.Declarations;
        var lists = declarations.AttributeLists;

        // The empty name is the model of a document without a document type declaration: the document step reports it.
        _context = declarations.Name.Length == 0 ? null : context;
        _elementNames = new SpanInterningNameTable(lists.Count + 1, lists.Count + 1);
        _rules = new ElementRules?[lists.Count + 1];
        if (_context is null)
        {
            return;
        }

        foreach (var pair in lists)
        {
            if (!declarations.Elements.ContainsKey(pair.Key))
            {
                context.Report(
                    DtdValidityCodes.XML5902,
                    DtdValidityCodes.XML5902.DefaultSeverity,
                    default,
                    "attributes are declared for element type '" + pair.Key + "'"
                );
            }

            _ = _elementNames.TryIntern(pair.Key.AsSpan(), out var symbol);
            _rules[symbol.Value] = Compile(context, pair.Key, pair.Value);
        }
    }

    /// <inheritdoc />
    public void StartElement(in ValidationElement element)
    {
        if (_context is not { IsStopped: false } context)
        {
            return;
        }

        var rules = _elementNames.TryGet(element.Name.Span, out var symbol) ? _rules[symbol.Value] : null;
        var attributes = element.Attributes.Span;
        if (rules is not null)
        {
            CheckRequired(context, in element, rules, attributes);
        }

        for (var i = 0; i < attributes.Length; i++)
        {
            CheckAttribute(context, in element, rules, in attributes[i]);
        }
    }

    /// <inheritdoc />
    public void EntityReference(in ValidationEntityReference reference) { }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, (int)XmlTextKind.Text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)XmlTextKind.Reference);
    }

    /// <inheritdoc />
    public void Comment(in XmlPosition position) { }

    /// <inheritdoc />
    public void ProcessingInstruction(in XmlPosition position) { }

    /// <inheritdoc />
    public void EndElement(in ValidationElement element) { }

    /// <inheritdoc />
    public void EndDocument() { }

    // The properties of the attribute list of one element type: No Duplicate Tokens, Attribute Default Value Syntactically
    // Correct, the expanded fixed defaults, and the required attributes.
    private ElementRules Compile(
        DtdValidationContext context,
        string element,
        IReadOnlyList<DtdAttributeDefinition> definitions
    )
    {
        var fixedValues = new string?[definitions.Count];
        var required = new List<int>();
        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            CheckDuplicateTokens(context, element, definition);
            if (definition.DefaultKind == DtdDefaultKind.Required)
            {
                required.Add(i);
            }

            if (
                definition.DefaultValue is not { } literal
                || !DtdAttributeValues.TryNormalizeDefault(
                    literal,
                    definition.Type,
                    context.Declarations,
                    _limits,
                    out var normalized
                )
            )
            {
                continue;
            }

            if (!DtdAttributeValues.Matches(definition, normalized.AsSpan()))
            {
                context.Report(
                    DtdValidityCodes.XML5021,
                    IssueSeverity.Error,
                    default,
                    "attribute '"
                        + definition.Name
                        + "' of element type '"
                        + element
                        + "' has the default value '"
                        + literal
                        + "'"
                );
            }

            if (definition.DefaultKind == DtdDefaultKind.Fixed)
            {
                fixedValues[i] = normalized;
            }
        }

        return new ElementRules(new DtdElementAttributes(definitions), fixedValues, [.. required]);
    }

    private static void CheckDuplicateTokens(
        DtdValidationContext context,
        string element,
        DtdAttributeDefinition definition
    )
    {
        if (definition.Type is not (DtdAttributeType.Enumeration or DtdAttributeType.Notation))
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in definition.Values)
        {
            if (!seen.Add(token))
            {
                context.Report(
                    DtdValidityCodes.XML5018,
                    IssueSeverity.Error,
                    default,
                    "token '"
                        + token
                        + "' appears more than once in attribute '"
                        + definition.Name
                        + "' of element type '"
                        + element
                        + "'"
                );
            }
        }
    }

    private static void CheckRequired(
        DtdValidationContext context,
        in ValidationElement element,
        ElementRules rules,
        ReadOnlySpan<ValidationAttributeInfo> attributes
    )
    {
        foreach (var index in rules.Required)
        {
            var name = rules.Attributes[index].Name;
            if (!IsPresent(attributes, name.AsSpan()))
            {
                context.Report(
                    DtdValidityCodes.XML5020,
                    IssueSeverity.Error,
                    context.Locate(element.Position),
                    "attribute '" + name + "' of element '" + element.Name.ToString() + "' is absent"
                );
            }
        }
    }

    private static bool IsPresent(ReadOnlySpan<ValidationAttributeInfo> attributes, ReadOnlySpan<char> name)
    {
        for (var i = 0; i < attributes.Length; i++)
        {
            if (attributes[i].Name.Span.SequenceEqual(name))
            {
                return true;
            }
        }

        return false;
    }

    private static void CheckAttribute(
        DtdValidationContext context,
        in ValidationElement element,
        ElementRules? rules,
        in ValidationAttributeInfo attribute
    )
    {
        if (rules is null || !rules.Attributes.TryFind(attribute.Name.Span, out var index))
        {
            context.Report(
                DtdValidityCodes.XML5005,
                IssueSeverity.Error,
                context.Locate(attribute.Position),
                "attribute '"
                    + attribute.Name.ToString()
                    + "' of element '"
                    + element.Name.ToString()
                    + "' is not declared"
            );

            return;
        }

        // The declaration of a defaulted attribute was checked at BeginDocument.
        if (!attribute.IsSpecified)
        {
            return;
        }

        var definition = rules.Attributes[index];
        var value = attribute.Value.Span;
        switch (definition.Type)
        {
            case DtdAttributeType.NmToken:
                if (!DtdAttributeValues.IsNmtoken(value))
                {
                    ReportNameToken(context, in attribute, value);
                }

                break;
            case DtdAttributeType.NmTokens:
                CheckNameTokens(context, in attribute);
                break;
            case DtdAttributeType.Enumeration:
                CheckEnumeration(context, in attribute, definition);
                break;
            default:
                break;
        }

        if (rules.Fixed[index] is { } fixedValue)
        {
            CheckFixed(context, in attribute, fixedValue);
        }
    }

    private static void CheckEnumeration(
        DtdValidationContext context,
        in ValidationAttributeInfo attribute,
        DtdAttributeDefinition definition
    )
    {
        if (DtdAttributeValues.Contains(definition.Values, attribute.Value.Span))
        {
            return;
        }

        context.Report(
            DtdValidityCodes.XML5019,
            IssueSeverity.Error,
            context.Locate(attribute.Position),
            "attribute '"
                + attribute.Name.ToString()
                + "' has the value '"
                + attribute.Value.ToString()
                + "', which is not one of its declared values"
        );
    }

    private static void CheckFixed(
        DtdValidationContext context,
        in ValidationAttributeInfo attribute,
        string fixedValue
    )
    {
        if (attribute.Value.Span.SequenceEqual(fixedValue.AsSpan()))
        {
            return;
        }

        context.Report(
            DtdValidityCodes.XML5022,
            IssueSeverity.Error,
            context.Locate(attribute.Position),
            "attribute '"
                + attribute.Name.ToString()
                + "' has the value '"
                + attribute.Value.ToString()
                + "', but is fixed to '"
                + fixedValue
                + "'"
        );
    }

    // The names of an NMTOKENS value are separated by one space (production 8); an empty token is not a name token.
    private static void CheckNameTokens(DtdValidationContext context, in ValidationAttributeInfo attribute)
    {
        var rest = attribute.Value.Span;
        bool last;
        do
        {
            var token = DtdAttributeValues.NextToken(ref rest, out last);
            if (!DtdAttributeValues.IsNmtoken(token))
            {
                ReportNameToken(context, in attribute, token);
            }
        } while (!last);
    }

    private static void ReportNameToken(
        DtdValidationContext context,
        in ValidationAttributeInfo attribute,
        ReadOnlySpan<char> token
    ) =>
        context.Report(
            DtdValidityCodes.XML5014,
            IssueSeverity.Error,
            context.Locate(attribute.Position),
            "attribute '"
                + attribute.Name.ToString()
                + "' has the value '"
                + token.ToString()
                + "', which is not a name token"
        );

    // What one element type needs at a start tag: the definitions by attribute name, the expanded fixed defaults by
    // definition index, and the indexes of the required attributes.
    private sealed class ElementRules
    {
        internal ElementRules(DtdElementAttributes attributes, string?[] fixedValues, int[] required)
        {
            Attributes = attributes;
            Fixed = fixedValues;
            Required = required;
        }

        internal DtdElementAttributes Attributes { get; }

        internal string?[] Fixed { get; }

        internal int[] Required { get; }
    }
}
