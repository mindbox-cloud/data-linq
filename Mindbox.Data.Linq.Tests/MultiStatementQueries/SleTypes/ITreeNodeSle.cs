namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;

/// <summary>
/// Tree like statement from tree statements. 
/// Example:
///     Tree statement: (USer.Id == 10) || (User.Name == "asdf") 
///     ITreePartSle parts:
///                                 (USer.Id == 10)     ||       (User.Name == "asdf")
///                             User.Id          10           User.Name          "asdf"
/// </summary>
interface ITreeNodeSle : ISimplifiedLinqExpression
{
    ISimplifiedLinqExpression ParentExpression { get; set; }
}
