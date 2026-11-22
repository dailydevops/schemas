namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>One case of the suite.</summary>
/// <param name="Id">The id: the case path relative to the suite root, for example <c>iso-reporting/svrl-diagnostic.xml</c>.</param>
/// <param name="TestSet">The test set: the directory of the case.</param>
/// <param name="Schemas">The serialized Schematron schemas; a case checks the expected outcome with each of them.</param>
/// <param name="Instance">The serialized instance, the document named <c>document.xml</c>.</param>
/// <param name="Documents">All documents of the case in source order, the instance included.</param>
/// <param name="Expect">The expected outcome.</param>
/// <param name="Phase">The phase to validate with, if any.</param>
/// <param name="Assertions">
/// The XPath assertions on the SVRL output. The runner compares the outcome and the observation of the validator;
/// the validator of the Engine folder evaluates the assertions on the SVRL it produced.
/// </param>
/// <param name="Bindings">The ids of the query bindings whose suites list the case.</param>
internal sealed record SchematronCase(
    string Id,
    string TestSet,
    IReadOnlyList<string> Schemas,
    string Instance,
    IReadOnlyList<SchematronDocument> Documents,
    SchematronOutcome Expect,
    string? Phase,
    IReadOnlyList<string> Assertions,
    IReadOnlyList<string> Bindings
);
