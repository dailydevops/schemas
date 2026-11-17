namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The seam between the runner and the schema compiler and instance validator; a fake fills it until XSD-75.</summary>
internal interface IXstsValidator
{
    /// <summary>Compiles the schema documents of a schema test.</summary>
    /// <param name="xstsCase">The schema test.</param>
    /// <param name="cancellationToken">A token to cancel the compilation.</param>
    /// <returns>The observed outcome; <see cref="XstsOutcome.NotImplemented"/> when nothing was compiled.</returns>
    Task<XstsOutcome> CompileSchemaAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken);

    /// <summary>Validates the instance of an instance test against the schema documents of its group; they may be empty.</summary>
    /// <param name="xstsCase">The instance test.</param>
    /// <param name="cancellationToken">A token to cancel the validation.</param>
    /// <returns>The observed outcome; <see cref="XstsOutcome.NotImplemented"/> when nothing was validated.</returns>
    Task<XstsOutcome> ValidateInstanceAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken);
}
