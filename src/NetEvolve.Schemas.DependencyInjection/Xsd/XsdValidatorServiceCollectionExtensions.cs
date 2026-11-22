#pragma warning disable NE0002 // Approved namespace Microsoft.Extensions.DependencyInjection (design #111 DP-9, decision on #125)

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.IO;
using NetEvolve.Arguments;
using NetEvolve.Schemas;

/// <summary>Registers XSD validators as lazily compiled singletons, plain or keyed.</summary>
/// <remarks>
/// <para>
/// Both methods delegate to <see cref="SchemaValidatorServiceCollectionExtensions"/> and share its rules: <c>TryAdd</c> semantics, a
/// keyed registration that also registers <see cref="ISchemaValidator"/> under the key, and the same limitations (a keyed container
/// is required and <c>KeyedService.AnyKey</c> is rejected).
/// </para>
/// <para>
/// Nothing is opened, read, or compiled at registration. The first resolve opens the schema stream, adds it to a new
/// <see cref="XsdValidatorBuilder"/>, runs the configure callback, and builds the validator, which every later resolve returns. A
/// problem of the schema set surfaces from that first resolve as a <see cref="SchemaCompilationException"/>; a failed build is not
/// cached, so the next resolve opens the schema again. The version is XSD 1.1 unless the configure callback calls
/// <c>WithVersion</c>; strict 1.0 is <c>WithVersion(XsdVersion.V10)</c>. A schema set of several documents is the stream plus the
/// documents the configure callback adds with <c>AddSchema</c>.
/// </para>
/// <para>The stream returned by the schema callback belongs to the registration: it is disposed right after the build, whether the build succeeds or not.</para>
/// <para>
/// Limitation: the builder never disposes a stream, and the registration disposes only the one returned by the schema callback. A
/// stream that the configure callback passes to <c>AddSchema</c> belongs to that callback (prefer the character and byte overloads).
/// Until XSD-78 is done, version 1.1 is selectable but its additional features are not available.
/// </para>
/// </remarks>
public static class XsdValidatorServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="IXsdValidator"/> that is compiled on first resolve.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="schema">Opens the schema stream on the first resolve; the stream is disposed after the build.</param>
    /// <param name="configure">Adds further schema documents and sets the builder options, for example the version, on the first resolve, or <see langword="null"/>.</param>
    /// <param name="systemId">The system identifier and base URI of the schema for relative references, or <see langword="null"/>.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddXsdValidator(
        this IServiceCollection services,
        Func<IServiceProvider, Stream> schema,
        Action<IServiceProvider, XsdValidatorBuilder>? configure = null,
        string? systemId = null
    )
    {
        Argument.ThrowIfNull(services);
        Argument.ThrowIfNull(schema);

        return services.AddSchemaValidator<IXsdValidator>(provider =>
        {
            using var stream = schema(provider);
            var builder = SchemaValidator.ForXsd().AddSchema(stream, systemId);
            configure?.Invoke(provider, builder);
            return builder.Build();
        });
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IXsdValidator"/> that is compiled on first resolve; <see cref="ISchemaValidator"/>
    /// under the same key forwards to it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The key that names exactly one schema set.</param>
    /// <param name="schema">Opens the schema stream on the first resolve; the stream is disposed after the build.</param>
    /// <param name="configure">Adds further schema documents and sets the builder options, for example the version, on the first resolve, or <see langword="null"/>.</param>
    /// <param name="systemId">The system identifier and base URI of the schema for relative references, or <see langword="null"/>.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddKeyedXsdValidator(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, object?, Stream> schema,
        Action<IServiceProvider, object?, XsdValidatorBuilder>? configure = null,
        string? systemId = null
    )
    {
        Argument.ThrowIfNull(services);
        Argument.ThrowIfNull(schema);

        return services.AddKeyedSchemaValidator<IXsdValidator>(serviceKey, (provider, key) =>
        {
            using var stream = schema(provider, key);
            var builder = SchemaValidator.ForXsd().AddSchema(stream, systemId);
            configure?.Invoke(provider, key, builder);
            return builder.Build();
        });
    }
}
