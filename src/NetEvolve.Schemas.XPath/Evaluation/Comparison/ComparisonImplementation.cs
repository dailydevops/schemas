namespace NetEvolve.Schemas.XPath.Evaluation.Comparison;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The implementation of a value comparison for one pair of operand types (XPath 2.0, B.2).</summary>
/// <param name="relation">The relation the implementation was looked up with, so that one implementation can serve every relation of a type pair.</param>
/// <param name="left">The left operand: one atomic item of the left type, with <c>xs:untypedAtomic</c> already cast.</param>
/// <param name="right">The right operand: one atomic item of the right type.</param>
/// <returns>Whether the relation holds.</returns>
/// <remarks>An implementation is stateless, because one table serves every evaluation. It raises <c>XPathException</c> for a dynamic error.</remarks>
internal delegate bool ComparisonImplementation(ComparisonRelation relation, in XdmItem left, in XdmItem right);
