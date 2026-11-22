namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// A type alternative (Structures 1.1, 3.12): a test and the type that an element takes when the test is true. An alternative
/// without a test is the default alternative. The type is a by-name reference that the resolver binds; an anonymous type stays as
/// syntax until the type builders construct it, as for an element declaration.
/// </summary>
/// <remarks>
/// Limitations: the compilation pipeline neither builds nor resolves alternatives until it is wired (XSD-78), so a caller binds the
/// reference with <see cref="Bind"/> or <see cref="BindInlineType"/>. The test is compiled with the full XPath 2.0 of the XPath
/// package, not the subset that Structures 1.1 requires at least; the base URI of the expression is not set.
/// </remarks>
internal sealed class TypeAlternative : IReferencingComponent
{
    private readonly XsdReference[] _references;

    private TypeAlternative(
        DocumentLocation location,
        XPathExpression? test,
        bool isDefault,
        XsdReference[] references,
        ISchemaSyntaxElement? inlineTypeSyntax
    )
    {
        Location = location;
        Test = test;
        IsDefault = isDefault;
        _references = references;
        InlineTypeSyntax = inlineTypeSyntax;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.TypeAlternative;

    /// <inheritdoc />
    /// <remarks>A type alternative has no name.</remarks>
    public XPathQName Name => default;

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <inheritdoc />
    public int ReferenceCount => _references.Length;

    /// <summary>Gets the compiled test, <see langword="null"/> for the default alternative and for a test that did not compile.</summary>
    public XPathExpression? Test { get; }

    /// <summary>Gets a value indicating whether this is the default alternative, which has no test.</summary>
    public bool IsDefault { get; }

    /// <summary>Gets the type definition once it is bound, <see langword="null"/> while it is unresolved or anonymous and not yet constructed.</summary>
    public IXsdComponent? TypeDefinition { get; private set; }

    /// <summary>Gets the syntax of the anonymous type, <see langword="null"/> when the type is named.</summary>
    public ISchemaSyntaxElement? InlineTypeSyntax { get; }

    /// <inheritdoc />
    public XsdReference GetReference(int index) => _references[index];

    /// <inheritdoc />
    public void Bind(int index, IXsdComponent target)
    {
        Argument.ThrowIfNull(target);

        TypeDefinition = target;
    }

    /// <summary>Receives the type that the type builders constructed from <see cref="InlineTypeSyntax"/>.</summary>
    /// <param name="type">The anonymous type.</param>
    internal void BindInlineType(IXsdComponent type)
    {
        Argument.ThrowIfNull(type);

        TypeDefinition = type;
    }

    /// <summary>Creates an alternative whose type is unresolved.</summary>
    /// <param name="location">The location of the <c>alternative</c> element.</param>
    /// <param name="test">The compiled test, <see langword="null"/> for the default alternative or when the test did not compile.</param>
    /// <param name="isDefault">Whether the alternative has no test, so it is the default alternative.</param>
    /// <param name="typeName">The expanded name of the type, <see langword="default"/> for an anonymous type or none.</param>
    /// <param name="inlineTypeSyntax">The anonymous type, <see langword="null"/> when <paramref name="typeName"/> is given or there is none.</param>
    /// <returns>The alternative.</returns>
    internal static TypeAlternative Create(
        DocumentLocation location,
        XPathExpression? test,
        bool isDefault,
        XPathQName typeName,
        ISchemaSyntaxElement? inlineTypeSyntax
    ) =>
        new(
            location,
            test,
            isDefault,
            typeName == default ? [] : [new XsdReference(XsdSymbolSpace.Types, typeName, location, false)],
            inlineTypeSyntax
        );
}
