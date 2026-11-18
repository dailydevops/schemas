namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System.Collections.Generic;

/// <summary>
/// The recorded code table of design #137 AP-24, plus NES3502 from the decision recorded on #180 and NES3503 from design #150 EP-16 (added by #249, the first consumer). A rename, removal,
/// renumbering, or addition fails the catalog tests until this table is changed deliberately.
/// </summary>
internal static class RecordedCodes
{
    internal sealed record Row(string Field, int Number, string SymbolicName, string Topic, string Stage);

    private const string Structure = "schema structure and selection";
    private const string Modules = "modules, documents, and expansion";
    private const string Declarations = "declarations and static expressions";
    private const string Variables = "variables and dynamic evaluation";
    private const string Findings = "assertion findings";
    private const string Output = "report output";

    internal static IReadOnlyList<Row> Rows { get; } =
    [
        new("NotImplemented", 3001, "SCH-NOT-IMPLEMENTED", Structure, "stub"),
        new("RootInvalid", 3002, "SCH-ROOT-INVALID", Structure, "load"),
        new("ElementMisplaced", 3003, "SCH-ELEMENT-MISPLACED", Structure, "load"),
        new("ElementUnknown", 3004, "SCH-ELEMENT-UNKNOWN", Structure, "load"),
        new("AttributeMissing", 3005, "SCH-ATTRIBUTE-MISSING", Structure, "load"),
        new("AttributeInvalid", 3006, "SCH-ATTRIBUTE-INVALID", Structure, "load"),
        new("Unsupported", 3007, "SCH-UNSUPPORTED", Structure, "load"),
        new("EditionMismatch", 3008, "SCH-EDITION-MISMATCH", Structure, "load"),
        new("EditionUnknown", 3009, "SCH-EDITION-UNKNOWN", Structure, "load"),
        new("BindingUnsupported", 3010, "SCH-BINDING-UNSUPPORTED", Structure, "load"),
        new("IdDuplicate", 3011, "SCH-ID-DUPLICATE", Structure, "resolve"),
        new("IdrefUnresolved", 3012, "SCH-IDREF-UNRESOLVED", Structure, "resolve"),
        new("PhaseUnknown", 3013, "SCH-PHASE-UNKNOWN", Structure, "compile"),
        new("IncludeCycle", 3100, "SCH-INCLUDE-CYCLE", Modules, "resolve"),
        new("ModuleInvalid", 3101, "SCH-MODULE-INVALID", Modules, "resolve"),
        new("DocumentDenied", 3102, "SCH-DOC-DENIED", Modules, "load"),
        new("AbstractTargetInvalid", 3110, "SCH-ABSTRACT-INVALID", Modules, "expand"),
        new("ParamMissing", 3111, "SCH-PARAM-MISSING", Modules, "expand, compile"),
        new("ParamUnknown", 3112, "SCH-PARAM-UNKNOWN", Modules, "expand, compile"),
        new("LetDuplicate", 3200, "SCH-LET-DUPLICATE", Declarations, "compile"),
        new("LetType", 3201, "SCH-LET-TYPE", Declarations, "build, evaluate"),
        new("ExpressionInvalid", 3202, "SCH-EXPR-INVALID", Declarations, "compile"),
        new("PatternUnsupported", 3203, "SCH-PATTERN-UNSUPPORTED", Declarations, "compile"),
        new("XsltUnsupported", 3204, "SCH-XSLT-UNSUPPORTED", Declarations, "compile"),
        new("VariableUndefined", 3300, "SCH-VAR-UNDEFINED", Variables, "compile, evaluate"),
        new("EvaluationFailed", 3301, "SCH-EVAL-FAILED", Variables, "evaluate"),
        new("AssertionFailed", 3400, "SCH-ASSERT-FAILED", Findings, "evaluate"),
        new("ReportFired", 3401, "SCH-REPORT-FIRED", Findings, "evaluate"),
        new("SvrlBufferLimit", 3500, "SCH-SVRL-BUFFER-LIMIT", Output, "evaluate"),
        new("NoActivePattern", 3501, "SCH-NO-ACTIVE-PATTERN", Output, "evaluate"),
        new("IssueLimitStop", 3502, "SCH-ISSUE-LIMIT-STOP", Output, "evaluate"),
        new("EvaluationLimitStop", 3503, "SCH-EVAL-LIMIT-STOP", Output, "evaluate"),
    ];
}
