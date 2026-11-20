namespace NetEvolve.Schemas.XPath.Tests.Unit.Compatibility;

using static CompatibilityFixture;

/// <summary>
/// XPath 1.0 has the namespace axis and XPath 2.0 requires it in compatibility mode, but the engine does not support it (DEF-42, #54;
/// approved in #123): the step is the static error XPST0010 in every mode, a documented deviation from XPath 1.0.
/// </summary>
public class NamespaceAxisTests
{
    [Test]
    [Arguments("namespace::*", true)]
    [Arguments("namespace::*", false)]
    [Arguments("/r/namespace::p", true)]
    [Arguments("/r/b/namespace::node()", true)]
    [Arguments("count(/r/namespace::*)", true)]
    [Arguments("count(/r/namespace::*)", false)]
    public async Task NamespaceAxis_IsAStaticErrorInEveryMode(string expression, bool compatibility)
    {
        var compiled = TryCompile(expression, compatibility, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0010");
    }
}
