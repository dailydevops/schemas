namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.XRechnung;

using System.Text.RegularExpressions;
using System.Xml.Linq;

/// <summary>Reads the scenario file of the KoSIT validator configuration.</summary>
internal static partial class KositScenarioReader
{
    private static readonly XNamespace Ns = "http://www.xoev.de/de/validator/framework/1/scenarios";

    /// <summary>Reads the scenarios in file order.</summary>
    /// <param name="xml">The text of <c>scenarios.xml</c>.</param>
    /// <returns>The scenarios.</returns>
    /// <exception cref="InvalidDataException">A scenario names a Schematron file that is no CEN or XRechnung rule set.</exception>
    public static IReadOnlyList<KositScenario> Read(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        return XDocument
            .Parse(xml)
            .Root!.Elements(Ns + "scenario")
            .Select(s => new KositScenario(
                (string)s.Element(Ns + "name")!,
                s.Elements(Ns + "namespace").ToDictionary(n => (string)n.Attribute("prefix")!, n => n.Value.Trim()),
                s.Element(Ns + "match")!.Value.Trim(),
                Location(s.Element(Ns + "validateWithXmlSchema")!),
                s.Elements(Ns + "validateWithSchematron").Select(e => RuleSet(Location(e))).ToList(),
                s.Descendants(Ns + "customLevel")
                    .ToDictionary(c => c.Value.Trim(), c => (string)c.Attribute("level")!, StringComparer.Ordinal)
            ))
            .ToList();
    }

    private static string Location(XElement step) => step.Descendants(Ns + "location").First().Value.Trim();

    private static KositRuleSet RuleSet(string location)
    {
        var match = RuleSetFile().Match(Path.GetFileName(location));
        return match.Success
            ? new KositRuleSet(
                match.Groups[1].Value == "EN16931" ? "cen" : "xrechnung",
                match.Groups[2].Value == "UBL" ? "ubl" : "cii"
            )
            : throw new InvalidDataException($"The Schematron step '{location}' is no CEN or XRechnung rule set.");
    }

    [GeneratedRegex("^(EN16931|XRechnung)-(UBL|CII)-validation[.]xsl$", RegexOptions.CultureInvariant)]
    private static partial Regex RuleSetFile();
}
