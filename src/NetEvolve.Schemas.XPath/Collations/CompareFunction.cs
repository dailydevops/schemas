namespace NetEvolve.Schemas.XPath.Collations;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Sequences;
using NetEvolve.Schemas.XPath.Functions.Strings;

/// <summary><c>fn:compare</c> (Functions and Operators 1.0, 7.3.2).</summary>
internal sealed class CompareFunction : TextFunction
{
    private readonly CollationRegistry _registry;
    private readonly Uri? _staticBaseUri;

    /// <summary>Initializes a new instance of the <see cref="CompareFunction"/> class.</summary>
    public CompareFunction()
        : this(CollationRegistry.Default, null) { }

    /// <summary>Initializes a new instance of the <see cref="CompareFunction"/> class.</summary>
    /// <param name="registry">The collations.</param>
    /// <param name="staticBaseUri">The static base URI.</param>
    public CompareFunction(CollationRegistry registry, Uri? staticBaseUri)
        : base("compare", 2, 3, [OptionalString, OptionalString, OneString])
    {
        _registry = registry;
        _staticBaseUri = staticBaseUri;
    }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        _ = _registry.Resolve(CollationArgument.CodepointUri, _staticBaseUri);
        return XPathEvaluationStatus.Ok;
    }
}
