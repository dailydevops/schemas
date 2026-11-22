namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

/// <summary>The family of a constraint of the specifications.</summary>
internal enum ConstraintKind
{
    /// <summary>A well-formedness constraint of XML 1.0 (Fifth Edition).</summary>
    WellFormedness = 0,

    /// <summary>A validity constraint of XML 1.0 (Fifth Edition).</summary>
    Validity = 1,

    /// <summary>A namespace constraint of Namespaces in XML 1.0 (Third Edition).</summary>
    Namespace = 2,
}
