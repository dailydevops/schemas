namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary>A QName whose prefix stays unresolved (name resolution is XP-52).</summary>
internal sealed class QualifiedName : SyntaxNode
{
    /// <summary>Initializes a new instance of the <see cref="QualifiedName"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="prefix">Gets the prefix, or <see langword="null"/> when the name has none.</param>
    /// <param name="localName">Gets the local name.</param>
    public QualifiedName(SourceSpan location, string? prefix, string localName)
        : base(location)
    {
        Prefix = prefix;
        LocalName = localName;
    }

    /// <summary>Gets the prefix, or <see langword="null"/> when the name has none.</summary>
    public string? Prefix { get; }

    /// <summary>Gets the local name.</summary>
    public string LocalName { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children => [];

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitQualifiedName(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitQualifiedName(this);
}
