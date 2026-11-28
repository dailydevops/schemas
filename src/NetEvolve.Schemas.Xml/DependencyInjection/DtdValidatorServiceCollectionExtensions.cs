#pragma warning disable NE0002 // Approved namespace Microsoft.Extensions.DependencyInjection (design #111 DP-9, decision on #125)

namespace Microsoft.Extensions.DependencyInjection;

using System;
using NetEvolve.Schemas;

/// <summary>Registers DTD validators as lazily built singletons, plain or keyed.</summary>
/// <remarks>
/// <para>
/// Both methods delegate to <see cref="SchemaValidatorServiceCollectionExtensions"/> and share its rules: <c>TryAdd</c> semantics, a
/// keyed registration that also registers <see cref="ISchemaValidator"/> under the key, and the same limitations (a keyed container
/// is required and <c>KeyedService.AnyKey</c> is rejected).
/// </para>
/// <para>
/// Nothing is built at registration. The first resolve creates a <see cref="DtdValidatorBuilder"/> through
/// <c>SchemaValidator.ForDtd()</c>, runs the configure callback, and builds the validator, which every later resolve returns.
/// The resolver stays deny-by-default: an external subset is refused unless the callback sets a resolver and enables the external
/// subset. A DTD is part of the document, so the registration takes no schema.
/// </para>
/// <para>Limitation: XML 1.1 documents are not supported (DEF-50).</para>
/// </remarks>
public static class DtdValidatorServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="IDtdValidator"/> that is built on first resolve.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the builder options, for example the resolver, on the first resolve, or <see langword="null"/>.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddDtdValidator(
        this IServiceCollection services,
        Action<IServiceProvider, DtdValidatorBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSchemaValidator<IDtdValidator>(provider =>
        {
            var builder = SchemaValidator.ForDtd();
            configure?.Invoke(provider, builder);
            return builder.Build();
        });
    }

    /// <summary>
    /// Registers a keyed singleton <see cref="IDtdValidator"/> that is built on first resolve; <see cref="ISchemaValidator"/>
    /// under the same key forwards to it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The key that names exactly one validator.</param>
    /// <param name="configure">Sets the builder options, for example the resolver, on the first resolve, or <see langword="null"/>.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddKeyedDtdValidator(
        this IServiceCollection services,
        object serviceKey,
        Action<IServiceProvider, object?, DtdValidatorBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddKeyedSchemaValidator<IDtdValidator>(serviceKey, (provider, key) =>
        {
            var builder = SchemaValidator.ForDtd();
            configure?.Invoke(provider, key, builder);
            return builder.Build();
        });
    }
}
