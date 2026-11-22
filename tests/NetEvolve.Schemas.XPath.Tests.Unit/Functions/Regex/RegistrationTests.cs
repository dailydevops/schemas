namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Regex;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

/// <summary>The regular expression functions are registered in the default catalog with the 2.0 arities, and use the name characters of XSD 1.0 (decision D3 of #266).</summary>
public class RegistrationTests
{
    [Test]
    [Arguments("matches", 2, true)]
    [Arguments("matches", 3, true)]
    [Arguments("replace", 3, true)]
    [Arguments("replace", 4, true)]
    [Arguments("tokenize", 2, true)]
    [Arguments("tokenize", 3, true)]
    [Arguments("matches", 1, false)]
    [Arguments("matches", 4, false)]
    [Arguments("replace", 2, false)]
    [Arguments("replace", 5, false)]
    [Arguments("tokenize", 1, false)]
    [Arguments("tokenize", 4, false)]
    public async Task DefaultCatalog_HasTheArities(string local, int arity, bool registered) =>
        await Assert.That(SequencesHarness.TryResolve(local, arity, out _)).IsEqualTo(registered);

    [Test]
    [Arguments("matches('a', '\\i')", "true")]
    [Arguments("matches('Ĳ', '\\i')", "false")]
    [Arguments("matches('Ĳ', '\\c')", "false")]
    [Arguments("matches('‌', '\\c')", "false")]
    [Arguments("matches('\U00010000', '\\i')", "false")]
    [Arguments("matches('ı', '\\i')", "true")]
    public async Task NameEscapes_FollowTheSecondEditionOfXml10(string expression, string expected) =>
        await Assert.That(ApiFixture.Run(expression)).IsEqualTo(expected);
}
