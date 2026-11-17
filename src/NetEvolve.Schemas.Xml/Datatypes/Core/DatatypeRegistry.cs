namespace NetEvolve.Schemas.Xml.Datatypes.Core;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The built types of one XSD version. Types are created lazily while the constructor runs; once the constructor returns nothing changes, so it is safe to share between threads.</summary>
public sealed class DatatypeRegistry
{
    private readonly Dictionary<string, DatatypeRegistration> _pending = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IDatatypeDescriptor> _built = new(StringComparer.Ordinal);
    private readonly List<IDatatypeDescriptor> _all = [];

    /// <summary>Initializes a new instance of the <see cref="DatatypeRegistry"/> class.</summary>
    /// <param name="version">The XSD version; registrations whose first version is later are left out.</param>
    /// <param name="registrations">The registrations.</param>
    public DatatypeRegistry(XsdVersion version, IReadOnlyList<DatatypeRegistration> registrations)
    {
        Argument.ThrowIfNull(registrations);
        Version = version;
        for (var i = 0; i < registrations.Count; i++)
        {
            if (registrations[i].FirstVersion <= version)
            {
                _pending.Add(registrations[i].Name, registrations[i]);
            }
        }

        for (var i = 0; i < registrations.Count; i++)
        {
            if (registrations[i].FirstVersion <= version)
            {
                _ = Get(registrations[i].Name);
            }
        }
    }

    /// <summary>Gets the XSD version of the registry.</summary>
    public XsdVersion Version { get; }

    /// <summary>Gets all types, dependencies before the types derived from them.</summary>
    public IReadOnlyList<IDatatypeDescriptor> All => _all;

    /// <summary>Looks up a type by name.</summary>
    /// <param name="name">The name of the type.</param>
    /// <param name="descriptor">The type, <see langword="null"/> when unknown.</param>
    /// <returns><see langword="true"/> when the type exists in this version.</returns>
    public bool TryGet(string name, out IDatatypeDescriptor? descriptor)
    {
        var found = _built.TryGetValue(name, out var built);
        descriptor = built;
        return found;
    }

    /// <summary>Gets a type by name, creating it first when a registration is waiting for it.</summary>
    /// <param name="name">The name of the type.</param>
    /// <returns>The type.</returns>
    /// <exception cref="InvalidOperationException">The type is unknown in this version, or its registration depends on itself.</exception>
    public IDatatypeDescriptor Get(string name)
    {
        if (_built.TryGetValue(name, out var built))
        {
            return built;
        }

        if (!_pending.TryGetValue(name, out var registration))
        {
            throw new InvalidOperationException(
                $"The datatype '{name}' is not registered for XSD {Version}, or its registration depends on itself."
            );
        }

        // Removed before the factory runs: a cycle then ends in the exception above instead of a stack overflow.
        _ = _pending.Remove(name);
        var created = registration.Create(this);
        _built.Add(name, created);
        _all.Add(created);
        return created;
    }
}
