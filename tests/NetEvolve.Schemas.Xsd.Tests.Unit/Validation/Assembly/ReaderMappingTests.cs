namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Contracts;

public class ReaderMappingTests
{
    private const string Simple = "<root xmlns=\"urn:t\"><item>1</item></root>";

    [Test]
    public async Task Validate_NotWellFormed_ReportsTheReaderErrorWithItsPosition()
    {
        var result = AssemblyFixture.Run(AssemblyFixture.Schema, "<root xmlns=\"urn:t\">\n<item>1</root>");

        await Assert.That(result.IsValid).IsFalse();
        var issue = result.Issues[^1];
        await Assert.That(issue.Code.Number).IsBetween(1000, 1999);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Location.Source).IsEqualTo(AssemblyFixture.InstanceId);
        await Assert.That(issue.Location.Line).IsEqualTo(2);
        await Assert.That(result.IsTruncated).IsFalse();
    }

    [Test]
    public async Task Validate_EmptyDocument_ReportsTheReaderError()
    {
        var result = AssemblyFixture.Run(AssemblyFixture.Schema, string.Empty);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues[^1].Code.Number).IsBetween(1000, 1999);
    }

    [Test]
    public async Task Validate_DepthBeyondMaxDepth_StopsWithTheCoreDepthCode()
    {
        var validator = AssemblyFixture.Build(AssemblyFixture.Schema, limits: ValidationLimits.Default.WithMaxDepth(2));

        var result = AssemblyFixture.Run(validator, "<a><b><c><d/></c></b></a>");

        await Assert.That(result.Issues[^1].Code).IsEqualTo(CoreDiagnosticCodes.DepthLimitExceeded);
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validate_DepthWithinMaxDepth_IsNotStopped()
    {
        var validator = AssemblyFixture.Build(AssemblyFixture.Schema, limits: ValidationLimits.Default.WithMaxDepth(2));

        var result = AssemblyFixture.Run(validator, Simple);

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_MoreAttributesThanAllowed_StopsWithTheCoreAttributeCode()
    {
        var validator = AssemblyFixture.Build(
            AssemblyFixture.Schema,
            limits: ValidationLimits.Default.WithMaxAttributesPerElement(1)
        );

        var result = AssemblyFixture.Run(validator, "<root a=\"1\" b=\"2\"/>");

        await Assert.That(result.Issues[^1].Code).IsEqualTo(CoreDiagnosticCodes.AttributeLimitExceeded);
    }

    [Test]
    public async Task Validate_EntityExpansionBeyondTheLimit_StopsWithTheCoreEntityCodes()
    {
        var validator = AssemblyFixture.Build(
            AssemblyFixture.Schema,
            dtd: true,
            limits: ValidationLimits.Default.WithMaxEntityExpansion(4).WithMaxEntityDepth(1)
        );

        var expansion = AssemblyFixture.Run(
            validator,
            "<!DOCTYPE root [<!ENTITY e \"0123456789\">]><root xmlns=\"urn:t\"><item>&e;</item></root>"
        );
        var depth = AssemblyFixture.Run(
            validator,
            "<!DOCTYPE root [<!ENTITY a \"x\"><!ENTITY b \"&a;\"><!ENTITY c \"&b;\">]><root xmlns=\"urn:t\"><item>&c;</item></root>"
        );

        await Assert.That(expansion.Issues[^1].Code).IsEqualTo(CoreDiagnosticCodes.EntityExpansionLimitExceeded);
        await Assert.That(depth.Issues[^1].Code).IsEqualTo(CoreDiagnosticCodes.EntityDepthLimitExceeded);
    }

    [Test]
    public async Task Validate_Doctype_IsRejectedByDefault_AndReadWithDtdProcessing()
    {
        const string Instance = "<!DOCTYPE root [<!ENTITY e \"1\">]><root xmlns=\"urn:t\"><item>&e;</item></root>";

        var rejected = AssemblyFixture.Run(AssemblyFixture.Schema, Instance);
        var read = AssemblyFixture.Run(AssemblyFixture.Schema, Instance, dtd: true);

        await Assert.That(rejected.IsValid).IsFalse();
        await Assert.That(rejected.Issues[^1].Code).IsEqualTo(XmlErrorCodes.XML2001.Code);
        await Assert.That(read.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_CanceledWhileTheStreamIsBuffered_ThrowsOperationCanceled()
    {
        using var source = new CancellationTokenSource();
        using var stream = new CancelingEndStream(Encoding.UTF8.GetBytes(Simple), source);

        await Assert.That(() => ValidateStream(stream, source.Token)).Throws<OperationCanceledException>();
    }

    [Test]
    public async Task ValidateAsync_CanceledWhileTheStreamIsBuffered_ThrowsOperationCanceled()
    {
        using var source = new CancellationTokenSource();
        using var stream = new CancelingEndStream(Encoding.UTF8.GetBytes(Simple), source);
        var validator = AssemblyFixture.Build(AssemblyFixture.Schema);

        await Assert
            .That(async () => await validator.ValidateAsync(stream, AssemblyFixture.InstanceId, source.Token))
            .Throws<OperationCanceledException>();
    }

    private static ValidationResult ValidateStream(Stream stream, CancellationToken token) =>
        AssemblyFixture.Build(AssemblyFixture.Schema).Validate(stream, AssemblyFixture.InstanceId, token);

    /// <summary>Cancels the token when the buffering ends, so the cancellation reaches the validator core.</summary>
    private sealed class CancelingEndStream(byte[] content, CancellationTokenSource source) : MemoryStream(content)
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = base.Read(buffer, offset, count);
            if (read == 0)
            {
                Fire();
            }

            return read;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = base.Read(buffer.Span);
            if (read == 0)
            {
                Fire();
            }

            return new ValueTask<int>(read);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => Task.FromResult(Read(buffer, offset, count));

        private void Fire() => source.Cancel();
    }
}
