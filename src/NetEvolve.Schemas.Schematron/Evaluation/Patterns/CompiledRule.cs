namespace NetEvolve.Schemas.Schematron.Evaluation.Patterns;

using NetEvolve.Schemas.Schematron.Evaluation.Annotations;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>A rule compiled once: its context in the scope of its pattern and its assertions in the scope of the rule. Immutable and shared by every run.</summary>
internal sealed class CompiledRule
{
    internal CompiledRule(
        RuleScope scope,
        CompiledPattern context,
        string id,
        string contextText,
        CompiledAssertion[] assertions,
        CompiledAnnotations? annotations = null
    )
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(contextText);
        ArgumentNullException.ThrowIfNull(assertions);

        Scope = scope;
        Context = context;
        Id = id;
        ContextText = contextText;
        Assertions = assertions;
        Annotations = annotations ?? CompiledAnnotations.None;
    }

    /// <summary>Gets the rule with its variables and the scope its assertions compile in.</summary>
    internal RuleScope Scope { get; }

    /// <summary>Gets the compiled <c>context</c>.</summary>
    internal CompiledPattern Context { get; }

    /// <summary>Gets the author id of the rule, empty when it has none.</summary>
    internal string Id { get; }

    /// <summary>Gets the raw <c>context</c> expression.</summary>
    internal string ContextText { get; }

    /// <summary>Gets the assertions in document order.</summary>
    internal CompiledAssertion[] Assertions { get; }

    /// <summary>Gets the <c>role</c> and <c>flag</c> of the rule, before inheritance.</summary>
    internal CompiledAnnotations Annotations { get; }
}
