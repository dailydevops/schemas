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
    private const string XmlSchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    private readonly SchemaNamespaceScope _scope;

    /// <summary>Initializes a new instance of the <see cref="TypeAlternativeBuilder"/> class.</summary>
    /// <param name="scope">The namespace the schema document contributes to.</param>
    internal TypeAlternativeBuilder(SchemaNamespaceScope scope) => _scope = scope;

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.TypeAlternative;

    /// <inheritdoc />
    /// <remarks>An alternative is local to its element, so it is not added to the registry.</remarks>
    /// <exception cref="NotSupportedException"><paramref name="element"/> is not a <see cref="SchemaSyntaxNode"/>, the only syntax with parent and namespace scope.</exception>
    public IXsdComponent Build(ISchemaSyntaxElement element, IComponentRegistry registry, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(reporter);

        var node =
            element as SchemaSyntaxNode
            ?? throw new NotSupportedException("Only a SchemaSyntaxNode can be built into a component.");
        return BuildAlternative(node, reporter);
    }

    /// <summary>Builds an alternative from an <c>alternative</c> syntax node.</summary>
    /// <param name="node">The syntax.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The alternative.</returns>
    internal TypeAlternative BuildAlternative(SchemaSyntaxNode node, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(reporter);

        var test = CompileTest(node, reporter);
        var (typeName, inline) = ReadType(node, reporter);
        return TypeAlternative.Create(node.Location, test, node.FindAttribute("test") is null, typeName, inline);
    }

    private XPathExpression? CompileTest(SchemaSyntaxNode node, XsdIssueReporter reporter)
    {
        var text = node.FindAttribute("test");
        if (text is null)
        {
            return null;
        }

        var result = XPathExpression.TryCompile(text, CreateStaticContext(node, text));
        if (result.Success)
        {
            return result.Expression;
        }

        reporter.Report(
            XsdRuleCodes.XpathValid,
            node.Location,
            "The test '" + text + "' is not a valid XPath expression: " + result.Diagnostic.Message
        );
        return null;
    }

    private XPathStaticContext CreateStaticContext(SchemaSyntaxNode node, string text)
    {
        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("xs", SchemaGrammar.Namespace)
            .WithNamespace("xsi", XmlSchemaInstance)
            .WithNamespace("fn", FunctionNamespace);
        if (XPathParser.TryParse(text.AsSpan(), out var tree, out _))
        {
            var prefixes = new HashSet<string>(StringComparer.Ordinal);
            CollectPrefixes(tree!, prefixes);
            foreach (var prefix in prefixes)
            {
                if (node.TryResolvePrefix(prefix, out var namespaceName))
                {
                    _ = statics.WithNamespace(prefix, namespaceName);
                }
            }
        }

        var defaultNamespace = XPathDefaultNamespace.Resolve(node, _scope, XsdVersion.V11);
        return (defaultNamespace.Length == 0 ? statics : statics.WithDefaultElementNamespace(defaultNamespace)).Build();
    }

    private static void CollectPrefixes(SyntaxNode node, HashSet<string> prefixes)
    {
        var prefix = node switch
        {
            QualifiedName name => name.Prefix,
            NameTest test => test.Prefix,
            _ => null,
        };
        if (!string.IsNullOrEmpty(prefix))
        {
            _ = prefixes.Add(prefix!);
        }

        var children = node.Children;
        for (var i = 0; i < children.Count; i++)
        {
            CollectPrefixes(children[i], prefixes);
        }
    }

    private (XPathQName TypeName, ISchemaSyntaxElement? Inline) ReadType(
        SchemaSyntaxNode node,
        XsdIssueReporter reporter
    )
    {
        ISchemaSyntaxElement? inline = null;
        for (var i = 0; i < node.ChildCount && inline is null; i++)
        {
            var child = node.GetChildNode(i);
            if (child.Name.LocalName is "simpleType" or "complexType")
            {
                inline = child;
            }
        }

        var type = node.FindAttribute("type");
        if (type is null)
        {
            if (inline is null)
            {
                reporter.Report(
                    XsdRuleCodes.SrcTa,
                    node.Location,
                    "A type alternative must have a type attribute or an anonymous type."
                );
            }

            return (default, inline);
        }

        if (inline is not null)
        {
            reporter.Report(
                XsdRuleCodes.SrcTa,
                node.Location,
                "A type alternative must not have both a type attribute and an anonymous type."
            );
        }

        if (_scope.TryResolve(node, type, out var typeName))
        {
            return (typeName, null);
        }

        reporter.Report(
            XsdRuleLookup.SrcResolve,
            node.Location,
            $"The type '{type}' is not a QName with a declared prefix."
        );
        return (default, null);
    }
}
