namespace NetEvolve.Schemas.XPath.Functions.Binding;

using System;
using System.Threading;
using NetEvolve.Arguments;

/// <summary>
/// Collects the function groups through their registration files. Each group folder has one registration file whose module
/// initializer stages a <c>Register(FunctionCatalogBuilder)</c> method here; the first read of <see cref="Registry"/> freezes the
/// catalog, so there is no shared list of groups. An instance per test keeps the global <see cref="Default"/> untouched.
/// </summary>
/// <remarks>Thread-safe and lock-free. Limitation: a group whose registration file never runs is not found; a source generator is the upgrade path (#123 AP8).</remarks>
internal sealed class FunctionCatalog
{
    private State _state = new([], null);

    /// <summary>Gets the catalog of the built-in functions that the module initializers of the registration files stage.</summary>
    public static FunctionCatalog Default { get; } = new();

    /// <summary>Gets the frozen registry; the first read runs every staged registration.</summary>
    /// <exception cref="InvalidOperationException">Two functions register the same expanded name for an overlapping arity and version.</exception>
    public FunctionRegistry Registry
    {
        get
        {
            while (true)
            {
                var current = Volatile.Read(ref _state);
                if (current.Registry is { } frozen)
                {
                    return frozen;
                }

                var builder = new FunctionCatalogBuilder();
                for (var i = 0; i < current.Staged.Length; i++)
                {
                    current.Staged[i](builder);
                }

                // A registration staged in the meantime changes the state, so the freeze runs again over the longer list.
                var next = new State(current.Staged, builder.Build());
                if (ReferenceEquals(Interlocked.CompareExchange(ref _state, next, current), current))
                {
                    return next.Registry!;
                }
            }
        }
    }

    /// <summary>Stages the registration of a group.</summary>
    /// <param name="registration">The registration; it runs once, when the catalog freezes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registration"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The catalog is frozen.</exception>
    public void Stage(Action<FunctionCatalogBuilder> registration)
    {
        Argument.ThrowIfNull(registration);

        while (true)
        {
            var current = Volatile.Read(ref _state);
            if (current.Registry is not null)
            {
                throw new InvalidOperationException(
                    "The function catalog is frozen; a group must be staged before the first lookup."
                );
            }

            var staged = new Action<FunctionCatalogBuilder>[current.Staged.Length + 1];
            Array.Copy(current.Staged, staged, current.Staged.Length);
            staged[current.Staged.Length] = registration;
            if (ReferenceEquals(Interlocked.CompareExchange(ref _state, new State(staged, null), current), current))
            {
                return;
            }
        }
    }

    private sealed class State(Action<FunctionCatalogBuilder>[] staged, FunctionRegistry? registry)
    {
        public Action<FunctionCatalogBuilder>[] Staged { get; } = staged;

        public FunctionRegistry? Registry { get; } = registry;
    }
}
