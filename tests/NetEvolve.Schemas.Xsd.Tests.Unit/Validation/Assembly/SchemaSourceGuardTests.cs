namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SchemaSourceGuardTests
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    [Test]
    public async Task AddSchema_NullStream_Throws() =>
        await Assert.That(() => SchemaValidator.ForXsd().AddSchema((Stream)null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Build_BytesOverwrittenAfterAddSchema_StillCompilesTheOriginal(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = Utf8.GetBytes(AssemblyFixture.Schema);
        var builder = SchemaValidator.ForXsd().AddSchema(bytes.AsMemory(), AssemblyFixture.SchemaId);
        Array.Clear(bytes);

        var validator = await builder.BuildAsync(cancellationToken);

        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance).IsValid).IsTrue();
    }

    [Test]
    public async Task Build_CharsOverwrittenAfterAddSchema_StillCompilesTheOriginal(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var chars = AssemblyFixture.Schema.ToCharArray();
        var builder = SchemaValidator.ForXsd().AddSchema(chars.AsMemory(), AssemblyFixture.SchemaId);
        Array.Clear(chars);

        var validator = await builder.BuildAsync(cancellationToken);

        await Assert.That(AssemblyFixture.Run(validator, AssemblyFixture.ValidInstance).IsValid).IsTrue();
    }

    [Test]
    public async Task Build_StreamThatCannotSeek_IsReadOnceAndReplayed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new UnseekableStream(Utf8.GetBytes(AssemblyFixture.Schema));
        var builder = SchemaValidator.ForXsd().AddSchema(stream, AssemblyFixture.SchemaId);

        var first = await builder.BuildAsync(cancellationToken);
        var second = await builder.BuildAsync(cancellationToken);

        await Assert.That(AssemblyFixture.Run(first, AssemblyFixture.ValidInstance).IsValid).IsTrue();
        await Assert.That(AssemblyFixture.Run(second, AssemblyFixture.ValidInstance).IsValid).IsTrue();
    }

    private sealed class UnseekableStream(byte[] content) : MemoryStream(content)
    {
        public override bool CanSeek => false;
    }
}
