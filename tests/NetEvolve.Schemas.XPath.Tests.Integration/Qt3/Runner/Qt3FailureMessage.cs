namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using System.Text;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>Formats the failure message of a test set: the first twenty ids plus the total.</summary>
internal static class Qt3FailureMessage
{
    private const int MaxListed = 20;

    public static string Format(TestSetReport report)
    {
        var builder = new StringBuilder();
        Append(builder, report.TestSet, "regression(s) (fail, not in the baseline)", report.Regressions);
        Append(builder, report.TestSet, "unexpected pass(es) (pass, in the baseline)", report.UnexpectedPasses);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string testSet, string label, IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        _ = builder.Append(testSet).Append(": ").Append(ids.Count).Append(' ').Append(label).Append(": ");
        for (var i = 0; i < Math.Min(ids.Count, MaxListed); i++)
        {
            _ = builder.Append(i == 0 ? "" : ", ").Append(ids[i]);
        }
        if (ids.Count > MaxListed)
        {
            _ = builder.Append(", ... and ").Append(ids.Count - MaxListed).Append(" more");
        }
        _ = builder.Append('\n');
    }
}
