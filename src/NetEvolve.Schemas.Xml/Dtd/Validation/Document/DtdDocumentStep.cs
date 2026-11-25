namespace NetEvolve.Schemas.Xml.Dtd.Validation.Document;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>
/// The validation step for the document-level validity constraints (XML 1.0, design #122, XML-67): Root Element Type (the
/// root element has the name of the document type declaration), Standalone Document Declaration with its four clauses
/// (defaults, entities, normalization, white space in element content), Unique Element Type Declaration, No Duplicate Types,
/// Proper Declaration/PE Nesting, Proper Group/PE Nesting, Proper Conditional Section/PE Nesting, and the validity form of
/// Entity Declared. It also reports the two reports of the code table that this work package owns: a document without a
/// document type declaration (<see cref="DtdValidityCodes.XML5910"/>) and an element type that a declaration mentions but
/// never declares (<see cref="DtdValidityCodes.XML5901"/>, a warning).
/// </summary>
/// <remarks>
/// <para>
/// Declaration-time checks run at <see cref="BeginDocument"/>: No Duplicate Types, the undeclared mentions, and the parse-time
/// facts of <see cref="IDtdAnomalySource"/>. The standalone clauses and the root element type are decided from the events.
/// A document without a document type declaration is recognized by the empty name of the model (an empty model fires
/// <see cref="BeginDocument"/> as well, design #122, D2); it reports <c>XML5910</c> once and nothing else.
/// </para>
/// <para>
/// Limitations: the four constraints that depend on parse-time facts (Unique Element Type Declaration and the three nesting
/// constraints) are reported only for a model that implements <see cref="IDtdAnomalySource"/>, which the parser model does not
/// yet (#608); declaration-time reports use <see cref="DtdValidationContext.DocumentTypeLocation"/> until per-declaration
/// locations exist (#608). The third clause of Standalone Document Declaration reads
/// <see cref="ValidationAttributeInfo.IsChangedByNormalization"/>, which the attribute normalization (XML-16) sets, and it
/// applies to every type except CDATA (the tokenized, notation and enumerated types), as sections 2.9 and 3.3.3 word it. The
/// second clause sees references in content only, because the entity reference event is not raised in attribute values; with
/// <c>standalone="yes"</c> the reader already refuses such a reference as a well-formedness error (WFC Entity Declared), so
/// the clause is reached only when a pipeline passes the event on. Entities declared in the replacement text of an internal
/// parameter entity do not count as external (#700). White space is the literal white space of production 3 and the line
/// break that the reader reports as a reference (the limitation of the content model step). An attribute is matched to its
/// definition by a linear scan of the attribute list of its element, which stays cheap for the attribute counts that the
/// reader limits allow.
/// </para>
/// <para>Not thread-safe; one instance validates one document at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class DtdDocumentStep : IDtdValidationStep
{
    private const byte ExternalContent = 1;
    private const byte WhiteSpaceReported = 2;

    private readonly HashSet<string> _names = new(StringComparer.Ordinal);
    private readonly Stack<DtdContentParticle> _particles = new();
    private Frame[] _frames = new Frame[16];
    private DtdValidationContext? _context;
    private int _depth;
    private bool _hasDocumentType;
    private bool _hasExternalContent;
    private bool _hasExternalAttributes;

    /// <inheritdoc />
    public void BeginDocument(DtdValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _depth = 0;
        _hasExternalContent = false;
        _hasExternalAttributes = false;
        var declarations = context.Declarations;
        _hasDocumentType = declarations.Name.Length > 0;
        if (!_hasDocumentType)
        {
            context.Report(DtdValidityCodes.XML5910, DtdValidityCodes.XML5910.DefaultSeverity, default);

            return;
        }

        if (declarations is IDtdAnomalySource source)
        {
            ReportFacts(context, source);
        }

        ReportElementDeclarations(context, declarations);
        if (context.IsStandalone)
        {
            FindExternalDeclarations(declarations);
        }
    }

    /// <inheritdoc />
    public void StartElement(in ValidationElement element)
    {
        if (_context is not { IsStopped: false } context)
        {
            return;
        }

        var declarations = context.Declarations;
        if (_depth == 0 && _hasDocumentType && !element.Name.Span.SequenceEqual(declarations.Name.AsSpan()))
        {
            var detail = "root element '" + element.Name.ToString() + "', document type '" + declarations.Name + "'";
            context.Report(
                DtdValidityCodes.XML5001,
                DtdValidityCodes.XML5001.DefaultSeverity,
                context.Locate(element.Position),
                detail
            );
        }

        var frame = default(Frame);
        if (context.IsStandalone && (_hasExternalContent || _hasExternalAttributes))
        {
            // ponytail: one string per element, only for a standalone document that has external declarations.
            var name = element.Name.ToString();
            if (
                _hasExternalContent
                && declarations.Elements.TryGetValue(name, out var declaration)
                && declaration.InExternalText
                && declaration.ContentKind == DtdContentKind.Children
            )
            {
                frame = new Frame(ExternalContent, name);
            }

            if (_hasExternalAttributes && declarations.AttributeLists.TryGetValue(name, out var list))
            {
                CheckAttributes(context, name, list, in element);
            }
        }

        if (_depth == _frames.Length)
        {
            Array.Resize(ref _frames, _depth * 2);
        }

        _frames[_depth++] = frame;
    }

    /// <inheritdoc />
    public void EntityReference(in ValidationEntityReference reference)
    {
        if (_context is not { IsStopped: false } context)
        {
            return;
        }

        var location = context.Locate(reference.Position);
        if (reference.Outcome == DtdEntityOutcome.Undeclared)
        {
            context.Report(
                DtdValidityCodes.XML5024,
                DtdValidityCodes.XML5024.DefaultSeverity,
                location,
                "entity '" + reference.Name.ToString() + "'"
            );

            return;
        }

        if (!context.IsStandalone)
        {
            return;
        }

        var name = reference.Name.ToString();
        if (
            !IsPredefined(name)
            && context.Declarations.GeneralEntities.TryGetValue(name, out var entity)
            && entity.InExternalText
        )
        {
            context.ReportStandalone(XmlStandaloneClause.Entities, location, "entity '" + name + "'");
        }
    }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, (int)XmlTextKind.Text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)XmlTextKind.Reference);

        if (_context is not { IsStopped: false } context || _depth == 0 || text.IsEmpty)
        {
            return;
        }

        ref var frame = ref _frames[_depth - 1];
        if (frame.Flags != ExternalContent)
        {
            return;
        }

        var at = FirstWhiteSpace(text, kind);
        if (at < 0)
        {
            return;
        }

        frame.Flags |= WhiteSpaceReported;
        var offending = new XmlPosition(position.Line, position.Column + at, position.CharOffset + at);
        context.ReportStandalone(
            XmlStandaloneClause.WhiteSpace,
            context.Locate(offending),
            "element '" + frame.Name + "' has element content"
        );
    }

    /// <inheritdoc />
    public void Comment(in XmlPosition position) { }

    /// <inheritdoc />
    public void ProcessingInstruction(in XmlPosition position) { }

    /// <inheritdoc />
    public void EndElement(in ValidationElement element)
    {
        if (_depth > 0)
        {
            _depth--;
        }
    }

    /// <inheritdoc />
    public void EndDocument() { }

    // Unique Element Type Declaration and the three nesting constraints, from the facts of the parser.
    private static void ReportFacts(DtdValidationContext context, IDtdAnomalySource source)
    {
        var facts = source.Anomalies;
        for (var i = 0; i < facts.Count; i++)
        {
            var fact = facts[i];
            var code = fact.Kind switch
            {
                DtdAnomalyKind.ElementRedeclared => DtdValidityCodes.XML5006,
                DtdAnomalyKind.DeclarationNesting => DtdValidityCodes.XML5002,
                DtdAnomalyKind.GroupNesting => DtdValidityCodes.XML5007,
                _ => DtdValidityCodes.XML5023,
            };
            context.Report(code, code.DefaultSeverity, fact.Location, fact.Name is null ? null : "'" + fact.Name + "'");
        }
    }

    // No Duplicate Types in mixed content and the undeclared mentions in every content model.
    private void ReportElementDeclarations(DtdValidationContext context, IDtdDeclarations declarations)
    {
        var mentioned = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in declarations.Elements)
        {
            var element = pair.Value;
            if (element.Content is null)
            {
                continue;
            }

            if (element.ContentKind == DtdContentKind.Mixed)
            {
                _names.Clear();
                foreach (var item in element.Content.Items)
                {
                    if (item.Name is null)
                    {
                        continue;
                    }

                    if (!_names.Add(item.Name))
                    {
                        context.Report(
                            DtdValidityCodes.XML5008,
                            DtdValidityCodes.XML5008.DefaultSeverity,
                            default,
                            "'" + item.Name + "' in the declaration of '" + element.Name + "'"
                        );
                    }

                    ReportUndeclared(context, declarations, mentioned, item.Name, element.Name);
                }
            }
            else if (element.ContentKind == DtdContentKind.Children)
            {
                _particles.Push(element.Content);
                while (_particles.Count > 0)
                {
                    var particle = _particles.Pop();
                    if (particle.Name is not null)
                    {
                        ReportUndeclared(context, declarations, mentioned, particle.Name, element.Name);
                    }

                    // Reversed, so that the members come off the stack in document order.
                    for (var i = particle.Items.Count - 1; i >= 0; i--)
                    {
                        _particles.Push(particle.Items[i]);
                    }
                }
            }
        }
    }

    private static void ReportUndeclared(
        DtdValidationContext context,
        IDtdDeclarations declarations,
        HashSet<string> mentioned,
        string name,
        string declaring
    )
    {
        if (!declarations.Elements.ContainsKey(name) && mentioned.Add(name))
        {
            context.Report(
                DtdValidityCodes.XML5901,
                DtdValidityCodes.XML5901.DefaultSeverity,
                default,
                "'" + name + "' in the declaration of '" + declaring + "'"
            );
        }
    }

    // Whether the standalone clauses have anything to look at: an element with element content or an attribute that
    // stands in external text.
    private void FindExternalDeclarations(IDtdDeclarations declarations)
    {
        foreach (var pair in declarations.Elements)
        {
            if (pair.Value.InExternalText && pair.Value.ContentKind == DtdContentKind.Children)
            {
                _hasExternalContent = true;

                break;
            }
        }

        foreach (var pair in declarations.AttributeLists)
        {
            foreach (var definition in pair.Value)
            {
                if (definition.InExternalText)
                {
                    _hasExternalAttributes = true;

                    return;
                }
            }
        }
    }

    // Clauses 1 and 3: an attribute with a default value declared externally is absent from the document (it arrives
    // unspecified), and an attribute of a non-CDATA type declared externally whose value normalization changed.
    private static void CheckAttributes(
        DtdValidationContext context,
        string element,
        IReadOnlyList<DtdAttributeDefinition> definitions,
        in ValidationElement tag
    )
    {
        foreach (var attribute in tag.Attributes.Span)
        {
            var definition = Find(definitions, attribute.Name.Span);
            if (definition is not { InExternalText: true })
            {
                continue;
            }

            if (!attribute.IsSpecified && definition.DefaultKind is DtdDefaultKind.Default or DtdDefaultKind.Fixed)
            {
                context.ReportStandalone(
                    XmlStandaloneClause.Defaults,
                    context.Locate(tag.Position),
                    "attribute '" + definition.Name + "' of '" + element + "'"
                );
            }
            else if (attribute.IsSpecified && attribute.IsChangedByNormalization && IsNormalized(definition.Type))
            {
                context.ReportStandalone(
                    XmlStandaloneClause.Normalization,
                    context.Locate(attribute.Position),
                    "attribute '" + definition.Name + "' of '" + element + "'"
                );
            }
        }
    }

    private static DtdAttributeDefinition? Find(
        IReadOnlyList<DtdAttributeDefinition> definitions,
        ReadOnlySpan<char> name
    )
    {
        for (var i = 0; i < definitions.Count; i++)
        {
            if (name.SequenceEqual(definitions[i].Name.AsSpan()))
            {
                return definitions[i];
            }
        }

        return null;
    }

    // Section 3.3.3 normalizes every declared type except CDATA: the tokenized, notation and enumerated types.
    private static bool IsNormalized(DtdAttributeType type) => type != DtdAttributeType.Cdata;

    private static bool IsPredefined(string name) => name is "amp" or "lt" or "gt" or "apos" or "quot";

    // The offset of the first white space (production 3), -1 when there is none. A CDATA section and the text of a
    // reference are character data, except the single line feed that the reader reports for every line break.
    private static int FirstWhiteSpace(ReadOnlySpan<char> text, XmlTextKind kind)
    {
        if (kind == XmlTextKind.CData)
        {
            return -1;
        }

        if (kind == XmlTextKind.Reference)
        {
            return text.Length == 1 && text[0] == '\n' ? 0 : -1;
        }

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is ' ' or '\t' or '\r' or '\n')
            {
                return i;
            }
        }

        return -1;
    }

    private struct Frame
    {
        internal Frame(byte flags, string? name)
        {
            Flags = flags;
            Name = name;
        }

        internal byte Flags;

        internal string? Name;
    }
}
