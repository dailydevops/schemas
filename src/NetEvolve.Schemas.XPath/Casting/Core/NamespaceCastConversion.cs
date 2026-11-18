namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversion of a row whose lexical form depends on the namespaces in scope, such as the cast of a string to <c>xs:QName</c>: the atomic
/// value of the source type to a value of the target type.
/// </summary>
/// <param name="source">The value to cast, an atomic item of the source type of the row.</param>
/// <param name="namespaces">The namespaces that resolve the prefix of a lexical form; <see langword="null"/> when the language version does not let the operand be cast from its lexical form.</param>
/// <param name="result">The converted value, <see langword="default"/> unless the status is <see cref="CastStatus.Ok"/>.</param>
/// <returns>The status; a failure leaves the choice of the error to the caller.</returns>
internal delegate CastStatus NamespaceCastConversion(
    in XdmItem source,
    INamespaceContext? namespaces,
    out XdmItem result
);
