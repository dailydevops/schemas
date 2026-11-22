namespace NetEvolve.Schemas.Xsd.Assertions.Facet;

using System.Globalization;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// An assertion facet of a simple type (XSD 1.1, Datatypes section 4.3.13): the <c>test</c> of an <c>assertion</c> element, compiled once when
/// the schema is compiled with the variable <c>$value</c> declared.
/// </summary>
/// <remarks>
/// <para>
/// A test that does not compile is a schema error of the rule <c>xpath-valid</c>, located at the <c>assertion</c> element, the same rule that
/// the assertions of a complex type use. Which assertions a type holds, with the ones it inherits, is <see cref="FacetAssertions"/>; whether a
/// value satisfies a test is decided by <see cref="FacetAssertionEvaluator"/>, with no tree and no focus.
/// </para>
/// <para>
/// Limitations: the type is not a facet of the Xml package, because the architecture tests allow the Xsd package no facet of its own (#193),
/// so the assertions are held beside the type, not in its facet list; the caller builds the static context, which holds the namespace
/// bindings of the <c>assertion</c> element and its resolved <c>xpathDefaultNamespace</c>; the XPath engine does not type statically (design
/// #123), so a type error shows when the test is evaluated and is a dynamic error then; nothing reads the <c>assertion</c> element or calls
/// this class until the compile pipeline is wired (XSD-78).
/// </para>
/// </remarks>
internal sealed class FacetAssertion
{
    /// <summary>Gets the name of the variable that holds the value, <c>$value</c> (Datatypes 4.3.13.4).</summary>
    internal static readonly XPathQName ValueVariable = new(string.Empty, "value");

    private FacetAssertion(string test, XPathExpression expression, DocumentLocation location)
    {
        Test = test;
        Expression = expression;
        Location = location;
    }

    /// <summary>Gets the text of the <c>test</c> attribute.</summary>
    public string Test { get; }

    /// <summary>Gets the compiled test.</summary>
    public XPathExpression Expression { get; }

    /// <summary>Gets the location of the <c>assertion</c> element.</summary>
    public DocumentLocation Location { get; }

    /// <summary>Compiles the <c>test</c> of an <c>assertion</c> element.</summary>
    /// <param name="test">The text of the <c>test</c> attribute.</param>
    /// <param name="context">The static context of the element; the variable <c>$value</c> is added to it.</param>
    /// <param name="location">The location of the <c>assertion</c> element.</param>
    /// <param name="reporter">The reporter of schema errors.</param>
    /// <returns>The assertion, <see langword="null"/> after an issue was reported.</returns>
    internal static FacetAssertion? TryCreate(
        string test,
        XPathStaticContext context,
        DocumentLocation location,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(reporter);

        var result = XPathExpression.TryCompile(test, context.ToBuilder().WithVariable(ValueVariable).Build());
        if (!result.Success)
        {
            var diagnostic = result.Diagnostic;
            reporter.Report(
                XsdRuleCodes.XpathValid,
                location,
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

        return new FacetAssertion(test, result.Expression!, location);
    }
}
