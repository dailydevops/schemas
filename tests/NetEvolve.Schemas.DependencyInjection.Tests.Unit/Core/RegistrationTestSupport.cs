namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Core;

using NetEvolve.Schemas.Testing.Fakes;

internal static class RegistrationTestSupport
{
    internal static SchemaCompilationException CompilationFailure() =>
        new([new ValidationIssue(new DiagnosticCode(1), IssueSeverity.Error, "compile failure", default)]);

    internal static FakeValidator Throw() => throw CompilationFailure();
}
