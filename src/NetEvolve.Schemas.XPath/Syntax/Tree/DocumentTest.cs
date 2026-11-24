namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>document-node()</c> (production DocumentTest).</summary>
internal sealed class DocumentTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="DocumentTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="element">Gets the element or schema-element test, or <see langword="null"/>.</param>
    public DocumentTest(SourceSpan location, KindTest? element)
        : base(location) => Element = element;

    /// <summary>Gets the element or schema-element test, or <see langword="null"/>.</summary>
    public KindTest? Element { get; }

    /// <inheritdoc />
    private protected override IReadOnlyList<SyntaxNode> CreateChildren()
    {
        List<SyntaxNode> children = [];
        if (Element is not null)
        {
            children.Add(Element);
        }
        return children.AsReadOnly();
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitDocumentTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitDocumentTest(this);
}
