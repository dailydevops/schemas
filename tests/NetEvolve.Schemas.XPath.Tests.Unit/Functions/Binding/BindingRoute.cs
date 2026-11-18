namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

/// <summary>The three ways a function is registered.</summary>
public enum BindingRoute
{
    /// <summary>Through the catalog of built-in functions.</summary>
    BuiltIn,

    /// <summary>Through a host library of the static context.</summary>
    Host,

    /// <summary>Through the layer of user-defined functions.</summary>
    User,
}
