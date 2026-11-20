namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>Equality of the values of two simple types in the value space, through the datatype contract.</summary>
internal sealed class ValueSpace
{
    /// <summary>Initializes a new instance of the <see cref="ValueSpace"/> class.</summary>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="version">The XSD version.</param>
    internal ValueSpace(ISimpleValueAssessor assessor, XsdVersion version)
    {
        Argument.ThrowIfNull(assessor);
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
    }

    /// <summary>Computes a hash that is the same for all values that are equal in the value space.</summary>
    /// <param name="type">The simple type of the value.</param>
    /// <param name="lexical">The lexical value.</param>
    /// <returns>The hash.</returns>
    internal int Hash(IXsdComponent type, ReadOnlySpan<char> lexical) => throw new NotSupportedException();

    /// <summary>Determines whether two values are equal in the value space.</summary>
    /// <param name="typeA">The simple type of the first value.</param>
    /// <param name="a">The first lexical value.</param>
    /// <param name="typeB">The simple type of the second value.</param>
    /// <param name="b">The second lexical value.</param>
    /// <returns><see langword="true"/> when the values are equal.</returns>
    internal bool AreEqual(IXsdComponent typeA, ReadOnlySpan<char> a, IXsdComponent typeB, ReadOnlySpan<char> b) =>
        throw new NotSupportedException();
}
