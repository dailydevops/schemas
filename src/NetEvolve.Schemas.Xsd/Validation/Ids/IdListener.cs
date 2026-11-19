namespace NetEvolve.Schemas.Xsd.Validation.Ids;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>The listener of the ID, IDREF, and NOTATION checks (XSD-14, #303).</summary>
internal sealed class IdListener : ValidationListener
{
    /// <summary>Initializes a new instance of the <see cref="IdListener"/> class.</summary>
    /// <param name="assessor">The datatype contract.</param>
    /// <param name="schema">The schema set that holds the notation declarations.</param>
    internal IdListener(ISimpleValueAssessor assessor, XsdSchemaSet schema)
    {
        Argument.ThrowIfNull(assessor);
        Argument.ThrowIfNull(schema);
    }

    /// <inheritdoc />
    public override ListenerInterest Interest => ListenerInterest.None;
}
