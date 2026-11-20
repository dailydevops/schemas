namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>The values of the fields of one selected node (the key-sequence of Structures section 3.11.4), with the hash of all of them.</summary>
internal sealed class KeySequence
{
    internal KeySequence(string[] lexicals, IXsdComponent[] types, int hash)
    {
        Lexicals = lexicals;
        Types = types;
        Hash = hash;
    }

    /// <summary>Gets the lexical value of every field.</summary>
    internal string[] Lexicals { get; }

    /// <summary>Gets the simple type of every value.</summary>
    internal IXsdComponent[] Types { get; }

    /// <summary>Gets the hash of the values in the value space.</summary>
    internal int Hash { get; }
}
