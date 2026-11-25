namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

/// <summary>
/// Namespace layering rules inside the Schematron package (design #137 sections 2 and 9). The first-level namespaces are
/// the layers: the pipeline runs Parsing, Loading, Expansion, then the compile families (Bindings, Patterns, Xslt), then
/// Evaluation; Results (the issue mapper) and Svrl (the report writer) consume the event contract and nothing of the
/// stages. Each rule names the layers a layer may use and forbids every other namespace of the package, so a namespace that
/// a later work package adds is forbidden until a rule allows it.
/// </summary>
/// <remarks>
/// Loading may use Xslt: stage Resolve links the XSLT modules of a schema (design #150 EP-14, #832).
/// Limitations: Patterns, Xslt, Scopes, Phases, and Diagnostics are not constrained, because no contract places them.
/// Evaluation talks to the event interface in Seams only (design AP-27) and does not reach into Results.
/// </remarks>
internal static class SchematronLayering
{
    internal static IReadOnlyList<LayerRule> Rules { get; } =
    [
        LayerRules.Only("ModelDependsOnNothingElseInThePackage", "Model"),
        LayerRules.Only("SeamsDependOnlyOnTheModelAndDiagnostics", "Seams", "Model", "Diagnostics"),
        LayerRules.Only("ParsingDependsOnlyOnTheModelAndSeams", "Parsing", "Model", "Seams", "Diagnostics", "Editions"),
        LayerRules.Only(
            "LoadingDependsOnlyOnParsingTheModelAndSeams",
            "Loading",
            "Parsing",
            "Model",
            "Seams",
            "Diagnostics",
            "Xslt"
        ),
        LayerRules.Only("ExpansionDependsOnlyOnTheModelAndSeams", "Expansion", "Model", "Seams", "Diagnostics"),
        LayerRules.Only(
            "EvaluationDoesNotReachIntoTheOtherStages",
            "Evaluation",
            "Model",
            "Seams",
            "Diagnostics",
            "Scopes"
        ),
        LayerRules.Only("MapperDependsOnlyOnTheModelAndSeams", "Results", "Model", "Seams", "Diagnostics"),
        LayerRules.Only("WriterDependsOnlyOnTheModelAndSeams", "Svrl", "Model", "Seams", "Diagnostics"),
        LayerRules.Only("BindingsDependOnlyOnTheModelAndSeams", "Bindings", "Model", "Seams", "Diagnostics"),
    ];
}
