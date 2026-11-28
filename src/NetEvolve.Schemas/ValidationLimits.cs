namespace NetEvolve.Schemas;

/// <summary>Immutable validation limits. Every limit is finite and bounded by a ceiling.</summary>
/// <remarks>The defaults are the approved moderate values; they are provisional until benchmarked (see the validation limits design).</remarks>
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

    private ValidationLimits(
        int maxIssues,
        long maxInputSize,
        int maxDepth,
        int maxAttributesPerElement,
        long maxEntityExpansion,
        int maxEntityDepth,
        int maxExternalResources,
        long maxResourceSize
    )
    {
        MaxIssues = maxIssues;
        MaxInputSize = maxInputSize;
        MaxDepth = maxDepth;
        MaxAttributesPerElement = maxAttributesPerElement;
        MaxEntityExpansion = maxEntityExpansion;
        MaxEntityDepth = maxEntityDepth;
        MaxExternalResources = maxExternalResources;
        MaxResourceSize = maxResourceSize;
    }

    /// <summary>Gets the default limits.</summary>
    public static ValidationLimits Default { get; } =
        new(1_000, 64L * 1024 * 1024, 256, 1_024, 10_000_000, 16, 16, 16L * 1024 * 1024);

    /// <summary>Gets the maximum number of issues kept in a result.</summary>
    public int MaxIssues { get; }

    /// <summary>Gets the maximum input size in bytes for byte and stream input, in characters for char input.</summary>
    public long MaxInputSize { get; }

    /// <summary>Gets the maximum element nesting depth.</summary>
    public int MaxDepth { get; }

    /// <summary>Gets the maximum number of attributes per element.</summary>
    public int MaxAttributesPerElement { get; }

    /// <summary>Gets the maximum number of characters produced by entity expansion.</summary>
    public long MaxEntityExpansion { get; }

    /// <summary>Gets the maximum nesting depth of entity references.</summary>
    public int MaxEntityDepth { get; }

    /// <summary>Gets the maximum number of external resources per validation or compilation.</summary>
    public int MaxExternalResources { get; }

    /// <summary>Gets the maximum size in bytes of one resolved resource.</summary>
    public long MaxResourceSize { get; }

    /// <summary>Returns a copy with another <see cref="MaxIssues"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxIssues(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        return new(
            value,
            MaxInputSize,
            MaxDepth,
            MaxAttributesPerElement,
            MaxEntityExpansion,
            MaxEntityDepth,
            MaxExternalResources,
            MaxResourceSize
        );
    }

    /// <summary>Returns a copy with another <see cref="MaxInputSize"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxInputSize(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxInputSizeCeiling);

        return new(
            MaxIssues,
            value,
            MaxDepth,
            MaxAttributesPerElement,
            MaxEntityExpansion,
            MaxEntityDepth,
            MaxExternalResources,
            MaxResourceSize
        );
    }

    /// <summary>Returns a copy with another <see cref="MaxDepth"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxDepth(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxDepthCeiling);

        return new(
            MaxIssues,
            MaxInputSize,
            value,
            MaxAttributesPerElement,
            MaxEntityExpansion,
            MaxEntityDepth,
            MaxExternalResources,
            MaxResourceSize
        );
    }

    /// <summary>Returns a copy with another <see cref="MaxAttributesPerElement"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxAttributesPerElement(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        return new(
            MaxIssues,
            MaxInputSize,
            MaxDepth,
            value,
            MaxEntityExpansion,
            MaxEntityDepth,
            MaxExternalResources,
            MaxResourceSize
        );
    }

    /// <summary>Returns a copy with another <see cref="MaxEntityExpansion"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxEntityExpansion(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        return new(
            MaxIssues,
            MaxInputSize,
            MaxDepth,
            MaxAttributesPerElement,
            value,
            MaxEntityDepth,
            MaxExternalResources,
            MaxResourceSize
        );
    }

    /// <summary>Returns a copy with another <see cref="MaxEntityDepth"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxEntityDepth(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxEntityDepthCeiling);

        return new(
            MaxIssues,
            MaxInputSize,
            MaxDepth,
            MaxAttributesPerElement,
            MaxEntityExpansion,
            value,
            MaxExternalResources,
            MaxResourceSize
        );
    }

    /// <summary>Returns a copy with another <see cref="MaxExternalResources"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxExternalResources(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        return new(
            MaxIssues,
            MaxInputSize,
            MaxDepth,
            MaxAttributesPerElement,
            MaxEntityExpansion,
            MaxEntityDepth,
            value,
            MaxResourceSize
        );
    }

    /// <summary>Returns a copy with another <see cref="MaxResourceSize"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public ValidationLimits WithMaxResourceSize(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxResourceSizeCeiling);

        return new(
            MaxIssues,
            MaxInputSize,
            MaxDepth,
            MaxAttributesPerElement,
            MaxEntityExpansion,
            MaxEntityDepth,
            MaxExternalResources,
            value
        );
    }
}
