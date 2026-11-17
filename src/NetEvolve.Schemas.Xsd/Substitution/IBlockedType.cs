namespace NetEvolve.Schemas.Xsd.Substitution;

using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A type definition that prohibits substitutions (the <c>block</c> of a complex type).</summary>
internal interface IBlockedType : IXsdComponent
{
    /// <summary>Gets the prohibited substitutions: any of extension and restriction.</summary>
    XsdDerivationMethod Block { get; }
}
