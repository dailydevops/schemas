#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>The start of a run. Text members are valid only during the callback.</summary>
public readonly ref struct SchematronRunInfo
{
    internal SchematronRunInfo(
        ReadOnlySpan<char> title,
        ReadOnlySpan<char> phase,
        ReadOnlySpan<char> schemaVersion,
        SchematronEdition edition,
        IReadOnlyList<SchematronNamespace> namespaces,
        ReadOnlySpan<char> instanceSystemId
    )
    {
        Title = title;
        Phase = phase;
        SchemaVersion = schemaVersion;
        Edition = edition;
        Namespaces = namespaces;
        InstanceSystemId = instanceSystemId;
    }

    /// <summary>Gets the schema title, empty when none.</summary>
    public ReadOnlySpan<char> Title { get; }

    /// <summary>Gets the effective phase id, empty for <c>#ALL</c>.</summary>
    public ReadOnlySpan<char> Phase { get; }

    /// <summary>Gets the schema version, empty when none.</summary>
    public ReadOnlySpan<char> SchemaVersion { get; }

    /// <summary>Gets the edition.</summary>
    public SchematronEdition Edition { get; }

    /// <summary>Gets the namespace prefixes declared by the schema.</summary>
    public IReadOnlyList<SchematronNamespace> Namespaces { get; }

    /// <summary>Gets the system identifier of the instance, empty when none.</summary>
    public ReadOnlySpan<char> InstanceSystemId { get; }
}
