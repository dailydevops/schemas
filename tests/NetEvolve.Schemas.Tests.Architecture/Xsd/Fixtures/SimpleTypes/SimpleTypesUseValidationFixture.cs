namespace NetEvolve.Schemas.Tests.Architecture.Xsd.Fixtures.SimpleTypes;

/// <summary>Exempt fixture: simple type checks may use the runtime validator, so the rule must not report it.</summary>
internal static class SimpleTypesUseValidationFixture
{
    public static object Create() => new Validation.ValidatorFixture();
}
