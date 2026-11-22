namespace NetEvolve.Schemas.Xsd.Assertions.Facet;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Checks the assertion facets of a simple type against a value (XSD 1.1, Datatypes section 4.3.13): every <see cref="FacetAssertion"/> that
/// <see cref="FacetAssertions"/> lists for the type, the inherited ones included, is evaluated with the variable <c>$value</c> bound to the
/// typed value and no context item, no position, and no size. No tree is built, so the check stays streaming.
/// </summary>
/// <remarks>
/// <para>
/// <c>$value</c> is the sequence of the items of the typed value (<see cref="TypedValues"/>): one item for an atomic type, one item per token
/// for a list type, and for a union type the item of the member that the datatype contract accepts, typed as that member. A test that is
/// false, that has no effective boolean value, or that raises a dynamic or type error, such as XPDY0002 for a reference to the context item,
/// makes the value invalid; the issue names the test and the error code. The caller passes a value that is valid for its datatype and
/// reuses the evaluation context of its run, which is reset here.
/// </para>
/// <para>
/// Limitations: XSD 1.1 accepts the union member that is valid including its own assertions, while the datatype contract picks the first
/// member that its facets accept and does not know assertions; the assertions of the item type of a list and of the member types of a union
/// are checked by the check of those types, which the caller runs for them, not by the check of the list or union itself (#949).
/// </para>
/// </remarks>
internal static class FacetAssertionEvaluator
{
    /// <summary>Checks the assertion facets of a type against a value and reports every failure.</summary>
    /// <param name="assertions">The assertions of the schema.</param>
    /// <param name="type">The simple type.</param>
    /// <param name="text">The lexical value.</param>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="context">The evaluation context of the run; it is reset.</param>
    /// <param name="location">The location of the value.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns><see langword="true"/> when the type has no assertion facet or every one holds.</returns>
    internal static bool Check(
        FacetAssertions assertions,
        SimpleTypeDefinition type,
        string text,
        ISimpleValueAssessor assessor,
        XPathEvaluationContext context,
        DocumentLocation location,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(assertions);
        Argument.ThrowIfNull(type);
        Argument.ThrowIfNull(text);
        Argument.ThrowIfNull(assessor);
        Argument.ThrowIfNull(context);
        Argument.ThrowIfNull(reporter);

        var tests = assertions.Of(type);
        if (tests.Count == 0)
        {
            return true;
        }

        Bind(type, text, assessor, context);
        var holds = true;
        for (var i = 0; i < tests.Count; i++)
        {
            if (Failure(tests[i], context) is { } failure)
            {
                holds = false;
                reporter.Report(
                    XsdRuleCodes.CvcAssertionsValid,
                    location,
                    "The value '" + text + "' does not satisfy the assertion '" + tests[i].Test + "': " + failure + "."
                );
            }
        }

        return holds;
    }

    // $value is the empty sequence, one item, or the items of a list; only a list copies them into an array.
    private static void Bind(
        SimpleTypeDefinition type,
        string text,
        ISimpleValueAssessor assessor,
        XPathEvaluationContext context
    )
    {
        var items = new List<XdmItem>(1);
        TypedValues.Append(type, text, assessor, items);
        context.Reset();
        context.Variables.Set(
            FacetAssertion.ValueVariable,
            items.Count switch
            {
                0 => XdmSequence.Empty,
                1 => XdmSequence.Create(items[0]),
                _ => XdmSequence.Create(items.ToArray()),
            }
        );
    }

    // Null when the test is true; otherwise why it is not: false, no effective boolean value, or a dynamic or type error.
    private static string? Failure(FacetAssertion assertion, XPathEvaluationContext context)
    {
        var result = assertion.Expression.TryEvaluate(context);
        if (result.Status != XPathEvaluationStatus.Ok)
        {
            return Describe(result.Diagnostic);
        }

        try
        {
            return XPathAtomization.EffectiveBooleanValue(result.Value) ? null : "the test is false";
        }
        catch (XPathException error)
        {
            return Describe(error.Diagnostic);
        }
    }

    private static string Describe(XPathDiagnostic diagnostic) =>
        "the test raised the error " + diagnostic.Code + " (" + diagnostic.Message + ")";
}
