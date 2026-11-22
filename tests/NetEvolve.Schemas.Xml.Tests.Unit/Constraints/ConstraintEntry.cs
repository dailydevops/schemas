namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System.Collections.Generic;

/// <summary>One row of the coverage matrix: a constraint by its official name and the tests that prove it.</summary>
/// <remarks>
/// A reference is the full name of a unit test without the root namespace of this assembly, <c>Type.Method</c>, or
/// <c>xmlconf:</c> and the identifier of a case of the W3C XML Conformance Test Suite (<c>testset/ID</c>). An entry with a
/// <see cref="Gap"/> is a constraint that is not enforced yet; it lists the tests of the part that works (the step level) as well.
/// </remarks>
/// <param name="Kind">The family of the constraint.</param>
/// <param name="Name">The official name, as the specification labels the constraint.</param>
/// <param name="Section">The section that defines the constraint.</param>
/// <param name="Code">The alias of the error code that a violation reports (an entry of an error table).</param>
/// <param name="Positive">The tests with a conforming document, which the constraint must accept.</param>
/// <param name="Negative">The tests with a violating document, which the constraint must reject.</param>
/// <param name="Gap">The gap when the constraint is not enforced yet, otherwise <see langword="null"/>.</param>
/// <param name="Note">A limitation or a remark that belongs to the constraint, or an empty string.</param>
internal sealed record ConstraintEntry(
    ConstraintKind Kind,
    string Name,
    string Section,
    string Code,
    IReadOnlyList<string> Positive,
    IReadOnlyList<string> Negative,
    ConstraintGap? Gap = null,
    string Note = ""
)
{
    /// <summary>Gets the key of the entry, the family and the name: a name such as Entity Declared exists in two families.</summary>
    internal string Key => Kind + "/" + Name;
}
