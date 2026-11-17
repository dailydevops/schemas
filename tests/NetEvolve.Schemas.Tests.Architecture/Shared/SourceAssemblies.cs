namespace NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>Loads the library assemblies under test.</summary>
internal static class SourceAssemblies
{
    internal const string Schemas = "NetEvolve.Schemas";
    internal const string Xml = "NetEvolve.Schemas.Xml";
    internal const string XPath = "NetEvolve.Schemas.XPath";
    internal const string Schematron = "NetEvolve.Schemas.Schematron";
    internal const string Xsd = "NetEvolve.Schemas.Xsd";
    internal const string DependencyInjection = "NetEvolve.Schemas.DependencyInjection";

    public static IEnumerable<string> Names() => [Schemas, Xml, XPath, Schematron, Xsd, DependencyInjection];

    internal static System.Reflection.Assembly Load(string name) => System.Reflection.Assembly.Load(name);
}
