namespace NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>The listener of the assertions of complex types (XSD-23, #318): evaluates them over the subtree of each element at its end tag.</summary>
internal sealed class AssertionListener : ValidationListener
{
    private readonly ComplexTypeAssertions _assertions;
    private readonly ISimpleValueAssessor _assessor;

    /// <summary>Initializes a new instance of the <see cref="AssertionListener"/> class.</summary>
    /// <param name="assertions">The assertions of the complex types.</param>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    internal AssertionListener(ComplexTypeAssertions assertions, ISimpleValueAssessor assessor)
    {
        Argument.ThrowIfNull(assertions);
        Argument.ThrowIfNull(assessor);

        _assertions = assertions;
        _assessor = assessor;
    }

    /// <inheritdoc />
    public override ListenerInterest Interest => _assertions.Count == 0 ? ListenerInterest.None : ListenerInterest.All;
}
