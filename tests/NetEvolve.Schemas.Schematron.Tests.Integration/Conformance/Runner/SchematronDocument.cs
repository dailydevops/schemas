namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>A named document of a case, such as the instance or an included schema module.</summary>
/// <param name="FileName">The <c>filename</c> the document has when a validator materializes it.</param>
/// <param name="Xml">The serialized document element.</param>
internal sealed record SchematronDocument(string FileName, string Xml);
