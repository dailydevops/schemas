#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, AP-3, AP-29)

namespace NetEvolve.Schemas;

/// <summary>An immutable, thread-safe validator for documents against their document type declaration. Not intended for implementation by consumers.</summary>
/// <remarks>Limitation: the validator is available when XML-69 lands; until then <c>DtdValidatorBuilder.Build</c> fails with a <see cref="System.NotSupportedException"/>.</remarks>
public interface IDtdValidator : ISchemaValidator { }
