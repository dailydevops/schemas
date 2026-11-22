namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

/// <summary>The rule of the coverage matrix that a <see cref="ConstraintViolation"/> breaks.</summary>
internal enum ConstraintViolationKind
{
    /// <summary>A constraint code of an error table has no entry.</summary>
    MissingEntry = 0,

    /// <summary>Two entries have the same family and name.</summary>
    DuplicateEntry = 1,

    /// <summary>An entry names a code that no error table has.</summary>
    UnknownCode = 2,

    /// <summary>An entry disagrees with the error table about the family, the name, or the section of its code.</summary>
    Mismatch = 3,

    /// <summary>An entry that is not a gap has no positive test or case.</summary>
    MissingPositive = 4,

    /// <summary>An entry that is not a gap has no negative test or case.</summary>
    MissingNegative = 5,

    /// <summary>A mapped test does not exist.</summary>
    UnknownTest = 6,

    /// <summary>A mapped case of the conformance suite does not exist.</summary>
    UnknownXmlConfCase = 7,

    /// <summary>A reference is empty or has no usable form.</summary>
    InvalidReference = 8,

    /// <summary>A gap names no issue or no existing probe.</summary>
    InvalidGap = 9,
}
