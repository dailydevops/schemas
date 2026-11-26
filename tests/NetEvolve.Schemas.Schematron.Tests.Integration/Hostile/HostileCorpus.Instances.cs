namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The cases of hostile instances: depth, attributes, entity expansion, and an assertion that fails everywhere.</summary>
internal static partial class HostileCorpus
{
    private static IEnumerable<HostileCase> Instances()
    {
        var rule = Schema(Rule("true()"));
        yield return Make(
            "instance-deep-nesting",
            rule,
            Repeat("<a>", 100_000) + Repeat("</a>", 100_000),
            CoreDiagnosticCodes.DepthLimitExceeded.Number
        );
        yield return Make(
            "instance-many-attributes",
            rule,
            "<a " + string.Concat(Enumerable.Range(0, 100_000).Select(static i => $"x{i}=\"1\" ")) + "/>",
            CoreDiagnosticCodes.AttributeLimitExceeded.Number
        );

        var laughs = new System.Text.StringBuilder("<!DOCTYPE a [<!ENTITY e0 \"lol\">");
        for (var i = 1; i <= 9; i++)
        {
            _ = laughs
                .Append("<!ENTITY e")
                .Append(i)
                .Append(" \"")
                .Append(Repeat("&e" + (i - 1) + ";", 10))
                .Append("\">");
        }

        var billion = laughs.Append("]><a>&e9;</a>").ToString();
        yield return Make("instance-entity-expansion", rule, billion, XmlErrorCodes.XML2001.Code.Number);
        yield return Make(
            "schema-entity-expansion",
            billion
                .Replace("<a>&e9;</a>", Schema(Rule("true()")), StringComparison.Ordinal)
                .Replace("<!DOCTYPE a", "<!DOCTYPE schema", StringComparison.Ordinal),
            One,
            XmlErrorCodes.XML2001.Code.Number
        );

        yield return Make(
            "svrl-report-size",
            Schema(Rule("false()", "b")),
            "<a>" + Repeat("<b/>", 200_000) + "</a>",
            SchematronDiagnosticCodes.AssertionFailed.Number
        );
    }
}
