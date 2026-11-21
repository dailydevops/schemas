namespace NetEvolve.Schemas.Tests.Architecture.Xsd.Fixtures.Elements;

/// <summary>Violating fixture: a schema component family that depends on the runtime validator.</summary>
internal static class ElementsUseValidationFixture
{
    public static object Create() => new Validation.ValidatorFixture();
}
