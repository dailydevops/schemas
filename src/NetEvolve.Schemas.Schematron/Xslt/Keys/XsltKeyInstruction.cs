namespace NetEvolve.Schemas.Schematron.Xslt.Keys;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:key</c> (XSLT 2.0 section 16.3): a top-level declaration with a <c>name</c>, a <c>match</c> pattern, and the key value as a
/// <c>use</c> expression or as the content of the element. The pattern is compiled by the shared pattern matcher with the key use
/// (design #193: one matcher for rule contexts and keys), the expression by the binding of the schema. Every problem is reported
/// through the context and the compile continues after one; a failed compile returns <see langword="false"/> with a default problem,
/// which means that the problems are already reported.
/// </summary>
/// <remarks>
/// Limitations: only the codepoint collation is supported (DEF-43, #55), so another <c>collation</c> is XTSE1210 as an explicit
/// error; the XSLT 3.0 attribute <c>composite</c> is reported as not supported (#779); <c>match</c> is limited to the pattern subset of the
/// shared matcher, so <c>id()</c> inside it is the unsupported-pattern error (#67); <c>key()</c> may start the pattern (#780).
/// The compile scope of the element is the scope the compiler gives a declaration: the schema-level
/// names, as for <c>xsl:function</c> (design #150), so <c>match</c> and <c>use</c> see no phase, pattern, or rule variable.
/// </remarks>
internal sealed class XsltKeyInstruction : IXsltInstruction
{
    /// <summary>The URI of the codepoint collation, the only collation of the package.</summary>
    internal const string CodepointCollation = "http://www.w3.org/2005/xpath-functions/collation/codepoint";

    private const string ContentConstraint = "XTSE1205";
    private const string CollationConstraint = "XTSE1210";
    private const string UnboundPrefixConstraint = "XTSE0280";

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "key");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Declaration;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var ok = TryReadName(context, out var name, out var written);
        ok &= TryReadCollation(context);
        ok &= TryRejectComposite(context);
        ok &= TryCompileMatch(context, out var match);
        ok &= TryCompileValue(context, out var use, out var body);
        if (!ok)
        {
            return false;
        }

        executable = new XsltKeyDeclaration(
            new XsltKeyComponent(
                name,
                written,
                match!,
                use,
                body,
                XsltElementResolver.LocationOf(context.Element, null)
            )
        );
        return true;
    }

    private static bool TryReadName(XsltCompileContext context, out XPathQName name, out string written)
    {
        name = default;
        var element = context.Element;
        if (!XsltElementReader.TryReadVariableName(context.Problems, element, "xsl:key", out written))
        {
            return false;
        }

        // The name is a QName now; an unprefixed key name is in no namespace, as the name of a variable is.
        _ = XsltElementReader.TryParseQName(written, out var prefix, out var local);
        var namespaceName = string.Empty;
        if (prefix is not null && !XsltElementReader.TryResolvePrefix(element, prefix, out namespaceName))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The name '{written}' of the key uses the prefix '{prefix}', which is not bound to a namespace.",
                element,
                UnboundPrefixConstraint
            );
            return false;
        }

        name = new XPathQName(namespaceName, local);
        return true;
    }

    private static bool TryReadCollation(XsltCompileContext context)
    {
        if (!FlowCompile.TryReadConstant(context, context.Element, "collation", "xsl:key", out var collation))
        {
            return false;
        }

        if (collation is null || string.Equals(collation, CodepointCollation, StringComparison.Ordinal))
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.XsltUnsupported,
            $"The collation '{collation}' of the key is not supported; only the codepoint collation is (DEF-43, #55).",
            context.Element,
            CollationConstraint
        );
        return false;
    }

    private static bool TryRejectComposite(XsltCompileContext context)
    {
        if (!XsltElementReader.TryGetAttribute(context.Element, "composite", out _))
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.XsltUnsupported,
            "The composite attribute of xsl:key is an XSLT 3.0 feature and is not supported.",
            context.Element
        );
        return false;
    }

    private static bool TryCompileMatch(XsltCompileContext context, out CompiledPattern? match)
    {
        match = null;
        var element = context.Element;
        if (!XsltElementReader.TryGetAttribute(element, "match", out var text))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                "The xsl:key element has no match attribute.",
                element
            );
            return false;
        }

        var binding = context.Binding
            ?? throw new InvalidOperationException(
                "The compiler of the context gave no query binding for an expression."
            );
        var matcher = new PatternMatcher(binding);
        if (matcher.TryCompile(text.AsSpan(), PatternUse.KeyMatch, context.Scope, out var compiled, out var found))
        {
            match = compiled;
            return true;
        }

        context.Problems.Add(in found);
        return false;
    }

    // The key value is the use attribute or the content, one of the two (XTSE1205).
    private static bool TryCompileValue(
        XsltCompileContext context,
        out QueryExpression? use,
        out XsltBodyExecutable? body
    )
    {
        use = null;
        body = null;
        var element = context.Element;
        var hasUse = XsltElementReader.TryGetAttribute(element, "use", out var text);
        var hasContent = XsltVariableInstruction.HasContent(element);
        if (hasUse == hasContent)
        {
            XsltProblems.Report(
                context.Problems,
                hasUse ? SchematronDiagnosticCodes.ElementMisplaced : SchematronDiagnosticCodes.AttributeMissing,
                hasUse
                    ? "The xsl:key element has either a use attribute or content, not both."
                    : "The xsl:key element has neither a use attribute nor content.",
                element,
                ContentConstraint
            );
            return false;
        }

        return hasUse
            ? FlowCompile.TryCompileExpression(context, text, QueryExpressionKind.Value, out use)
            : FlowCompile.TryCompileBody(context, element.Node.FirstChild, out body, out _);
    }
}
