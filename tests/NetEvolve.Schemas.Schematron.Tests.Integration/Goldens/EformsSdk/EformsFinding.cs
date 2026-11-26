namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.EformsSdk;

/// <summary>One failed assertion or successful report of an SVRL document, with the validation stage of its active pattern.</summary>
/// <param name="Pattern">The id of the active pattern the finding belongs to, or an empty string.</param>
/// <param name="Stage">The validation stage of the active pattern, or <c>0</c> when the pattern id names no stage.</param>
/// <param name="Id">The id of the assertion, or an empty string.</param>
/// <param name="Role">The role of the assertion, or an empty string.</param>
/// <param name="Location">The location of the finding, or an empty string.</param>
/// <param name="Text">The text of the finding with collapsed white space.</param>
internal sealed record EformsFinding(string Pattern, int Stage, string Id, string Role, string Location, string Text)
{
    /// <summary>Gets the key that identifies a finding in the comparison of fired rule ids and locations.</summary>
    public string Key => string.Join('\t', Id, Role, Location);
}
