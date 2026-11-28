namespace NetEvolve.Schemas.Schematron.Xslt.Registry;

using System;
using System.Threading;

/// <summary>
/// The staged registrations of the instruction packages and the default registry built from them (design #150 EP-10). A
/// registration file stages one <c>Register</c> delegate and builds nothing; the default registry is built once, at its
/// first use, and closes the catalog.
/// </summary>
internal sealed class XsltRegistryCatalog
{
    private readonly Lazy<XsltInstructionRegistry> _default;

    // The staged registrations; replaced copy-on-write, and null once the default registry is built (the catalog is closed).
    private Action<XsltRegistryBuilder>[]? _staged = [];

    internal XsltRegistryCatalog() => _default = new Lazy<XsltInstructionRegistry>(Build);

    /// <summary>Gets the catalog that the registration files of the assembly stage into.</summary>
    internal static XsltRegistryCatalog Shared { get; } = new();

    /// <summary>Gets the default registry; the first call builds it, later calls return the same instance.</summary>
    internal XsltInstructionRegistry Default => _default.Value;

    /// <summary>Gets whether the default registry has been built.</summary>
    internal bool IsDefaultCreated => _default.IsValueCreated;

    /// <summary>Stages a registration; nothing runs until the default registry is built.</summary>
    /// <param name="register">The registration of one instruction folder.</param>
    /// <exception cref="InvalidOperationException">The default registry is already built.</exception>
    internal void Stage(Action<XsltRegistryBuilder> register)
    {
        ArgumentNullException.ThrowIfNull(register);

        while (true)
        {
            var current = Volatile.Read(ref _staged)
                ?? throw new InvalidOperationException(
                    "The XSLT instruction catalog is closed: the default registry was already built."
                );

            var next = new Action<XsltRegistryBuilder>[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = register;
            if (ReferenceEquals(Interlocked.CompareExchange(ref _staged, next, current), current))
            {
                return;
            }
        }
    }

    private XsltInstructionRegistry Build()
    {
        var snapshot = Interlocked.Exchange(ref _staged, null) ?? [];

        var builder = new XsltRegistryBuilder();
        foreach (var register in snapshot)
        {
            register(builder);
        }

        return builder.Build();
    }
}
