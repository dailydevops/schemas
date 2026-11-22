namespace NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The static context of the <c>test</c> of an assertion (XSD 1.1, Structures section 3.13.2): the prefixes of the <c>assert</c> element, the
/// default element namespace from <c>xpathDefaultNamespace</c>, and the variable <c>$value</c> of section 3.13.4.1.
/// </summary>
/// <remarks>
/// Limitations: the prefixes <c>fn</c>, <c>xs</c>, and <c>xsi</c> are predeclared, as XPath 2.0 section 2.1.1 does, and the bindings of the
/// schema document win over them; the schema syntax model lists no prefix of an element, so a prefix is bound when the text of the test
/// shows it before a colon and the element resolves it, which also binds a prefix inside a string literal and does no harm.
/// </remarks>
internal static class AssertionStaticContext
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";
    private const string InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>Gets the name of the variable that holds the value of the element, <c>$value</c>.</summary>
    internal static readonly XPathQName ValueVariable = new(string.Empty, "value");

    /// <summary>Builds the static context of an assertion.</summary>
    /// <param name="node">The <c>assert</c> element.</param>
    /// <param name="test">The text of the test.</param>
    /// <param name="defaultElementNamespace">The namespace of the unprefixed element names, empty for none.</param>
    /// <returns>The static context.</returns>
    internal static XPathStaticContext Create(SchemaSyntaxNode node, string test, string defaultElementNamespace)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(defaultElementNamespace);

        var builder = XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("fn", FunctionNamespace)
            .WithNamespace("xs", SchemaGrammar.Namespace)
            .WithNamespace("xsi", InstanceNamespace)
            .WithVariable(ValueVariable)
            .WithDefaultElementNamespace(defaultElementNamespace.Length == 0 ? null : defaultElementNamespace);

        for (var colon = 0; colon < test.Length; colon++)
        {
            if (test[colon] != ':')
            {
                continue;
            }

            // An axis name ends in two colons and carries no prefix.
            if (colon + 1 < test.Length && test[colon + 1] == ':')
            {
                colon++;
                continue;
            }

            var start = colon;
            while (start > 0 && IsNameCharacter(test[start - 1]))
            {
                start--;
            }

            if (start == colon)
            {
                continue;
            }

            var prefix = test.Substring(start, colon - start);
            if (node.TryResolvePrefix(prefix, out var namespaceName) && namespaceName.Length > 0)
            {
                _ = builder.WithNamespace(prefix, namespaceName);
            }
        }

        return builder.Build();
    }

    private static bool IsNameCharacter(char c) => char.IsLetterOrDigit(c) || c is '_' or '-' or '.';
}
