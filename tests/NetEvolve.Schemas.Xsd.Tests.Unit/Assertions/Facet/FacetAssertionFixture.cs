namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.Facet;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.Xsd.Assertions.Facet;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

/// <summary>Builds simple types that carry assertion facets over the real datatypes, runs the evaluation, and reads the issues it reported.</summary>
internal sealed class FacetAssertionFixture : IDisposable
{
    internal static readonly DocumentLocation Value = new("file:///i.xml", 9, 4);

    private readonly CountingResolver _resolver = new();
    private readonly Dictionary<SimpleTypeDefinition, IReadOnlyList<FacetAssertion>> _own = [];

    internal FacetAssertionFixture()
    {
        Types = new FacetFixture();
        Context = new XPathEvaluationContext { ResourceResolver = _resolver };
    }

    internal FacetFixture Types { get; }

    internal XPathEvaluationContext Context { get; }

    /// <summary>Gets how often the evaluation asked the resolver, the one route through which an expression could load a document into a tree.</summary>
    internal int ResolverCalls => _resolver.Calls;

    internal IReadOnlyList<ValidationIssue> Issues => Types.Issues;

    /// <summary>The static context of an assertion: the prefix <c>xs</c> bound, as the schema document of a test would.</summary>
    internal static XPathStaticContext StaticContext { get; } =
        XPathStaticContext.Default.ToBuilder().WithNamespace("xs", FacetFixture.XsNamespace).Build();

    /// <summary>Compiles an assertion that must compile.</summary>
    internal FacetAssertion Assertion(string test, int line = 12)
    {
        ArgumentNullException.ThrowIfNull(test);

        return FacetAssertion.TryCreate(
            test,
            StaticContext,
            new DocumentLocation("file:///s.xsd", line, 7),
            Types.Reporter
        ) ?? throw new InvalidOperationException("The assertion does not compile: " + test);
    }

    /// <summary>A base type (see <see cref="FacetFixture.Base"/>) that declares the given assertions.</summary>
    internal SimpleTypeDefinition Base(string type, params string[] tests) => Declare(Types.Base(type), tests);

    /// <summary>A restriction of a type that declares the given assertions besides those it inherits.</summary>
    internal SimpleTypeDefinition Derive(SimpleTypeDefinition parent, params string[] tests) =>
        Declare(Types.Derive(parent), tests);

    internal bool Check(SimpleTypeDefinition type, string text) =>
        FacetAssertionEvaluator.Check(Table(), type, text, Types.Assessor, Context, Value, Types.Reporter);

    internal FacetAssertions Table() => new(_own);

    public void Dispose() => Context.Dispose();

    private SimpleTypeDefinition Declare(SimpleTypeDefinition type, string[] tests)
    {
        var own = new List<FacetAssertion>(tests.Length);
        foreach (var test in tests)
        {
            own.Add(Assertion(test));
        }

        _own[type] = own;
        return type;
    }

    private sealed class CountingResolver : IXPathResourceResolver
    {
        internal int Calls { get; private set; }

        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ResourceResult.NotFound();
        }
    }
}
