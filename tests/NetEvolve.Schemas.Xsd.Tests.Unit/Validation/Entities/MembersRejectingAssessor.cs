namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Entities;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>A datatype fake for unions: the anonymous union is valid, and every named member type rejects the value.</summary>
internal sealed class MembersRejectingAssessor : ISimpleValueAssessor
{
    public ValidationResult Assess(IXsdComponent simpleType, ReadOnlySpan<char> lexical, out int memberIndex)
    {
        memberIndex = -1;
        return simpleType.Name.LocalName.Length == 0
            ? ValidationResult.Valid
            : ValidationResult.Invalid(DatatypeError.InvalidLexicalForm, FacetKind.None, 0);
    }
}
