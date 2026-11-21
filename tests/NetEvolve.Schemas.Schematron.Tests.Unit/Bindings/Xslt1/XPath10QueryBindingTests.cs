namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Xslt1;

using System;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Bindings.Xslt1;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;

/// <summary>
/// The default bindings: a missing <c>queryBinding</c> and the names <c>xslt</c>, <c>xslt1</c>, and <c>xpath</c> resolve to the binding
/// whose static context turns XPath 1.0 compatibility mode on (SCH-16, #310); <c>xpath2</c>, <c>xslt2</c>, and <c>xslt3</c> keep the
/// XPath 2.0 contract.
/// </summary>
public class XPath10QueryBindingTests
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

    private static XPath10QueryBinding Resolve(string? queryBinding)
    {
        var ok = QueryBindingRegistry.Default.TryResolve(Schema(queryBinding), out var binding, out _);
        return ok ? (XPath10QueryBinding)binding : throw new InvalidOperationException(queryBinding);
    }

    private static QueryStaticScope Scope(string? baseUri = null) =>
        new([], CompatibilityFixture.VariableNames, baseUri, SchematronEdition.Iso2016);

    // The binding is resolved through the registry first, so a name that does not reach the compatibility binding fails the test.
    private static string Run(string? queryBinding, string expression)
    {
        _ = Resolve(queryBinding);
        return CompatibilityFixture.Run(expression, XPath10QueryBinding.CreateStaticContext(Scope()));
    }

    [Test]
    [Arguments(null, "xslt", true)]
    [Arguments("xslt", "xslt", true)]
    [Arguments("XSLT", "xslt", true)]
    [Arguments("xslt1", "xslt1", true)]
    [Arguments("xpath", "xpath", false)]
    public async Task Resolve_DefaultNames_ReturnTheCompatibilityBinding(
        string? queryBinding,
        string name,
        bool allowsXsltInstructions
    )
    {
        var ok = QueryBindingRegistry.Default.TryResolve(Schema(queryBinding), out var binding, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(binding).IsTypeOf<XPath10QueryBinding>();
        await Assert.That(binding.Name).IsEqualTo(name);
        await Assert.That(binding.AllowsXsltInstructions).IsEqualTo(allowsXsltInstructions);
    }

    [Test]
    [Arguments("xpath2")]
    [Arguments("xslt2")]
    [Arguments("xslt3")]
    public async Task Resolve_OtherSupportedNames_KeepTheXPath20Binding(string queryBinding)
    {
        var ok = QueryBindingRegistry.Default.TryResolve(Schema(queryBinding), out var binding, out _);

        await Assert.That(ok).IsTrue();
        await Assert.That(binding).IsTypeOf<XPathQueryBinding>();
    }

    [Test]
    [Arguments(null)]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    [Arguments("xpath")]
    public async Task StaticContext_OfTheDefaultNames_TurnsCompatibilityModeOn(string? queryBinding)
    {
        _ = Resolve(queryBinding);
        var statics = XPath10QueryBinding.CreateStaticContext(Scope());

        await Assert.That(statics.XPath10Compatibility).IsTrue();
        await Assert.That(statics.Version).IsEqualTo(XPathStaticContext.Default.Version);
    }

    [Test]
    [Arguments(null)]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    [Arguments("xpath")]
    public async Task Expression_WithTwoNodes_ConvertsToTheStringOfTheFirstNode(string? queryBinding)
    {
        await Assert.That(Run(queryBinding, "string($a)")).IsEqualTo("string:1");
        await Assert.That(Run(queryBinding, "string-length($a)")).IsEqualTo("integer:1");
        await Assert.That(Run(queryBinding, "concat($a, '-', $b)")).IsEqualTo("string:1-");
    }

    [Test]
    [Arguments(null)]
    [Arguments("xslt")]
    [Arguments("xslt1")]
    [Arguments("xpath")]
    public async Task Expression_ComparingANodeSetWithANumber_UsesNumberComparison(string? queryBinding)
    {
        // The empty element b is the empty string: 1.0 converts it to NaN, which equals nothing; 2.0 casts it and fails.
        await Assert.That(Run(queryBinding, "$b = 1")).IsEqualTo("boolean:false");
        await Assert.That(Run(queryBinding, "$b != 1")).IsEqualTo("boolean:true");
        await Assert.That(Run(queryBinding, "$a = 2")).IsEqualTo("boolean:true");
        await Assert.That(Run(queryBinding, "$a > 1")).IsEqualTo("boolean:true");
    }

    [Test]
    public async Task Expression_WithoutCompatibilityMode_FailsWhereTheDefaultNamesSucceed()
    {
        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVariable(new XPathQName(string.Empty, "a"))
            .WithVariable(new XPathQName(string.Empty, "b"))
            .Build();

        await Assert.That(statics.XPath10Compatibility).IsFalse();
        await Assert.That(CompatibilityFixture.Run("string($a)", statics)).IsEqualTo("XPTY0004");
        await Assert.That(CompatibilityFixture.Run("$b = 1", statics)).IsEqualTo("FORG0001");
    }

    [Test]
    public async Task StaticContext_CarriesPrefixedNamespacesVariablesAndBaseUri()
    {
        var scope = new QueryStaticScope(
            [
                new SchematronNamespace(SchemaAt, "p", "urn:p"),
                new SchematronNamespace(SchemaAt, string.Empty, "urn:default"),
            ],
            ["x", string.Empty, "y"],
            "http://example.com/base.sch",
            SchematronEdition.Iso2016
        );

        var statics = XPath10QueryBinding.CreateStaticContext(scope);

        await Assert.That(statics.TryResolveNamespace("p", out var uri)).IsTrue();
        await Assert.That(uri).IsEqualTo("urn:p");
        await Assert.That(statics.TryResolveNamespace(string.Empty, out _)).IsFalse();
        await Assert.That(statics.TryGetVariableType(new XPathQName(string.Empty, "x"), out _)).IsTrue();
        await Assert.That(statics.TryGetVariableType(new XPathQName(string.Empty, "y"), out _)).IsTrue();
        await Assert.That(statics.TryGetVariableType(new XPathQName(string.Empty, "z"), out _)).IsFalse();
        await Assert.That(statics.BaseUri).IsEqualTo(new Uri("http://example.com/base.sch"));
    }

    [Test]
    [Arguments(null)]
    [Arguments("relative/base.sch")]
    public async Task StaticContext_WithoutAnAbsoluteBaseUri_HasNone(string? declared)
    {
        var statics = XPath10QueryBinding.CreateStaticContext(Scope(declared));

        await Assert.That(statics.BaseUri).IsNull();
    }

    [Test]
    public async Task StaticContext_WithANullScope_Throws() =>
        await Assert.That(() => XPath10QueryBinding.CreateStaticContext(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Constructor_WithANullName_Throws() =>
        await Assert.That(() => new XPath10QueryBinding(null!, false)).Throws<ArgumentNullException>();

    [Test]
    public async Task Compile_AndRun_UseTheCompatibilityMode()
    {
        var binding = Resolve(null);
        var ok = binding.TryCompile("'abc' = 1", QueryExpressionKind.Boolean, Scope(), out var expression, out _);
        using var run = binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);

        var status = run.TryEvaluateBoolean(expression, default, out var value);

        await Assert.That(ok).IsTrue();
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(value).IsFalse();
    }
}
