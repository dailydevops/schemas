namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity;

using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>The attribute definitions of one element type.</summary>
/// <param name="Element">The element type name.</param>
/// <param name="Definitions">The definitions in declaration order.</param>
internal readonly record struct Attlist(string Element, DtdAttributeDefinition[] Definitions);
