namespace NetEvolve.Schemas.Xsd.Components;

using System;

/// <summary>Finds the rules of the catalog that the component phase reports.</summary>
internal static class XsdRuleLookup
{
    internal static readonly XsdRule SrcResolve = Find("src-resolve");
    internal static readonly XsdRule SchPropsCorrect = Find("sch-props-correct");
    internal static readonly XsdRule StPropsCorrect = Find("st-props-correct");
    internal static readonly XsdRule CtPropsCorrect = Find("ct-props-correct");
    internal static readonly XsdRule MgPropsCorrect = Find("mg-props-correct");
    internal static readonly XsdRule AgPropsCorrect = Find("ag-props-correct");
    internal static readonly XsdRule CosEquivDerivedOkRec = Find("cos-equiv-derived-ok-rec");

    private static XsdRule Find(string id)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (string.Equals(rule.Id, id, StringComparison.Ordinal))
            {
                return rule;
            }
        }

        throw new InvalidOperationException("The rule catalog has no rule '" + id + "'.");
    }
}
