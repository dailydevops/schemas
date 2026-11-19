namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Document;

/// <summary>
/// Hand-built declaration models and a scripted session for the document step: events are sent one call at a time with
/// explicit positions, so the tests need neither a declaration parser nor a reader. Defaults are applied the way XML-16
/// applies them (an attribute that the tag does not carry arrives with <c>IsSpecified</c> false).
/// </summary>
internal static class DocumentHarness
{
    internal const string SystemId = "doc.xml";

    /// <summary>The location of the document type declaration; declaration-time issues report here unless a fact has its own location.</summary>
    internal static readonly DocumentLocation DoctypeLocation = new(SystemId, 2, 3);

    internal static XmlPosition At(int line, int column) => new(line, column, 0);

    internal static DtdElementDeclaration Element(
        string name,
        DtdContentKind kind = DtdContentKind.Any,
        bool external = false
    ) => new(name, kind, null, external);

    /// <summary>An element with element content (a sequence of the names), as the parser builds it.</summary>
    internal static DtdElementDeclaration Children(string name, bool external, params string[] names)
    {
        var items = new List<DtdContentParticle>(names.Length);
        foreach (var n in names)
        {
            items.Add(new DtdContentParticle(DtdParticleKind.Name, n, DtdOccurrence.Once, null));
        }

        return new DtdElementDeclaration(
            name,
            DtdContentKind.Children,
            new DtdContentParticle(DtdParticleKind.Sequence, null, DtdOccurrence.Once, items),
            external
        );
    }

    internal static DtdElementDeclaration Children(string name, DtdContentParticle content) =>
        new(name, DtdContentKind.Children, content);

    /// <summary>Mixed content, built as the parser builds it: a choice of the names, repeated when there are names.</summary>
    internal static DtdElementDeclaration Mixed(string name, params string[] names)
    {
        var items = new List<DtdContentParticle>(names.Length);
        foreach (var n in names)
        {
            items.Add(new DtdContentParticle(DtdParticleKind.Name, n, DtdOccurrence.Once, null));
        }

        return new DtdElementDeclaration(
            name,
            DtdContentKind.Mixed,
            new DtdContentParticle(
                DtdParticleKind.Choice,
                null,
                names.Length > 0 ? DtdOccurrence.ZeroOrMore : DtdOccurrence.Once,
                items
            )
        );
    }

    internal static DtdAttributeDefinition Attribute(
        string name,
        DtdAttributeType type,
        DtdDefaultKind kind,
        string? value,
        bool external
    ) => new(name, type, null, kind, value, external);

    internal static DtdEntityDeclaration Entity(string name, bool external) =>
        new(name, false, "text", null, null, null, null, external);

    /// <summary>Builds a model; the root name is empty for a document without a document type declaration.</summary>
    internal static DtdDeclarations Declare(
        string root,
        IEnumerable<DtdElementDeclaration>? elements = null,
        IEnumerable<(string Element, DtdAttributeDefinition Definition)>? attributes = null,
        IEnumerable<DtdEntityDeclaration>? entities = null
    )
    {
        var builder = new DtdDeclarationBuilder(root, publicId: null, systemId: null);
        foreach (var element in elements ?? [])
        {
            builder.AddElement(element);
        }

        foreach (var (element, definition) in attributes ?? [])
        {
            builder.AddAttribute(element, definition);
        }

        foreach (var entity in entities ?? [])
        {
            builder.AddEntity(entity);
        }

        return builder.Build();
    }

    /// <summary>An attribute of a start tag as the reader delivers it.</summary>
    internal static ValidationAttributeInfo Attr(
        string name,
        string value,
        DtdAttributeType? type,
        bool specified = true,
        bool changed = false,
        int line = 1,
        int column = 1
    ) => new(name.AsMemory(), value.AsMemory(), type, specified, At(line, column), changed);

    /// <summary>One document run: the step, its context, and the issues.</summary>
    internal sealed class Session
    {
        private readonly DtdDocumentStep _step = new();

        internal Session(IDtdDeclarations declarations, bool standalone = false)
        {
            Issues = new ValidationIssueCollector(ValidationLimits.Default);
            Context = new DtdValidationContext(
                declarations,
                DtdValidationOptions.Default,
                Issues,
                standalone,
                SystemId,
                DoctypeLocation
            );
            _step.BeginDocument(Context);
        }

        internal ValidationIssueCollector Issues { get; }

        internal DtdValidationContext Context { get; }

        internal IReadOnlyList<ValidationIssue> Result => Issues.ToResult().Issues;

        internal Session Start(string name, XmlPosition position, params ValidationAttributeInfo[] attributes)
        {
            _step.StartElement(new ValidationElement(name.AsMemory(), position, attributes));

            return this;
        }

        internal Session End(string name)
        {
            _step.EndElement(new ValidationElement(name.AsMemory(), default));

            return this;
        }

        internal Session Text(string text, XmlTextKind kind, XmlPosition position)
        {
            _step.Text(text.AsSpan(), kind, in position);

            return this;
        }

        internal Session Reference(string name, DtdEntityOutcome outcome, XmlPosition position)
        {
            _step.EntityReference(new ValidationEntityReference(name.AsMemory(), position, outcome));

            return this;
        }

        internal Session Finish()
        {
            _step.EndDocument();

            return this;
        }
    }
}
