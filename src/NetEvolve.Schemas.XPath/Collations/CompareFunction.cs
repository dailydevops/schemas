namespace NetEvolve.Schemas.XPath.Collations;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:compare</c> (Functions and Operators 1.0, 7.3.2): -1, 0, or 1 for the order of two strings in a collation, and the empty sequence
/// when an argument is the empty sequence. Without a collation argument the codepoint collation applies. The collation argument is
/// looked up in the registry (FOCH0002 when it is not registered) before the arguments are read, so an unknown collation is an error
/// also for an empty argument.
/// </summary>
/// <remarks>
/// Limitation: a call reaches the evaluation context only, which carries neither the static base URI nor the host collations, so the
/// function holds a registry and a static base URI. The one in the default catalog holds the built-in registry and no base URI: a
/// relative collation URI is FOCH0002 there, and a host that wants a relative URI or its own collation registers a function built over
/// its own registry (the layering of the function registry lets it override this one). Wiring the static context to a call is #752; the default
/// collation of the static context is XP-08 (#187).
/// </remarks>
internal sealed class CompareFunction : TextFunction
{
    private readonly CollationRegistry _registry;
    private readonly Uri? _staticBaseUri;

    /// <summary>Initializes a new instance of the <see cref="CompareFunction"/> class over the built-in collations and no static base URI.</summary>
    public CompareFunction()
        : this(CollationRegistry.Default, null) { }

    /// <summary>Initializes a new instance of the <see cref="CompareFunction"/> class.</summary>
    /// <param name="registry">The collations a collation argument can name.</param>
    /// <param name="staticBaseUri">The static base URI against which a relative collation URI is resolved, <see langword="null"/> for none.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    public CompareFunction(CollationRegistry registry, Uri? staticBaseUri)
        : base("compare", 2, 3, [OptionalString, OptionalString, OneString])
    {
        ArgumentNullException.ThrowIfNull(registry);

        _registry = registry;
        _staticBaseUri = staticBaseUri;
    }

    /// <inheritdoc />
    /// <exception cref="Errors.XPathException">The collation is not registered (FOCH0002).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        var collation =
            arguments.Length > 2 ? _registry.Resolve(Text(arguments[2]), _staticBaseUri) : CodepointCollation.Instance;

        if (!arguments[0].IsEmpty && !arguments[1].IsEmpty)
        {
            // A host collation may answer with any number; the function answers with the sign.
            var order = Math.Sign(collation.Compare(Text(arguments[0]), Text(arguments[1])));
            invocation.Result = XdmSequence.Create(XdmItem.FromInteger(order));
        }

        return XPathEvaluationStatus.Ok;
    }
}
