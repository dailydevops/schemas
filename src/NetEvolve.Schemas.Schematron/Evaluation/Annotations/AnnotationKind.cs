namespace NetEvolve.Schemas.Schematron.Evaluation.Annotations;

/// <summary>The attributes of an assertion, rule, or pattern that annotate a finding.</summary>
internal enum AnnotationKind : byte
{
    /// <summary>The <c>role</c> attribute: one string, inherited from the assertion, the rule, then the pattern or group.</summary>
    Role = 0,

    /// <summary>The <c>flag</c> attribute: a token list, inherited from the assertion, then the rule.</summary>
    Flag = 1,

    /// <summary>The <c>severity</c> attribute: one opaque token, set on the assertion only.</summary>
    Severity = 2,
}
