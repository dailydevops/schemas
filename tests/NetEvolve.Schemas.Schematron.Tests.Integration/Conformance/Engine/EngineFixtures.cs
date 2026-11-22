namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Builds small cases for the engine tests.</summary>
internal static class EngineFixtures
{
    private const string Sch = "http://purl.oclc.org/dsdl/schematron";

    /// <summary>A schema with one rule on the document node and one assert or report.</summary>
    public static string Schema(string test, string element = "assert") =>
        $"""<sch:schema xmlns:sch="{Sch}"><sch:pattern><sch:rule context="/"><sch:{element} test="{test}"/></sch:rule></sch:pattern></sch:schema>""";

    public static SchematronCase Case(
        SchematronOutcome expect,
        IReadOnlyList<string> schemas,
        IReadOnlyList<string>? assertions = null,
        string? phase = null,
        string instance = "<root xmlns=\"\"/>",
        params SchematronDocument[] extra
    ) =>
        new(
            "set/fixture.xml",
            "set",
            schemas,
            instance,
            [new SchematronDocument("document.xml", instance), .. extra],
            expect,
            phase,
            assertions ?? [],
            ["xslt-2"]
        );
}
