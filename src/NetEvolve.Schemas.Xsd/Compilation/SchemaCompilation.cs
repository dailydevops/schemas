namespace NetEvolve.Schemas.Xsd.Compilation;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>The outcome of a schema compilation: the checked schema set and its compiled content models, or the issues.</summary>
/// <remarks>The set and the model are <see langword="null"/> when the result has an error, because a schema that has an error is not a valid schema (Structures 5.1).</remarks>
internal sealed class SchemaCompilation
{
    internal SchemaCompilation(
        XsdSchemaSet? set,
        ValidationModel? model,
        ValidationResult result,
        IReadOnlyDictionary<ElementDeclaration, IReadOnlyList<IdentityConstraintDefinition>>? identityConstraints = null
    )
    {
        Argument.ThrowIfNull(result);

        Set = set;
        Model = model;
        Result = result;
        IdentityConstraints = identityConstraints ?? NoConstraints;
    }

    private static IReadOnlyDictionary<
        ElementDeclaration,
        IReadOnlyList<IdentityConstraintDefinition>
    > NoConstraints { get; } = new Dictionary<ElementDeclaration, IReadOnlyList<IdentityConstraintDefinition>>();

    /// <summary>Gets the immutable schema set, <see langword="null"/> when the schema has an error.</summary>
    public XsdSchemaSet? Set { get; }

    /// <summary>Gets the content models compiled for the complex types of the set, <see langword="null"/> when the schema has an error.</summary>
    public ValidationModel? Model { get; }

    /// <summary>Gets every issue that the phases collected, in the order they were found.</summary>
    public ValidationResult Result { get; }

    /// <summary>Gets the identity constraints that each element declaration of the set carries, in document order; an element without any is not listed.</summary>
    public IReadOnlyDictionary<
        ElementDeclaration,
        IReadOnlyList<IdentityConstraintDefinition>
    > IdentityConstraints { get; }

    /// <summary>Gets a value indicating whether the schema is valid and the set is available.</summary>
    public bool IsValid => Set is not null;
}
