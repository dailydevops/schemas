namespace NetEvolve.Schemas.Tests.Architecture.XPath.Fixtures.Evaluation;

/// <summary>Violating fixture: a type of the package that depends on the Schematron package.</summary>
internal static class EvaluationUsesSchematronFixture
{
    public static object Create() => new Schematron.SchematronFixture();
}
