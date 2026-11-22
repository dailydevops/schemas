namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System.Collections.Generic;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Compilation;

/// <summary>
/// The second composition root of the package (design #146 DP-5): compiles the schema documents of the builder into a checked,
/// immutable schema and assembles the validator that runs on it.
/// </summary>
/// <remarks>
/// A schema that has an error never yields a validator: the build throws a <see cref="SchemaCompilationException"/> with every issue
/// that the compilation collected. Without any schema document the build succeeds, and every element of an instance is undeclared.
/// </remarks>
internal static class ValidatorAssembly
{
    /// <summary>Compiles the inputs and assembles the validator.</summary>
    /// <param name="version">The XSD version.</param>
    /// <param name="dtdProcessing">Whether the validator reads the document type declaration of an instance.</param>
    /// <param name="limits">The limits.</param>
    /// <param name="resolver">The resolver for the documents that a schema document refers to.</param>
    /// <param name="inputs">The schema documents.</param>
    /// <param name="hintsEnabled">Whether the validator loads the schema documents that the hints of an instance name, through <paramref name="resolver"/>.</param>
    /// <param name="cancellationToken">The token that is observed between the documents and at every resolver call.</param>
    /// <returns>The validator.</returns>
    /// <exception cref="SchemaCompilationException">The schema has an error.</exception>
    internal static XsdValidator Create(
        XsdVersion version,
        bool dtdProcessing,
        ValidationLimits limits,
        IResourceResolver resolver,
        IReadOnlyList<SchemaInput> inputs,
        CancellationToken cancellationToken,
        bool hintsEnabled = false
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(inputs);

        var compilation = new SchemaCompiler(version, resolver, limits).Compile(inputs, cancellationToken);
        if (!compilation.IsValid)
        {
            throw new SchemaCompilationException(compilation.Result.Issues);
        }

        return new XsdValidator(
            new ValidationCore(compilation, version, dtdProcessing, limits, hintsEnabled ? resolver : null),
            version,
            limits,
            resolver
        );
    }
}
