namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

using System.Xml.Linq;

/// <summary>Reads an <c>environment</c> element of the catalog, a test set, or a test case.</summary>
internal static class Qt3EnvironmentReader
{
    private static readonly XNamespace Ns = "http://www.w3.org/2010/09/qt-fots-catalog";

    /// <summary>Reads one environment.</summary>
    /// <param name="environment">The <c>environment</c> element.</param>
    /// <param name="directory">The directory of the file that declares it; the file attributes are relative to it.</param>
    /// <returns>The environment.</returns>
    public static Qt3Environment Read(XElement environment, string directory)
    {
        var sources = new List<Qt3Source>();
        foreach (var source in environment.Elements(Ns + "source"))
        {
            var file = (string?)source.Attribute("file");
            if (file is not null)
            {
                sources.Add(
                    new Qt3Source(
                        (string?)source.Attribute("role") ?? ".",
                        Path.GetFullPath(Path.Combine(directory, file)),
                        (string?)source.Attribute("uri")
                    )
                );
            }
        }

        var parameters = new List<Qt3Param>();
        foreach (var parameter in environment.Elements(Ns + "param"))
        {
            parameters.Add(new Qt3Param((string)parameter.Attribute("name")!, (string?)parameter.Attribute("select")));
        }

        var namespaces = new List<Qt3Namespace>();
        foreach (var binding in environment.Elements(Ns + "namespace"))
        {
            namespaces.Add(
                new Qt3Namespace(
                    (string?)binding.Attribute("prefix") ?? string.Empty,
                    (string)binding.Attribute("uri")!
                )
            );
        }

        var baseUri = (string?)environment.Element(Ns + "static-base-uri")?.Attribute("uri");
        return new Qt3Environment(
            sources,
            parameters,
            namespaces,
            string.Equals(baseUri, "#UNDEFINED", StringComparison.Ordinal) ? null : baseUri
        );
    }
}
