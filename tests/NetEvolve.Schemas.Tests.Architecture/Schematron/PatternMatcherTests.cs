namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Elsewhere;
using NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Patterns;
using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>The pattern matcher has one home and is used by the rule contexts and by <c>xsl:key</c>.</summary>
public class PatternMatcherTests
{
    private const string FixtureHome = "NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Patterns";
    private const string Consumers = "NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Consumers";

    private static System.Reflection.Assembly SchematronAssembly => SourceAssemblies.Load(SourceAssemblies.Schematron);

    private static Type MatcherSeam => SchematronAssembly.GetType(PatternMatcherRules.Seam, throwOnError: true)!;

    [Test]
    public async Task Seam_IsAnInternalInterface()
    {
        await Assert.That(MatcherSeam.IsInterface).IsTrue();
        await Assert.That(MatcherSeam.IsPublic).IsFalse();
    }

    [Test]
    public async Task Seam_HasExactlyOneImplementation_InTheNamespaceOfTheContract()
    {
        var implementations = PatternMatcherRules.Implementations(SchematronAssembly, MatcherSeam);

        await Assert.That(implementations).Count().IsEqualTo(1);
        await Assert.That(PatternMatcherRules.Violations(implementations, PatternMatcherRules.Home)).IsEmpty();
    }

    [Test]
    [Arguments(PatternMatcherRules.RuleContexts)]
    [Arguments(PatternMatcherRules.KeyTables)]
    public async Task Seam_IsUsedBy(string consumerNamespace) =>
        await Assert
            .That(PatternMatcherRules.Consumers(SchematronAssembly, consumerNamespace, MatcherSeam))
            .IsNotEmpty();

    [Test]
    public async Task FixtureSeamWithTwoImplementations_IsReported()
    {
        var implementations = PatternMatcherRules.Implementations(
            typeof(PatternMatcherTests).Assembly,
            typeof(IMatcherSeamFixture)
        );

        await Assert.That(implementations).IsEquivalentTo([typeof(FirstMatcherFixture), typeof(SecondMatcherFixture)]);
        await Assert.That(PatternMatcherRules.Violations(implementations, FixtureHome)).IsNotEmpty();
    }

    [Test]
    public async Task FixtureSeamWithoutImplementation_IsReported() =>
        await Assert.That(PatternMatcherRules.Violations([], FixtureHome)).IsNotEmpty();

    [Test]
    public async Task FixtureImplementationOutsideTheNamedNamespace_IsReported() =>
        await Assert.That(PatternMatcherRules.Violations([typeof(SecondMatcherFixture)], FixtureHome)).IsNotEmpty();

    [Test]
    public async Task FixtureSeamWithOneImplementationInTheNamedNamespace_IsNotReported() =>
        await Assert.That(PatternMatcherRules.Violations([typeof(FirstMatcherFixture)], FixtureHome)).IsEmpty();

    [Test]
    public async Task FixtureNamespaceUsingTheSeam_IsFoundAndAnIdleOneIsNot()
    {
        var assembly = typeof(PatternMatcherTests).Assembly;
        var seam = typeof(IMatcherSeamFixture);

        await Assert
            .That(PatternMatcherRules.Consumers(assembly, $"{Consumers}.Using", seam))
            .IsEquivalentTo([$"{Consumers}.Using.UsesMatcherFixture"]);
        await Assert.That(PatternMatcherRules.Consumers(assembly, $"{Consumers}.Idle", seam)).IsEmpty();
    }
}
