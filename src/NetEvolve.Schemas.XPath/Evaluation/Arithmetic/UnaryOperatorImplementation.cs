namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The implementation of a unary arithmetic operator for one operand type (XPath 2.0, B.2).</summary>
/// <param name="operand">The operand: one atomic item, with <c>xs:untypedAtomic</c> already cast.</param>
/// <returns>The result.</returns>
/// <remarks>An implementation is stateless, because one table serves every evaluation. It raises <c>XPathException</c> for a dynamic error.</remarks>
internal delegate XdmItem UnaryOperatorImplementation(in XdmItem operand);
