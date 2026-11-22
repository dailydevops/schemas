namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>xsl:for-each-group</c> (XSLT 2.0 section 14.3): a required <see langword="select"/>, exactly one of <c>group-by</c>,
/// <c>group-adjacent</c>, <c>group-starting-with</c>, and <c>group-ending-with</c> (XTSE1080), an optional <c>collation</c> for the
/// first two (XTSE1090), the <c>xsl:sort</c> elements that order the groups, and a sequence constructor that runs once per group. The
/// expressions are compiled by the binding of the schema, and a pattern by the shared pattern matcher (design #193). Every problem is
/// reported through the context and the compile continues after one.
/// </summary>
/// <remarks>
/// Limitations: only the codepoint collation is supported, so another <c>collation</c> is the unsupported construct error (DEF-43,
/// #55); the XSLT 3.0 attribute <c>composite</c> is the unsupported construct error under every binding (DEF-54, #66); the pattern
/// is limited to the subset of the shared matcher (DEF-60, DEF-55); the attributes are constants, not attribute value templates
/// (#681). A pattern is compiled with <see cref="PatternUse.RuleContext"/>, the use of a match pattern of an instruction, because the
/// matcher has no use of its own for grouping and makes no difference between its uses yet.
/// </remarks>
internal sealed class XsltForEachGroupInstruction : IXsltInstruction
{
    private const string CodepointCollation = "http://www.w3.org/2005/xpath-functions/collation/codepoint";
    private const string AttributeConstraint = "XTSE1080";
    private const string CollationConstraint = "XTSE1090";
    private static readonly string[] GroupingAttributes =
    [
        "group-by",
        "group-adjacent",
        "group-starting-with",
        "group-ending-with",
    ];

    /// <inheritdoc />
    public XPathQName Name { get; } = new(XsltInstructionRegistry.Namespace, "for-each-group");

    /// <inheritdoc />
    public XsltPosition Position => XsltPosition.Instruction;

    /// <inheritdoc />
    public bool TryCompile(XsltCompileContext context, out XsltExecutable executable, out SchematronProblem problem)
    {
        ArgumentNullException.ThrowIfNull(context);

        executable = null!;
        problem = default;
        var binding = context.Binding
            ?? throw new InvalidOperationException(
                "The compiler of the context gave no query binding for an expression."
            );
        var element = context.Element;
        QueryExpression? select = null;
        bool ok;
        if (XsltElementReader.TryGetAttribute(element, "select", out var text))
        {
            ok = FlowCompile.TryCompileExpression(context, text, QueryExpressionKind.Value, out select);
        }
        else
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeMissing,
                "The xsl:for-each-group element has no select attribute.",
                element
            );
            ok = false;
        }

        ok &= TryRejectComposite(context);
        ok &= TryCompileGrouping(context, binding, out var grouping);

        var keys = new List<XsltSortKey>();
        ok &= XsltSortInstruction.TryReadKeys(context, keys, out var first);
        ok &= FlowCompile.TryCompileBody(context, first, out var body, out var slotEnd);
        if (!ok)
        {
            return false;
        }

        executable = new XsltForEachGroupExecutable(
            select!,
            grouping!,
            keys.ToArray(),
            body,
            slotEnd,
            XsltElementResolver.LocationOf(element, null)
        );
        return true;
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
            "The composite attribute of xsl:for-each-group is an XSLT 3.0 feature and is not supported (DEF-54, #66).",
            context.Element
        );
        return false;
    }

    // Exactly one of the four attributes (XTSE1080); the collation is for the two that compare keys (XTSE1090).
    private static bool TryCompileGrouping(
        XsltCompileContext context,
        IQueryBinding binding,
        out XsltGrouping? grouping
    )
    {
        grouping = null;
        var element = context.Element;
        var texts = new string[GroupingAttributes.Length];
        var found = 0;
        var which = -1;
        var keyed = false;
        for (var i = 0; i < texts.Length; i++)
        {
            if (XsltElementReader.TryGetAttribute(element, GroupingAttributes[i], out texts[i]))
            {
                found++;
                which = i;
                keyed |= i < 2;
            }
        }

        var ok = TryCheckCollation(context, keyed);
        if (found != 1)
        {
            XsltProblems.Report(
                context.Problems,
                found == 0 ? SchematronDiagnosticCodes.AttributeMissing : SchematronDiagnosticCodes.AttributeInvalid,
                found == 0
                    ? "The xsl:for-each-group element has none of the attributes group-by, group-adjacent, group-starting-with, and group-ending-with."
                    : "The xsl:for-each-group element has more than one of the attributes group-by, group-adjacent, group-starting-with, and group-ending-with.",
                element,
                AttributeConstraint
            );
            return false;
        }

        return TryCompileChosen(context, binding, which, texts[which], out grouping) & ok;
    }

    // The key expression of group-by (0) and group-adjacent (1), or the pattern of group-starting-with (2) and group-ending-with (3).
    private static bool TryCompileChosen(
        XsltCompileContext context,
        IQueryBinding binding,
        int which,
        string text,
        out XsltGrouping? grouping
    )
    {
        grouping = null;
        var location = XsltElementResolver.LocationOf(context.Element, null);
        if (which < 2)
        {
            if (!FlowCompile.TryCompileExpression(context, text, QueryExpressionKind.Value, out var key))
            {
                return false;
            }

            grouping =
                which == 0 ? new XsltGroupByGrouping(key, location) : new XsltGroupAdjacentGrouping(key, location);
            return true;
        }

        var matcher = new PatternMatcher(binding);
        if (
            !matcher.TryCompile(
                text.AsSpan(),
                PatternUse.RuleContext,
                context.Scope,
                out var pattern,
                out var patternProblem
            )
        )
        {
            context.Problems.Add(in patternProblem);
            return false;
        }

        grouping = new XsltPatternGrouping(matcher, pattern, ending: which == 3, location);
        return true;
    }

    private static bool TryCheckCollation(XsltCompileContext context, bool keyed)
    {
        if (
            !FlowCompile.TryReadConstant(context, context.Element, "collation", "xsl:for-each-group", out var collation)
        )
        {
            return false;
        }

        if (collation is null)
        {
            return true;
        }

        if (!keyed)
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.AttributeInvalid,
                "The collation attribute of xsl:for-each-group needs a group-by or a group-adjacent attribute.",
                context.Element,
                CollationConstraint
            );
            return false;
        }

        if (string.Equals(collation, CodepointCollation, StringComparison.Ordinal))
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.XsltUnsupported,
            $"The collation '{collation}' is not supported; only the codepoint collation is (DEF-43, #55).",
            context.Element
        );
        return false;
    }
}
