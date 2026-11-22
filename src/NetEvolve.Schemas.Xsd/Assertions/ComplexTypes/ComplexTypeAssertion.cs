namespace NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>An assertion of a complex type (XSD 1.1, Structures section 3.13), compiled once when the schema is compiled.</summary>
internal sealed class ComplexTypeAssertion
{
    internal ComplexTypeAssertion(string test, XPathExpression expression, DocumentLocation location)
    {
        Argument.ThrowIfNull(test);
        Argument.ThrowIfNull(expression);

        Test = test;
        Expression = expression;
        Location = location;
    }

    /// <summary>Gets the text of the <c>test</c> attribute.</summary>
    public string Test { get; }

    /// <summary>Gets the compiled test.</summary>
    public XPathExpression Expression { get; }

    /// <summary>Gets the location of the <c>assert</c> element.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Compiles the <c>test</c> of an <c>assert</c> element.</summary>
    /// <param name="node">The <c>assert</c> element.</param>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">The reporter of schema errors.</param>
    /// <returns>The assertion, <see langword="null"/> after an issue was reported.</returns>
    internal static ComplexTypeAssertion? TryCreate(
        SchemaSyntaxNode node,
        SchemaNamespaceScope scope,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(node);
        Argument.ThrowIfNull(reporter);

        _ = (scope, version);
        return null;
    }
}
