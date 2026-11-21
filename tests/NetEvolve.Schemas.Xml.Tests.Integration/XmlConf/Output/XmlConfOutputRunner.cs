namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

using NetEvolve.Schemas.Conformance.Results;

internal static class XmlConfOutputRunner
{
    public const string NeedsDtdProcessing = "needs DTD processing";

    public static Task<IReadOnlyList<CaseResult>> RunAsync(
        XmlConfSelection selection,
        IReadOnlyDictionary<string, string> outputs,
        string suiteRoot,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();
}
