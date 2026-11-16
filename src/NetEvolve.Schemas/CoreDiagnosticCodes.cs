namespace NetEvolve.Schemas;

/// <summary>The exhaustive catalog of core diagnostic codes.</summary>
public static class CoreDiagnosticCodes
{
    /// <summary>The input exceeds <c>MaxInputSize</c>.</summary>
    public static readonly DiagnosticCode InputSizeLimitExceeded = new(1);

    /// <summary>The element nesting exceeds <c>MaxDepth</c>.</summary>
    public static readonly DiagnosticCode DepthLimitExceeded = new(2);

    /// <summary>An element exceeds <c>MaxAttributesPerElement</c>.</summary>
    public static readonly DiagnosticCode AttributeLimitExceeded = new(3);

    /// <summary>Entity expansion exceeds <c>MaxEntityExpansion</c>.</summary>
    public static readonly DiagnosticCode EntityExpansionLimitExceeded = new(4);

    /// <summary>Entity nesting exceeds <c>MaxEntityDepth</c>.</summary>
    public static readonly DiagnosticCode EntityDepthLimitExceeded = new(5);

    /// <summary>The number of external resources exceeds <c>MaxExternalResources</c>.</summary>
    public static readonly DiagnosticCode ExternalResourceLimitExceeded = new(6);

    /// <summary>A resolved resource exceeds <c>MaxResourceSize</c>.</summary>
    public static readonly DiagnosticCode ResourceSizeLimitExceeded = new(7);

    /// <summary>The resolver refused a resource.</summary>
    public static readonly DiagnosticCode ResourceDenied = new(100);

    /// <summary>A relative reference has no absolute base.</summary>
    public static readonly DiagnosticCode ResourceBaseUriMissing = new(101);

    /// <summary>The resolver found nothing.</summary>
    public static readonly DiagnosticCode ResourceNotFound = new(102);

    /// <summary>The reference is not a valid URI reference after escaping.</summary>
    public static readonly DiagnosticCode ResourceReferenceInvalid = new(103);
}
