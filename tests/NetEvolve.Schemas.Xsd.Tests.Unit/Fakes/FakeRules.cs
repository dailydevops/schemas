namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System;

/// <summary>Looks rules up in the public catalog.</summary>
internal static class FakeRules
{
    public static XsdRule Find(DiagnosticCode code)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (rule.Code == code)
            {
                return rule;
            }
        }

        throw new InvalidOperationException($"No rule {code} in the catalog.");
    }
}
