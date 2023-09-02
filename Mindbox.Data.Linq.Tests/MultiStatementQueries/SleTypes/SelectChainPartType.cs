namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

enum SelectChainPartType
{
    /// <summary>
    /// Select with single inner chain.
    /// </summary>
    Simple,
    /// <summary>
    /// Anonymous type, that has several chains inside
    /// </summary>
    Complex,
}
