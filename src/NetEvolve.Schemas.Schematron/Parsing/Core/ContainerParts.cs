namespace NetEvolve.Schemas.Schematron.Parsing.Core;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>The parts of a <c>schema</c> or <c>library</c> collected while its children are read.</summary>
internal sealed class ContainerParts
{
    internal SchematronTitle? Title { get; set; }

    internal SchematronDiagnostics? Diagnostics { get; set; }

    internal List<SchematronParagraph> Paragraphs { get; } = [];

    internal List<SchematronLet> Lets { get; } = [];

    internal List<SchematronNamespace> Namespaces { get; } = [];

    internal List<SchematronPhase> Phases { get; } = [];

    internal List<SchematronPatternBase> Patterns { get; } = [];
}
