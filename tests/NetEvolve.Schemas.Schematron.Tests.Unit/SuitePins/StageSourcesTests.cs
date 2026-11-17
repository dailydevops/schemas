namespace NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;

using System.Text.Json;

public class StageSourcesTests
{
    private const string Stage4Dir = "tests/NetEvolve.Schemas.Schematron.Tests.Integration/Goldens/Stage4Globals/";
    private const string Stage5Dir = "tests/NetEvolve.Schemas.Schematron.Tests.Integration/Goldens/Stage5Grouping/";

    private static JsonElement Stage(string name)
    {
        var path = Path.Combine(SuitePinFiles.SuitesDirectory, "stage-sources.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty(name).Clone();
    }

    private static string[] Strings(JsonElement stage, string property) =>
        [.. stage.GetProperty(property).EnumerateArray().Select(e => e.GetString()!)];

    [Test]
    public async Task Stage4_PointsToConformanceSuiteCasesAndOwnSchemas_NotToAFetchedRuleSet()
    {
        var stage = Stage("stage4");
        var cases = Strings(stage, "cases");
        var schemas = Strings(stage, "ownSchemas");

        await Assert.That(stage.TryGetProperty("url", out _)).IsFalse();
        await Assert.That(stage.TryGetProperty("sha256", out _)).IsFalse();
        await Assert.That(stage.GetProperty("suite").GetString()).IsEqualTo("schematron-conformance");
        await Assert.That(cases).IsNotEmpty();
        await Assert.That(cases.All(c => c.EndsWith(".xml", StringComparison.Ordinal))).IsTrue();
        await Assert.That(schemas).IsNotEmpty();
        await Assert.That(schemas.All(s => s.StartsWith(Stage4Dir, StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Stage5_PointsToOwnTestSchemas_NotToAFetchedRuleSet()
    {
        var stage = Stage("stage5");
        var schemas = Strings(stage, "ownSchemas");

        await Assert.That(stage.TryGetProperty("url", out _)).IsFalse();
        await Assert.That(stage.TryGetProperty("sha256", out _)).IsFalse();
        await Assert.That(stage.TryGetProperty("suite", out _)).IsFalse();
        await Assert.That(schemas).IsNotEmpty();
        await Assert.That(schemas.All(s => s.StartsWith(Stage5Dir, StringComparison.Ordinal))).IsTrue();
    }
}
