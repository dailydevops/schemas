namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using System.Buffers;
using System.Text;
using System.Xml.Linq;
using NetEvolve.Schemas.Schematron.Svrl;
using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;
using NetEvolve.Schemas.Testing.Fakes;
using LibraryValidator = NetEvolve.Schemas.ISchematronValidator;

/// <summary>
/// The Schematron validator behind the runner seam: it builds the validator of the public entry point on the real Xml and XPath engines,
/// validates the instance of a case with the SVRL report sink, and evaluates the assertions of the case on the SVRL output.
/// </summary>
/// <remarks>
/// <para>
/// The documents of a case are served from memory under one base URI, so <c>include</c>, <c>extends</c>, and the documents of a pattern
/// resolve against them and nothing else. A case with several schemas must give the expected outcome with each of them, and its assertions
/// must hold for the SVRL output of each of them.
/// </para>
/// <para>
/// The outcome is <see cref="SchematronOutcome.Error"/> when the schema does not compile or the SVRL output holds an <c>svrl:error</c>,
/// <see cref="SchematronOutcome.Invalid"/> when it holds a failed assertion or a successful report, whatever its role, and
/// <see cref="SchematronOutcome.Valid"/> otherwise. A condition that is no outcome of the specification, an unexpected exception or a
/// truncated run, is reported as <see cref="SchematronOutcome.NotImplemented"/> with its reason, which fails every case.
/// </para>
/// <para>
/// Limitation: every build failure is an <see cref="SchematronOutcome.Error"/>, also a not-supported diagnostic, so a case that expects an
/// error could pass on a limit of the library. Two selected cases expect an error: <c>rule-extends-no-abstract</c> and <c>rule-abstract-scope-error</c> pass with
/// the code of a real semantic error.
/// </para>
/// </remarks>
internal sealed class SchematronEngine : ISchematronValidator
{
    private const string BaseUri = "https://schematron-conformance.invalid/case/";
    private const string InstanceName = "document.xml";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    public static SchematronEngine Instance { get; } = new();

    public Task<SchematronObservation> ValidateAsync(
        SchematronCase schematronCase,
        string binding,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(schematronCase);
        ArgumentException.ThrowIfNullOrWhiteSpace(binding);
        return Task.FromResult(Run(schematronCase, binding, cancellationToken));
    }

    private static SchematronObservation Run(
        SchematronCase schematronCase,
        string binding,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new InMemoryResourceResolver();
        foreach (var document in schematronCase.Documents)
        {
            _ = resolver.Add(BaseUri + document.FileName, Utf8.GetBytes(document.Xml));
        }

        var instance = Utf8.GetBytes(schematronCase.Instance);
        for (var i = 0; i < schematronCase.Schemas.Count; i++)
        {
            var observation = RunSchema(schematronCase, i, binding, resolver, instance, cancellationToken);
            if (observation.Outcome != schematronCase.Expect || observation.Reason is not null)
            {
                return schematronCase.Schemas.Count == 1
                    ? observation
                    : observation with
                    {
                        Reason =
                            $"Schema {i + 1} of {schematronCase.Schemas.Count}"
                            + (observation.Reason is null ? "." : ": " + observation.Reason),
                    };
            }
        }

        return new SchematronObservation(schematronCase.Expect);
    }

    private static SchematronObservation RunSchema(
        SchematronCase schematronCase,
        int index,
        string binding,
        InMemoryResourceResolver resolver,
        byte[] instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        LibraryValidator validator;
        try
        {
            var builder = SchemaValidator
                .ForSchematron(
                    Utf8.GetBytes(WithQueryBinding(schematronCase.Schemas[index], binding)),
                    BaseUri + "schema-" + index + ".sch"
                )
                .WithResolver(resolver);
            validator = (schematronCase.Phase is { } phase ? builder.WithPhase(phase) : builder).Build();
        }
        catch (SchemaCompilationException exception)
        {
            return Observe(schematronCase, SchematronOutcome.Error, exception.Message);
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            return Unexpected(exception);
        }

        var output = new ArrayBufferWriter<byte>();
        try
        {
            using var sink = new SvrlReportSink(output);
            var result = validator.WithReportSink(sink).Validate(instance, BaseUri + InstanceName, cancellationToken);
            if (result.IsTruncated)
            {
                return new SchematronObservation(SchematronOutcome.NotImplemented, "The validation was truncated.");
            }
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
        {
            return Unexpected(exception);
        }

        return Judge(schematronCase, output.WrittenMemory);
    }

    /// <summary>Sets the <c>queryBinding</c> of the schema element to the binding of the run, as the upstream driver does; the suite id <c>xslt-2</c> is the name <c>xslt2</c>.</summary>
    private static string WithQueryBinding(string schema, string binding)
    {
        var document = XDocument.Parse(schema, LoadOptions.PreserveWhitespace);
        document.Root!.SetAttributeValue("queryBinding", binding.Replace("-", "", StringComparison.Ordinal));
        return document.ToString(SaveOptions.DisableFormatting);
    }

    internal static SchematronObservation Judge(SchematronCase schematronCase, ReadOnlyMemory<byte> svrl)
    {
        SvrlReport report;
        try
        {
            report = SvrlReport.Load(svrl);
        }
        catch (InvalidDataException exception)
        {
            return new SchematronObservation(SchematronOutcome.NotImplemented, exception.Message);
        }

        var outcome =
            report.HasError ? SchematronOutcome.Error
            : report.Holds("//svrl:failed-assert | //svrl:successful-report") ? SchematronOutcome.Invalid
            : SchematronOutcome.Valid;
        var failures = report.Check(schematronCase.Assertions);
        return failures.Count > 0
            ? new SchematronObservation(outcome, string.Join(" ", failures))
            : Observe(
                schematronCase,
                outcome,
                outcome == SchematronOutcome.Error ? "The SVRL holds an svrl:error." : null
            );
    }

    // The detail explains a mismatch only; an outcome that equals the expectation has nothing to explain.
    private static SchematronObservation Observe(
        SchematronCase schematronCase,
        SchematronOutcome outcome,
        string? detail
    ) => new(outcome, outcome == schematronCase.Expect ? null : detail);

    private static SchematronObservation Unexpected(Exception exception) =>
        new(
            SchematronOutcome.NotImplemented,
            $"An unexpected exception: {exception.GetType().Name}: {exception.Message}"
        );
}
