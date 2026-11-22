#pragma warning disable NE0002 // Approved namespace Microsoft.Extensions.DependencyInjection (design #111 DP-9)

namespace Microsoft.Extensions.DependencyInjection;

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetEvolve.Arguments;
using NetEvolve.Schemas;

/// <summary>Registers plain and keyed schema validators as lazily built singletons.</summary>
/// <remarks>
/// Every method has <c>TryAdd</c> semantics: a repeated registration for the same service type (and key) is a silent
/// no-op and its factory is never invoked. Nothing is built at registration; the factory runs on the first resolve and
/// a failure such as <see cref="SchemaCompilationException"/> surfaces unwrapped and is not cached.
/// Limitation: keyed registration needs a container that implements <see cref="IKeyedServiceProvider"/>
/// (Microsoft.Extensions.DependencyInjection 8.0 or later); there is no fallback for other containers.
/// </remarks>
public static class SchemaValidatorServiceCollectionExtensions
{
    /// <summary>Registers a singleton validator that is built by <paramref name="factory"/> on first resolve.</summary>
    /// <typeparam name="TValidator">The validator type, which is also the registered service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Builds the validator.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddSchemaValidator<TValidator>(
        this IServiceCollection services,
        Func<IServiceProvider, TValidator> factory
    )
        where TValidator : class, ISchemaValidator
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.TryAddSingleton(factory);
        return services;
    }

    /// <summary>
    /// Registers a keyed singleton validator that is built by <paramref name="factory"/> on first resolve, and forwards
    /// <see cref="ISchemaValidator"/> under the same key to it.
    /// </summary>
    /// <typeparam name="TValidator">The validator type, which is also the registered service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The key that names exactly one validator.</param>
    /// <param name="factory">Builds the validator from the provider and the key.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddKeyedSchemaValidator<TValidator>(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, object?, TValidator> factory
    )
        where TValidator : class, ISchemaValidator
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceKey);
        ArgumentNullException.ThrowIfNull(factory);

        // Explicit check (#111 DP-5, issue #125 decision): no ArgumentOutOfRangeException.ThrowIf* guard takes an object, ThrowIfEqual<T> needs IEquatable<T>.
        if (ReferenceEquals(serviceKey, KeyedService.AnyKey))
        {
#pragma warning disable RS0030 // #125 DP-5 explicit AnyKey check: no ArgumentOutOfRangeException.ThrowIf equivalent for an object sentinel (decision recorded on #125)
            throw new ArgumentException(
                "KeyedService.AnyKey is a resolution wildcard, not a registrable key.",
                nameof(serviceKey)
            );
#pragma warning restore RS0030
        }

        services.TryAddKeyedSingleton(serviceKey, factory);

        if (typeof(TValidator) != typeof(ISchemaValidator))
        {
            services.TryAddKeyedSingleton<ISchemaValidator>(serviceKey, static (provider, key) =>
                provider.GetRequiredKeyedService<TValidator>(key)
            );
        }

        return services;
    }
}
