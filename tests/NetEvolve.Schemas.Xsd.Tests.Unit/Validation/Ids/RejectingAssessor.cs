namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>A datatype fake that rejects every value that starts with a digit.</summary>
internal sealed class RejectingAssessor : ISimpleValueAssessor
{
    public ValidationResult Assess(IXsdComponent simpleType, ReadOnlySpan<char> lexical, out int memberIndex)
    {
        memberIndex = -1;
        return lexical.Length > 0 && char.IsDigit(lexical[0])
            ? ValidationResult.Invalid(DatatypeError.InvalidLexicalForm, FacetKind.None, 0)
            : ValidationResult.Valid;
    }
}
