namespace NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;

using System;
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
/// A test that does not compile is a schema error of the rule <c>as-props-correct</c> (Assertion Properties Correct, design #146 DP-17), located at the <c>assert</c> element. Whether the test
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

        var found = new List<ComplexTypeAssertion>();
        Add(type, scope, version, reporter, found);
        for (var i = 0; i < type.ChildCount; i++)
        {
            var content = type.GetChildNode(i);
            if (!IsSchemaElement(content, "complexContent") && !IsSchemaElement(content, "simpleContent"))
            {
                continue;
            }

            for (var j = 0; j < content.ChildCount; j++)
            {
                var derivation = content.GetChildNode(j);
                if (IsSchemaElement(derivation, "extension") || IsSchemaElement(derivation, "restriction"))
                {
                    Add(derivation, scope, version, reporter, found);
                }
            }
        }

        return found;
    }

    private static void Add(
        SchemaSyntaxNode owner,
        SchemaNamespaceScope scope,
        XsdVersion version,
        XsdIssueReporter reporter,
        List<ComplexTypeAssertion> found
    )
    {
        for (var i = 0; i < owner.ChildCount; i++)
        {
            var child = owner.GetChildNode(i);
            if (IsSchemaElement(child, "assert") && TryCreate(child, scope, version, reporter) is { } assertion)
            {
                found.Add(assertion);
            }
        }
    }

    private static bool IsSchemaElement(SchemaSyntaxNode node, string localName) =>
        string.Equals(node.Name.LocalName, localName, StringComparison.Ordinal)
        && string.Equals(node.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal);

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
                XsdRuleCodes.AsPropsCorrect,
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
