namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>Name test or wildcard (productions NameTest and Wildcard).</summary>
internal sealed class NameTest : NodeTest
{
    /// <summary>Initializes a new instance of the <see cref="NameTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="kind">Gets the kind.</param>
    /// <param name="prefix">Gets the prefix; set for a QName that has one and for <c>prefix:*</c>, <see langword="null"/> otherwise.</param>
    /// <param name="localName">Gets the local name; set for a QName and for <c>*:local</c>, <see langword="null"/> otherwise.</param>
    /// <param name="namespaceUri">Gets the namespace URI of a <c>Q{uri}local</c> name, otherwise <see langword="null"/>.</param>
    public NameTest(
        SourceSpan location,
        NameTestKind kind,
        string? prefix,
        string? localName,
        string? namespaceUri = null
    )
        : base(location)
    {
        Kind = kind;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = namespaceUri;
    }

    /// <summary>Gets the kind.</summary>
    public NameTestKind Kind { get; }

    /// <summary>Gets the prefix; set for a QName that has one and for <c>prefix:*</c>, <see langword="null"/> otherwise.</summary>
    public string? Prefix { get; }

    /// <summary>Gets the local name; set for a QName and for <c>*:local</c>, <see langword="null"/> otherwise.</summary>
    public string? LocalName { get; }

    /// <summary>Gets the namespace URI of a <c>Q{uri}local</c> name, otherwise <see langword="null"/>.</summary>
    public string? NamespaceUri { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitNameTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitNameTest(this);
}
