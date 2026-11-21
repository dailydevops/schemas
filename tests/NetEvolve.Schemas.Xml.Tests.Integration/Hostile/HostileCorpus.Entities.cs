namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

public static partial class HostileCorpus
{
    // Entities in levels, each referencing the one below it fold times.
    private static string Laughs(int levels, int fold)
    {
        var builder = new StringBuilder("<?xml version=\"1.0\"?><!DOCTYPE lolz [<!ENTITY lol0 \"lol\">");
        for (var level = 1; level <= levels; level++)
        {
            _ = builder.Append(
                CultureInfo.InvariantCulture,
                $"<!ENTITY lol{level} \"{Repeat($"&lol{level - 1};", fold)}\">"
            );
        }

        return builder.Append("]>").ToString();
    }

    // A chain of general entities, each one referencing the one below it once.
    private static string Chain(int levels)
    {
        var builder = new StringBuilder("<!DOCTYPE r [<!ENTITY e0 \"x\">");
        for (var level = 1; level <= levels; level++)
        {
            _ = builder.Append(CultureInfo.InvariantCulture, $"<!ENTITY e{level} \"&e{level - 1};\">");
        }

        return builder.Append("]><r>&e").Append(levels).Append(";</r>").ToString();
    }

    // External parameter entities that reference the next one, so the references nest on the entity stack.
    private static Dictionary<string, Func<string>> ExternalParameterChain(int levels)
    {
        var entries = new List<(string Name, Func<string> Content)>
        {
            ("ext.dtd", () => ExternalParameterDeclarations(levels) + "%p0;"),
        };
        for (var level = 0; level <= levels; level++)
        {
            var next = level;
            entries.Add(($"p{next}.ent", () => next == levels ? string.Empty : $"%p{next + 1};"));
        }

        return Resources([.. entries]);
    }

    private static string ExternalParameterDeclarations(int levels)
    {
        var builder = new StringBuilder();
        for (var level = 0; level <= levels; level++)
        {
            _ = builder.Append(CultureInfo.InvariantCulture, $"<!ENTITY % p{level} SYSTEM \"{Origin}p{level}.ent\">");
        }

        return builder.ToString();
    }

    // Parameter entities that multiply by ten nine times over, used by a general entity.
    private static string ParameterLaughs()
    {
        var builder = new StringBuilder("<!ENTITY % p0 \"lol\">");
        for (var level = 1; level <= 9; level++)
        {
            _ = builder.Append(
                CultureInfo.InvariantCulture,
                $"<!ENTITY % p{level} \"{Repeat($"%p{level - 1};", 10)}\">"
            );
        }

        return builder.Append("<!ENTITY e \"%p9;\">").ToString();
    }

    private static string Doubling()
    {
        var builder = new StringBuilder("<!DOCTYPE r [<!ENTITY e0 \"x\">");
        for (var level = 1; level <= 30; level++)
        {
            _ = builder.Append(CultureInfo.InvariantCulture, $"<!ENTITY e{level} \"&e{level - 1};&e{level - 1};\">");
        }

        return builder.Append("]><r>&e30;</r>").ToString();
    }

    private static readonly string Big = Repeat("A", 60_000);
    private static readonly string BigReferences = Repeat("&big;", 200);

    private static IEnumerable<HostileCase> GeneralEntities()
    {
        yield return new HostileCase(
            "general-laughs-nine-levels-in-content",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1109,
            () => Laughs(9, 10) + "<lolz>&lol9;</lolz>"
        );
        yield return new HostileCase(
            "general-laughs-nine-levels-in-attribute",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1112,
            () => Laughs(9, 10) + "<lolz a=\"&lol9;\"/>"
        );
        yield return new HostileCase(
            "general-quadratic-in-content",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1109,
            () => $"<!DOCTYPE r [<!ENTITY big \"{Big}\">]><r>{BigReferences}</r>"
        );
        yield return new HostileCase(
            "general-quadratic-in-attribute",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1112,
            () => $"<!DOCTYPE r [<!ENTITY big \"{Big}\">]><r a=\"{BigReferences}\"/>"
        );
        yield return new HostileCase(
            "general-doubling-thirty-levels",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1110,
            Doubling
        );
        yield return new HostileCase(
            "general-chain-seventeen-levels",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1110,
            () => Chain(17)
        );
        yield return new HostileCase(
            "general-chain-thousand-levels",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1110,
            () => Chain(1_000)
        );
    }

    private static IEnumerable<HostileCase> RecursiveAndTightEntities()
    {
        yield return new HostileCase(
            "general-self-reference",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML0020,
            () => "<!DOCTYPE r [<!ENTITY a \"&a;\">]><r>&a;</r>"
        );
        yield return new HostileCase(
            "general-mutual-reference",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML0020,
            () => "<!DOCTYPE r [<!ENTITY a \"&b;\"><!ENTITY b \"&a;\">]><r>&a;</r>"
        );
        yield return new HostileCase(
            "general-expansion-limit-of-five",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1109,
            () => "<!DOCTYPE r [<!ENTITY e \"abcdef\">]><r>&e;</r>",
            Limits: XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(5)
        );
        yield return new HostileCase(
            "general-depth-limit-of-one",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1110,
            () => "<!DOCTYPE r [<!ENTITY a \"x\"><!ENTITY b \"&a;\">]><r>&b;</r>",
            Limits: XmlReaderLimits.Default.WithMaxEntityDepth(1)
        );
    }

    private static IEnumerable<HostileCase> ExternalGeneralEntities()
    {
        yield return new HostileCase(
            "general-external-self-reference",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML0020,
            () => $"<!DOCTYPE r [<!ENTITY e SYSTEM \"{Origin}e.xml\">]><r>&e;</r>",
            HostileAccess.Allowed,
            Resources: Resources(("e.xml", () => "<a>&e;</a>"))
        );
        yield return new HostileCase(
            "general-external-huge-replacement-text",
            HostileCategory.GeneralEntityBomb,
            XmlErrorCodes.XML1109,
            () => $"<!DOCTYPE r [<!ENTITY e SYSTEM \"{Origin}e.xml\">]><r>&e;</r>",
            HostileAccess.Allowed,
            Resources: Resources(("e.xml", () => "<a>" + Repeat("A", 11_000_000) + "</a>"))
        );
        yield return new HostileCase(
            "parameter-laughs-in-external-subset",
            HostileCategory.ParameterEntityBomb,
            XmlErrorCodes.XML1109,
            () => $"<!DOCTYPE r SYSTEM \"{Origin}ext.dtd\"><r>&e;</r>",
            HostileAccess.Allowed,
            Resources: Resources(("ext.dtd", ParameterLaughs))
        );
        yield return new HostileCase(
            "parameter-quadratic-in-external-subset",
            HostileCategory.ParameterEntityBomb,
            XmlErrorCodes.XML1109,
            () => $"<!DOCTYPE r SYSTEM \"{Origin}ext.dtd\"><r/>",
            HostileAccess.Allowed,
            Resources: Resources(("ext.dtd", () => $"<!ENTITY % big \"<!--{Big}-->\">{Repeat("%big;", 200)}"))
        );
    }

    private static IEnumerable<HostileCase> ParameterEntities()
    {
        yield return new HostileCase(
            "parameter-quadratic-in-internal-subset",
            HostileCategory.ParameterEntityBomb,
            XmlErrorCodes.XML1109,
            () => $"<!DOCTYPE r [<!ENTITY % big \"<!--{Big}-->\">{Repeat("%big;", 200)}]><r/>"
        );
        yield return new HostileCase(
            "parameter-chain-twenty-levels-of-external-entities",
            HostileCategory.ParameterEntityBomb,
            XmlErrorCodes.XML1110,
            () => $"<!DOCTYPE r SYSTEM \"{Origin}ext.dtd\"><r/>",
            HostileAccess.Allowed,
            Resources: ExternalParameterChain(20)
        );
        yield return new HostileCase(
            "parameter-mutual-reference-of-external-entities",
            HostileCategory.ParameterEntityBomb,
            XmlErrorCodes.XML0020,
            () => $"<!DOCTYPE r SYSTEM \"{Origin}ext.dtd\"><r/>",
            HostileAccess.Allowed,
            Resources: Resources(
                ("ext.dtd", () => $"<!ENTITY % a SYSTEM \"{Origin}a.ent\"><!ENTITY % b SYSTEM \"{Origin}b.ent\">%a;"),
                ("a.ent", () => "%b;"),
                ("b.ent", () => "%a;")
            )
        );
        yield return new HostileCase(
            "parameter-external-self-reference",
            HostileCategory.ParameterEntityBomb,
            XmlErrorCodes.XML0020,
            () => $"<!DOCTYPE r [<!ENTITY % p SYSTEM \"{Origin}p.ent\"> %p;]><r/>",
            HostileAccess.Allowed,
            Resources: Resources(("p.ent", () => "%p;"))
        );
    }

    private static IEnumerable<HostileCase> Floods()
    {
        yield return new HostileCase(
            "parameter-entity-references-100000-in-internal-subset",
            HostileCategory.ParameterEntityBomb,
            DtdLimitCodes.XML1119,
            () => $"<!DOCTYPE r [<!ENTITY % p \"<!ENTITY e 'x'>\">{Repeat("%p;", 100_000)}]><r/>"
        );
        yield return new HostileCase(
            "parameter-entity-references-100000-in-external-subset",
            HostileCategory.ParameterEntityBomb,
            DtdLimitCodes.XML1119,
            () => $"<!DOCTYPE r SYSTEM \"{Origin}ext.dtd\"><r/>",
            HostileAccess.Allowed,
            Resources: Resources(("ext.dtd", () => $"<!ENTITY % p \"<!ENTITY e 'x'>\">{Repeat("%p;", 100_000)}"))
        );
        yield return new HostileCase(
            "declarations-100001-in-external-subset",
            HostileCategory.OtherLimit,
            DtdLimitCodes.XML1119,
            () => $"<!DOCTYPE r SYSTEM \"{Origin}ext.dtd\"><r/>",
            HostileAccess.Allowed,
            Resources: Resources(("ext.dtd", () => Repeat("<!ENTITY e \"x\">", 100_001)))
        );
        yield return new HostileCase(
            "external-entity-references-100-of-one-resource",
            HostileCategory.GeneralEntityBomb,
            DtdLimitCodes.XML1117,
            () => $"<!DOCTYPE r [<!ENTITY e SYSTEM \"{Origin}e.xml\">]><r>{Repeat("&e;", 100)}</r>",
            HostileAccess.Allowed,
            Resources: Resources(("e.xml", () => "<a/>"))
        );
        yield return new HostileCase(
            "external-parameter-entity-references-100-of-one-resource",
            HostileCategory.ParameterEntityBomb,
            DtdLimitCodes.XML1117,
            () => $"<!DOCTYPE r SYSTEM \"{Origin}ext.dtd\"><r/>",
            HostileAccess.Allowed,
            Resources: Resources(
                ("ext.dtd", () => $"<!ENTITY % p SYSTEM \"{Origin}p.ent\">{Repeat("%p;", 100)}"),
                ("p.ent", () => "<!-- x -->")
            )
        );
    }
}
