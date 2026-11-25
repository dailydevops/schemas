#pragma warning disable NE0002 // Approved namespace Microsoft.Extensions.DependencyInjection (design #111 DP-9, decision on #125)

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.IO;
using NetEvolve.Arguments;
using NetEvolve.Schemas;

/// <summary>Registers Schematron validators as lazily compiled singletons, plain or keyed.</summary>
/// <remarks>
/// <para>
/// Both methods delegate to <see cref="SchemaValidatorServiceCollectionExtensions"/> and share its rules: <c>TryAdd</c> semantics, a
/// keyed registration that also registers <see cref="ISchemaValidator"/> under the key, and the same limitations (a keyed container
/// is required and <c>KeyedService.AnyKey</c> is rejected).
/// </para>
/// <para>
/// Nothing is opened, read, or compiled at registration. The first resolve opens the schema, runs the configure callback, and builds
/// the validator, which every later resolve returns. A problem of the schema surfaces from that first resolve as a
/// <see cref="SchemaCompilationException"/>; a failed build is not cached, so the next resolve opens the schema again.
/// </para>
/// <para>The stream returned by the schema callback belongs to the registration: it is disposed right after the build, whether the build succeeds or not.</para>
/// </remarks>
public static class SchematronValidatorServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="ISchematronValidator"/> that is compiled on first resolve.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="schema">Opens the schema stream on the first resolve; the stream is disposed after the build.</param>
    /// <param name="configure">Sets the builder options, for example the phase, on the first resolve, or <see langword="null"/>.</param>
    /// <param name="systemId">The base URI of the schema for relative <c>include</c> references, or <see langword="null"/>.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddSchematronValidator(
        this IServiceCollection services,
        Func<IServiceProvider, Stream> schema,
        Action<IServiceProvider, SchematronValidatorBuilder>? configure = null,
        string? systemId = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(schema);

        return services.AddSchemaValidator<ISchematronValidator>(provider =>
        {
            using var stream = schema(provider);
            var builder = SchemaValidator.ForSchematron(stream, systemId);
            configure?.Invoke(provider, builder);
            return builder.Build();
        });
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="ISchematronValidator"/> that is compiled on first resolve; <see cref="ISchemaValidator"/>
    /// under the same key forwards to it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The key that names exactly one validator.</param>
    /// <param name="schema">Opens the schema stream on the first resolve; the stream is disposed after the build.</param>
    /// <param name="configure">Sets the builder options, for example the phase, on the first resolve, or <see langword="null"/>.</param>
    /// <param name="systemId">The base URI of the schema for relative <c>include</c> references, or <see langword="null"/>.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddKeyedSchematronValidator(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, object?, Stream> schema,
        Action<IServiceProvider, object?, SchematronValidatorBuilder>? configure = null,
        string? systemId = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(schema);

        return services.AddKeyedSchemaValidator<ISchematronValidator>(serviceKey, (provider, key) =>
        {
            using var stream = schema(provider, key);
            var builder = SchemaValidator.ForSchematron(stream, systemId);
            configure?.Invoke(provider, key, builder);
            return builder.Build();
        });
    }
}
