namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.ParserOnly;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// The engine of the parser-only runs: it parses the query and reports the syntax error, if any. Without an
/// evaluator a query that parses has no result, so it reports <see cref="EngineResult.NotBound"/>.
/// </summary>
internal sealed class ParserOnlyEngine : IQt3Engine
{
    public EngineResult Evaluate(Qt3Case testCase)
    {
        var query = testCase.QueryFile is null ? testCase.Query : File.ReadAllText(testCase.QueryFile);
        return XPathParser.TryParse(query.AsSpan(), out _, out var diagnostic)
            ? EngineResult.NotBound
            : EngineResult.Error(new QName(diagnostic.QName.Namespace, diagnostic.QName.LocalName));
    }
}
