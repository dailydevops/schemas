#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, #146 DP-1)

namespace NetEvolve.Schemas;

/// <summary>
/// The XSD entry point. C# 14 callers write <c>SchemaValidator.ForXsd()</c>; compilers older than C# 14 call
/// <see cref="XsdSchemaValidatorExtensions.ForXsd"/> on this class directly.
/// </summary>
public static class XsdSchemaValidatorExtensions
{
    extension(SchemaValidator)
    {
        /// <summary>Creates a builder for an XSD validator. The default is XSD 1.1.</summary>
        /// <returns>A new builder.</returns>
        public static XsdValidatorBuilder ForXsd() => new();
    }
}
