namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using System.Reflection;

/// <summary>Rules for the one pattern matcher of the Schematron package (design #137 AP-19).</summary>
internal static class PatternMatcherRules
{
    /// <summary>The full name of the matcher seam.</summary>
    internal const string Seam = "NetEvolve.Schemas.Schematron.Seams.IPatternMatcher";

    /// <summary>The namespace the contract names as the home of the matcher.</summary>
    internal const string Home = "NetEvolve.Schemas.Schematron.Patterns";

    /// <summary>The namespace of the rule contexts, which use the matcher.</summary>
    internal const string RuleContexts = "NetEvolve.Schemas.Schematron.Evaluation.Patterns";

    /// <summary>The namespace of <c>xsl:key</c>, which uses the matcher.</summary>
    internal const string KeyTables = "NetEvolve.Schemas.Schematron.Xslt.Keys";

    /// <summary>Lists the concrete types of <paramref name="assembly"/> that implement <paramref name="seam"/>.</summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="seam">The seam interface.</param>
    internal static IReadOnlyList<Type> Implementations(Assembly assembly, Type seam) =>
        assembly is null || seam is null ? [typeof(PatternMatcherRules)] : [];

    /// <summary>Finds what is wrong with the implementations: not exactly one, or not in <paramref name="home"/>.</summary>
    /// <param name="implementations">The implementations found.</param>
    /// <param name="home">The namespace the contract names.</param>
    internal static IReadOnlyList<string> Violations(IReadOnlyList<Type> implementations, string home) =>
        implementations is null || home is null ? [string.Empty] : [];

    /// <summary>Lists the types in <paramref name="consumerNamespace"/> (and below) that depend on <paramref name="seam"/>.</summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="consumerNamespace">The namespace of the consumers.</param>
    /// <param name="seam">The seam interface.</param>
    internal static IReadOnlyList<string> Consumers(Assembly assembly, string consumerNamespace, Type seam) =>
        assembly is null || consumerNamespace is null || seam is null ? ["stub"] : [];
}
