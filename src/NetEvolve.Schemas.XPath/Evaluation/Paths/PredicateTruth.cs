namespace NetEvolve.Schemas.XPath.Evaluation.Paths;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The truth value of a predicate (XPath 2.0, 3.2.2).</summary>
internal static class PredicateTruth
{
    /// <summary>Evaluates a predicate value for the item at a position.</summary>
    /// <param name="value">The value of the predicate expression.</param>
    /// <param name="position">The one-based position of the item in the sequence being filtered.</param>
    /// <returns>
    /// For a single numeric value, whether it equals the position; for every other value, the effective boolean value. A numeric value
    /// that is not an integer, and NaN, therefore never selects an item.
    /// </returns>
    /// <exception cref="XPathException">The value has no effective boolean value (FORG0006).</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="position"/> is below one.</exception>
    public static bool Test(in XdmSequence value, int position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(position, 1);

        if (value.Count == 1)
        {
            var item = value[0];
            if (item.TryGetInteger(out var integer))
            {
                return integer == position;
            }

            if (item.TryGetDouble(out var wide))
            {
                return wide == position;
            }

            if (item.TryGetFloat(out var narrow))
            {
                return narrow == position;
            }

            // The decimal family also holds integers beyond 64 bits; the comparison ignores the scale, so 1.0 equals position 1.
            if (item.TryGetDecimal(out var number))
            {
                return number.CompareTo(new XsdDecimal(position, 0)) == 0;
            }
        }

        return XPathAtomization.EffectiveBooleanValue(value);
    }
}
