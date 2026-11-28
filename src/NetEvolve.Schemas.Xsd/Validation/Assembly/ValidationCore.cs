namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Input.Sequence;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Traversal;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;
using NetEvolve.Schemas.Xsd.Assertions.Facet;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Entities;
using NetEvolve.Schemas.Xsd.Validation.Ids;
using NetEvolve.Schemas.Xsd.Validation.SchemaLocation;
using NetEvolve.Schemas.Xsd.Validation.Tree;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>
/// The one synchronous core behind every input kind (design #146 DP-11): the immutable compiled schema, and a run per call that
/// pumps the reader into a streaming validator with its listeners and returns the issues. A run holds all of its state, so one core
/// serves any number of threads.
/// </summary>
/// <remarks>
/// <para>
/// A contiguous input goes through the span reader and a multi-segment sequence through the sequence reader, which feeds the
/// segments to the parser one after the other without flattening them. An error of the reader is an issue: a limit of the reader
/// that a core limit names stops the run with the core code (NES0002 to NES0005), every other error is reported with its Xml code.
/// </para>
/// <para>
/// A tree run (XSD-79, #361) pumps a node of the document model into the same sink instead of a reader, and appends the annotation
/// listener last; it adds the document events that the pump does not emit for an element, so a subtree run begins and ends like a document.
/// The locations of its issues take the positions that the tree keeps, and their source is the document URI of the tree.
/// </para>
/// <para>
/// Limitations: DTD processing reads the internal subset only (design #146 DP-21); the assessment is strict and declaration driven
/// (DP-23); the ID and the entity listener exist
/// only when the schema names a type of their check (<see cref="ListenerDemand"/>), so an <c>xsi:type</c> that names such a built-in
/// type for a schema that never names it is not checked. A tree run does not see the document type declaration, which the model does not
/// keep (#731).
/// </para>
/// </remarks>
internal sealed class ValidationCore : IValidationCore
{
    private readonly ValidationModel _model;
    private readonly DescriptorValueAssessor _assessor;
    private readonly IReadOnlyDictionary<ElementDeclaration, IReadOnlyList<IdentityConstraintDefinition>> _constraints;
    private readonly XsdVersion _version;
    private readonly ComplexTypeAssertions? _assertions;
    private readonly FacetAssertions? _facetAssertions;
    private readonly bool _hasTypeAlternatives;
    private readonly bool _needsIds;
    private readonly bool _needsEntities;
    private readonly ValidationLimits _limits;
    private readonly XmlReaderOptions _options;
    private readonly IResourceResolver? _hintResolver;

    internal ValidationCore(
        SchemaCompilation compilation,
        XsdVersion version,
        bool dtdProcessing,
        ValidationLimits limits,
        IResourceResolver? hintResolver = null
    )
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        ArgumentNullException.ThrowIfNull(compilation.Model);

        _model = compilation.Model;
        _constraints = compilation.IdentityConstraints;
        _assessor = new DescriptorValueAssessor(version);
        _version = version;
        _assertions = compilation.Assertions;
        _facetAssertions = compilation.FacetAssertions;
        _hasTypeAlternatives = compilation.HasTypeAlternatives;
        _needsIds = compilation.NeedsIdListener;
        _needsEntities = compilation.NeedsEntityListener;
        _limits = limits;
        _hintResolver = hintResolver;
        _options = XmlReaderOptions
            .Default.WithDtdProcessing(dtdProcessing)
            .WithLimits(
                XmlReaderLimits
                    .Default.WithMaxElementDepth(limits.MaxDepth)
                    .WithMaxAttributesPerElement(limits.MaxAttributesPerElement)
                    .WithMaxEntityExpansionCharacters(limits.MaxEntityExpansion)
                    .WithMaxEntityDepth(limits.MaxEntityDepth)
            );
    }

    /// <inheritdoc />
    public ISimpleValueAssessor Assessor => _assessor;

    /// <inheritdoc />
    public ValidationResult ValidateTree(
        XmlNode node,
        IAnnotationSink annotations,
        TreeIssueLog issues,
        out XmlDocumentTree copy,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(node.Tree, nameof(node));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)node.Kind, (int)XmlNodeKind.Element, nameof(node));
        ArgumentNullException.ThrowIfNull(annotations);
        ArgumentNullException.ThrowIfNull(issues);

        var collector = new ValidationIssueCollector(_limits);
        var defaults = new TreeDefaults(annotations);
        var run = new Run(this, collector, node.DocumentUri?.OriginalString, cancellationToken, defaults);
        copy = run.PumpTree(node, issues, defaults);

        return collector.ToResult();
    }

    /// <inheritdoc />
    public ValidationResult Validate(in DocumentSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var collector = new ValidationIssueCollector(_limits);
        var run = new Run(this, collector, source.SystemId, cancellationToken);
        if (source.Kind == DocumentSourceKind.Bytes)
        {
            if (source.IsSingleSegment)
            {
                using var reader = XmlPipelineReader.Create(source.Bytes, _options);
                run.Pump(reader);
            }
            else
            {
                using var reader = XmlSequenceReader.Create(source.ByteSequence, _options);
                run.Pump(reader);
            }
        }
        else if (source.IsSingleSegment)
        {
            using var reader = XmlPipelineReader.Create(source.Chars, _options);
            run.Pump(reader);
        }
        else
        {
            using var reader = XmlSequenceReader.Create(source.CharSequence, _options);
            run.Pump(reader);
        }

        return collector.ToResult();
    }

    // The state of one call: the collector, the listeners, and the validator that the reader pumps into.
    private sealed class Run
    {
        private readonly ValidationCore _core;
        private readonly ValidationIssueCollector _collector;
        private readonly string? _systemId;
        private readonly CancellationToken _cancellationToken;
        private readonly IAnnotationSink? _annotations;

        internal Run(
            ValidationCore core,
            ValidationIssueCollector collector,
            string? systemId,
            CancellationToken cancellationToken,
            IAnnotationSink? annotations = null
        )
        {
            ArgumentNullException.ThrowIfNull(core);
            ArgumentNullException.ThrowIfNull(collector);

            _core = core;
            _collector = collector;
            _systemId = systemId;
            _cancellationToken = cancellationToken;
            _annotations = annotations;
        }

        internal void Pump<TReader>(TReader reader)
            where TReader : IXmlReader, IXmlPositionSource
        {
            ArgumentNullException.ThrowIfNull(reader);

            var sink = CreateSink();
            sink.UsePositions(reader);
            if (reader.Pump(ref sink) != XmlReadStatus.Completed)
            {
                Report(reader.Diagnostic);
            }
        }

        // The model-to-sink pump drives the same sink as a reader does, so a tree run has no validator of its own (design #146 DP-11).
        // The pump emits no document events for an element, so a subtree run adds them: the listeners need the begin of the run.
        internal XmlDocumentTree PumpTree(XmlNode node, TreeIssueLog issues, TreeDefaults defaults)
        {
            var inner = CreateSink();
            var builder = new XmlTreeBuilder(inner.Names, node.DocumentUri);
            try
            {
                var sink = new TreeRunSink(inner, builder, _collector, issues, defaults);
                var isElement = node.Kind == XmlNodeKind.Element;
                if (isElement)
                {
                    sink.StartDocument(default);
                }

                XmlTreePump.Pump(node, sink);
                if (isElement)
                {
                    sink.EndDocument();
                }

                return builder.Complete();
            }
            finally
            {
                builder.Dispose();
            }
        }

        private SessionSink CreateSink()
        {
            var entities = _core._needsEntities ? new EntityListener(_core._assessor, _core._options.Limits) : null;
            var reporter = new XsdIssueReporter(_collector);
            var hints = _core._hintResolver is null
                ? null
                : new SchemaLocationHints(
                    _core._version,
                    _core._hintResolver,
                    _core._limits,
                    reporter,
                    _systemId,
                    _cancellationToken
                );
            var validator = new StreamingValidator(
                _core._model,
                _core._assessor,
                reporter,
                _core._limits,
                _systemId,
                Listeners(entities, hints),
                readerLimits: _core._options.Limits,
                facetAssertions: _core._facetAssertions,
                typeAlternatives: _core._hasTypeAlternatives,
                hints: hints
            );

            return new SessionSink(validator, entities, _cancellationToken);
        }

        // The fixed order of the listeners: ID, entity, identity constraint, assertion; each exists only when the schema needs it (design #146 DP-12).
        // The annotation listener comes last and exists only when the run has a sink, because it reads the validity that the others settle.
        private List<ValidationListener> Listeners(EntityListener? entities, SchemaLocationHints? hints)
        {
            var listeners = new List<ValidationListener>(4);
            if (_core._needsIds)
            {
                listeners.Add(new IdListener(_core._assessor, _core._model.Schema));
            }

            if (entities is not null)
            {
                listeners.Add(entities);
            }

            if (_core._constraints.Count > 0)
            {
                listeners.Add(
                    new IdentityConstraintListener(_core._assessor, _core._version, declaration =>
                        declaration is ElementDeclaration element
                        && _core._constraints.TryGetValue(element, out var constraints)
                            ? constraints
                            : hints?.ConstraintsOf(declaration)
                    )
                );
            }

            if (_core._assertions is not null)
            {
                listeners.Add(new AssertionListener(_core._assertions, _core._assessor));
            }

            if (_annotations is not null)
            {
                listeners.Add(new AnnotationListener(_annotations));
            }

            return listeners;
        }

        private void Report(XmlDiagnostic? diagnostic)
        {
            ArgumentNullException.ThrowIfNull(diagnostic);

            var code = diagnostic.Code;
            var position = diagnostic.Position;
            var location = new DocumentLocation(_systemId, position.Line, position.Column);
            if (code.CoreCode.Number != 0)
            {
                // A limit of the reader that a core limit names ends the run with the core code (design #103 AP-18).
                _collector.Stop(
                    new ValidationIssue(code.CoreCode, IssueSeverity.Error, code.Message, location, code.Constraint)
                );
                return;
            }

            _collector.Add(
                new ValidationIssue(code.Code, IssueSeverity.Error, code.Message, location, code.Constraint)
            );
        }
    }
}
