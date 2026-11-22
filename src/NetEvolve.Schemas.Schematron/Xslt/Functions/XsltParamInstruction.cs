namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Globals;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// <c>xsl:param</c> in the head of an <c>xsl:function</c> (XSLT 2.0 section 10.3): a name and an optional <see langword="as"/> type, and no
/// default value. It is a structural child: it is registered for <see cref="XsltPosition.Child"/>, so it is misplaced everywhere
/// else, and the function reads it directly with <see cref="TryRead"/>. The entry is also the global <c>xsl:param</c> of a
/// stylesheet (SCH-66, #252), registered for <see cref="XsltPosition.Declaration"/>: a top-level element, which has the schema or a
/// stylesheet module as its parent, is read by <see cref="XsltGlobalCompile"/> and compiled by the planner of the scopes.
/// </summary>
/// <remarks>
/// Limitation: the <c>xsl:param</c> of a template is not compiled here; the registry holds one entry per name, so the template
/// package extends the positions of this entry together with the registration file.
/// </remarks>
internal sealed class XsltParamInstruction : IXsltInstruction
{
    private const string DefaultValueConstraint = "XTSE0760";
    private const string NoDefaultValue =
        "A parameter of a stylesheet function has no default value, so xsl:param is empty there.";

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "param");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Child | XsltPosition.Declaration;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        problem = default;
        if (XsltGlobalCompile.IsTopLevel(context.Element))
        {
            return XsltGlobalCompile.TryCompile(context, true, out executable);
        }

        executable = null!;
        if (!TryRead(context, context.Element, context.Scope.VariableNames.Count, out var parameter))
        {
            return false;
        }

        executable = new XsltParamDeclaration(parameter);
        return true;
    }

    /// <summary>Reads an <c>xsl:param</c> element; every problem is reported through the context.</summary>
    /// <param name="context">The compile context; its problem sink receives the problems.</param>
    /// <param name="element">The <c>xsl:param</c> element.</param>
    /// <param name="slot">The slot the argument is bound to.</param>
    /// <param name="parameter">The parameter when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the element is a valid parameter.</returns>
    internal static bool TryRead(
        XsltCompileContext context,
        XPathNode element,
        int slot,
        out XsltFunctionParameter parameter
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(element.Tree);

        parameter = null!;
        var ok = XsltElementReader.TryReadVariableName(context.Problems, element, "xsl:param", out var name);
        ok &= TryReadType(context, element, name, out var type);
        ok &= !HasDefaultValue(context, element);
        if (!ok)
        {
            return false;
        }

        parameter = new XsltFunctionParameter(name, slot, type, XsltElementResolver.LocationOf(element, null));
        return true;
    }

    private static bool TryReadType(
        XsltCompileContext context,
        XPathNode element,
        string name,
        out XsltSequenceType? type
    )
    {
        type = null;
        if (!XsltElementReader.TryGetAttribute(element, "as", out var typeText))
        {
            return true;
        }

        if (XsltSequenceType.TryParse(typeText, XsltElementReader.StaticContext(element), out type, out var diagnostic))
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.ExpressionInvalid,
            $"The type '{typeText}' of the parameter '{name}' is not valid: {diagnostic.Message}",
            element,
            diagnostic.Code
        );
        return false;
    }

    // A select attribute or content is a default value; the first one found is reported.
    private static bool HasDefaultValue(XsltCompileContext context, XPathNode element)
    {
        if (XsltElementReader.TryGetAttribute(element, "select", out _))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                NoDefaultValue,
                element,
                DefaultValueConstraint
            );
            return true;
        }

        for (var child = element.Node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            var isElement = child.Kind == XmlNodeKind.Element;
            if (isElement || (child.Kind == XmlNodeKind.Text && !XsltElementReader.IsWhitespace(child.StringValue)))
            {
                XsltProblems.Report(
                    context.Problems,
                    SchematronDiagnosticCodes.ElementMisplaced,
                    NoDefaultValue,
                    isElement ? new XPathNode(child) : element,
                    DefaultValueConstraint
                );
                return true;
            }
        }

        return false;
    }
}
