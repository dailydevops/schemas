namespace NetEvolve.Schemas.XPath.Types;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;

/// <summary>
/// The signature of a function item: the types of its parameters and of its result (XPath 3.0, 2.5.5.2). Immutable.
/// </summary>
/// <remarks>
/// Limitation: a built-in function declares no result type, so its signature gives <c>item()*</c> as the result; a function test that
/// names a narrower result type therefore does not match a built-in function.
/// </remarks>
internal sealed class FunctionSignature
{
    private readonly SequenceTypeInfo[] _parameters;

    /// <summary>Initializes a new instance of the <see cref="FunctionSignature"/> class.</summary>
    /// <param name="parameters">The parameter types.</param>
    /// <param name="result">The result type.</param>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> or <paramref name="result"/> is <see langword="null"/>.</exception>
    public FunctionSignature(IReadOnlyList<SequenceTypeInfo> parameters, SequenceTypeInfo result)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(result);

        _parameters = new SequenceTypeInfo[parameters.Count];
        for (var i = 0; i < _parameters.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(parameters[i]);

            _parameters[i] = parameters[i];
        }

        Result = result;
    }

    /// <summary>Gets the number of parameters.</summary>
    public int Arity => _parameters.Length;

    /// <summary>Gets the result type.</summary>
    public SequenceTypeInfo Result { get; }

    /// <summary>Gets the type of a parameter.</summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The type.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the parameters.</exception>
    public SequenceTypeInfo Parameter(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _parameters.Length);

        return _parameters[index];
    }

    /// <summary>Creates the signature that keeps the parameters at the given positions.</summary>
    /// <param name="kept">The zero-based positions, in order.</param>
    /// <returns>The signature of a partial application: the kept parameters and the same result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="kept"/> is <see langword="null"/>.</exception>
    public FunctionSignature Keep(IReadOnlyList<int> kept)
    {
        ArgumentNullException.ThrowIfNull(kept);

        var parameters = new SequenceTypeInfo[kept.Count];
        for (var i = 0; i < parameters.Length; i++)
        {
            parameters[i] = Parameter(kept[i]);
        }

        return new FunctionSignature(parameters, Result);
    }

    /// <summary>
    /// Determines whether a function with this signature is an instance of a function test: the arity is the same, each parameter type
    /// of the test is a subtype of the parameter type here, and the result type here is a subtype of the result type of the test.
    /// </summary>
    /// <param name="test">The signature of the function test.</param>
    /// <returns><see langword="true"/> when the function matches the test.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    public bool IsSubtypeOf(FunctionSignature test)
    {
        ArgumentNullException.ThrowIfNull(test);

        if (_parameters.Length != test._parameters.Length)
        {
            return false;
        }

        for (var i = 0; i < _parameters.Length; i++)
        {
            if (!test._parameters[i].IsSubtypeOf(_parameters[i]))
            {
                return false;
            }
        }

        return Result.IsSubtypeOf(test.Result);
    }
}
