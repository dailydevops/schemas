#pragma warning disable CA1065, MA0025 // Contract stub: the owning work package replaces the throwing bodies.

namespace NetEvolve.Schemas;

/// <summary>Immutable validation limits. Every limit is finite and bounded by a ceiling.</summary>
/// <remarks>Limitation: properties and <c>With*</c> methods throw until FND-55 lands; the ceilings are final.</remarks>
public sealed class ValidationLimits
{
    /// <summary>The highest allowed <see cref="MaxDepth"/>.</summary>
    public const int MaxDepthCeiling = 10_000;

    /// <summary>The highest allowed <see cref="MaxEntityDepth"/>.</summary>
    public const int MaxEntityDepthCeiling = 1_000;

    /// <summary>The highest allowed <see cref="MaxInputSize"/>.</summary>
    public const long MaxInputSizeCeiling = 2_000_000_000;

    /// <summary>The highest allowed <see cref="MaxResourceSize"/>.</summary>
    public const long MaxResourceSizeCeiling = 2_000_000_000;

    private ValidationLimits() { }

    /// <summary>Gets the default limits.</summary>
    public static ValidationLimits Default => throw Stub.For("FND-55");

    /// <summary>Gets the maximum number of issues kept in a result.</summary>
    public int MaxIssues => throw Stub.For("FND-55");

    /// <summary>Gets the maximum input size in bytes for byte and stream input, in characters for char input.</summary>
    public long MaxInputSize => throw Stub.For("FND-55");

    /// <summary>Gets the maximum element nesting depth.</summary>
    public int MaxDepth => throw Stub.For("FND-55");

    /// <summary>Gets the maximum number of attributes per element.</summary>
    public int MaxAttributesPerElement => throw Stub.For("FND-55");

    /// <summary>Gets the maximum number of characters produced by entity expansion.</summary>
    public long MaxEntityExpansion => throw Stub.For("FND-55");

    /// <summary>Gets the maximum nesting depth of entity references.</summary>
    public int MaxEntityDepth => throw Stub.For("FND-55");

    /// <summary>Gets the maximum number of external resources per validation or compilation.</summary>
    public int MaxExternalResources => throw Stub.For("FND-55");

    /// <summary>Gets the maximum size in bytes of one resolved resource.</summary>
    public long MaxResourceSize => throw Stub.For("FND-55");

    /// <summary>Returns a copy with another <see cref="MaxIssues"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxIssues(int value) => throw Stub.For("FND-55");

    /// <summary>Returns a copy with another <see cref="MaxInputSize"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxInputSize(long value) => throw Stub.For("FND-55");

    /// <summary>Returns a copy with another <see cref="MaxDepth"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxDepth(int value) => throw Stub.For("FND-55");

    /// <summary>Returns a copy with another <see cref="MaxAttributesPerElement"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxAttributesPerElement(int value) => throw Stub.For("FND-55");

    /// <summary>Returns a copy with another <see cref="MaxEntityExpansion"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxEntityExpansion(long value) => throw Stub.For("FND-55");

    /// <summary>Returns a copy with another <see cref="MaxEntityDepth"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxEntityDepth(int value) => throw Stub.For("FND-55");

    /// <summary>Returns a copy with another <see cref="MaxExternalResources"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxExternalResources(int value) => throw Stub.For("FND-55");

    /// <summary>Returns a copy with another <see cref="MaxResourceSize"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxResourceSize(long value) => throw Stub.For("FND-55");
}
