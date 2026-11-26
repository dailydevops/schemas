namespace NetEvolve.Schemas.Xsd.Components;

using System;
using NetEvolve.Schemas.Formats;

/// <summary>Finds the rules of the catalog that the component phase reports.</summary>
internal static class XsdRuleLookup
{
    internal static readonly XsdRule SrcResolve = Find("src-resolve");
    internal static readonly XsdRule SchPropsCorrect = Find("sch-props-correct");
    internal static readonly XsdRule StPropsCorrect = Find("st-props-correct");
    internal static readonly XsdRule CtPropsCorrect = Find("ct-props-correct");
    internal static readonly XsdRule MgPropsCorrect = Find("mg-props-correct");
    internal static readonly XsdRule AgPropsCorrect = Find("ag-props-correct");
    internal static readonly XsdRule SrcAttributeGroup = Find("src-attribute_group");
    internal static readonly XsdRule CosEquivDerivedOkRec = Find("cos-equiv-derived-ok-rec");
    internal static readonly XsdRule SrcElement = Find("src-element");
    internal static readonly XsdRule SrcModelGroupDefn = Find("src-model_group_defn");
    internal static readonly XsdRule SrcNotation = Find("src-notation");
    internal static readonly XsdRule SrcWildcard = Find("src-wildcard");
    internal static readonly XsdRule PPropsCorrect = Find("p-props-correct");
    internal static readonly XsdRule CosElementConsistent = Find("cos-element-consistent");

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

    /// <summary>Finds the rule with a code; a code missing from the catalog is a defect and throws.</summary>
    /// <param name="code">The code of the rule.</param>
    /// <returns>The rule.</returns>
    internal static XsdRule Find(DiagnosticCode code)
    {
        foreach (var rule in XsdRuleCatalog.All)
        {
            if (rule.Code == code)
            {
                return rule;
            }
        }

        throw new InvalidOperationException($"The rule catalog has no rule for the code {code}.");
    }
}
