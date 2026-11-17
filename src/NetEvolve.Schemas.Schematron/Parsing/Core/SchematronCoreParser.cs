namespace NetEvolve.Schemas.Schematron.Parsing.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Placeholder of the core parser; replaced by the implementation commit.</summary>
internal static class SchematronCoreParser
{
    internal static bool TryParse(XmlNode node, string? systemId, IProblemSink problems, out SchematronSchema? schema)
    {
        Argument.ThrowIfNull(node.Tree);
        Argument.ThrowIfNull(problems);

        _ = systemId;
        schema = null;
        return false;
    }
}
