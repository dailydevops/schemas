#pragma warning disable NE0002 // Approved namespace Microsoft.Extensions.DependencyInjection (design #111 DP-9, decision on #125)

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.IO;
using NetEvolve.Schemas;

/// <summary>Registers Schematron validators as lazily compiled singletons, plain or keyed.</summary>
public static class SchematronValidatorServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="ISchematronValidator"/>.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="schema">Opens the schema.</param>
    /// <param name="configure">Configures the builder, or <see langword="null"/>.</param>
    /// <param name="systemId">The base URI of the schema, or <see langword="null"/>.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddSchematronValidator(
        this IServiceCollection services,
        Func<IServiceProvider, Stream> schema,
        Action<IServiceProvider, SchematronValidatorBuilder>? configure = null,
        string? systemId = null
    ) => throw new NotSupportedException();

    /// <summary>Registers a keyed singleton <see cref="ISchematronValidator"/>.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceKey">The key.</param>
    /// <param name="schema">Opens the schema.</param>
    /// <param name="configure">Configures the builder, or <see langword="null"/>.</param>
    /// <param name="systemId">The base URI of the schema, or <see langword="null"/>.</param>
    /// <returns>The same <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddKeyedSchematronValidator(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, object?, Stream> schema,
        Action<IServiceProvider, object?, SchematronValidatorBuilder>? configure = null,
        string? systemId = null
    ) => throw new NotSupportedException();
}
