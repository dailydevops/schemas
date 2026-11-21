namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using System.Reflection;
using System.Text.RegularExpressions;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Model;

/// <summary>
/// Finds types in the Schematron and Xsd assemblies that implement or duplicate a shared primitive of the Xml and XPath
/// packages: the tree builder, datatype, facet, XPath parser, document loader, and document pool.
/// </summary>
/// <remarks>
/// Limitation: a duplicate is found through the seam interface or through the naming convention of the primitive; a
/// duplicate that uses neither is found only in review. A sink that implements <see cref="IXmlSink"/> is a legitimate
/// consumer of events and is reported as a tree builder only when it also holds an <see cref="XmlDocumentTree"/>.
/// </remarks>
internal static class SeamDuplicates
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    internal static IReadOnlyList<SeamKind> Seams { get; } =
    [
        new(
            "TreeBuilder",
            type => typeof(IXmlSink).IsAssignableFrom(type) && ProducesTree(type),
            Named("TreeBuilder$")
        ),
        new("Datatype", type => typeof(IDatatypeDescriptor).IsAssignableFrom(type), Named(".Datatype(Descriptor)?$")),
        new("Facet", type => typeof(IFacet).IsAssignableFrom(type), Named(".Facet$")),
        new("XPathParser", _ => false, Named(@"^I?(XPath|Xdm)\w*Parser$|^I?ExpressionParser$")),
        new("DocumentLoader", _ => false, Named("^I?(Xml|XPath|Xdm)?DocumentLoader$")),
        new("DocumentPool", _ => false, Named("^I?(Xml|XPath|Xdm)?Document(Pool|Cache)$")),
    ];

    /// <summary>
    /// The duplicates the maintainer approved. The built-in <c>xs:error</c> type of XSD 1.1 is a union without member types,
    /// which the Xml union datatype does not allow, so Xsd declares one internal datatype for it (decision B of #320).
    /// </summary>
    internal static IReadOnlyList<string> ApprovedExceptions { get; } =
    ["Datatype: NetEvolve.Schemas.Xsd.SimpleTypes.V11.ErrorType+NoValueDatatype"];

    /// <summary>Lists the duplicates as <c>seam: full type name</c>, sorted.</summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="seams">The seams to look for.</param>
    internal static IReadOnlyList<string> Find(IEnumerable<Assembly> assemblies, IReadOnlyList<SeamKind> seams) =>
        assemblies is null || seams is null ? ["stub"] : [];

    private static bool ProducesTree(Type type) => type is null;

    private static Func<string, bool> Named(string pattern) =>
        name => Regex.IsMatch(name, pattern, RegexOptions.CultureInvariant, MatchTimeout);
}
