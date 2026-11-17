namespace NetEvolve.Schemas.XPath.Syntax.Tree;

using System.Collections.Generic;

/// <summary><c>element()</c> (productions ElementTest, ElementNameOrWildcard, ElementName).</summary>
internal sealed class ElementTest : KindTest
{
    /// <summary>Initializes a new instance of the <see cref="ElementTest"/> class.</summary>
    /// <param name="location">The source location.</param>
    /// <param name="name">Gets the element name, or <see langword="null"/> for <c>*</c> or none.</param>
    /// <param name="typeName">Gets the type name (production TypeName), or <see langword="null"/>.</param>
    /// <param name="isNillable">Gets a value indicating whether the type name is followed by <c>?</c>.</param>
    public ElementTest(SourceSpan location, QualifiedName? name, QualifiedName? typeName, bool isNillable)
        : base(location)
    {
        Name = name;
        TypeName = typeName;
        IsNillable = isNillable;
    }

    /// <summary>Gets the element name, or <see langword="null"/> for <c>*</c> or none.</summary>
    public QualifiedName? Name { get; }

    /// <summary>Gets the type name (production TypeName), or <see langword="null"/>.</summary>
    public QualifiedName? TypeName { get; }

    /// <summary>Gets a value indicating whether the type name is followed by <c>?</c>.</summary>
    public bool IsNillable { get; }

    /// <inheritdoc />
    public override IReadOnlyList<SyntaxNode> Children
    {
        get
        {
            List<SyntaxNode> children = [];
            if (Name is not null)
            {
                children.Add(Name);
            }
            if (TypeName is not null)
            {
                children.Add(TypeName);
            }
            return children.AsReadOnly();
        }
    }

    /// <inheritdoc />
    public override void Accept(SyntaxVisitor visitor) => visitor.VisitElementTest(this);

    /// <inheritdoc />
    public override TResult? Accept<TResult>(SyntaxVisitor<TResult> visitor)
        where TResult : default => visitor.VisitElementTest(this);
}
