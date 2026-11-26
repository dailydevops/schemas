namespace NetEvolve.Schemas.Xml.Dtd.Validation.Notations;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// The validation step for the entity and notation constraints of XML 1.0 (design #122, XML-66): Entity Name (an ENTITY value
/// names a declared unparsed entity, an ENTITIES value is names of them), Notation Attributes (a NOTATION value is one of the
/// notations of its list), One Notation Per Element Type, No Notation on Empty Element (section 3.3.1), Notation Declared
/// (section 4.2.2), and Unique Notation Name (section 4.7).
/// </summary>
/// <remarks>
/// <para>
/// Everything that is a property of the declarations is decided once at <see cref="BeginDocument"/>, over the complete model,
/// so a notation that is declared after its use in an attribute list or an unparsed entity declaration is accepted. The values
/// are checked when their start tag arrives: an ENTITY or ENTITIES value against the unparsed general entities, a NOTATION
/// value against the list of its attribute definition. A value is reported at the position of its attribute, and for ENTITIES
/// only the offending token; a notation that a list names but nobody declares is reported once at <see cref="BeginDocument"/>
/// and not again for every element. The names are interned at <see cref="BeginDocument"/>, so a valid document allocates
/// nothing per element.
/// </para>
/// <para>
/// Limitations: (1) The specification files the requirement that every notation of a NOTATION attribute type is declared
/// under Notation Attributes; the code table of design #122 gives the message of <see cref="DtdValidityCodes.XML5025"/> to the
/// notations of an attribute type as well, so this step reports an undeclared listed notation as Notation Declared
/// (<see cref="DtdValidityCodes.XML5025"/>) and keeps <see cref="DtdValidityCodes.XML5015"/> for the value. (2) The model keeps
/// the first declaration of a notation and drops the others; Unique Notation Name is therefore decided from the dropped names
/// that the parser model records, and a declaration view that is not the parser model records none, so it reports nothing
/// for this constraint until the anomalies of #608 are part of the view. (3) The optional warning for an entity that is declared
/// more than once (<see cref="DtdValidityCodes.XML5904"/>) is decided from the dropped declarations that the parser model
/// records as well, and a view that is not the parser model reports none (#608). (4) The value of an
/// attribute arrives normalized (XML 1.0, section 3.3.3), so ENTITIES names are separated by exactly one space (production 6)
/// and any other separator, a leading or trailing space, or an empty value is a token that names no entity. A defaulted value
/// is checked like a specified one, at each element that receives it. (5) Declaration-time reports use
/// <see cref="DtdValidationContext.DocumentTypeLocation"/> until per-declaration locations exist (#608).
/// </para>
/// <para>Not thread-safe; one instance validates one document at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class DtdNotationStep : IDtdValidationStep
{
    private DtdValidationContext? _context;
    private SpanInterningNameTable _unparsedEntities = new(int.MaxValue);
    private SpanInterningNameTable _elementsWithNotations = new(int.MaxValue);
    private DtdAttributeDefinition[]?[] _notationAttributes = [];

    /// <inheritdoc />
    public void BeginDocument(DtdValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _unparsedEntities = new SpanInterningNameTable(int.MaxValue);
        _elementsWithNotations = new SpanInterningNameTable(int.MaxValue);
        _notationAttributes = [];

        var declarations = context.Declarations;
        if (declarations is DtdDeclarations parsed)
        {
            foreach (var name in parsed.DuplicateNotations)
            {
                context.Report(
                    DtdValidityCodes.XML5026,
                    IssueSeverity.Error,
                    default,
                    "notation '" + name + "' is declared more than once; the first declaration binds"
                );
            }
        }

        if (declarations is DtdDeclarations repeated)
        {
            foreach (var entity in repeated.DuplicateEntities)
            {
                context.Report(
                    DtdValidityCodes.XML5904,
                    DtdValidityCodes.XML5904.DefaultSeverity,
                    default,
                    (entity.IsParameter ? "parameter" : "general")
                        + " entity '"
                        + entity.Name
                        + "' is declared more than once; the first declaration binds"
                );
            }
        }

        foreach (var entity in declarations.GeneralEntities.Values)
        {
            CheckEntityDeclaration(context, entity);
        }

        foreach (var pair in declarations.AttributeLists)
        {
            CheckAttributeList(context, pair.Key, pair.Value);
        }
    }

    /// <inheritdoc />
    public void StartElement(in ValidationElement element)
    {
        if (_context is null || _context.IsStopped)
        {
            return;
        }

        var attributes = element.Attributes.Span;
        for (var i = 0; i < attributes.Length && !_context.IsStopped; i++)
        {
            ref readonly var attribute = ref attributes[i];
            switch (attribute.DeclaredType)
            {
                case DtdAttributeType.Entity:
                    CheckEntity(in attribute, attribute.Value.Span, "value");
                    break;
                case DtdAttributeType.Entities:
                    CheckEntities(in attribute);
                    break;
                case DtdAttributeType.Notation:
                    CheckNotation(in element, in attribute);
                    break;
                default:
                    break;
            }
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

    // Notation Declared for an unparsed entity; the name joins the table the ENTITY values are matched against.
    private void CheckEntityDeclaration(DtdValidationContext context, DtdEntityDeclaration entity)
    {
        if (entity.NotationName is not { } notation)
        {
            return;
        }

        _ = _unparsedEntities.TryIntern(entity.Name.AsSpan(), out _);
        if (!context.Declarations.Notations.ContainsKey(notation))
        {
            context.Report(
                DtdValidityCodes.XML5025,
                IssueSeverity.Error,
                default,
                "unparsed entity '" + entity.Name + "' names the notation '" + notation + "', which is not declared"
            );
        }
    }

    // One Notation Per Element Type, No Notation on Empty Element, and the notations a NOTATION attribute type lists.
    private void CheckAttributeList(
        DtdValidationContext context,
        string element,
        IReadOnlyList<DtdAttributeDefinition> definitions
    )
    {
        var isEmpty =
            context.Declarations.Elements.TryGetValue(element, out var declaration)
            && declaration.ContentKind == DtdContentKind.Empty;
        List<DtdAttributeDefinition>? notationAttributes = null;
        foreach (var definition in definitions)
        {
            if (definition.Type != DtdAttributeType.Notation)
            {
                continue;
            }

            notationAttributes ??= [];
            ReportNotationAttribute(context, element, definition, notationAttributes, isEmpty);
            notationAttributes.Add(definition);
        }

        if (notationAttributes is not null)
        {
            _ = _elementsWithNotations.TryIntern(element.AsSpan(), out var symbol);
            if (symbol.Value >= _notationAttributes.Length)
            {
                Array.Resize(ref _notationAttributes, symbol.Value + 1);
            }

            _notationAttributes[symbol.Value] = notationAttributes.ToArray();
        }
    }

    // The checks of one NOTATION attribute definition; the earlier NOTATION attributes of the element type are already collected.
    private static void ReportNotationAttribute(
        DtdValidationContext context,
        string element,
        DtdAttributeDefinition definition,
        List<DtdAttributeDefinition> earlier,
        bool isEmpty
    )
    {
        if (earlier.Count > 0)
        {
            context.Report(
                DtdValidityCodes.XML5016,
                IssueSeverity.Error,
                default,
                "element type '"
                    + element
                    + "' declares the NOTATION attribute '"
                    + definition.Name
                    + "' after '"
                    + earlier[0].Name
                    + "'"
            );
        }

        if (isEmpty)
        {
            context.Report(
                DtdValidityCodes.XML5017,
                IssueSeverity.Error,
                default,
                "element type '"
                    + element
                    + "' is declared EMPTY but has the NOTATION attribute '"
                    + definition.Name
                    + "'"
            );
        }

        foreach (var notation in definition.Values)
        {
            if (!context.Declarations.Notations.ContainsKey(notation))
            {
                context.Report(
                    DtdValidityCodes.XML5025,
                    IssueSeverity.Error,
                    default,
                    "attribute '"
                        + definition.Name
                        + "' of element type '"
                        + element
                        + "' lists the notation '"
                        + notation
                        + "', which is not declared"
                );
            }
        }
    }

    // The names of an ENTITIES value are separated by one space (production 6); an empty token names no entity.
    private void CheckEntities(in ValidationAttributeInfo attribute)
    {
        var rest = attribute.Value.Span;
        while (!_context!.IsStopped)
        {
            var space = rest.IndexOf(' ');
            CheckEntity(in attribute, space < 0 ? rest : rest.Slice(0, space), "token");
            if (space < 0)
            {
                break;
            }

            rest = rest.Slice(space + 1);
        }
    }

    // Entity Name: the token is the name of a declared unparsed general entity. An empty token is the empty string of the table.
    private void CheckEntity(in ValidationAttributeInfo attribute, ReadOnlySpan<char> token, string what)
    {
        if (token.Length > 0 && _unparsedEntities.TryGet(token, out _))
        {
            return;
        }

        var name = token.ToString();
        var reason =
            _context!.Declarations.GeneralEntities.TryGetValue(name, out var entity) && entity.NotationName is null
                ? "which names a parsed entity"
                : "which is not the name of a declared unparsed entity";
        _context.Report(
            DtdValidityCodes.XML5013,
            IssueSeverity.Error,
            _context.Locate(attribute.Position),
            "attribute '" + attribute.Name.ToString() + "' has the " + what + " '" + name + "', " + reason
        );
    }

    // Notation Attributes: the value is exactly one of the notations of the definition's list.
    private void CheckNotation(in ValidationElement element, in ValidationAttributeInfo attribute)
    {
        if (
            !_elementsWithNotations.TryGet(element.Name.Span, out var symbol)
            || symbol.Value >= _notationAttributes.Length
            || _notationAttributes[symbol.Value] is not { } definitions
        )
        {
            return;
        }

        var value = attribute.Value.Span;
        foreach (var definition in definitions)
        {
            if (!definition.Name.AsSpan().SequenceEqual(attribute.Name.Span))
            {
                continue;
            }

            // An index loop: foreach over the list interface would box an enumerator for every element.
            var notations = definition.Values;
            for (var i = 0; i < notations.Count; i++)
            {
                if (notations[i].AsSpan().SequenceEqual(value))
                {
                    return;
                }
            }

            _context!.Report(
                DtdValidityCodes.XML5015,
                IssueSeverity.Error,
                _context.Locate(attribute.Position),
                "attribute '"
                    + attribute.Name.ToString()
                    + "' has the value '"
                    + value.ToString()
                    + "', which is not one of its notations"
            );

            return;
        }
    }
}
