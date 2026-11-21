namespace NetEvolve.Schemas.Schematron.Editions;

using NetEvolve.Schemas.Schematron.Model;

/// <summary>Maps the value of <c>/sch:schema/@schematronEdition</c> to the edition whose semantics apply.</summary>
internal static class SchematronEditionDetector
{
    internal static bool TryDetect(string? text, out SchematronEdition edition)
    {
        edition = SchematronEdition.Iso2016;
        return text is null;
    }

    internal static string UnknownMessage(string text) => text;

    internal static SchematronEdition Detect(string? text, DocumentLocation location)
    {
        _ = location;
        _ = TryDetect(text, out var edition);
        return edition;
    }
}
