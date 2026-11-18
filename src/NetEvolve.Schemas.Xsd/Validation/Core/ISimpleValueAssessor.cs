namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using NetEvolve.Schemas.Xsd.Contracts;
using DatatypeResult = NetEvolve.Schemas.Xml.Datatypes.Contracts.ValidationResult;

/// <summary>
/// The datatype contract of the streaming validator: checks a lexical value against a simple type component (design #146
/// DP-9). The validator maps a failure to the rule of the failed facet and never inspects a datatype itself.
/// </summary>
internal interface ISimpleValueAssessor
{
    /// <summary>Checks a lexical value.</summary>
    /// <param name="simpleType">The simple type component: a built-in type or a simple type definition.</param>
    /// <param name="lexical">The lexical value as it appears in the instance.</param>
    /// <param name="memberIndex">The index of the accepting union member, <c>-1</c> when the type is no union or nothing accepted.</param>
    /// <returns>The result of the datatype, with the failed facet when a facet failed.</returns>
    DatatypeResult Assess(IXsdComponent simpleType, ReadOnlySpan<char> lexical, out int memberIndex);
}
