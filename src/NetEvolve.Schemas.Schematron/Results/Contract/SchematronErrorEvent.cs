#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

using System;

/// <summary>A run-level or pattern-scoped error, the <c>svrl:error</c> attributes. Text members are valid only during the callback.</summary>
public readonly ref struct SchematronErrorEvent
{
    internal SchematronErrorEvent(
        ReadOnlySpan<char> document,
        ReadOnlySpan<char> location,
        ReadOnlySpan<char> code,
        ReadOnlySpan<char> message,
        ValidationIssue? issue
    )
    {
        Document = document;
        Location = location;
        Code = code;
        Message = message;
        Issue = issue;
    }

    /// <summary>Gets the instance system identifier, empty when none.</summary>
    public ReadOnlySpan<char> Document { get; }

    /// <summary>Gets the location path of the node, or <c>/</c> when no node exists.</summary>
    public ReadOnlySpan<char> Location { get; }

    /// <summary>Gets the code text, <c>NES</c> plus four digits.</summary>
    public ReadOnlySpan<char> Code { get; }

    /// <summary>Gets the message.</summary>
    public ReadOnlySpan<char> Message { get; }

    /// <summary>Gets the mapped issue; <see langword="null"/> for errors that are not issues.</summary>
    public ValidationIssue? Issue { get; }
}
