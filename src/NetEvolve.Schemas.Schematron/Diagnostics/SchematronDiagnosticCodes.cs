#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the code catalog (design #137 AP-23)

namespace NetEvolve.Schemas.Schematron;

/// <summary>
/// The closed catalog of Schematron diagnostic codes, NES3000 to NES3999. Blocks are by topic: 30xx schema structure and
/// selection, 31xx modules, documents, and expansion, 32xx declarations and static expressions, 33xx variables and dynamic
/// evaluation, 34xx assertion findings, 35xx report output. Each field name is the PascalCase form of the symbolic name
/// listed in its remarks, except where the remarks give the symbolic name explicitly.
/// </summary>
public static class SchematronDiagnosticCodes
{
    /// <summary>A contract stage is still a stub. Symbolic name <c>SCH-NOT-IMPLEMENTED</c>, stage stub.</summary>
    public static readonly DiagnosticCode NotImplemented = new(3001);

    /// <summary>The root is neither <c>schema</c> nor <c>library</c> in the Schematron namespace. <c>SCH-ROOT-INVALID</c>, stage load.</summary>
    public static readonly DiagnosticCode RootInvalid = new(3002);

    /// <summary>A Schematron element is in a place the grammar forbids. <c>SCH-ELEMENT-MISPLACED</c>, stage load.</summary>
    public static readonly DiagnosticCode ElementMisplaced = new(3003);

    /// <summary>An unknown element in the Schematron namespace. <c>SCH-ELEMENT-UNKNOWN</c>, stage load.</summary>
    public static readonly DiagnosticCode ElementUnknown = new(3004);

    /// <summary>A required attribute is absent. <c>SCH-ATTRIBUTE-MISSING</c>, stage load.</summary>
    public static readonly DiagnosticCode AttributeMissing = new(3005);

    /// <summary>An attribute value violates its datatype. <c>SCH-ATTRIBUTE-INVALID</c>, stage load.</summary>
    public static readonly DiagnosticCode AttributeInvalid = new(3006);

    /// <summary>A construct that release 1 rejects by name. <c>SCH-UNSUPPORTED</c>, stage load.</summary>
    public static readonly DiagnosticCode Unsupported = new(3007);

    /// <summary>A 2025-only construct without <c>schematronEdition="2025"</c>. <c>SCH-EDITION-MISMATCH</c>, stage load.</summary>
    public static readonly DiagnosticCode EditionMismatch = new(3008);

    /// <summary>The <c>schematronEdition</c> value is not known. <c>SCH-EDITION-UNKNOWN</c>, stage load.</summary>
    public static readonly DiagnosticCode EditionUnknown = new(3009);

    /// <summary>An unknown or unsupported <c>queryBinding</c>. <c>SCH-BINDING-UNSUPPORTED</c>, stage load.</summary>
    public static readonly DiagnosticCode BindingUnsupported = new(3010);

    /// <summary>A duplicate authored id. <c>SCH-ID-DUPLICATE</c>, stage resolve.</summary>
    public static readonly DiagnosticCode IdDuplicate = new(3011);

    /// <summary>An id reference that matches nothing. <c>SCH-IDREF-UNRESOLVED</c>, stage resolve.</summary>
    public static readonly DiagnosticCode IdrefUnresolved = new(3012);

    /// <summary>The requested phase matches no <c>phase</c>. <c>SCH-PHASE-UNKNOWN</c>, stage compile.</summary>
    public static readonly DiagnosticCode PhaseUnknown = new(3013);

    /// <summary>An <c>include</c> cycle. <c>SCH-INCLUDE-CYCLE</c>, stage resolve.</summary>
    public static readonly DiagnosticCode IncludeCycle = new(3100);

    /// <summary>A loaded module has the wrong root. <c>SCH-MODULE-INVALID</c>, stage resolve.</summary>
    public static readonly DiagnosticCode ModuleInvalid = new(3101);

    /// <summary><c>@documents</c> under the deny-all resolver. Symbolic name <c>SCH-DOC-DENIED</c>, stage load.</summary>
    public static readonly DiagnosticCode DocumentDenied = new(3102);

    /// <summary>An <c>is-a</c> or <c>extends</c> target is missing, not abstract, or cyclic. Symbolic name <c>SCH-ABSTRACT-INVALID</c>, stage expand.</summary>
    public static readonly DiagnosticCode AbstractTargetInvalid = new(3110);

    /// <summary>A parameter without a value. <c>SCH-PARAM-MISSING</c>, stages expand and compile.</summary>
    public static readonly DiagnosticCode ParamMissing = new(3111);

    /// <summary>A parameter the target does not declare. <c>SCH-PARAM-UNKNOWN</c>, stages expand and compile.</summary>
    public static readonly DiagnosticCode ParamUnknown = new(3112);

    /// <summary>Two <see langword="let"/>, or a <c>param</c> and a <see langword="let"/>, of one name at one level, which includes a hoisted pattern <see langword="let"/> (#615). <c>SCH-LET-DUPLICATE</c>, stage compile.</summary>
    public static readonly DiagnosticCode LetDuplicate = new(3200);

    /// <summary>A <see langword="let"/> value does not match its <see langword="as"/> type. <c>SCH-LET-TYPE</c>, stages build and evaluate.</summary>
    public static readonly DiagnosticCode LetType = new(3201);

    /// <summary>A static error in an expression. Symbolic name <c>SCH-EXPR-INVALID</c>, stage compile.</summary>
    public static readonly DiagnosticCode ExpressionInvalid = new(3202);

    /// <summary>A rule context outside the supported pattern subset. <c>SCH-PATTERN-UNSUPPORTED</c>, stage compile.</summary>
    public static readonly DiagnosticCode PatternUnsupported = new(3203);

    /// <summary>An XSLT element that is not registered. <c>SCH-XSLT-UNSUPPORTED</c>, stage compile.</summary>
    public static readonly DiagnosticCode XsltUnsupported = new(3204);

    /// <summary>A reference to a variable not in scope. Symbolic name <c>SCH-VAR-UNDEFINED</c>, stages compile and evaluate.</summary>
    public static readonly DiagnosticCode VariableUndefined = new(3300);

    /// <summary>A dynamic evaluation error. Symbolic name <c>SCH-EVAL-FAILED</c>, stage evaluate.</summary>
    public static readonly DiagnosticCode EvaluationFailed = new(3301);

    /// <summary>A failed <c>assert</c>. Symbolic name <c>SCH-ASSERT-FAILED</c>, stage evaluate.</summary>
    public static readonly DiagnosticCode AssertionFailed = new(3400);

    /// <summary>A successful <c>report</c>. <c>SCH-REPORT-FIRED</c>, stage evaluate.</summary>
    public static readonly DiagnosticCode ReportFired = new(3401);

    /// <summary>The SVRL pattern buffer exceeded its limit; an SVRL <c>error</c> element, no issue. <c>SCH-SVRL-BUFFER-LIMIT</c>, stage evaluate.</summary>
    public static readonly DiagnosticCode SvrlBufferLimit = new(3500);

    /// <summary>The run evaluated no pattern; an SVRL <c>error</c> element, no issue. <c>SCH-NO-ACTIVE-PATTERN</c>, stage evaluate.</summary>
    public static readonly DiagnosticCode NoActivePattern = new(3501);

    /// <summary>
    /// Evaluation stopped at the first dropped error after <c>MaxIssues</c>; reported only in the run summary and as an SVRL
    /// comment, never as an issue. <c>SCH-ISSUE-LIMIT-STOP</c>, stage evaluate.
    /// </summary>
    public static readonly DiagnosticCode IssueLimitStop = new(3502);

    /// <summary>
    /// An evaluation limit was breached, such as the depth of nested stylesheet function calls; the run ends with one error issue
    /// (design #150 EP-16). <c>SCH-EVAL-LIMIT-STOP</c>, stage evaluate.
    /// </summary>
    public static readonly DiagnosticCode EvaluationLimitStop = new(3503);
}
