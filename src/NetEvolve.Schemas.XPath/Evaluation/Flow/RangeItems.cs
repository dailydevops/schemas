namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using System.Collections;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The integers from <c>first</c> to <c>last</c>, produced one at a time: the source of the lazy sequence of a range expression.</summary>
/// <remarks>The source holds two numbers, so a range of a billion items costs the same as one of two; each enumeration starts again at the first integer.</remarks>
internal sealed class RangeItems : IEnumerable<XdmItem>
{
    private readonly long _first;
    private readonly long _last;

    /// <summary>Initializes a new instance of the <see cref="RangeItems"/> class.</summary>
    /// <param name="first">The first integer.</param>
    /// <param name="last">The last integer, not below <paramref name="first"/>.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="last"/> is below <paramref name="first"/>.</exception>
    public RangeItems(long first, long last)
    {
        Argument.ThrowIfLessThan(last, first);
        _first = first;
        _last = last;
    }

    /// <inheritdoc />
    public IEnumerator<XdmItem> GetEnumerator() => new Enumerator(_first, _last);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator : IEnumerator<XdmItem>
    {
        private readonly long _first;
        private readonly long _last;
        private long _current;
        private bool _started;
        private bool _finished;

        public Enumerator(long first, long last)
        {
            _first = first;
            _last = last;
        }

        public XdmItem Current => XdmItem.FromInteger(_current);

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_finished)
            {
                return false;
            }

            if (!_started)
            {
                _started = true;
                _current = _first;
                return true;
            }

            // The comparison comes before the increment, so a range that ends at long.MaxValue does not overflow.
            if (_current == _last)
            {
                _finished = true;
                return false;
            }

            _current++;
            return true;
        }

        public void Reset()
        {
            _started = false;
            _finished = false;
        }

        public void Dispose() { }
    }
}
