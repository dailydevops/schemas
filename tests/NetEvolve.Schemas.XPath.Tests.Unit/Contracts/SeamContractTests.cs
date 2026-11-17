namespace NetEvolve.Schemas.XPath.Tests.Unit.Contracts;

using System;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

public class SeamContractTests
{
    private static bool IsPublic(Type type) => type.IsPublic || type.IsNestedPublic;

    [Test]
    public async Task Seam_FakeSubsetProfile_ReceivesParsedTreeAndCanReject()
    {
        var tree = new TreeFactory().Int("7");
        var accepting = new FakeSubsetProfile(reject: false);
        var rejecting = new FakeSubsetProfile(reject: true);

        var accepted = accepting.Accepts(tree, out var none);
        var rejected = rejecting.Accepts(tree, out var violation);

        await Assert.That(accepted).IsTrue();
        await Assert.That(none).IsEqualTo(default(XPathDiagnostic));
        await Assert.That(accepting.Received).IsSameReferenceAs(tree);
        await Assert.That(rejected).IsFalse();
        await Assert.That(rejecting.Received).IsSameReferenceAs(tree);
        await Assert.That(violation.Code).IsEqualTo("XPST0003");
    }

    [Test]
    public async Task Surface_SyntaxTreeTypes_AreNotPublic()
    {
        var assembly = typeof(XPathExpression).Assembly;
        var leaked = new System.Collections.Generic.List<string>();

        foreach (var type in assembly.GetTypes())
        {
            var seam = string.Equals(type.Namespace, "NetEvolve.Schemas.XPath.Abstractions", StringComparison.Ordinal)
                && type.Name == nameof(IXPathSubsetProfile);
            var tree = type.Namespace?.StartsWith("NetEvolve.Schemas.XPath.Syntax", StringComparison.Ordinal) == true;
            if ((seam || tree) && IsPublic(type))
            {
                leaked.Add(type.FullName!);
            }
        }

        await Assert.That(leaked).IsEmpty();
        await Assert.That(typeof(IXPathSubsetProfile).IsNotPublic).IsTrue();
    }

    [Test]
    public async Task Fakes_AreInternalToTheTestAssembly()
    {
        var leaked = new System.Collections.Generic.List<string>();

        foreach (var type in typeof(FakeNodeSource).Assembly.GetTypes())
        {
            if (type.Namespace == typeof(FakeNodeSource).Namespace && IsPublic(type))
            {
                leaked.Add(type.FullName!);
            }
        }

        await Assert.That(leaked).IsEmpty();
        await Assert.That(typeof(FakeNodeSource).IsNotPublic).IsTrue();
    }

    [Test]
    public async Task Resolver_Absent_RaisesFodc0002()
    {
        using var context = new XPathEvaluationContext();

        var failure = Assert.Throws<XPathException>(() => context.RequireResourceResolver());

        await Assert.That(context.ResourceResolver).IsNull();
        await Assert.That(failure.Diagnostic.Code).IsEqualTo("FODC0002");
    }

    [Test]
    public async Task Resolver_CoreResolver_IsAssignable()
    {
        var core = new DenyAllResourceResolver();
        using var context = new XPathEvaluationContext();

        var wrapped = XPathResourceResolverAdapter.Wrap(core);
        context.ResourceResolver = wrapped;

        await Assert.That(wrapped).IsNotNull();
        await Assert.That(wrapped).IsAssignableTo<IResourceResolver>();
        await Assert.That(context.RequireResourceResolver()).IsSameReferenceAs(wrapped);
        await Assert.That(XPathResourceResolverAdapter.Wrap(null)).IsNull();
        await Assert.That(XPathResourceResolverAdapter.Wrap(wrapped)).IsSameReferenceAs(wrapped);
        await Assert.That(typeof(IXPathResourceResolver).GetMembers(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)).IsEmpty();
    }

    [Test]
    public async Task Resolver_Adapter_DelegatesToTheCoreResolver()
    {
        var request = new ResourceRequest(ResourceKind.XPathDocument, "urn:x", null, null);
        var wrapped = XPathResourceResolverAdapter.Wrap(new DenyAllResourceResolver())!;

        using var result = wrapped.Resolve(request, default);

        await Assert.That(result.Status).IsEqualTo(ResourceStatus.Denied);
    }
}
