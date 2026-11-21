namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Finds source files that use System.Xml.Schema.</summary>
internal static class SourceScan
{
    /// <summary>Lists the project and C# files below a directory that name System.Xml.Schema in code; comments are ignored.</summary>
    /// <param name="directory">The directory to scan.</param>
    /// <returns>The paths of the files, relative to the directory, with <c>/</c> separators, in ordinal order.</returns>
    public static IReadOnlyList<string> FindSystemXmlSchemaUsage(string directory) =>
        throw new NotSupportedException("Not implemented yet.");
}
