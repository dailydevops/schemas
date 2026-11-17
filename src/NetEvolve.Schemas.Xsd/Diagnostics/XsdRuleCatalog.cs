#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the rule catalog (design #146 DP-1)

namespace NetEvolve.Schemas.Xsd;

using System.Collections.Generic;

/// <summary>The complete catalog of Xsd rules, ordered by code.</summary>
public static class XsdRuleCatalog
{
    /// <summary>Gets every rule, ordered by <see cref="XsdRule.Code"/>.</summary>
    public static IReadOnlyList<XsdRule> All { get; } = Join();

    private static List<XsdRule> Join()
    {
        var all = new List<XsdRule>();
        all.AddRange(XsdRuleCodes.EngineRules);
        all.AddRange(XsdRuleCodes.SchemaRepresentationRules);
        all.AddRange(XsdRuleCodes.SchemaComponentRules);
        all.AddRange(XsdRuleCodes.ValidationRules);
        return all;
    }
}
