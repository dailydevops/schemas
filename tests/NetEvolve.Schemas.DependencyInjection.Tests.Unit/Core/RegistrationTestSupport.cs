namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit.Core;

using System.Runtime.CompilerServices;
using NetEvolve.Schemas.Testing.Fakes;

internal static class RegistrationTestSupport
{
    // The SchemaCompilationException constructors throw until FND-09 lands, so an uninitialized instance stands in for a compile failure.
    internal static SchemaCompilationException CompilationFailure() =>
        (SchemaCompilationException)RuntimeHelpers.GetUninitializedObject(typeof(SchemaCompilationException));

    internal static FakeValidator Throw() => throw CompilationFailure();
}
