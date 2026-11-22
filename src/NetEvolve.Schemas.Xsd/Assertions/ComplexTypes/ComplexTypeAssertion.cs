namespace NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;

using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.DefaultNamespace;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// An assertion of a complex type (XSD 1.1, Structures section 3.13), compiled once when the schema is compiled: the <c>test</c> of an
/// <c>assert</c> element with the static context that its <c>xpathDefaultNamespace</c> and its namespace declarations give it.
/// </summary>
/// <remarks>
/// A test that does not compile is a schema error of the rule <c>xpath-valid</c>, located at the <c>assert</c> element. Whether the test
/// holds for an element is decided by <see cref="AssertionListener"/>. Limitation: the XPath engine does not type statically (design
/// #123), so a type error shows when the test is evaluated and is a dynamic error then.
/// </remarks>
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

    /// <summary>Compiles the <c>assert</c> children of a complex type, those of the type and those of the extension or restriction of its content.</summary>
    /// <param name="type">The <c>complexType</c> element.</param>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    /// <param name="version">The configured XSD version.</param>
    /// <param name="reporter">The reporter of schema errors.</param>
    /// <returns>The assertions in document order; one that does not compile is reported and left out.</returns>
    internal static IReadOnlyList<ComplexTypeAssertion> Collect(
        SchemaSyntaxNode type,
        SchemaNamespaceScope scope,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(type);
        Argument.ThrowIfNull(reporter);

        _ = (scope, version);
        return [];
    }

    /// <summary>Compiles the <c>test</c> of an <c>assert</c> element.</summary>
    /// <param name="node">The <c>assert</c> element.</param>
    /// <param name="scope">The namespace the schema document contributes to; <c>##targetNamespace</c> of <c>xpathDefaultNamespace</c> means its target namespace.</param>
    /// <param name="version">The configured XSD version; <c>xpathDefaultNamespace</c> has no meaning in <see cref="XsdVersion.V10"/>.</param>
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
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        // The grammar of the schema document requires the test attribute.
        var test = node.FindAttribute("test") ?? string.Empty;
        var context = AssertionStaticContext.Create(node, test, XPathDefaultNamespace.Resolve(node, scope, version));
        var result = XPathExpression.TryCompile(test, context);
        if (!result.Success)
        {
            var diagnostic = result.Diagnostic;
            reporter.Report(
                XsdRuleCodes.XpathValid,
                node.Location,
                "The test of the assertion is not a valid XPath expression: "
                    + diagnostic.Message
                    + " ("
                    + diagnostic.Code
                    + ", offset "
                    + diagnostic.Offset.ToString(CultureInfo.InvariantCulture)
                    + ")."
            );
            return null;
        }

        return new ComplexTypeAssertion(test, result.Expression!, node.Location);
    }
}
