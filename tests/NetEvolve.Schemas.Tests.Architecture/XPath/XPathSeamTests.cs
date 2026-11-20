namespace NetEvolve.Schemas.Tests.Architecture.XPath;

using System.Reflection;
using NetArchTest.Rules;
using NetEvolve.Schemas.Tests.Architecture.Shared;
using NetEvolve.Schemas.XPath.Api;

/// <summary>Pins how the XPath assembly exposes its internals: only to Schematron, Xsd, and its own test assemblies.</summary>
public class XPathSeamTests
{
    // Internal namespaces that Schematron and Xsd may use through InternalsVisibleTo; Subsets joins with XP-57.
    private static readonly string[] SeamNamespaces =
    [
        "NetEvolve.Schemas.XPath.Syntax.Tree",
        "NetEvolve.Schemas.XPath.Abstractions",
        "NetEvolve.Schemas.XPath.Subsets",
        // The Schematron pattern matcher parses with XPathParser, builds step tests with XPathNodeTest, and walks nodes with XPathNodeAccessors (SCH-54).
        "NetEvolve.Schemas.XPath.Syntax.Parser",
        "NetEvolve.Schemas.XPath.Evaluation.Axes",
        "NetEvolve.Schemas.XPath.Xdm.Nodes",
        // The Schematron function instructions apply the function conversion rules and match sequence types of an xsl:function with the XPath code, instead of a second copy (SCH-60, #249).
        "NetEvolve.Schemas.XPath.Functions.Binding",
        "NetEvolve.Schemas.XPath.Types",
        // The Schematron sort of xsl:for-each orders its keys with the XPath comparison of numbers, strings by codepoint, and booleans, instead of a third copy (SCH-82, #255).
        "NetEvolve.Schemas.XPath.Evaluation.Comparison",
        // The Xsd annotation source types the value of an annotated node with the casting table and atomizes with its annotation slot, instead of a second copy of the lexical mappings (XSD-16, #305).
        "NetEvolve.Schemas.XPath.Casting.Core",
        "NetEvolve.Schemas.XPath.Evaluation.Atomization",
    ];

    private static readonly string[] AllowedFriends =
    [
        SourceAssemblies.Schematron,
        SourceAssemblies.Xsd,
        SourceAssemblies.XPath + ".Tests.Architecture",
        SourceAssemblies.XPath + ".Tests.Unit",
        SourceAssemblies.XPath + ".Tests.Integration",
        // The Schematron tests build an xs:integer beyond 64 bits, which no public cast produces yet (#719).
        SourceAssemblies.Schematron + ".Tests.Unit",
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
        var violations = FindDependencies(SourceAssemblies.Load(name), SeamNamespaces, internalOnly: true);

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task FixtureUsingATypeOutsideTheSeam_IsReported()
    {
        var violations = FindDependencies(typeof(XPathSeamTests).Assembly, SeamNamespaces, internalOnly: false);

        await Assert.That(string.Join("|", violations)).Contains(nameof(OutsideSeamFixture));
    }

    private static string[] FriendNames(Assembly assembly) =>
        [
            .. assembly
                .GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>()
                .Select(attribute => attribute.AssemblyName.Split(',')[0]),
        ];

    private static IReadOnlyList<string> FindDependencies(
        Assembly consumer,
        string[] allowedNamespaces,
        bool internalOnly
    )
    {
        var forbidden = SourceAssemblies
            .Load(SourceAssemblies.XPath)
            .GetTypes()
            .Where(type =>
                !type.IsNested
                && !(internalOnly && type.IsPublic)
                && !allowedNamespaces.Contains(type.Namespace)
                && IsXPathType(type)
            )
            .Select(type => type.FullName!)
            .ToArray();

        return forbidden.Length == 0
            ? []
            : Types.InAssembly(consumer).ShouldNot().HaveDependencyOnAny(forbidden).GetResult().FailingTypeNames ?? [];
    }

    // A consumer with access to the internals binds the embedded attributes, polyfills, and synthesized helper types of the
    // compiler (nullable, readonly, ref struct, collection expressions) to the copies in the XPath assembly instead of
    // embedding its own. Only the types that the XPath package declares in its own namespaces are seam candidates.
    private static bool IsXPathType(Type type) =>
        type.Namespace is { } ns && ns.StartsWith(SourceAssemblies.XPath, StringComparison.Ordinal);

    /// <summary>Violating fixture: depends on a type of the XPath assembly outside the seam namespaces.</summary>
    internal static class OutsideSeamFixture
    {
        internal static XPathQName Create() => new("urn:x", "x");
    }
}
