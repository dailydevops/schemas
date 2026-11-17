namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

internal static class BuilderTestExtensions
{
    // The synchronous Build is the contract under test; async test methods reach it through this non-async helper.
    internal static ISchematronValidator BuildNow(this SchematronValidatorBuilder builder) => builder.Build();
}
