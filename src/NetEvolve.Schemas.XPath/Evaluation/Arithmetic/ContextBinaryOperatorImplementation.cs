namespace NetEvolve.Schemas.XPath.Evaluation.Arithmetic;

using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The implementation of a binary arithmetic operator for one pair of operand types that reads the dynamic context (XPath 2.0, B.2):
/// the subtraction of two dates, times, or dateTimes substitutes the implicit timezone for a missing one (Functions and Operators 1.0, 10.8).
/// </summary>
/// <param name="left">The left operand: one atomic item of the left type, with <c>xs:untypedAtomic</c> already cast.</param>
/// <param name="right">The right operand: one atomic item of the right type.</param>
/// <param name="context">The dynamic context of the evaluation.</param>
/// <returns>The result.</returns>
/// <remarks>An implementation is stateless, because one table serves every evaluation. It raises <c>XPathException</c> for a dynamic error.</remarks>
internal delegate XdmItem ContextBinaryOperatorImplementation(
    in XdmItem left,
    in XdmItem right,
    XPathEvaluationContext context
);
