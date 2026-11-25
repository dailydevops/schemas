namespace NetEvolve.Schemas.Xml.Tests.Unit.DependencyInjection;

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NetEvolve.Schemas.Testing.Fakes;
using NetEvolve.Schemas.Xml.Contracts;

public class DtdRegistrationTests
{
    private const string Valid = "<!DOCTYPE note [<!ELEMENT note (#PCDATA)>]><note>text</note>";
    private const string Invalid = "<!DOCTYPE note [<!ELEMENT note EMPTY>]><note>text</note>";
    private const string OtherValid = "<!DOCTYPE memo [<!ELEMENT memo EMPTY>]><memo/>";
    private const string OtherInvalid = "<!DOCTYPE memo [<!ELEMENT memo EMPTY>]><memo>x</memo>";
    private const string SystemId = "http://example.org/doc/main.xml";
    private const string SubsetUri = "http://example.org/doc/ext.dtd";
    private const string ExternalSubsetDocument = "<!DOCTYPE r SYSTEM \"ext.dtd\">\n<r/>";

    private static bool IsValid(ISchemaValidator validator, string xml, CancellationToken cancellationToken) =>
        validator.Validate(xml.AsSpan(), null, cancellationToken).IsValid;

    [Test]
    public async Task Plain_ResolvesOneSingletonValidator_ThatAcceptsValidAndRejectsInvalidDocuments(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var services = new ServiceCollection().AddDtdValidator();
        using var provider = services.BuildServiceProvider();

        var validator = provider.GetRequiredService<IDtdValidator>();

        await Assert.That(services.Count).IsEqualTo(1);
        await Assert.That(services.Single().Lifetime).IsEqualTo(ServiceLifetime.Singleton);
        await Assert.That(provider.GetRequiredService<IDtdValidator>()).IsSameReferenceAs(validator);
        await Assert.That(IsValid(validator, Valid, cancellationToken)).IsTrue();
        var result = validator.Validate(Invalid.AsSpan(), null, cancellationToken);
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Keyed_ResolvesDistinctSingletonValidatorsPerKey(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var provider = new ServiceCollection()
            .AddKeyedDtdValidator("a")
            .AddKeyedDtdValidator("b")
            .BuildServiceProvider();

        var a = provider.GetRequiredKeyedService<IDtdValidator>("a");
        var b = provider.GetRequiredKeyedService<IDtdValidator>("b");

        await Assert.That(a).IsNotSameReferenceAs(b);
        await Assert.That(provider.GetRequiredKeyedService<IDtdValidator>("a")).IsSameReferenceAs(a);
        await Assert.That(provider.GetRequiredKeyedService<IDtdValidator>("b")).IsSameReferenceAs(b);
        await Assert.That(provider.GetRequiredKeyedService<ISchemaValidator>("a")).IsSameReferenceAs(a);
        await Assert.That(IsValid(a, Valid, cancellationToken)).IsTrue();
        await Assert.That(IsValid(a, Invalid, cancellationToken)).IsFalse();
        await Assert.That(IsValid(b, OtherValid, cancellationToken)).IsTrue();
        await Assert.That(IsValid(b, OtherInvalid, cancellationToken)).IsFalse();
    }

    [Test]
    public async Task WithoutAResolver_AnExternalSubset_IsRefusedAsResolverDenied(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var provider = new ServiceCollection().AddDtdValidator().AddKeyedDtdValidator("k").BuildServiceProvider();

        IDtdValidator[] validators =
        [
            provider.GetRequiredService<IDtdValidator>(),
            provider.GetRequiredKeyedService<IDtdValidator>("k"),
        ];
        foreach (var validator in validators)
        {
            var result = validator.Validate(ExternalSubsetDocument.AsSpan(), SystemId, cancellationToken);

            await Assert.That(result.IsValid).IsFalse();
            await Assert.That(result.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML2002.Code);
        }
    }

    [Test]
    public async Task ConfigureCallback_WithAnInMemoryResolver_LetsTheExternalSubsetValidate(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolver = new InMemoryResourceResolver().Add(SubsetUri, Encoding.UTF8.GetBytes("<!ELEMENT r EMPTY>"));
        using var provider = new ServiceCollection()
            .AddDtdValidator((_, builder) => builder.WithResolver(resolver).WithAllowExternalSubset(true))
            .AddKeyedDtdValidator("k", (_, _, builder) => builder.WithResolver(resolver).WithAllowExternalSubset(true))
            .BuildServiceProvider();

        await Assert
            .That(
                provider
                    .GetRequiredService<IDtdValidator>()
                    .Validate(ExternalSubsetDocument.AsSpan(), SystemId, cancellationToken)
                    .IsValid
            )
            .IsTrue();
        await Assert
            .That(
                provider
                    .GetRequiredKeyedService<IDtdValidator>("k")
                    .Validate(ExternalSubsetDocument.AsSpan(), SystemId, cancellationToken)
                    .IsValid
            )
            .IsTrue();
    }
}
