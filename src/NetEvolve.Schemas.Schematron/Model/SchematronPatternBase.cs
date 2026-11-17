namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>Base of <c>pattern</c> and <see langword="group"/>, which share all members.</summary>
public abstract class SchematronPatternBase : SchematronNode
{
    private protected SchematronPatternBase(
        DocumentLocation location,
        string? id,
        bool isAbstract,
        string? isA,
        string? documents,
        string? role,
        SchematronTitle? title,
        IReadOnlyList<SchematronParagraph> paragraphs,
        IReadOnlyList<SchematronParameter> parameters,
        IReadOnlyList<SchematronLet> lets,
        IReadOnlyList<SchematronRule> rules,
        DocumentLocation? expandedFrom
    )
        : base(location)
    {
        Id = id;
        IsAbstract = isAbstract;
        IsA = isA;
        Documents = documents;
        Role = role;
        Title = title;
        Paragraphs = paragraphs;
        Parameters = parameters;
        Lets = lets;
        Rules = rules;
        ExpandedFrom = expandedFrom;
    }

    /// <summary>Gets the <c>id</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Id { get; }

    /// <summary>Gets a value indicating whether the pattern is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets the <c>is-a</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? IsA { get; }

    /// <summary>Gets a value indicating whether the element is a <see langword="group"/>.</summary>
    public bool IsGroup => this is SchematronGroup;

    /// <summary>Gets the raw <c>documents</c> expression, or <see langword="null"/> when absent.</summary>
    public string? Documents { get; }

    /// <summary>Gets the <c>role</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Role { get; }

    /// <summary>Gets the title, or <see langword="null"/> when absent.</summary>
    public SchematronTitle? Title { get; }

    /// <summary>Gets the paragraphs.</summary>
    public IReadOnlyList<SchematronParagraph> Paragraphs { get; }

    /// <summary>Gets the parameters of an abstract pattern.</summary>
    public IReadOnlyList<SchematronParameter> Parameters { get; }

    /// <summary>Gets the variables.</summary>
    public IReadOnlyList<SchematronLet> Lets { get; }

    /// <summary>Gets the rules in document order.</summary>
    public IReadOnlyList<SchematronRule> Rules { get; }

    /// <summary>Gets the location of the <c>is-a</c> instantiation site for expanded content, <see langword="null"/> for authored content.</summary>
    public DocumentLocation? ExpandedFrom { get; }
}
