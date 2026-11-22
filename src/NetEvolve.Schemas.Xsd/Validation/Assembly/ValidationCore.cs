namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Input.Sequence;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Entities;
using NetEvolve.Schemas.Xsd.Validation.Ids;
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
/// Limitations: DTD processing reads the internal subset only (design #146 DP-21); the assessment is strict and declaration driven
/// (DP-23); a violation that a listener finds does not mark the element invalid yet (#605); the listeners of the ID and the entity
/// checks are created for every run, not only when the schema has an ID or an ENTITY typed component.
/// </para>
/// </remarks>
internal sealed class ValidationCore : IValidationCore
{
    private readonly ValidationModel _model;
    private readonly DescriptorValueAssessor _assessor;
    private readonly IReadOnlyDictionary<ElementDeclaration, IReadOnlyList<IdentityConstraintDefinition>> _constraints;
    private readonly XsdVersion _version;
    private readonly ValidationLimits _limits;
    private readonly XmlReaderOptions _options;

    internal ValidationCore(
        SchemaCompilation compilation,
        XsdVersion version,
        bool dtdProcessing,
        ValidationLimits limits
    )
    {
        Argument.ThrowIfNull(compilation);
        Argument.ThrowIfNull(limits);
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        Argument.ThrowIfNull(compilation.Model);

        _model = compilation.Model;
        _constraints = compilation.IdentityConstraints;
        _assessor = new DescriptorValueAssessor(version);
        _version = version;
        _limits = limits;
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

        internal Run(
            ValidationCore core,
            ValidationIssueCollector collector,
            string? systemId,
            CancellationToken cancellationToken
        )
        {
            Argument.ThrowIfNull(core);
            Argument.ThrowIfNull(collector);

            _core = core;
            _collector = collector;
            _systemId = systemId;
            _cancellationToken = cancellationToken;
        }

        internal void Pump<TReader>(TReader reader)
            where TReader : IXmlReader
        {
            Argument.ThrowIfNull(reader);

            var entities = new EntityListener(_core._assessor, _core._options.Limits);
            var validator = new StreamingValidator(
                _core._model,
                _core._assessor,
                new XsdIssueReporter(_collector),
                _core._limits,
                _systemId,
                Listeners(entities),
                readerLimits: _core._options.Limits
            );
            var sink = new SessionSink(validator, entities, _cancellationToken);

            if (reader.Pump(ref sink) != XmlReadStatus.Completed)
            {
                Report(reader.Diagnostic);
            }
        }

        // The fixed order of the listeners: ID, entity, identity constraint (design #146 DP-12).
        private List<ValidationListener> Listeners(EntityListener entities)
        {
            var listeners = new List<ValidationListener>(3)
            {
                new IdListener(_core._assessor, _core._model.Schema),
                entities,
            };
            if (_core._constraints.Count > 0)
            {
                listeners.Add(
                    new IdentityConstraintListener(_core._assessor, _core._version, declaration =>
                        declaration is ElementDeclaration element
                        && _core._constraints.TryGetValue(element, out var constraints)
                            ? constraints
                            : null
                    )
                );
            }

            return listeners;
        }

        private void Report(XmlDiagnostic? diagnostic)
        {
            Argument.ThrowIfNull(diagnostic);

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
