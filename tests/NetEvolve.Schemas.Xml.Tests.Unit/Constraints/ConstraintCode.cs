namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

/// <summary>A row of the error tables as the coverage matrix reads it.</summary>
/// <param name="Alias">The alias, for example <c>XML0005</c>.</param>
/// <param name="Kind">The family of the constraint the code stands for, or <see langword="null"/> for a code of a production, a limit, or an encoding.</param>
/// <param name="Name">The official name of the constraint, or the label of the code when it is no constraint.</param>
/// <param name="Section">The section of the constraint, or an empty string when the table has none.</param>
internal sealed record ConstraintCode(string Alias, ConstraintKind? Kind, string Name, string Section);
