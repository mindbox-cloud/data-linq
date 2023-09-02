namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

/// <summary>
/// Single statement from chained statements. 
/// Example
///     Chained statement: User.CustomerAction.CustomField
///     IChainPartSle instances: User, CustomerAction, CustomerField
/// </summary>
interface IChainPart
{
    ChainSle Chain { get; set; }
}