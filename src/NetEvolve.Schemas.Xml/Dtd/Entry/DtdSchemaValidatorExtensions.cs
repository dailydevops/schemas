#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, AP-3)

namespace NetEvolve.Schemas;

/// <summary>
/// The DTD entry point. C# 14 callers write <c>SchemaValidator.ForDtd()</c>; compilers older than C# 14 call
/// <see cref="DtdSchemaValidatorExtensions.ForDtd"/> on this class directly.
/// </summary>
public static class DtdSchemaValidatorExtensions
{
    extension(SchemaValidator)
    {
        /// <summary>Creates a builder for a DTD validator. External access is denied unless the builder enables it.</summary>
        /// <returns>A new builder.</returns>
        public static DtdValidatorBuilder ForDtd() => new();
    }
}
