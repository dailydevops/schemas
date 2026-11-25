namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using System.Globalization;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;

public class AssertionIdentifierTests
{
    [Test]
    public async Task Create_AssertionId_Wins()
    {
        var id = AssertionIdentifier.Create("a1", "r1", 3, 2, 1);

        await Assert.That(id).IsEqualTo("a1");
    }

    [Test]
    public async Task Create_RuleIdPlusIndex_WhenAssertionHasNoId()
    {
        var id = AssertionIdentifier.Create(null, "r1", 3, 2, 4);

        await Assert.That(id).IsEqualTo("r1#a4");
    }

    [Test]
    public async Task Create_NoIds_UsesPatternRuleAndAssertionIndex()
    {
        var id = AssertionIdentifier.Create(null, null, 3, 2, 4);

        await Assert.That(id).IsEqualTo("#p3.r2.a4");
    }

    [Test]
    public async Task Create_IsStableAcrossRuns()
    {
        var first = AssertionIdentifier.Create(null, null, 12, 34, 56);
        var second = AssertionIdentifier.Create(null, null, 12, 34, 56);

        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task Create_Generated_NeverCollidesWithAuthorId()
    {
        // The hash sign is not an NCName character, so an author id can never equal a generated one.
        var id = AssertionIdentifier.Create(null, null, 1, 1, 1);
        var ruleBased = AssertionIdentifier.Create(null, "r", 1, 1, 1);

        await Assert.That(id.Contains('#', StringComparison.Ordinal)).IsTrue();
        await Assert.That(ruleBased.Contains('#', StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Create_IsA_InstantiatedTwice_IdsAreDistinct()
    {
        var first = AssertionIdentifier.Create("a1", null, 1, 1, 1, expansion: 1);
        var second = AssertionIdentifier.Create("a1", null, 2, 1, 1, expansion: 2);
        var plain = AssertionIdentifier.Create("a1", null, 3, 1, 1);

        await Assert.That(first).IsEqualTo("a1#i1");
        await Assert.That(second).IsEqualTo("a1#i2");
        await Assert.That(plain).IsEqualTo("a1");
    }

    [Test]
    public async Task Create_RuleBasedId_IgnoresTheExpansion()
    {
        // AP-13 gives the expansion suffix to author assertion ids only.
        var id = AssertionIdentifier.Create(null, "r1", 1, 1, 2, expansion: 3);

        await Assert.That(id).IsEqualTo("r1#a2");
    }

    [Test]
    [Arguments("tr-TR")]
    [Arguments("ar-SA")]
    public async Task Create_IsInvariantUnderCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            var id = AssertionIdentifier.Create(null, null, 1234, 5, 6);

            await Assert.That(id).IsEqualTo("#p1234.r5.a6");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
