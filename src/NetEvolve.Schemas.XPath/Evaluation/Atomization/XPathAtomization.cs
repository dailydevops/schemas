namespace NetEvolve.Schemas.XPath.Evaluation.Atomization;

using System;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

internal static class XPathAtomization
{
    public static XdmSequence Atomize(in XdmSequence sequence) => throw new NotSupportedException();

    public static XdmSequence Atomize(in XdmSequence sequence, IXdmTypeAnnotationSource source) =>
        throw new NotSupportedException();

    public static bool EffectiveBooleanValue(in XdmSequence sequence) => throw new NotSupportedException();
}
