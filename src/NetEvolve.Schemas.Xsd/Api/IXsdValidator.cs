#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, #146 DP-1)

namespace NetEvolve.Schemas;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>An immutable, thread-safe validator for XML Schema documents. Not intended for implementation by consumers.</summary>
/// <remarks>Limitation: tree validation and annotated results are added by later work packages (XSD-79).</remarks>
public interface IXsdValidator : ISchemaValidator
{
    /// <summary>Gets the XSD version the validator was built for.</summary>
    XsdVersion Version { get; }
}
