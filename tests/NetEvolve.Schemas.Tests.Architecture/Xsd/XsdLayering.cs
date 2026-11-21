namespace NetEvolve.Schemas.Tests.Architecture.Xsd;

using NetEvolve.Schemas.Tests.Architecture.Schematron;

/// <summary>
/// Namespace layering rules inside the Xsd package (design #146 DP-1 and sections 2 to 4). The first-level namespaces are
/// the layers: the schema document syntax is read first, composition and the component registry build on it, and the
/// runtime validator comes last. Each <c>Only</c> rule names the layers a layer may use and forbids every other namespace
/// of the package, so a namespace that a later work package adds is forbidden until a rule allows it.
/// </summary>
/// <remarks>
/// Limitations: the other families (Versioning, Elements, Attributes, ComplexTypes, Wildcards, Derivation, ContentModels,
/// and so on) are constrained only by the rule that keeps them off the runtime validator. SimpleTypes and
/// IdentityConstraints are exempt from that rule because their checks and listeners attach to the validator today.
/// </remarks>
internal static class XsdLayering
{
    internal static IReadOnlyList<LayerRule> Rules { get; } =
    [
        LayerRules.Only(
            "SchemaDocumentsDependOnlyOnContractsAndDiagnostics",
            "SchemaDocuments",
            "Contracts",
            "Diagnostics"
        ),
        LayerRules.Only("ContractsDependOnlyOnDiagnostics", "Contracts", "Diagnostics"),
        LayerRules.Only(
            "CompositionDependsOnlyOnTheSchemaDocumentSyntax",
            "Composition",
            "SchemaDocuments",
            "Contracts",
            "Diagnostics",
            "Override"
        ),
        LayerRules.Only(
            "ComponentRegistryDependsOnlyOnTheSchemaDocumentSyntax",
            "Components",
            "SchemaDocuments",
            "Contracts",
            "Diagnostics"
        ),
        LayerRules.Never(
            "SchemaModelDoesNotDependOnTheRuntimeValidator",
            "Validation",
            "IdentityConstraints",
            "SimpleTypes"
        ),
    ];
}
