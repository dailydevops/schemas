namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

internal sealed class FakeXstsValidator(Func<XstsSelectedCase, XstsOutcome> verdict) : IXstsValidator
{
    public List<string> Compiled { get; } = [];

    public List<string> Validated { get; } = [];

    public Task<XstsOutcome> CompileSchemaAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Compiled.Add(xstsCase.Id);
        return Task.FromResult(verdict(xstsCase));
    }

    public Task<XstsOutcome> ValidateInstanceAsync(XstsSelectedCase xstsCase, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validated.Add(xstsCase.Id);
        return Task.FromResult(verdict(xstsCase));
    }
}
