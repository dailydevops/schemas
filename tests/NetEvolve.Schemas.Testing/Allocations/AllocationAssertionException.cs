namespace NetEvolve.Schemas.Testing.Allocations;

/// <summary>Thrown when measured allocations exceed the allowed number of bytes.</summary>
public sealed class AllocationAssertionException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="AllocationAssertionException"/> class.</summary>
    public AllocationAssertionException() { }

    /// <summary>Initializes a new instance of the <see cref="AllocationAssertionException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    public AllocationAssertionException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="AllocationAssertionException"/> class.</summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The cause.</param>
    public AllocationAssertionException(string message, Exception innerException)
        : base(message, innerException) { }
}
