namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Testing.Fakes;

/// <summary>
/// TEMPORARY: pins the throwing contract stubs. Each test is replaced by real behavior tests when the owning
/// work package lands (FND-09, FND-10, FND-55, FND-56). Delete the matching test then.
/// </summary>
public class TemporaryStubTests
{
    // Instance stubs throw before touching state, so an uninitialized instance reaches them without the throwing constructor.
    private static T Blank<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static async Task AssertStubAsync(string workPackage, params Action[] members)
    {
        foreach (var member in members)
        {
            string? message = null;
            try
            {
                member();
            }
            catch (NotImplementedException ex)
            {
                message = ex.Message;
            }

            await Assert.That(message).IsNotNull();
            await Assert.That(message!).Contains(workPackage);
        }
    }

    [Test]
    public async Task Fnd09_ResultModelStubs_ThrowUntilImplemented()
    {
        var code = new DiagnosticCode(1);
        var location = default(DocumentLocation);
        var sameLocation = default(DocumentLocation);
        var issue = Blank<ValidationIssue>();
        var result = Blank<ValidationResult>();
        var exception = Blank<SchemaCompilationException>();

        await AssertStubAsync(
            "FND-09",
            () => _ = new ValidationIssue(code, IssueSeverity.Error, "m", location),
            () => _ = new DocumentLocation("s", 1, 1),
            () => _ = new ValidationResult([]),
            () => _ = new SchemaCompilationException([]),
            () => _ = new SchemaCompilationException([], null),
            () => _ = ValidationResult.Valid,
            () => _ = result.IsValid,
            () => _ = result.Issues,
            () => _ = result.IsTruncated,
            () => _ = result.HasDroppedErrors,
            () => _ = exception.Issues,
            () => _ = issue.Code,
            () => _ = issue.Severity,
            () => _ = issue.Message,
            () => _ = issue.Location,
            () => _ = issue.Constraint,
            () => _ = issue.ToString(),
            () => _ = location.Source,
            () => _ = location.Line,
            () => _ = location.Column,
            () => _ = location.Equals(location),
            () => _ = location.Equals((object)location),
            () => _ = location.GetHashCode(),
            () => _ = location.ToString(),
            () => _ = location == sameLocation,
            () => _ = location != sameLocation
        );
    }

    [Test]
    public async Task Fnd09_IssueCollectorStubs_ThrowUntilImplemented()
    {
        var collector = Blank<ValidationIssueCollector>();

        await AssertStubAsync(
            "FND-09",
            () => _ = new ValidationIssueCollector(null!),
            () => _ = collector.Count,
            () => _ = collector.IsStopped,
            () => collector.Add(null!),
            () => collector.Stop(null!),
            () => _ = collector.ToResult()
        );
    }

    [Test]
    public async Task Fnd10_ResourceStubs_ThrowUntilImplemented()
    {
        var resolver = Blank<DenyAllResourceResolver>();
        var request = new ResourceRequest(ResourceKind.SchemaDocument, "a.xsd", null, null);

        await AssertStubAsync(
            "FND-10",
            () => _ = ResourceResult.Found(Stream.Null, "sys"),
            () => _ = DenyAllResourceResolver.Instance,
            () => _ = resolver.Resolve(request, CancellationToken.None)
        );
    }

    [Test]
    public async Task Fnd55_LimitsStubs_ThrowUntilImplemented()
    {
        var limits = Blank<ValidationLimits>();

        await AssertStubAsync(
            "FND-55",
            () => _ = ValidationLimits.Default,
            () => _ = limits.MaxIssues,
            () => _ = limits.MaxInputSize,
            () => _ = limits.MaxDepth,
            () => _ = limits.MaxAttributesPerElement,
            () => _ = limits.MaxEntityExpansion,
            () => _ = limits.MaxEntityDepth,
            () => _ = limits.MaxExternalResources,
            () => _ = limits.MaxResourceSize,
            () => _ = limits.WithMaxIssues(1),
            () => _ = limits.WithMaxInputSize(1),
            () => _ = limits.WithMaxDepth(1),
            () => _ = limits.WithMaxAttributesPerElement(1),
            () => _ = limits.WithMaxEntityExpansion(1),
            () => _ = limits.WithMaxEntityDepth(1),
            () => _ = limits.WithMaxExternalResources(1),
            () => _ = limits.WithMaxResourceSize(1)
        );
    }

    [Test]
    public async Task Fnd55_BuilderStubs_ThrowUntilImplemented()
    {
        var builder = new FakeBuilder();
        var baseHook = new BaseHookBuilder();

        await AssertStubAsync(
            "FND-55",
            () => _ = builder.Limits,
            () => _ = builder.Resolver,
            () => _ = builder.WithLimits(null!),
            () => _ = builder.WithResolver(null!),
            () => _ = builder.Build(),
            () => _ = builder.BuildAsync().AsTask(),
            () => _ = baseHook.CallBaseHook(CancellationToken.None).AsTask()
        );
    }

    [Test]
    public async Task Fnd56_DocumentSourceStubs_ThrowUntilImplemented() =>
        await AssertStubAsync(
            "FND-56",
            () => _ = default(DocumentSource).Kind,
            () => _ = default(DocumentSource).SystemId,
            () => _ = default(DocumentSource).IsSingleSegment,
            () => _ = default(DocumentSource).Length,
            () => _ = default(DocumentSource).Bytes.Length,
            () => _ = default(DocumentSource).Chars.Length,
            () => _ = default(DocumentSource).ByteSequence,
            () => _ = default(DocumentSource).CharSequence
        );

    [Test]
    public async Task Fnd56_ValidatorBaseStubs_ThrowUntilImplemented()
    {
        var validator = Blank<StubValidator>();

        await AssertStubAsync(
            "FND-56",
            () => _ = new StubValidator(),
            () => _ = validator.ExposedLimits,
            () => _ = validator.ExposedResolver,
            () => _ = validator.ValidateAsync(Stream.Null).AsTask(),
            () => _ = validator.ValidateAsync(ReadOnlyMemory<byte>.Empty).AsTask(),
            () => _ = validator.ValidateAsync(ReadOnlyMemory<char>.Empty).AsTask(),
            () => _ = validator.ValidateAsync(ReadOnlySequence<byte>.Empty).AsTask(),
            () => _ = validator.ValidateAsync(ReadOnlySequence<char>.Empty).AsTask(),
            () => _ = validator.Validate(Stream.Null),
            () => _ = validator.Validate(ReadOnlySpan<byte>.Empty),
            () => _ = validator.Validate(ReadOnlySpan<char>.Empty),
            () => _ = validator.Validate(ReadOnlySequence<byte>.Empty),
            () => _ = validator.Validate(ReadOnlySequence<char>.Empty)
        );
    }

    private sealed class StubValidator : SchemaValidatorBase
    {
        public StubValidator()
            : base(null!, null!) { }

        public ValidationLimits ExposedLimits => Limits;

        public IResourceResolver ExposedResolver => Resolver;

        protected override ValidationResult ValidateCore(
            in DocumentSource source,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }

    private sealed class BaseHookBuilder : SchemaValidatorBuilder<BaseHookBuilder, FakeValidator>
    {
        public ValueTask<FakeValidator> CallBaseHook(CancellationToken cancellationToken) =>
            CreateValidatorAsync(null!, null!, cancellationToken);

        protected override FakeValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) =>
            throw new NotSupportedException();
    }
}
