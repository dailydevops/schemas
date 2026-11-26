namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

using NetEvolve.Schemas.Schematron;

/// <summary>The cases of the compile and evaluate stages: recursion, growth, and the size of the rules themselves.</summary>
internal static partial class HostileCorpus
{
    private static IEnumerable<HostileCase> Recursion()
    {
        yield return Make(
            "recursion-function",
            Schema(
                "<xsl:function name=\"f:r\"><xsl:param name=\"x\"/><xsl:sequence select=\"f:r($x + 1)\"/></xsl:function>"
                    + Rule("f:r(1) = 1")
            ),
            One,
            SchematronDiagnosticCodes.EvaluationLimitStop.Number
        );
        yield return Make(
            "recursion-function-mutual",
            Schema(
                "<xsl:function name=\"f:p\"><xsl:param name=\"x\"/><xsl:sequence select=\"f:q($x)\"/></xsl:function>"
                    + "<xsl:function name=\"f:q\"><xsl:param name=\"x\"/><xsl:sequence select=\"f:p($x)\"/></xsl:function>"
                    + Rule("f:p(1) = 1")
            ),
            One,
            SchematronDiagnosticCodes.EvaluationLimitStop.Number
        );
        yield return Make(
            "recursion-let-self",
            Schema(
                "<pattern><rule context=\"a\"><let name=\"v\" value=\"$v\"/><assert test=\"true()\">x</assert></rule></pattern>"
            ),
            One,
            SchematronDiagnosticCodes.VariableUndefined.Number
        );
        yield return Make(
            "recursion-extends-cycle",
            Schema(
                "<pattern><rule id=\"r1\" abstract=\"true\"><extends rule=\"r2\"/></rule>"
                    + "<rule id=\"r2\" abstract=\"true\"><extends rule=\"r1\"/></rule>"
                    + "<rule context=\"a\"><extends rule=\"r1\"/></rule></pattern>"
            ),
            One,
            SchematronDiagnosticCodes.AbstractTargetInvalid.Number
        );
        yield return Make(
            "recursion-abstract-pattern-self",
            Schema("<pattern id=\"p\" is-a=\"p\"/>"),
            One,
            SchematronDiagnosticCodes.AbstractTargetInvalid.Number
        );
    }

    private static IEnumerable<HostileCase> Growth()
    {
        yield return Make(
            "growth-sequence",
            Schema(Rule("count(1 to 1000000000000) gt 0")),
            One,
            SchematronDiagnosticCodes.EvaluationFailed.Number
        );
        yield return Make(
            "growth-string",
            Schema(Rule("string-length(string-join(for $i in 1 to 100000000 return 'xxxxxxxxxx', '')) gt 0")),
            One,
            SchematronDiagnosticCodes.EvaluationFailed.Number
        );
        yield return Make(
            "growth-string-doubling",
            Schema(
                "<xsl:function name=\"f:d\"><xsl:param name=\"s\"/><xsl:param name=\"n\"/>"
                    + "<xsl:sequence select=\"if ($n = 0) then $s else f:d(concat($s, $s), $n - 1)\"/></xsl:function>"
                    + Rule("string-length(f:d('xxxxxxxx', 60)) gt 0")
            ),
            One,
            SchematronDiagnosticCodes.EvaluationFailed.Number
        );
        yield return Make(
            "growth-key-huge-set",
            Schema("<xsl:key name=\"k\" match=\"b\" use=\"@id\"/>" + Rule("count(key('k', 'x')) = 0")),
            "<a>" + Repeat("<b id=\"x\"/>", 200_000) + "</a>",
            SchematronDiagnosticCodes.AssertionFailed.Number
        );
    }

    private static IEnumerable<HostileCase> Expressions()
    {
        yield return Make(
            "expression-long",
            Schema(Rule(Repeat("1 + ", 100_000) + "1 = 1")),
            One,
            SchematronDiagnosticCodes.ExpressionInvalid.Number
        );
        yield return Make(
            "expression-deep-parens",
            Schema(Rule(Repeat("(", 5_000) + "1" + Repeat(")", 5_000))),
            One,
            SchematronDiagnosticCodes.ExpressionInvalid.Number
        );
        yield return Make(
            "expression-deep-predicates",
            Schema(Rule(Repeat("b[", 5_000) + "true()" + Repeat("]", 5_000))),
            One,
            SchematronDiagnosticCodes.ExpressionInvalid.Number
        );
        yield return Make(
            "rules-many",
            Schema(
                "<pattern>"
                    + string.Concat(
                        Enumerable
                            .Range(0, 20_000)
                            .Select(static i =>
                                $"<rule context=\"r{i}\"><assert test=\"{(i == 19_999 ? "false()" : "true()")}\">x</assert></rule>"
                            )
                    )
                    + "</pattern>"
            ),
            "<r19999/>",
            SchematronDiagnosticCodes.AssertionFailed.Number
        );
        yield return Make(
            "patterns-many",
            Schema(
                string.Concat(
                    Enumerable
                        .Range(0, 5_000)
                        .Select(static i =>
                            $"<pattern><rule context=\"r{i}\"><assert test=\"{(i == 4_999 ? "false()" : "true()")}\">x</assert></rule></pattern>"
                        )
                )
            ),
            "<r4999/>",
            SchematronDiagnosticCodes.AssertionFailed.Number
        );
    }
}
