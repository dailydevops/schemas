namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Schematron;

using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Testing.Fakes;

internal static class SchematronRegistrationTestSupport
{
    internal const string Instance = "<invoice xmlns=\"urn:invoice\"><total>2500</total></invoice>";

    /// <summary>Fires on <see cref="Instance"/> because the invoice has no number.</summary>
    internal const string NumberSchema = """
        <schema xmlns="http://purl.oclc.org/dsdl/schematron">
          <ns prefix="i" uri="urn:invoice"/>
          <pattern id="p1">
            <rule context="i:invoice">
              <assert id="a1" test="i:number">An invoice needs a number.</assert>
            </rule>
          </pattern>
        </schema>
        """;

    /// <summary>Fires on <see cref="Instance"/> because the total is large.</summary>
    internal const string TotalSchema = """
        <schema xmlns="http://purl.oclc.org/dsdl/schematron">
          <ns prefix="i" uri="urn:invoice"/>
          <pattern id="p1">
            <rule context="i:invoice">
              <report id="r1" test="i:total &gt; 1000">The invoice is large.</report>
            </rule>
          </pattern>
        </schema>
        """;

    /// <summary>Two patterns, each of which fires on <see cref="Instance"/>; the phase <c>basic</c> selects the first only.</summary>
    internal const string PhaseSchema = """
        <schema xmlns="http://purl.oclc.org/dsdl/schematron">
          <ns prefix="i" uri="urn:invoice"/>
          <phase id="basic"><active pattern="p1"/></phase>
          <pattern id="p1">
            <rule context="i:invoice"><assert test="i:number">No number.</assert></rule>
          </pattern>
          <pattern id="p2">
            <rule context="i:invoice"><report test="i:total &gt; 1000">Large.</report></rule>
          </pattern>
        </schema>
        """;

    /// <summary>The rule of this schema lives in the module <c>rule.sch</c>, which only a resolver can supply.</summary>
    internal const string IncludingSchema = """
        <schema xmlns="http://purl.oclc.org/dsdl/schematron">
          <ns prefix="i" uri="urn:invoice"/>
          <pattern id="p1"><include href="rule.sch"/></pattern>
        </schema>
        """;

    internal const string IncludedRule =
        "<rule xmlns=\"http://purl.oclc.org/dsdl/schematron\" context=\"i:invoice\"><assert test=\"i:number\">Included number.</assert></rule>";

    internal const string InvalidSchema = "<schema xmlns=\"http://purl.oclc.org/dsdl/schematron\"><pattern><rule";

    internal static DisposeTrackingStream Open(string text) => new(Encoding.UTF8.GetBytes(text));

    internal static string Messages(ValidationResult result)
    {
        var messages = new List<string>();
        foreach (var issue in result.Issues)
        {
            messages.Add(issue.Message);
        }

        return string.Join("|", messages);
    }

    internal static ValidationResult Validate(ISchemaValidator validator) => validator.Validate(Instance.AsSpan());
}
