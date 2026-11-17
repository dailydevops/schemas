namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;

/// <summary>Shared documents and the parse call of the core parser tests.</summary>
internal static class CoreParserFixture
{
    internal const string SystemId = "file:///schemas/core.sch";

    internal static (SchematronSchema? Schema, List<SchematronProblem> Problems, bool Result) Parse(XmlElem root)
    {
        var problems = new FakeProblemSink();
        var result = SchematronCoreParser.TryParse(Build(root), SystemId, problems, out var schema);
        return (schema, problems.Problems, result);
    }

    /// <summary>A schema with every core element, each at its own position.</summary>
    internal static SchematronSchema Full()
    {
        var (schema, problems, result) = Parse(
            E(
                "schema",
                1,
                1,
                A("id", "s1"),
                A("schemaVersion", "1.0"),
                A("schematronEdition", "2025"),
                A("defaultPhase", "ph1"),
                A("queryBinding", "xslt2"),
                E("title", 2, 3, "Title"),
                E("ns", 3, 3, A("prefix", "x"), A("uri", "urn:x")),
                E("p", 4, 3, A("id", "p1"), A("class", "c"), A("icon", "i"), "Para"),
                E("let", 5, 3, A("name", "a"), A("value", "1"), A("as", "xs:integer")),
                E("phase", 6, 3, A("id", "ph1"), E("active", 7, 5, A("pattern", "pat1"), "Act")),
                E(
                    "pattern",
                    8,
                    3,
                    A("id", "pat1"),
                    A("role", "r"),
                    E(
                        "rule",
                        9,
                        5,
                        A("id", "r1"),
                        A("context", "//a"),
                        A("flag", "f1  f2"),
                        A("role", "rr"),
                        A("subject", "."),
                        E("let", 10, 7, A("name", "v"), A("value", "2")),
                        E(
                            "assert",
                            11,
                            7,
                            A("test", "x"),
                            A("id", "a1"),
                            A("flag", "A B"),
                            A("role", "ar"),
                            A("severity", "high"),
                            A("subject", "@s"),
                            A("diagnostics", "d1 d2"),
                            A("properties", "pr1"),
                            "Msg ",
                            E("name", 12, 9, A("path", ".")),
                            E("value-of", 13, 9, A("select", "@x")),
                            E("emph", 14, 9, "e")
                        ),
                        E("report", 15, 7, A("test", "y"), "Rep")
                    )
                ),
                E("diagnostics", 16, 3, E("diagnostic", 17, 5, A("id", "d1"), A("role", "dr"), "Diag"))
            )
        );
        return result && schema is not null
            ? schema
            : throw new InvalidOperationException("The fixture document must parse; problems: " + problems.Count);
    }
}
