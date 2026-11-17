namespace NetEvolve.Schemas.Tests.Architecture.XPath;

using System.Reflection;
using NetArchTest.Rules;
using NetEvolve.Schemas.Tests.Architecture.Shared;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>Pins how the XPath assembly exposes its internals: only to Schematron, Xsd, and its own test assemblies.</summary>
public class XPathSeamTests
{
    // Internal namespaces that Schematron and Xsd may use through InternalsVisibleTo; Subsets joins with XP-57.
    private static readonly string[] SeamNamespaces =
    [
        "NetEvolve.Schemas.XPath.Syntax.Tree",
        "NetEvolve.Schemas.XPath.Abstractions",
        "NetEvolve.Schemas.XPath.Subsets",
    ];

    private static readonly string[] AllowedFriends =
    [
        SourceAssemblies.Schematron,
        SourceAssemblies.Xsd,
        SourceAssemblies.XPath + ".Tests.Architecture",
        SourceAssemblies.XPath + ".Tests.Unit",
        SourceAssemblies.XPath + ".Tests.Integration",
    ];

    [Test]
    public async Task Surface_InternalsVisibleTo_OnlyToSchematronXsdAndTests()
    {
        var friends = FriendNames(SourceAssemblies.Load(SourceAssemblies.XPath));

        await Assert.That(friends).Contains(SourceAssemblies.Schematron).And.Contains(SourceAssemblies.Xsd);
        await Assert.That(friends.Where(name => !AllowedFriends.Contains(name))).IsEmpty();
    }

    [Test]
    [Arguments(SourceAssemblies.Schematron)]
    [Arguments(SourceAssemblies.Xsd)]
    public async Task Consumer_UsesOnlySeamNamespacesOfTheXPathInternals(string name)
    {
        var violations = FindInternalDependencies(SourceAssemblies.Load(name), SeamNamespaces);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task FixtureUsingAnInternalOutsideTheSeam_IsReported()
    {
        var violations = FindInternalDependencies(typeof(XPathSeamTests).Assembly, SeamNamespaces);

        await Assert.That(violations).Contains(typeof(OutsideSeamFixture).FullName!);
    }

    private static string[] FriendNames(Assembly assembly) =>
        [
            .. assembly
                .GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>()
                .Select(attribute => attribute.AssemblyName.Split(',')[0]),
        ];

    private static IReadOnlyList<string> FindInternalDependencies(Assembly consumer, string[] allowedNamespaces)
    {
        var forbidden = SourceAssemblies
            .Load(SourceAssemblies.XPath)
            .GetTypes()
            .Where(type => !type.IsNested && !type.IsPublic && !allowedNamespaces.Contains(type.Namespace))
            .Select(type => type.FullName!)
            .ToArray();

        return forbidden.Length == 0
            ? []
            : Types.InAssembly(consumer).ShouldNot().HaveDependencyOnAny(forbidden).GetResult().FailingTypeNames ?? [];
    }

    /// <summary>Violating fixture: depends on an internal type of the XPath assembly outside the seam namespaces.</summary>
    internal static class OutsideSeamFixture
    {
        internal static object Create() => XPathErrors.Exception(XPathErrorCodes.XPST0003);
    }
}
