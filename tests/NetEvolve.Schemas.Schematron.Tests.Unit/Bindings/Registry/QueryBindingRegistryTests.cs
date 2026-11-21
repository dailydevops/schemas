namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Registry;

using System;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

public class QueryBindingRegistryTests
{
    private static readonly DocumentLocation SchemaAt = new("binding.sch", 4, 7);

    private static SchematronSchema Schema(string? queryBinding) =>
        new(
            SchemaAt,
            null,
            [],
            [],
            [],
            [],
            null,
            null,
            null,
            null,
            null,
            SchematronEdition.Iso2016,
            null,
            queryBinding,
            [],
            [],
            [],
            []
        );

    [Test]
    [Arguments("xpath2")]
    [Arguments("xslt2")]
    public async Task Resolve_Xpath2AndXslt2_ReturnXPathBinding(string name)
    {
        var ok = QueryBindingRegistry.Default.TryResolve(Schema(name), out var binding, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(binding).IsTypeOf<XPathQueryBinding>();
        await Assert.That(binding.Name).IsEqualTo(name);
    }

    [Test]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    [Arguments("xslt3")]
    [Arguments("xpath")]
    public async Task Resolve_SubsetNamesOfDesignAp25_AreAccepted(string name)
    {
        var ok = QueryBindingRegistry.Default.TryResolve(Schema(name), out var binding, out _);

        await Assert.That(ok).IsTrue();
        await Assert
            .That(binding.AllowsXsltInstructions)
            .IsEqualTo(!string.Equals(name, "xpath", StringComparison.Ordinal));
    }

    [Test]
    public async Task Resolve_MissingQueryBinding_ReturnsXsltBinding()
    {
        var ok = QueryBindingRegistry.Default.TryResolve(Schema(null), out var binding, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(binding.Name).IsEqualTo("xslt");
    }

    [Test]
    [Arguments("xquery")]
    [Arguments("xquery3")]
    [Arguments("xquery31")]
    [Arguments("xquery4")]
    [Arguments("xpath4")]
    [Arguments("xslt4")]
    [Arguments("exslt")]
    [Arguments("stx")]
    [Arguments("xslt1.1")]
    [Arguments("xpath3")]
    [Arguments("xpath31")]
    [Arguments("nope")]
    public async Task Resolve_UnsupportedName_RaisesBindingUnsupported(string name)
    {
        var ok = QueryBindingRegistry.Default.TryResolve(Schema(name), out _, out var problem);

        await Assert.That(ok).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.BindingUnsupported);
        await Assert.That(problem.Message).Contains($"'{name}'");
    }

    [Test]
    public async Task Resolve_UnsupportedName_MessageListsExactlyTheRegisteredNames()
    {
        QueryBindingRegistry.Default.TryResolve(Schema("nope"), out _, out var problem);

        await Assert
            .That(problem.Message)
            .IsEqualTo(
                "The queryBinding 'nope' is not supported. Supported bindings: xpath, xpath2, xslt, xslt1, xslt2, xslt3."
            );
    }

    [Test]
    public async Task Resolve_UnsupportedName_CarriesLocationOfSchemaStartTag()
    {
        QueryBindingRegistry.Default.TryResolve(Schema("nope"), out _, out var problem);

        await Assert.That(problem.Location).IsEqualTo(SchemaAt);
    }

    [Test]
    public async Task Register_FakeBinding_ResolvesWithoutChangingResolver()
    {
        var fake = new FakeQueryBinding();
        var registry = new QueryBindingRegistry([fake]);

        var ok = registry.TryResolve(Schema("fake"), out var binding, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(binding).IsSameReferenceAs(fake);
    }

    [Test]
    [Arguments("XSLT2")]
    [Arguments("Xslt2")]
    [Arguments("xslt2")]
    public async Task Resolve_AnyCasing_ReturnsTheSameBinding(string name)
    {
        QueryBindingRegistry.Default.TryResolve(Schema("xslt2"), out var expected, out _);

        var ok = QueryBindingRegistry.Default.TryResolve(Schema(name), out var binding, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(binding).IsSameReferenceAs(expected);
    }

    [Test]
    [Arguments("XQUERY")]
    [Arguments("Nope")]
    public async Task Resolve_UnknownNameInAnyCasing_RaisesBindingUnsupported(string name)
    {
        var ok = QueryBindingRegistry.Default.TryResolve(Schema(name), out _, out var problem);

        await Assert.That(ok).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.BindingUnsupported);
    }

    [Test]
    public async Task XPathBinding_CompilesAnExpressionOfTheScope()
    {
        QueryBindingRegistry.Default.TryResolve(Schema("xpath2"), out var binding, out _);
        var scope = new QueryStaticScope([], [], null, SchematronEdition.Iso2016);

        var ok = binding.TryCompile("1 = 1", QueryExpressionKind.Boolean, scope, out var expression, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(expression).IsNotNull();
    }

    [Test]
    public async Task Constructor_Null_Throws() =>
        await Assert.That(() => new QueryBindingRegistry(null!)).Throws<ArgumentNullException>();
}
