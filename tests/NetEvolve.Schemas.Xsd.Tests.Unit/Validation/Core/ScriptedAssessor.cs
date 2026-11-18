namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// A datatype fake: the simple type is chosen by the local name of its component. The name int takes digits, short at most
/// three characters, letters letters only, and anything else every value.
/// </summary>
internal sealed class ScriptedAssessor : ISimpleValueAssessor
{
    public int Calls { get; private set; }

    public ValidationResult Assess(IXsdComponent simpleType, ReadOnlySpan<char> lexical, out int memberIndex)
    {
        Calls++;
        memberIndex = -1;
        switch (simpleType.Name.LocalName)
        {
            case "int":
                return AllDigits(lexical) ? ValidationResult.Valid : Lexical(0);
            case "short":
                return lexical.Length <= 3
                    ? ValidationResult.Valid
                    : ValidationResult.Invalid(DatatypeError.FacetViolation, FacetKind.MaxLength, 3);
            case "letters":
                return AllLetters(lexical)
                    ? ValidationResult.Valid
                    : ValidationResult.Invalid(DatatypeError.FacetViolation, FacetKind.Pattern, 0);
            case "union":
                memberIndex = lexical.Length == 0 ? -1 : 1;
                return ValidationResult.Valid;
            default:
                return ValidationResult.Valid;
        }
    }

    private static ValidationResult Lexical(int offset) =>
        ValidationResult.Invalid(DatatypeError.InvalidLexicalForm, FacetKind.None, offset);

    private static bool AllDigits(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var c in text)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool AllLetters(ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            if (!char.IsLetter(c))
            {
                return false;
            }
        }

        return true;
    }
}
