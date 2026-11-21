namespace NetEvolve.Schemas.Schematron.Xslt.Library.Formatting;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:decimal-format</c> (XSLT 2.0 section 16.4.1): a top-level declaration with an optional <c>name</c> and the attributes that set
/// the characters and strings of a decimal format. The element is read here; the format is merged and checked by
/// <see cref="XsltDecimalFormatTableBuilder"/>, and the picture string is read by the picture engine of the XPath package, which this
/// folder does not repeat. Every problem is reported through the context and the compile continues after one; a failed compile returns
/// <see langword="false"/> with a default problem, which means that the problems are already reported.
/// </summary>
/// <remarks>
/// Limitations: the decimal format of the XPath package holds UTF-16 code units, so a character attribute that is one character beyond
/// the Basic Multilingual Plane is reported as not supported; the XSLT 3.0 attribute <c>exponent-separator</c> is reported as not
/// supported, because every XSLT binding evaluates <c>format-number</c> with the XSLT 2.0 rules, which do not read it.
/// </remarks>
internal sealed class XsltDecimalFormatInstruction : IXsltInstruction
{
    private const string InvalidValueConstraint = "XTSE0020";
    private const string UnboundPrefixConstraint = "XTSE0280";

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "decimal-format");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Declaration;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var ok = TryReadName(context, out var name, out var written);
        ok &= TryRejectExponentSeparator(context);
        var values = new string?[XsltDecimalFormatDeclaration.Attributes.Count];
        for (var attribute = 0; attribute < values.Length; attribute++)
        {
            ok &= TryReadAttribute(context, attribute, values);
        }

        if (!ok)
        {
            return false;
        }

        executable = new XsltDecimalFormatDeclaration(
            name,
            written,
            values,
            XsltElementResolver.LocationOf(context.Element, null)
        );
        return true;
    }

    // The name is optional: no name is the unnamed format, which the XPath package asks for with the default name.
    private static bool TryReadName(XsltCompileContext context, out XPathQName name, out string written)
    {
        name = default;
        var element = context.Element;
        if (!XsltElementReader.TryGetAttribute(element, "name", out written))
        {
            return true;
        }

        if (!XsltElementReader.TryParseQName(written, out var prefix, out var local))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The name '{written}' of the xsl:decimal-format element is not a QName.",
                element,
                InvalidValueConstraint
            );
            return false;
        }

        // An unprefixed format name is in no namespace, as the name of a variable is.
        var namespaceName = string.Empty;
        if (prefix is not null && !XsltElementReader.TryResolvePrefix(element, prefix, out namespaceName))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                $"The name '{written}' of the decimal format uses the prefix '{prefix}', which is not bound to a namespace.",
                element,
                UnboundPrefixConstraint
            );
            return false;
        }

        name = new XPathQName(namespaceName, local);
        return true;
    }

    private static bool TryRejectExponentSeparator(XsltCompileContext context)
    {
        if (!XsltElementReader.TryGetAttribute(context.Element, "exponent-separator", out _))
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.XsltUnsupported,
            "The exponent-separator attribute of xsl:decimal-format is an XSLT 3.0 feature and is not supported; the XSLT 2.0 rules of format-number do not read it.",
            context.Element
        );
        return false;
    }

    private static bool TryReadAttribute(XsltCompileContext context, int attribute, string?[] values)
    {
        var name = XsltDecimalFormatDeclaration.Attributes[attribute];
        if (!XsltElementReader.TryGetAttribute(context.Element, name, out var value))
        {
            return true;
        }

        // infinity and NaN are strings; every other attribute is exactly one character (XTSE0020).
        if (attribute >= XsltDecimalFormatDeclaration.FirstStringAttribute || value.Length == 1)
        {
            values[attribute] = value;
            return true;
        }

        if (value.Length == 2 && char.IsHighSurrogate(value[0]) && char.IsLowSurrogate(value[1]))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.XsltUnsupported,
                $"The character '{value}' of the attribute '{name}' of xsl:decimal-format lies beyond the Basic Multilingual Plane and is not supported; a decimal format holds UTF-16 code units.",
                context.Element
            );
            return false;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.AttributeInvalid,
            $"The value '{value}' of the attribute '{name}' of the xsl:decimal-format element is not exactly one character.",
            context.Element,
            InvalidValueConstraint
        );
        return false;
    }
}
