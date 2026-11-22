namespace NetEvolve.Schemas.Schematron.Evaluation.Patterns;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Annotations;
using NetEvolve.Schemas.Schematron.Scopes;

/// <summary>An active pattern or group with its compiled rules in document order. Immutable and shared by every run.</summary>
internal sealed class CompiledRuleSet
{
    internal CompiledRuleSet(
        PatternScope scope,
        string id,
        string documents,
        string name,
        bool isGroup,
        CompiledRule[] rules,
        CompiledAnnotations? annotations = null
    )
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(rules);

        Scope = scope;
        Id = id;
        Documents = documents;
        Name = name;
        IsGroup = isGroup;
        Rules = rules;
        Annotations = annotations ?? CompiledAnnotations.None;
    }

    /// <summary>Gets the pattern with its variables and the scope its rule contexts compile in.</summary>
    internal PatternScope Scope { get; }

    /// <summary>Gets the author id, empty when none.</summary>
    internal string Id { get; }

    /// <summary>Gets the <c>documents</c> expression, empty when none.</summary>
    internal string Documents { get; }

    /// <summary>Gets the title text, empty when none.</summary>
    internal string Name { get; }

    /// <summary>Gets a value indicating whether every matching rule fires (a <see langword="group"/>) instead of the first one only.</summary>
    internal bool IsGroup { get; }

    internal CompiledRule[] Rules { get; }

    /// <summary>Gets the <c>role</c> of the pattern or group, before inheritance.</summary>
    internal CompiledAnnotations Annotations { get; }
}
