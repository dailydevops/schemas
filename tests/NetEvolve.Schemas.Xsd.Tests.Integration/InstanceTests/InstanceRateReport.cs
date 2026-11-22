namespace NetEvolve.Schemas.Xsd.Tests.Integration.InstanceTests;

using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The label of the row that the instance tests of XSD 1.0 publish in the job summary, beside the row of the whole suite and the row of the schema tests.</summary>
internal static class InstanceRateReport
{
    /// <summary>The package label of the row.</summary>
    public const string Package = XstsRateReport.Package + " XSTS 1.0 instance tests";
}
