namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Expansion bombs made of parameter entities end in the limit diagnostics (section 4.4.8, design #122 D9).</summary>
public class ParameterEntityExpansionLimitTests
{
    // Level n holds ten references to level n - 1, so one reference to the last level expands to 10 ^ levels characters.
    private static string Bomb(int levels, string reference, string leaf = "0123456789")
    {
        var subset = new StringBuilder($"<!ENTITY % a0 \"{leaf}\">");
        for (var level = 1; level <= levels; level++)
        {
            var text = string.Concat(Enumerable.Repeat($"{reference}a{level - 1};", 10));
            subset.Append(CultureInfo.InvariantCulture, $"<!ENTITY % a{level} \"{text}\">");
        }

        return subset.ToString();
    }

    [Test]
    public async Task BombInEntityValues_EndsInTheExpansionLimit()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(100_000);

        var (_, error) = SubsetHarness.ParseSubset(Bomb(12, "%") + "<!ENTITY g \"%a12;\">", limits);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1109);
    }

    [Test]
    public async Task BombInEntityValues_WithDefaultLimits_EndsInTheExpansionLimit()
    {
        var (_, error) = SubsetHarness.ParseSubset(Bomb(12, "%") + "<!ENTITY g \"%a12;\">");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1109);
    }

    [Test]
    public async Task BombInReferencesInsideDeclarations_EndsInTheExpansionLimit()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(1_000);

        var (_, error) = SubsetHarness.ParseSubset(Bomb(8, "&#37;", "|x") + "<!ELEMENT e (x%a8;)>", limits);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1109);
    }

    [Test]
    public async Task BombInReferencesBetweenDeclarations_EndsInTheExpansionLimit()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(1_000);
        var subset = new StringBuilder("<!ENTITY % a0 \"<!ENTITY g0 'x'>\">");
        for (var level = 1; level <= 8; level++)
        {
            subset.Append(
                CultureInfo.InvariantCulture,
                $"<!ENTITY % a{level} \"{string.Concat(Enumerable.Repeat($"&#37;a{level - 1};", 10))}\">"
            );
        }

        var (_, error) = SubsetHarness.ParseSubset(subset + "%a8;", limits);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1109);
    }

    [Test]
    public async Task ChainOfReferencesInsideADeclaration_BeyondTheDepthLimit_RaisesXml1110()
    {
        var subset =
            "<!ENTITY % a \"&#37;b;\"><!ENTITY % b \"&#37;c;\"><!ENTITY % c \"&#37;d;\"><!ENTITY % d \"EMPTY\"><!ELEMENT x %a;>";

        var (model, ok) = SubsetHarness.ParseSubset(subset);
        var (_, error) = SubsetHarness.ParseSubset(subset, XmlReaderLimits.Default.WithMaxEntityDepth(2));

        await Assert.That(ok).IsNull();
        await Assert.That(model).Contains("E:x:EMPTY");
        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1110);
    }

    [Test]
    public async Task SelfReferencingEntityInsideADeclaration_RaisesXml0020()
    {
        var (_, error) = SubsetHarness.ParseSubset("<!ENTITY % a \"&#37;a;\"><!ELEMENT x %a;>");

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0020);
    }

    [Test]
    public async Task ReferenceInAnEntityValue_ToTheEntityBeingDeclared_IsNotYetDeclared_AndSkipped()
    {
        var (model, error) = SubsetHarness.ParseSubset("<!ENTITY % a \"x%a;y\">");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("PE:a=\"xy\"");
    }

    [Test]
    public async Task ChainInEntityValues_BeyondTheDepthLimit_RaisesXml1110()
    {
        var subset = "<!ENTITY % a \"1\"><!ENTITY % b \"&#37;a;\"><!ENTITY % c \"&#37;b;\"><!ENTITY g \"%c;\">";

        var (_, error) = SubsetHarness.ParseSubset(subset, XmlReaderLimits.Default.WithMaxEntityDepth(2));
        var (model, ok) = SubsetHarness.ParseSubset(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1110);
        await Assert.That(ok).IsNull();
        await Assert.That(model).Contains("GE:g=\"1\"");
    }
}
