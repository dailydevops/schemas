namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System.Threading;
using NetEvolve.Schemas.Formats;

/// <summary>The one synchronous core behind every input kind of the XSD validator (design #146 DP-11).</summary>
internal interface IValidationCore
{
    /// <summary>Validates one instance document.</summary>
    /// <param name="source">The document, as the base class of the validator hands it over.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The result.</returns>
    ValidationResult Validate(in DocumentSource source, CancellationToken cancellationToken);
}
