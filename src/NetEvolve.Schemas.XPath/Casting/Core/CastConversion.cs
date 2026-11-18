namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The conversion of one row of the casting table: the atomic value of the source type to a value of the target type.</summary>
/// <param name="source">The value to cast, an atomic item of the source type of the row.</param>
/// <param name="result">The converted value, <see langword="default"/> unless the status is <see cref="CastStatus.Ok"/>.</param>
/// <returns>The status; a failure leaves the choice of the error to the caller.</returns>
internal delegate CastStatus CastConversion(in XdmItem source, out XdmItem result);
