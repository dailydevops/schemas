namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>rule</c> element, concrete or abstract.</summary>
public sealed class SchematronRule : SchematronNode
{
    internal SchematronRule(
        DocumentLocation location,
        string? id,
        string? context,
        string? visitEach,
        IReadOnlyList<string> flags,
        string? role,
        string? subject,
        bool isAbstract,
        SchematronTitle? title,
        IReadOnlyList<SchematronLet> lets,
        IReadOnlyList<SchematronNode> items,
        DocumentLocation? expandedFrom
    )
        : base(location)
    {
        Id = id;
        Context = context;
        VisitEach = visitEach;
        Flags = flags;
        Role = role;
        Subject = subject;
        IsAbstract = isAbstract;
        Title = title;
        Lets = lets;
        Items = items;
        ExpandedFrom = expandedFrom;
    }

    /// <summary>Gets the <c>id</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Id { get; }

    /// <summary>Gets the raw <c>context</c> expression, or <see langword="null"/> for an abstract rule.</summary>
    public string? Context { get; }

    /// <summary>Gets the raw <c>visit-each</c> expression, or <see langword="null"/> when absent.</summary>
    public string? VisitEach { get; }

    /// <summary>Gets the <c>flag</c> tokens.</summary>
    public IReadOnlyList<string> Flags { get; }

    /// <summary>Gets the <c>role</c> attribute, or <see langword="null"/> when absent.</summary>
    public string? Role { get; }

    /// <summary>Gets the raw <c>subject</c> expression, or <see langword="null"/> when absent.</summary>
    public string? Subject { get; }

    /// <summary>Gets a value indicating whether the rule is abstract.</summary>
    public bool IsAbstract { get; }

    /// <summary>Gets the title, or <see langword="null"/> when absent.</summary>
    public SchematronTitle? Title { get; }

    /// <summary>Gets the variables of the rule.</summary>
    public IReadOnlyList<SchematronLet> Lets { get; }

    /// <summary>Gets the assertions and paragraphs in document order.</summary>
    public IReadOnlyList<SchematronNode> Items { get; }

    /// <summary>Gets the location of the instantiation or <c>extends</c> site for expanded content, <see langword="null"/> for authored content.</summary>
    public DocumentLocation? ExpandedFrom { get; }
}
