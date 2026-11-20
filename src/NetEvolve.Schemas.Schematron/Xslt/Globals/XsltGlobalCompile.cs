namespace NetEvolve.Schemas.Schematron.Xslt.Globals;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Reads a top-level <c>xsl:variable</c> or <c>xsl:param</c> (XSLT 2.0 section 9.5). The registry holds one entry per name, so the
/// entries of <c>xsl:variable</c> and <c>xsl:param</c> in the function folder are also the declarations, and they hand a top-level
/// element to <see cref="TryCompile"/>, which only reads and checks it: the value is compiled by the planner of the scopes, in the
/// scope of the whole schema level (design #150 EP-4), because a global may read another one that is declared after it. Every
/// problem is reported through the context and the compile continues after one.
/// </summary>
/// <remarks>
/// Limitations: the content form of a global (a sequence constructor in place of <see langword="select"/>, which builds a temporary
/// tree) is #794 (the local form is #629); the attribute <see langword="static"/> and static parameters are an XSLT 3.0 feature (DEF-28, #40); the <see langword="as"/> type is limited to the
/// types the scopes of the package check (#392). Without <see langword="select"/> and content a variable is the empty string whether
/// or not <see langword="as"/> is present, as for a local variable (#629); a parameter with <see langword="as"/> and without a default is
/// required, because the empty sequence that XSLT 2.0 section 9.3 gives it does not match any of those types. A global that is not
/// used is never evaluated, so XTDE0640 and a failing value are errors only when the global is used (XSLT 2.0 section 9.8).
/// </remarks>
internal static class XsltGlobalCompile
{
    private const string SchematronNamespace = "http://purl.oclc.org/dsdl/schematron";
    private const string UnboundPrefixConstraint = "XTSE0280";
    private const string RequiredWithDefaultConstraint = "XTSE0010";
    private static readonly string[] YesNo = ["yes", "no"];

    /// <summary>
    /// Determines whether an element is a child of the root of a stylesheet module or of a schema, which makes a
    /// <c>variable</c> or <c>param</c> a global declaration. The compile context carries no position, so the parent decides.
    /// </summary>
    /// <param name="element">The <c>xsl:variable</c> or <c>xsl:param</c> element.</param>
    /// <returns><see langword="true"/> when the parent is <c>xsl:stylesheet</c>, <c>xsl:transform</c>, or a Schematron <c>schema</c> or <c>library</c>.</returns>
    internal static bool IsTopLevel(XPathNode element)
    {
        Argument.ThrowIfNull(element.Tree);

        var parent = element.Node.Parent;
        return XsltElementReader.IsXslt(parent, "stylesheet")
            || XsltElementReader.IsXslt(parent, "transform")
            || IsSchematron(parent, "schema")
            || IsSchematron(parent, "library");
    }

    /// <summary>Reads the declaration; every problem is reported through the context.</summary>
    /// <param name="context">The compile context.</param>
    /// <param name="isParameter"><see langword="true"/> for <c>xsl:param</c>, <see langword="false"/> for <c>xsl:variable</c>.</param>
    /// <param name="executable">The declaration when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the element is a valid declaration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    internal static bool TryCompile(XsltCompileContext context, bool isParameter, out XsltExecutable executable)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        var element = context.Element;
        var kind = isParameter ? "xsl:param" : "xsl:variable";
        var ok = TryReadName(context, kind, out var written, out var name);
        ok &= XsltVariableInstruction.TryReadDeclaredType(context, written, out var declared);
        ok &= TryRejectStatic(context, kind);
        var required = false;
        if (isParameter)
        {
            ok &= FlowCompile.TryReadChoice(context, element, "required", kind, YesNo, out var requiredText);
            required = string.Equals(requiredText, "yes", StringComparison.Ordinal);
        }

        var hasSelect = XsltElementReader.TryGetAttribute(element, "select", out var select);
        ok &= XsltVariableInstruction.CheckContent(context, hasSelect, kind);
        if (required && hasSelect)
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The parameter '{written}' is required, so it has no select attribute.",
                element,
                RequiredWithDefaultConstraint
            );
            ok = false;
        }

        if (!ok)
        {
            return false;
        }

        // A typed parameter without a default is required: the empty sequence does not match the type (XSLT 2.0 section 9.3).
        required |= isParameter && !hasSelect && XsltElementReader.TryGetAttribute(element, "as", out _);
        var value =
            hasSelect ? select
            : required ? null
            : "''";
        executable = new XsltGlobalDeclaration(
            name,
            new GlobalDeclaration(
                written,
                value,
                declared,
                isParameter,
                required,
                XsltElementResolver.LocationOf(element, null)
            )
        );
        return true;
    }

    // The name is a QName now; an unprefixed name of a variable is in no namespace.
    private static bool TryReadName(XsltCompileContext context, string kind, out string written, out XPathQName name)
    {
        name = default;
        var element = context.Element;
        if (!XsltElementReader.TryReadVariableName(context.Problems, element, kind, out written))
        {
            return false;
        }

        _ = XsltElementReader.TryParseQName(written, out var prefix, out var local);
        var namespaceName = string.Empty;
        if (prefix is not null && !XsltElementReader.TryResolvePrefix(element, prefix, out namespaceName))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The name '{written}' of the {kind} element uses the prefix '{prefix}', which is not bound to a namespace.",
                element,
                UnboundPrefixConstraint
            );
            return false;
        }

        name = new XPathQName(namespaceName, local);
        return true;
    }

    private static bool TryRejectStatic(XsltCompileContext context, string kind)
    {
        if (!XsltElementReader.TryGetAttribute(context.Element, "static", out _))
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.XsltUnsupported,
            $"The static attribute of {kind} is an XSLT 3.0 feature and is not supported (DEF-28, #40).",
            context.Element
        );
        return false;
    }

    private static bool IsSchematron(XmlNode node, string local)
    {
        if (node.Kind != XmlNodeKind.Element)
        {
            return false;
        }

        var names = node.Tree!.Names;
        var name = node.Name;
        return !name.NamespaceUri.IsEmpty
            && string.Equals(names.GetString(name.NamespaceUri), SchematronNamespace, StringComparison.Ordinal)
            && string.Equals(names.GetString(name.Local), local, StringComparison.Ordinal);
    }
}
