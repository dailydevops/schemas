namespace NetEvolve.Schemas.Conformance.Tests.Unit.Reporting;

using NetEvolve.Schemas.Conformance.Reporting;

public class ConformanceLegSelectionTests
{
    [Test]
    [Arguments("net10.0", true)]
    [Arguments("net8.0", false)]
    [Arguments("net9.0", false)]
    [Arguments("net11.0", false)]
    public async Task IsDesignated_NoConfiguration_SelectsOnlyTheDefaultLeg(string leg, bool expected)
    {
        await Assert.That(ConformanceLeg.DefaultLeg).IsEqualTo("net10.0");
        await Assert.That(ConformanceLeg.IsDesignated(leg, null)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments(" , ,")]
    public async Task IsDesignated_BlankConfiguration_SelectsTheDefaultLeg(string configured)
    {
        await Assert.That(ConformanceLeg.IsDesignated("net10.0", configured)).IsTrue();
        await Assert.That(ConformanceLeg.IsDesignated("net8.0", configured)).IsFalse();
    }

    [Test]
    [Arguments("net8.0,net10.0", "net8.0", true)]
    [Arguments("net8.0,net10.0", "net10.0", true)]
    [Arguments("net8.0,net10.0", "net9.0", false)]
    [Arguments(" net9.0 , NET11.0 ", "net9.0", true)]
    [Arguments(" net9.0 , NET11.0 ", "net11.0", true)]
    [Arguments("net9.0", "net10.0", false)]
    [Arguments("net99.0", "net10.0", false)]
    public async Task IsDesignated_ConfiguredLegs_ReplaceTheDefault(string configured, string leg, bool expected) =>
        await Assert.That(ConformanceLeg.IsDesignated(leg, configured)).IsEqualTo(expected);

    [Test]
    public async Task IsDesignated_NullLeg_Throws() =>
        await Assert.That(() => ConformanceLeg.IsDesignated(null!, null)).Throws<ArgumentNullException>();

    [Test]
    public async Task Attribute_IsASkipAttributeWithAReason()
    {
        var attribute = new ConformanceLegAttribute();

        await Assert.That(attribute).IsAssignableTo<SkipAttribute>();
        await Assert.That(attribute.Reason).Contains("designated leg");
    }
}
