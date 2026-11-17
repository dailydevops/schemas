namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Operator of an additive or multiplicative expression.</summary>
internal enum ArithmeticOperator
{
    /// <summary>+</summary>
    Add = 0,

    /// <summary>-</summary>
    Subtract = 1,

    /// <summary>*</summary>
    Multiply = 2,

    /// <summary>div</summary>
    Divide = 3,

    /// <summary>idiv</summary>
    IntegerDivide = 4,

    /// <summary>mod</summary>
    Modulo = 5,
}
