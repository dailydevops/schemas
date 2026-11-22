namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.DefaultNamespace;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// Builds a type alternative (Structures 1.1, 3.12.2) from the syntax of an <c>alternative</c> element and compiles its
/// <c>test</c> once, with the XPath package (design #146, DP-14). The type reference stays unresolved until the resolver runs.
/// </summary>
/// <remarks>
/// <para>
/// The static context of the test binds the namespace prefixes that the expression uses to the declarations in scope at the
/// element, plus <c>xs</c>, <c>xsi</c>, and <c>fn</c> as XPath 2.0 predeclares them (the XPath package does not, #876), and takes
/// <c>xpathDefaultNamespace</c> as the default element namespace (<see cref="XPathDefaultNamespace"/>). A static error of the
/// expression is a schema error (<c>xpath-valid</c>); the alternative is still built without a compiled test, so it never matches and the
/// table keeps its shape.
/// </para>
/// <para>
/// Decisions: exactly one of the <c>type</c> attribute and an anonymous type must be present (<c>src-ta</c>); an alternative
/// without either is reported and has no type.
/// </para>
/// <para>
/// Limitation: the syntax must come from <see cref="SchemaDocumentParser"/>, whose grammar already restricted the attributes. The
/// element is always read as XSD 1.1, because the construct is a 1.1 one and strict 1.0 mode reports it.
/// </para>
/// </remarks>
internal sealed class TypeAlternativeBuilder : IComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="TypeAlternativeBuilder"/> class.</summary>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    internal TypeAlternativeBuilder(SchemaNamespaceScope scope) => throw new NotSupportedException();

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.TypeAlternative;

    /// <inheritdoc />
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter) =>
        throw new NotSupportedException();

    /// <summary>Builds an alternative from an <c>alternative</c> syntax node.</summary>
    /// <param name="node">The syntax.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The alternative.</returns>
    internal TypeAlternative BuildAlternative(SchemaSyntaxNode node, XsdIssueReporter reporter) =>
        throw new NotSupportedException();
}
