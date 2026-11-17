namespace NetEvolve.Schemas.Tests.LegacyCompiler.Fake;

/// <summary>
/// Compiled with C# 13 (no extension member syntax): the entry point is reached through the named static extension class.
/// Limitation: the fake validator returns no <see cref="ValidationResult"/> until FND-09 lands, so the tests assert the recorded call.
/// </summary>
public class FakeFormatLegacyTests
{
    [Test]
    public async Task ForFake_BuildsValidatorReceivingContent(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var builder = FakeSchemaValidatorExtensions.ForFake();
        var validator = await builder.CreateFakeValidatorAsync(cancellationToken);

        _ = validator.Validate("<a/>"u8, "file:///doc.xml", cancellationToken);

        await Assert.That(validator.Calls).Count().IsEqualTo(1);
        await Assert.That(validator.Calls[0].SystemId).IsEqualTo("file:///doc.xml");
    }

    [Test]
    public async Task NamedExtensionClass_IsPublicStaticInEntryPointNamespace(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var type = typeof(FakeSchemaValidatorExtensions);

        await Assert.That(type.IsPublic).IsTrue();
        await Assert.That(type.IsAbstract && type.IsSealed).IsTrue();
        await Assert.That(type.Name).IsEqualTo("FakeSchemaValidatorExtensions");
        await Assert.That(type.Namespace).IsEqualTo("NetEvolve.Schemas");
    }
}
