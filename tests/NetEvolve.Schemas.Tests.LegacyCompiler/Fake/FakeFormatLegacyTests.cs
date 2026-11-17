namespace NetEvolve.Schemas.Tests.LegacyCompiler.Fake;

/// <summary>
/// Compiled with C# 13 (no extension member syntax): the entry point is reached through the named static extension class.
/// </summary>
public class FakeFormatLegacyTests
{
    [Test]
    public async Task ForFake_BuildsValidatorReceivingContent(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = FakeSchemaValidatorExtensions.ForFake();
        var validator = await builder.CreateFakeValidatorAsync(cancellationToken);

        var result = validator.Validate("<a/>"u8, "file:///doc.xml", cancellationToken);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(validator.Calls).Count().IsEqualTo(1);
        await Assert.That(validator.Calls[0].SystemId).IsEqualTo("file:///doc.xml");
    }
}
