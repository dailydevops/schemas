namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>The leg and runtime a differential run happens on.</summary>
/// <param name="TargetFramework">The short moniker of the test leg, for example <c>net10.0</c>.</param>
/// <param name="RuntimeVersion">The major and minor version of the running runtime, for example <c>10.0</c>.</param>
internal sealed record DifferentialEnvironment(string TargetFramework, string RuntimeVersion);
