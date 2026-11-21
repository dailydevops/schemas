namespace NetEvolve.Schemas.Tests.Architecture.Schematron.Fixtures.Isolation.Schematron;

/// <summary>Violating fixture: a type of the Schematron package that depends on a type of the Xsd package.</summary>
internal static class SchematronUsesXsdFixture
{
    public static object Create() => new Xsd.XsdFixture();
}
