namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>Collects the problems of one stage.</summary>
internal interface IProblemSink
{
    void Add(in SchematronProblem problem);
}
