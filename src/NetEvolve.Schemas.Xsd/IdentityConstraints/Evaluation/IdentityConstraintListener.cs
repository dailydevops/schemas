namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The listener of the identity constraints (XSD-15, #304; design #146 DP-13): <c>unique</c>, <c>key</c>, and <c>keyref</c> are
/// evaluated while the instance streams by.
/// </summary>
internal sealed class IdentityConstraintListener : ValidationListener
{
    private readonly ValueSpace _values;
    private readonly Func<IXsdComponent, IReadOnlyList<IdentityConstraintDefinition>?> _constraintsOf;

    /// <summary>Initializes a new instance of the <see cref="IdentityConstraintListener"/> class.</summary>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="version">The XSD version, which decides how values of the value space compare.</param>
    /// <param name="constraintsOf">Gives the identity constraints of an element declaration, <see langword="null"/> for none.</param>
    internal IdentityConstraintListener(
        ISimpleValueAssessor assessor,
        XsdVersion version,
        Func<IXsdComponent, IReadOnlyList<IdentityConstraintDefinition>?> constraintsOf
    )
    {
        Argument.ThrowIfNull(constraintsOf);

        _values = new ValueSpace(assessor, version);
        _constraintsOf = constraintsOf;
    }

    /// <inheritdoc />
    public override ListenerInterest Interest => ListenerInterest.All;
}
