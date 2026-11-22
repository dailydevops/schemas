#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, AP-3, AP-29)

namespace NetEvolve.Schemas;

/// <summary>An immutable, thread-safe validator for documents against their document type declaration. Not intended for implementation by consumers.</summary>
/// <remarks>
/// A validity error is an issue of severity error and never stops the run; a fatal status of the reader (a well-formedness error, a limit, or a denied external request) is the last issue.
/// Limitations: a stream and a sequence are buffered completely before the run (#926), and the run observes the cancellation token only before it starts.
/// </remarks>
public interface IDtdValidator : ISchemaValidator { }
