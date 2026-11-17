namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>phase</c> element.</summary>
public sealed class SchematronPhase : SchematronNode
{
    internal SchematronPhase(
        DocumentLocation location,
        string id,
        string? from,
        string? when,
        IReadOnlyList<SchematronParagraph> paragraphs,
        IReadOnlyList<SchematronLet> lets,
        IReadOnlyList<SchematronActive> actives
    )
        : base(location)
    {
        Id = id;
        From = from;
        When = when;
        Paragraphs = paragraphs;
        Lets = lets;
        Actives = actives;
    }

    /// <summary>Gets the phase id.</summary>
    public string Id { get; }

    /// <summary>Gets the <see langword="from"/> attribute, or <see langword="null"/> when absent.</summary>
    public string? From { get; }

    /// <summary>Gets the <see langword="when"/> attribute, or <see langword="null"/> when absent.</summary>
    public string? When { get; }

    /// <summary>Gets the paragraphs.</summary>
    public IReadOnlyList<SchematronParagraph> Paragraphs { get; }

    /// <summary>Gets the variables of the phase.</summary>
    public IReadOnlyList<SchematronLet> Lets { get; }

    /// <summary>Gets the activated patterns.</summary>
    public IReadOnlyList<SchematronActive> Actives { get; }
}
