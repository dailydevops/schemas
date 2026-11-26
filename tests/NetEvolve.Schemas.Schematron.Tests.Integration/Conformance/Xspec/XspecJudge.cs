namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using System.Globalization;
using System.Xml.Linq;
using NetEvolve.Arguments;

/// <summary>Checks an expectation against the SVRL output of a validation.</summary>
/// <remarks>
/// <para>
/// The checks follow the XSpec Schematron support: an assertion matches by its id (or the id of its rule or pattern when it has none), by its
/// role (likewise), by the node its location names (compared by node identity on the instance), and by the text of one of its message
/// parts. The negative expectations and <c>x:expect-valid</c> also require that a rule fired, so a run that checks nothing never passes.
/// </para>
/// <para>
/// Limitation: the role fallback and the id fallback use the nearest preceding <c>svrl:fired-rule</c> and <c>svrl:active-pattern</c>, the
/// layout of the ISO SVRL; the <c>ruleId</c>, <c>patternId</c>, and <c>groupId</c> attributes of the newer SVRL layout are not read.
/// </para>
/// <para>
/// <c>x:expect-valid</c> reads only the role on the assertion itself, with no fallback to the fired rule or active pattern, unlike the id and
/// role matching. An assertion without a role under a rule with <c>role="warn"</c> therefore counts as an error; this is deliberate and may need
/// revisiting when more XSpec suites run.
/// </para>
/// </remarks>
internal static class XspecJudge
{
    private static readonly XNamespace Svrl = "http://purl.oclc.org/dsdl/svrl";

    /// <summary>Checks one expectation.</summary>
    /// <param name="expectation">The expectation.</param>
    /// <param name="svrl">The SVRL output.</param>
    /// <param name="locator">The locator of the instance document.</param>
    /// <returns>The reason the expectation does not hold, or <see langword="null"/> when it holds.</returns>
    public static string? Check(XspecExpectation expectation, XDocument svrl, XspecNodeLocator locator)
    {
        Argument.ThrowIfNull(expectation);
        Argument.ThrowIfNull(svrl);
        Argument.ThrowIfNull(locator);
        var output = svrl.Root!;
        var hasFired = output.Elements(Svrl + "fired-rule").Any();
        if (expectation.Kind == XspecExpectKind.Valid)
        {
            if (!hasFired)
            {
                return "No rule fired.";
            }

            var severe = output
                .Elements()
                .Count(e =>
                    (e.Name == Svrl + "failed-assert" || e.Name == Svrl + "successful-report")
                    && (e.Attribute("role") is not { } role || IsError(role.Value))
                );
            return severe == 0 ? null : $"{severe} assertion(s) have the role error or fatal, or no role.";
        }

        var assert = expectation.Kind is XspecExpectKind.Assert or XspecExpectKind.NotAssert;
        var name = assert ? "failed-assert" : "successful-report";
        var count = Count(expectation, output, Svrl + name, locator, out var problem);
        if (problem is not null)
        {
            return problem;
        }

        if (expectation.Kind is XspecExpectKind.NotAssert or XspecExpectKind.NotReport)
        {
            return !hasFired ? "No rule fired."
                : count == 0 ? null
                : $"{count.ToString(CultureInfo.InvariantCulture)} matching svrl:{name} element(s) found.";
        }

        return expectation.Count is { } expected
            ? count == expected
                ? null
                : $"Expected {expected.ToString(CultureInfo.InvariantCulture)} matching svrl:{name} element(s), found {count.ToString(CultureInfo.InvariantCulture)}."
            : count > 0
                ? null
                : $"No matching svrl:{name} element.";
    }

    private static int Count(
        XspecExpectation expectation,
        XElement output,
        XName name,
        XspecNodeLocator locator,
        out string? problem
    )
    {
        problem = null;
        var count = 0;
        foreach (var element in output.Elements(name))
        {
            if (
                Matches(element, "id", expectation.Id)
                && Matches(element, "role", expectation.Role)
                && (expectation.Text is null || HasText(element, expectation.Text))
                && (expectation.Location is null || IsAt(element, expectation, output, locator, out problem))
            )
            {
                count++;
            }

            if (problem is not null)
            {
                return count;
            }
        }

        return count;
    }

    // The attribute of the assertion, else the one of its nearest preceding rule, else the one of its nearest preceding pattern.
    private static bool Matches(XElement element, string attribute, string? expected)
    {
        if (expected is null)
        {
            return true;
        }

        var actual = element.Attribute(attribute)?.Value
            ?? element.ElementsBeforeSelf(Svrl + "fired-rule").LastOrDefault()?.Attribute(attribute)?.Value
            ?? element.ElementsBeforeSelf(Svrl + "active-pattern").LastOrDefault()?.Attribute(attribute)?.Value;
        return string.Equals(actual, expected, StringComparison.Ordinal);
    }

    private static bool HasText(XElement element, string text) =>
        element
            .Elements()
            .Any(e =>
                (
                    e.Name == Svrl + "text"
                    || e.Name == Svrl + "diagnostic-reference"
                    || e.Name == Svrl + "property-reference"
                ) && string.Equals(Normalize(e.Value), text, StringComparison.Ordinal)
            );

    private static bool IsAt(
        XElement element,
        XspecExpectation expectation,
        XElement output,
        XspecNodeLocator locator,
        out string? problem
    )
    {
        problem = null;
        if (element.Attribute("location")?.Value is not { } location)
        {
            return false;
        }

        var bindings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var declaration in output.Elements(Svrl + "ns-prefix-in-attribute-values"))
        {
            if (declaration.Attribute("prefix")?.Value is { } prefix && declaration.Attribute("uri")?.Value is { } uri)
            {
                bindings[prefix] = uri;
            }
        }

        return locator.Same(expectation.Location!, expectation.Namespaces, location, bindings, out problem);
    }

    private static bool IsError(string role) =>
        string.Equals(role, "error", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "fatal", StringComparison.OrdinalIgnoreCase);

    /// <summary>Collapses runs of white space to one space and trims, like <c>normalize-space</c>.</summary>
    /// <param name="value">The text.</param>
    /// <returns>The normalized text.</returns>
    internal static string Normalize(string value) =>
        string.Join(" ", value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
}
