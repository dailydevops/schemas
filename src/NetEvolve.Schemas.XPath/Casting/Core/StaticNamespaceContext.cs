namespace NetEvolve.Schemas.XPath.Casting.Core;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Context;

/// <summary>
/// The namespace context of a cast: the prefixes of the static context, and for the empty prefix its default element namespace, which is the
/// namespace that an unprefixed lexical QName takes (XML Schema, QName).
/// </summary>
/// <remarks>Immutable and thread-safe, because the static context is.</remarks>
internal sealed class StaticNamespaceContext : INamespaceContext
{
    private readonly XPathStaticContext _context;

    /// <summary>Initializes a new instance of the <see cref="StaticNamespaceContext"/> class.</summary>
    /// <param name="context">The static context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public StaticNamespaceContext(XPathStaticContext context)
    {
        Argument.ThrowIfNull(context);

        _context = context;
    }

    /// <inheritdoc />
    public bool TryResolvePrefix(ReadOnlySpan<char> prefix, out string? namespaceName)
    {
        if (prefix.IsEmpty)
        {
            namespaceName = _context.DefaultElementNamespace;
            return namespaceName is not null;
        }

        return _context.TryResolveNamespace(prefix.ToString(), out namespaceName);
    }
}
