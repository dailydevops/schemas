#pragma warning disable CA1028 // Approved shape (#103): byte is the fixed underlying enum type.

namespace NetEvolve.Schemas;

/// <summary>Severity of a validation issue. A fatal condition is an <see cref="Error"/> that stops validation.</summary>
public enum IssueSeverity : byte
{
    /// <summary>Informational message.</summary>
    Information = 0,

    /// <summary>Warning that does not invalidate the document.</summary>
    Warning = 1,

    /// <summary>Error that invalidates the document.</summary>
    Error = 2,
}
