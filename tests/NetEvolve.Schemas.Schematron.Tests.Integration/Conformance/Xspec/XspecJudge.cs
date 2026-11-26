namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using System.Xml.Linq;

/// <summary>Checks an expectation against the SVRL output of a validation.</summary>
internal static class XspecJudge
{
    /// <summary>Checks one expectation.</summary>
    /// <param name="expectation">The expectation.</param>
    /// <param name="svrl">The SVRL output.</param>
    /// <param name="locator">The locator of the instance document.</param>
    /// <returns>The reason the expectation does not hold, or <see langword="null"/> when it holds.</returns>
    public static string? Check(XspecExpectation expectation, XDocument svrl, XspecNodeLocator locator) =>
        throw new NotImplementedException();
}
