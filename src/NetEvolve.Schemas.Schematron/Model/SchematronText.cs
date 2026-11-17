namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>Character data inside rich text.</summary>
public sealed class SchematronText : SchematronMessagePart
{
    internal SchematronText(DocumentLocation location, string text)
        : base(location) => Text = text;

    /// <summary>Gets the text as written.</summary>
    public string Text { get; }
}
