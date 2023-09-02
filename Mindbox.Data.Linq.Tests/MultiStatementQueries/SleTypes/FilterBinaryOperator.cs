namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

enum FilterBinaryOperator
{
    /// <summary>
    /// Equal. Only between 2 chains.
    /// </summary>
    ChainsEqual,
    /// <summary>
    /// Not equal. Only between 2 chains.
    /// </summary>
    ChainsNotEqual,
    /// <summary>
    /// Any other operator between 2 chains. For example: +, -, / and so on.
    /// </summary>
    ChainOther,
    /// <summary>
    /// And. Either left or right or both are FilterBinarySle
    /// </summary>
    FilterBinaryAnd,
    /// <summary>
    /// Or. Either left or right or both are FilterBinarySle
    /// </summary>
    FilterBinaryOr,
}
