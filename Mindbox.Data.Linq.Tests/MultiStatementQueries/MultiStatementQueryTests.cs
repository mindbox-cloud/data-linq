using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq.Mapping;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snapshooter.MSTest;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

[TestClass]
public class MultiStatementQueryTests
{
    [TestMethod]
    public void Translate_NoFilter_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().AsQueryable().Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TopLevelFilterAgainstConstant_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c2 => c2.TempPasswordEmail == "123").Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_ChainedWhere_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c1 => c1.AreaId == 10).Where(c2 => c2.TempPasswordEmail == "123").Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TopLevelFilterAgainstVariable_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TopLevelFilterAgainstBooleanField_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.IsDeleted).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TopLevelFilterAgainstNotBooleanField_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => !c.IsDeleted).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TopLevelMultipleSimleFilters_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail && c.Id > 10 && !c.IsDeleted).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TopLevelMultipleSimleFiltersOnSeveralWhereBlocks_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail).Where(c => c.Id > 10).Where(c => c.Id > 10 && !c.IsDeleted).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableLinkedViaReferenceAssociation_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.Area.Name == "SomeArea").Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    //[TestMethod]
    //public void Translate_TableLinkedViaReferenceChainAssociation_Success()
    //{
    //    // Arrange
    //    using var contextAndConnection = new DataContextAndConnection();

    //    // Act
    //    var queryExpression = contextAndConnection.DataContext
    //        .GetTable<Customer>().Where(c => c.Area.SubArea.Name == "SomeSubArea").Expression;
    //    var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

    //    // Assert
    //    query.CommandText.MatchSnapshot();
    //}

    //[TestMethod]
    //public void Translate_TableLinkedViaReferenceChainAssociationAndFilterOnAllAssociations_Success()
    //{
    //    // Arrange
    //    using var contextAndConnection = new DataContextAndConnection();

    //    // Act
    //    var queryExpression = contextAndConnection.DataContext
    //        .GetTable<Customer>()
    //        .Where(c => c.Area.Name == "SomeArea")
    //        .Where(c => c.Area.SubArea.Name == "SomeSubArea").Expression;
    //    var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

    //    // Assert
    //    query.CommandText.MatchSnapshot();
    //}

    //[TestMethod]
    //public void Translate_TableJoinByDataFieldViaWhere_Success()
    //{
    //    // Arrange
    //    using var contextAndConnection = new DataContextAndConnection();

    //    // Act
    //    var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
    //    var queryExpression = contextAndConnection.DataContext
    //        .GetTable<Customer>()
    //        .Where(c => customerActions.Where(ca => ca.CustomerId == c.Id).Any(ca => ca.ActionTemplateId == 10))
    //        .Expression;
    //    var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

    //    // Assert
    //    query.CommandText.MatchSnapshot();
    //}

    //[TestMethod]
    //public void Translate_TableJoinByAssociationFieldViaWhere_Success()
    //{
    //    // Arrange
    //    using var contextAndConnection = new DataContextAndConnection();

    //    // Act
    //    var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
    //    var queryExpression = contextAndConnection.DataContext
    //        .GetTable<Customer>()
    //        .Where(c => customerActions.Where(ca => ca.ActionTemplateId == 10).Where(ca => ca.Customer == c).Any())
    //        .Expression;
    //    var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

    //    // Assert
    //    query.CommandText.MatchSnapshot();
    //}

    //[TestMethod]
    //public void Translate_TableJoinByAssociationFieldPlusDataViaWhere_Success()
    //{
    //    // Arrange
    //    using var contextAndConnection = new DataContextAndConnection();

    //    // Act
    //    var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
    //    var queryExpression = contextAndConnection.DataContext
    //        .GetTable<Customer>()
    //        .Where(c => customerActions.Where(ca => ca.ActionTemplateId == 10).Where(ca => ca.Customer.Id == c.Id).Any())
    //        .Expression;
    //    var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

    //    // Assert
    //    query.CommandText.MatchSnapshot();
    //}

    //[TestMethod]
    //public void Translate_TableJoinByAssociationFollowedBySelectMany_Success()
    //{
    //    // Arrange
    //    using var contextAndConnection = new DataContextAndConnection();

    //    // Act
    //    var orders = contextAndConnection.DataContext.GetTable<RetailOrder>();
    //    var queryExpression = contextAndConnection.DataContext
    //        .GetTable<Customer>()
    //        .Where(c =>
    //            orders.Where(o => o.CurrentCustomer == c)
    //               .SelectMany(o => o.History.Single(hi => hi.IsCurrentOtherwiseNull != null).Purchases)
    //               .Where(p => p.PriceForCustomerOfLine / p.Count != null && p.PriceForCustomerOfLine / p.Count >= 123)
    //               .Any()
    //        )
    //        .Expression;
    //    var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

    //    // Assert
    //    query.CommandText.MatchSnapshot();
    //}

    // Several neested joins
    // Querable join
    // Select with anonympus types
    // SelectMany

    // See sample for more cases

    /*
    [TestMethod]
    public void Translate_TableJoinReversedByAssociationFollowedBySelectMany_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var orders = contextAndConnection.DataContext.GetTable<RetailOrder>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Where(c =>
                orders.Where(o => o.CurrentCustomer == c)
                   .SelectMany(o => o.History.Single(hi => hi.IsCurrentOtherwiseNull != null).Purchases)
                   .Where(p => p.PriceForCustomerOfLine / p.Count != null && p.PriceForCustomerOfLine / p.Count >= 123)
                   .Any()
            )
            .Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();

        var visitorContext = new VisitorContext(new DbColumnTypeProvider());
        var visitor = new ChainExpressionVisitor(visitorContext);
        visitor.Visit(queryExpression);

        //Console.WriteLine();
        //Console.WriteLine("Query:");
        //Console.WriteLine(visitor.Query.Dump());
    }*/
}

/// <summary>
/// Marker base interface.
/// </summary>
interface ISimplifiedLinqExpression
{
}

/// <summary>
/// Single statement from chained statmenets. 
/// Exmaple
///     Chained statement: User.CustomerAction.CustomField
///     IChainPartSle instances: User, CustomerAction, CustomerField
/// </summary>
interface IChainPart
{
    IChainSle Chain { get; set; }
}

static class ChainPartSleExtensions
{
    public static IChainPart GetNext(this IChainPart chainPart)
    {
        for (int i = 0; i < chainPart.Chain.Items.Count; i++)
        {
            if (chainPart == chainPart.Chain.Items[i])
                return i == 0 ? null : chainPart.Chain.Items[i-1];
        }
        throw new InvalidOperationException();
    }

    public static IChainPart GetPrevious(this IChainPart chainPart)
    {
        for (int i = 0; i < chainPart.Chain.Items.Count; i++)
        {
            if (chainPart == chainPart.Chain.Items[i])
                return i + 1 >= chainPart.Chain.Items.Count ? null : chainPart.Chain.Items[i+1];
        }
        throw new InvalidOperationException();
    }
}

/// <summary>
/// Chain of chainparts.
/// </summary>
interface IChainSle : ITreeNodeSle
{
    List<IChainPart> Items { get; }
    bool IsNegated { get; set; }
}

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

/// <summary>
/// Chain with tree sle.
/// </summary>
interface IChainPartAndTreeNodeSle : IChainPart, ITreeNodeSle
{

}

/// <summary>
/// Source of rows.
/// Examples:
///    TableSle
///    SelectSle
///    AssociationSle 
///    
/// Note: SelectSle and AssociationSle are analogs.
/// </summary>
interface IRowSourceChainPart : IChainPart
{
}

class ChainSle : IChainSle
{
    public List<IChainPart> Items { get; } = new List<IChainPart>();

    public ISimplifiedLinqExpression ParentExpression { get; set; }

    public bool IsNegated { get; set; }
}

class TableChainPart : IRowSourceChainPart
{
    public string Name { get; private set; }

    public IChainSle Chain { get; set; }

    public TableChainPart(string name)
    {
        Name = name;
    }
}

class ReferenceRowSourceChainPart : IChainPart
{
    public IChainSle Chain { get; set; }
    public IChainPart ReferenceRowSource { get; set; }
}

class ColumnAccessChainPart : IChainPart
{
    public IChainSle Chain { get; set; }
    public string ColumnName { get; set; }
}

class FixedValueChainPart : IChainPart
{
    public IChainSle Chain { get; set; }
}

class AssociationChainPart : IRowSourceChainPart
{
    public string ColumnName { get; set; }
    public string NextTableName { get; set; }
    public string NextTableColumnName { get; set; }
    public IChainSle Chain { get; set; }
}

class FilterChainPart : IChainPartAndTreeNodeSle
{
    public IChainSle Chain { get; set; }
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public ISimplifiedLinqExpression InnerExpression { get; set; }
}

class FilterBinarySle : ITreeNodeSle
{
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public ISimplifiedLinqExpression LeftExpression { get; set; }
    public ISimplifiedLinqExpression RightExpression { get; set; }
    public FilterBinaryOperator Operator { get; set; }
}

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

class SelectChainPart : IRowSourceChainPart, IChainPartAndTreeNodeSle
{
    public IChainSle Chain { get; set; }
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public ISimplifiedLinqExpression InnerExpression { get; set; }
}

delegate void SetTreeChildDelegate(ISimplifiedLinqExpression parent, ISimplifiedLinqExpression child);

class VisitorContext
{
    public Dictionary<ParameterExpression, IChainPart> ParameterToSle { get; private set; } = new();
    public IChainSle Root { get; set; }
    public IDbColumnTypeProvider ColumnTypeProvider { get; private set; }

    public IChainSle CurrentChain { get; private set; }
    public ITreeNodeSle CurrentTreeSle { get; private set; }
    public SetTreeChildDelegate CurrentTreeSleSetChildFunc { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columnTypeProvider">Column provider.</param>
    public VisitorContext(IDbColumnTypeProvider columnTypeProvider)
    {
        ColumnTypeProvider = columnTypeProvider;
    }

    /// <summary>
    /// Adds chain sle.
    /// </summary>
    /// <param name="chainSle">Chain.</param>
    public void AddChainPart(IChainPart newChainPartSle)
    {
        if (newChainPartSle is ITreeNodeSle)
            throw new InvalidOperationException();

        if (CurrentChain == null)
        {
            CurrentChain = new ChainSle();
            CurrentTreeSleSetChildFunc?.Invoke(CurrentTreeSle, CurrentChain);
            CurrentChain.ParentExpression = CurrentTreeSle;
            if (Root == null)
                Root = CurrentChain;
        }
        else if (CurrentTreeSleSetChildFunc != null)
            throw new InvalidOperationException("CurrentTreeSleSetChildFunc can exist only when new chain is created. As it is impossible to set link to middle of a chain.");

        CurrentChain.Items.Add(newChainPartSle);
        newChainPartSle.Chain = CurrentChain;

        CurrentTreeSleSetChildFunc = null;
        CurrentTreeSle = null;
    }

    /// <summary>
    /// Adds tree node.
    /// </summary>
    /// <param name="newTreeNodeSle">New tree node sle.</param>
    /// <param name="childSetFunc">Child set func.</param>
    public void AddTreeNode(ITreeNodeSle newTreeNodeSle, SetTreeChildDelegate childSetFunc)
    {
        if (newTreeNodeSle is IChainPart)
            throw new InvalidOperationException();
        if (Root == null)
            throw new InvalidOperationException();

        CurrentChain = null;
        CurrentTreeSleSetChildFunc(CurrentTreeSle, newTreeNodeSle);
        newTreeNodeSle.ParentExpression = CurrentTreeSle;
        CurrentTreeSle = newTreeNodeSle;
        CurrentTreeSleSetChildFunc = childSetFunc;
    }

    /// <summary>
    /// Add chain with root tree sle.
    /// </summary>
    /// <param name="newChainWithTree">New chain sle.</param>
    /// <param name="childSetFunc">Child set func.</param>
    public void AddChainWithTreeRoot(IChainPartAndTreeNodeSle newChainWithTree, SetTreeChildDelegate childSetFunc)
    {
        if (Root == null)
            throw new InvalidOperationException();

        // Set link in chain
        if (CurrentChain == null)
            throw new InvalidOperationException("Tree root if usualy filter and selector which is part of chain. There is something wrong if no chain exits");
        CurrentChain.Items.Add(newChainWithTree);
        newChainWithTree.Chain = CurrentChain;

        // This tree is always root of tree, we expect to traverse it as next step
        CurrentChain = null;
        CurrentTreeSle = newChainWithTree;
        CurrentTreeSleSetChildFunc = childSetFunc;
    }

    /// <summary>
    /// Moves to existing chain sle.
    /// </summary>
    /// <param name="chain">Existing sle.</param>
    public void MoveToChainSle(IChainSle chain)
    {
        CurrentChain = chain;
        CurrentTreeSle = null;
        CurrentTreeSleSetChildFunc = null;
    }

    /// <summary>
    /// Moves to existing sle.
    /// </summary>
    /// <param name="existingSle">Existing sle.</param>
    /// <param name="childSetFunc">Child set funnc.</param>
    public void MoveToTreeSle(ITreeNodeSle existingSle, SetTreeChildDelegate childSetFunc)
    {
        if (existingSle is IChainPart)
            throw new InvalidOperationException();

        CurrentChain = null;
        CurrentTreeSle = existingSle;
        CurrentTreeSleSetChildFunc = childSetFunc;
    }
}

class ChainExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public ChainExpressionVisitor(VisitorContext context)
    {
        _visitorContext = context;
    }

    private Expression UnwrpaNode(Expression node)
    {
        // Removes all converters.
        while (true)
        {
            if (node is not UnaryExpression unaryExpression)
                return node;
            if (unaryExpression.NodeType != ExpressionType.Convert)
                return node;
            node = unaryExpression.Operand;
        }
    }

    private Expression UnwrapNot(Expression expression, out bool isNegated)
    {
        isNegated = false;
        if (expression.NodeType != ExpressionType.Not)
            return expression;
        var unary = expression as UnaryExpression;
        isNegated = true;
        return unary.Operand;
    }

    public Expression Visit(Expression node)
    {
        node = UnwrapNot(node, out var isNegated);
        var chainCalls = ExpressionOrderFixer.GetReorderedChainCall(node).ToArray();
        if (chainCalls.Length == 0)
            throw new InvalidOperationException();
        var tableName = ExpressionHelpers.GetTableName(chainCalls[0]);
        if (!string.IsNullOrEmpty(tableName))
            chainCalls = chainCalls.Skip(1).ToArray();
        else if (string.IsNullOrEmpty(tableName) && chainCalls.Length > 1)
        {
            tableName = ExpressionHelpers.GetTableName(chainCalls[1]);
            if (!string.IsNullOrEmpty(tableName))
                chainCalls  = chainCalls.Skip(2).ToArray();
        }

        if (string.IsNullOrEmpty(tableName) && UnwrpaNode(chainCalls[0]) is ConstantExpression) // plain constant or variable
        {
            _visitorContext.AddChainPart(new FixedValueChainPart());
            return node;
        }

        IChainPart lastRowSourceSle;
        if (!string.IsNullOrEmpty(tableName))
            lastRowSourceSle = new TableChainPart(tableName);
        else
        {
            // May be we are accessing table via parameter 
            if (chainCalls[0] is ParameterExpression parameterExpression && _visitorContext.ParameterToSle.TryGetValue(parameterExpression, out var parameterSle))
            {
                lastRowSourceSle = new ReferenceRowSourceChainPart() { ReferenceRowSource = parameterSle };
                chainCalls = chainCalls.Skip(1).ToArray();
            }
            else
                throw new InvalidOperationException();
        }

        _visitorContext.AddChainPart(lastRowSourceSle);
        _visitorContext.CurrentChain.IsNegated = isNegated;

        // Visit all chain parts
        foreach (var chainItemExpression in chainCalls)
        {
            if (chainItemExpression is MethodCallExpression chainCallExpression &&
                (chainCallExpression.Method.DeclaringType == typeof(Queryable) || chainCallExpression.Method.DeclaringType == typeof(Enumerable)))
            {
                if (new[] { "Where", "Any", "Single" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
                {
                    var filterParameter = ExtractParameterVaribleFromFilterExpression(chainCallExpression.Arguments[1]);
                    _visitorContext.ParameterToSle.Add(filterParameter, lastRowSourceSle);
                    var filter = ExtractFilterLambdaBody(chainCallExpression.Arguments[1]);
                    var filterVisitor = new FilterExpressionVisitor(_visitorContext);
                    filterVisitor.Visit(filter);
                    _visitorContext.MoveToChainSle(filterVisitor.FilterSle.Chain);
                }
                else if ((new[] { "Select" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2) ||
                    (new[] { "SelectMany" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2))
                {
                    var filterParameter = ExtractParameterVaribleFromSelectExpression(chainCallExpression.Arguments[1]);
                    _visitorContext.ParameterToSle.Add(filterParameter, lastRowSourceSle);
                    var selectSle = new SelectChainPart();
                    _visitorContext.AddChainWithTreeRoot(selectSle, (p, c) => ((SelectChainPart)p).InnerExpression = c);
                    lastRowSourceSle = selectSle;
                    new ChainExpressionVisitor(_visitorContext).Visit(ExtractSelectLambdaBody(chainCallExpression.Arguments[1]));
                    _visitorContext.MoveToChainSle(selectSle.Chain);
                    continue;
                }
                else if (new[] { "Any" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 1)
                    continue;
                else if (new[] { "Single" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 1)
                    continue;
                else
                    throw new NotSupportedException();
            }
            else if (chainItemExpression is MemberExpression memberExpression)
            {
                if (memberExpression.Member is PropertyInfo memberProperty)
                {
                    if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                        _visitorContext.AddChainPart(new ColumnAccessChainPart() { ColumnName = memberProperty.Name });
                    else if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                    {
                        var associationAttribute = memberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                        var nextTableName = GetMetaTypeFromAssociation(memberProperty.PropertyType).CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                        var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                        var otherTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
                        var associationSle = new AssociationChainPart() { ColumnName = currentTableField, NextTableName = nextTableName, NextTableColumnName = otherTableField };
                        lastRowSourceSle = associationSle;
                        _visitorContext.AddChainPart(associationSle);
                    }
                }
                else
                    throw new InvalidOperationException();
            }
            else
                throw new InvalidOperationException();
        }
        return node;
    }

    private Type GetMetaTypeFromAssociation(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        return type;
    }

    private ParameterExpression ExtractParameterVaribleFromFilterExpression(Expression filterExpression)
    {
        LambdaExpression lambda;
        if (filterExpression is UnaryExpression unary)
        {
            if (unary.NodeType != ExpressionType.Quote || unary.IsLifted || unary.IsLiftedToNull || unary.Method != null)
                throw new NotSupportedException();
            lambda = (LambdaExpression)unary.Operand;
        }
        else
            lambda = (LambdaExpression)filterExpression;
        if (lambda.ReturnType != typeof(bool) || lambda.TailCall || !string.IsNullOrEmpty(lambda.Name) || lambda.Parameters.Count != 1)
            throw new NotSupportedException();
        return lambda.Parameters[0];
    }

    private ParameterExpression ExtractParameterVaribleFromSelectExpression(Expression filterExpression)
    {
        var unary = (UnaryExpression)filterExpression;
        if (unary.NodeType != ExpressionType.Quote || unary.IsLifted || unary.IsLiftedToNull || unary.Method != null)
            throw new NotSupportedException();
        var lambda = (LambdaExpression)unary.Operand;
        if (lambda.TailCall || !string.IsNullOrEmpty(lambda.Name) || lambda.Parameters.Count != 1)
            throw new NotSupportedException();
        return lambda.Parameters[0];
    }

    private Expression ExtractFilterLambdaBody(Expression expression)
    {
        if (expression is UnaryExpression unary)
        {
            if (unary.Method != null)
                throw new InvalidOperationException();
            if (unary.IsLifted || unary.IsLiftedToNull)
                throw new InvalidOperationException();
            return ((LambdaExpression)unary.Operand).Body;
        }
        return ((LambdaExpression)expression).Body;
    }

    private Expression ExtractSelectLambdaBody(Expression expression)
        => ExtractFilterLambdaBody(expression);
}


class FilterExpressionVisitor : ExpressionVisitor
{
    //private Stack<DataSource> _dataSourceStack = new();
    //private Stack<(ParameterExpression Parameter, DataSource DataSource)> _variablesOnStack = new();
    //private Stack<string> _context = new();
    private readonly VisitorContext _visitorContext;
    //private ISimplifiedLinqExpression _currentExpression;
    //private BinarySide? _currentBinarySide;

    //public TableSle SimplifiedExpression { get; private set; }

    /// <summary>
    /// Filter sle.
    /// </summary>
    public FilterChainPart FilterSle { get; private set; }

    public FilterExpressionVisitor(VisitorContext context)
    {
        _visitorContext = context;
        FilterSle = new FilterChainPart();
        _visitorContext.AddChainWithTreeRoot(FilterSle, (p, c) => ((FilterChainPart)p).InnerExpression = c);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var binarySle = new FilterBinarySle();
        switch (node.NodeType)
        {
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Divide:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Multiply:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
                binarySle.Operator = FilterBinaryOperator.ChainOther;
                break;
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                binarySle.Operator = FilterBinaryOperator.FilterBinaryAnd;
                break;
            case ExpressionType.Equal:
                binarySle.Operator = FilterBinaryOperator.ChainsEqual;
                break;
            case ExpressionType.NotEqual:
                binarySle.Operator = FilterBinaryOperator.FilterBinaryOr;
                break;
            case ExpressionType.Or:
            case ExpressionType.OrElse:
                binarySle.Operator = FilterBinaryOperator.FilterBinaryOr;
                break;
            default:
                throw new NotSupportedException();
        }
        _visitorContext.AddTreeNode(binarySle, (p, c) => ((FilterBinarySle)p).LeftExpression = c);
        Visit(node.Left);

        _visitorContext.MoveToTreeSle(binarySle, (p, c) => ((FilterBinarySle)p).RightExpression = c);
        Visit(node.Right);
        return node;
    }

    [return: NotNullIfNotNull("node")]
    public override Expression Visit(Expression node)
    {
        if (node is BinaryExpression)
            return base.Visit(node);
        else
            return new ChainExpressionVisitor(_visitorContext).Visit(node);
        //var chainCalls = ExpressionOrderFixer.GetReorderedChainCall(node).ToArray();
        //if (chainCalls.Length == 0)
        //    return base.Visit(node);
        //var tableName = ExpressionHelpers.GetTableName(chainCalls[0]);
        //if (!string.IsNullOrEmpty(tableName))
        //    chainCalls = chainCalls.Skip(1).ToArray();
        //else if (string.IsNullOrEmpty(tableName) && chainCalls.Length > 1)
        //{
        //    tableName = ExpressionHelpers.GetTableName(chainCalls[1]);
        //    if (!string.IsNullOrEmpty(tableName))
        //        chainCalls  = chainCalls.Skip(2).ToArray();
        //}
        //if (string.IsNullOrEmpty(tableName))
        //    return base.Visit(node);
        //if (SimplifiedExpression != null)
        //    throw new NotSupportedException();
        //SimplifiedExpression = new TableSle(tableName);
        //_currentExpression = SimplifiedExpression;

        //foreach (var chainItemExpression in chainCalls)
        //{
        //    if (chainItemExpression is MethodCallExpression chainCallExpression &&
        //        (chainCallExpression.Method.DeclaringType == typeof(Queryable) || chainCallExpression.Method.DeclaringType == typeof(Enumerable)))
        //    {
        //        if (new[] { "Where", "Any" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
        //        {
        //            Visit(chainCallExpression.Arguments[1]);
        //        }
        //        else if (new[] { "Select" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
        //            continue;
        //        else if (new[] { "SelectMany" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
        //            continue;
        //        else if (new[] { "Any" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 1)
        //            continue;
        //    }
        //}
        //return node;
    }

    //protected override Expression VisitMethodCall(MethodCallExpression node)
    //{

    //}

    //protected override Expression VisitBinary(BinaryExpression node)
    //{
    //    var table = PeekTableFromDataSources().Table;
    //    var condition = ExtractJoinCondition(table, node);
    //    if (condition != null)
    //        table.AddJoinCondition(condition);
    //    return base.VisitBinary(node);
    //}



    //private TableDataSource PeekTableFromDataSources()
    //{
    //    foreach (var dataSource in _dataSourceStack.Reverse())
    //    {
    //        if (dataSource is TableDataSource tableDataSource)
    //            return tableDataSource;
    //    }
    //    throw new NotSupportedException();
    //}

    //private StackPusher<string> PushContext(string contextItem)
    //{
    //    var toReturn = new StackPusher<string>(_context, contextItem);
    //    PrintContext();
    //    return toReturn;
    //}

    //private void PrintContext()
    //{
    //    if (_context.Count == 0)
    //        Console.WriteLine("Context is empty");
    //    else
    //        Console.WriteLine(string.Join(" -> ", _context));
    //}


    //private JoinCondition ExtractJoinCondition(TableNode table, BinaryExpression filter)
    //{
    //    if (filter.NodeType != ExpressionType.Equal)
    //        return null;
    //    var leftPart = ExtractTableField(filter.Left);
    //    if (leftPart == null)
    //        return null;
    //    var rightPart = ExtractTableField(filter.Right);
    //    if (rightPart == null)
    //        return null;
    //    if (leftPart.Table != table && rightPart.Table != table)
    //        return null;
    //    if (rightPart.Table == table)
    //        (leftPart, rightPart) = (rightPart, leftPart);
    //    return new JoinCondition(leftPart.Field, rightPart.Table, rightPart.Field);
    //}

    //private TableAndField ExtractTableField(Expression expression)
    //{
    //    expression = Unwrap(expression);

    //    if (expression is MemberExpression memberExpression && memberExpression.Member is PropertyInfo memberProperty)
    //    {
    //        if (memberExpression.Expression is ParameterExpression memberParameterExpression)
    //        {
    //            var table = GetTableFromExpression(memberParameterExpression);
    //            if (table != null)
    //            {
    //                // Column access. Like User.Name
    //                if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
    //                    return new TableAndField(table, memberProperty.Name);
    //                // Association access
    //                if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
    //                {
    //                    var associationAttribute = memberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
    //                    var nextTableName = memberProperty.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
    //                        .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
    //                    var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
    //                    return new TableAndField(table, currentTableField);
    //                }
    //            }
    //        }
    //        if (memberExpression.Expression is MemberExpression innerMemberExpression && innerMemberExpression.Member is PropertyInfo innerMemberProperty)
    //        {
    //            if (innerMemberExpression.Expression is ParameterExpression innerMemberParameterExpression)
    //            {
    //                var table = _variablesOnStack.Where(v => v.Parameter == innerMemberParameterExpression).Select(v => v.Table).SingleOrDefault();
    //                if (table != null)
    //                {
    //                    if (innerMemberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
    //                    {
    //                        var associationAttribute = innerMemberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
    //                        var nextTableName = innerMemberProperty.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
    //                            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
    //                        var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
    //                        var nextTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
    //                        if (nextTableField == memberProperty.Name)
    //                            return new TableAndField(table, currentTableField);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    if (expression is ParameterExpression parameterExpression)
    //    {
    //        var table = _variablesOnStack.Where(v => v.Parameter == parameterExpression).Select(v => v.Table).SingleOrDefault();
    //        if (table != null)
    //            return new TableAndField(table, _columnTypeProvider.GetPKFields(table.TableName).Single());
    //    }

    //    return null;

    //    TableNode GetTableFromExpression(ParameterExpression parameterExpression)
    //    {
    //        // Pickundelying table bypassin SelectTavble
    //        var table = _variablesOnStack.Where(v => v.Parameter == memberParameterExpression).Select(v => v.Table).SingleOrDefault();
    //    }

    //    static Expression Unwrap(Expression expression)
    //    {
    //        if (expression.NodeType == ExpressionType.Convert)
    //        {
    //            var unaryExpression = (UnaryExpression)expression;
    //            if (unaryExpression.IsLifted || unaryExpression.IsLiftedToNull || unaryExpression.Method != null)
    //                throw new NotSupportedException();
    //            return unaryExpression.Operand;
    //        }
    //        return expression;
    //    }
    //}

    //private record TableAndField(TableNode Table, string Field);

}







//    class DataSource { }

//    class TableDataSource : DataSource
//    {
//        public TableNode Table { get; private set; }

//        public TableDataSource(TableNode table)
//        {
//            Table=table;
//        }
//    }

//    class SelectDataSource : DataSource
//    {
//        public Dictionary<PropertyInfo, Expression> Mapping { get; } = new Dictionary<PropertyInfo, Expression>();
//    }



//    class QueryExpressionVisitor : ExpressionVisitor
//    {
//        private Stack<DataSource> _dataSourceStack = new();
//        private Stack<(ParameterExpression Parameter, DataSource DataSource)> _variablesOnStack = new();
//        private Stack<string> _context = new();
//        private readonly IDbColumnTypeProvider _columnTypeProvider;

//        public MultiStatementQuery Query { get; private set; } = new MultiStatementQuery();

//        public QueryExpressionVisitor(IDbColumnTypeProvider columnTypeProvider)
//        {
//            _columnTypeProvider= columnTypeProvider;
//        }

//        protected override Expression VisitConstant(ConstantExpression node)
//        {
//            var tableName = ExpressionHelpers.GetTableName(node);
//            if (!string.IsNullOrEmpty(tableName))
//            {
//                throw new InvalidOperationException("All tables should be extracted from chain calls.");
//            }

//            return base.VisitConstant(node);
//        }

//        protected override Expression VisitMember(MemberExpression node)
//        {
//            if (node.Expression is ConstantExpression)
//            {
//                var memberConstantValue = Expression.Lambda(node).Compile().DynamicInvoke();
//                var memberTableName = ExpressionHelpers.GetTableNameFromObject(memberConstantValue);
//                if (!string.IsNullOrEmpty(memberTableName))
//                {
//                    Console.WriteLine($"Table: {memberTableName}");
//                }
//            }

//            return base.VisitMember(node);
//        }

//        protected override Expression VisitMethodCall(MethodCallExpression node)
//        {
//            var chainCalls = ExpressionOrderFixer.GetReorderedChainCall(node).ToArray();
//            var tableName = ExpressionHelpers.GetTableName(chainCalls[0]);
//            if (!string.IsNullOrEmpty(tableName))
//                chainCalls = chainCalls.Skip(1).ToArray();
//            else if (string.IsNullOrEmpty(tableName) && chainCalls.Length > 1)
//            {
//                tableName = ExpressionHelpers.GetTableName(chainCalls[1]);
//                if (!string.IsNullOrEmpty(tableName))
//                    chainCalls  = chainCalls.Skip(2).ToArray();
//            }
//            if (!string.IsNullOrEmpty(tableName))
//            {
//                using (_dataSourceStack.ScopePush(new TableDataSource(Query.AddTable(tableName))))
//                using (PushContext(tableName))
//                {
//                    foreach (var chainItemExpression in chainCalls)
//                    {
//                        if (chainItemExpression is MethodCallExpression chainCallExpression &&
//                            (chainCallExpression.Method.DeclaringType == typeof(Queryable) || chainCallExpression.Method.DeclaringType == typeof(Enumerable)))
//                        {
//                            if (new[] { "Where", "Any" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
//                            {
//                                using (PushContext(chainCallExpression.Method.Name))
//                                using (_variablesOnStack.ScopePush((ExtractParameterVaribleFromFilterExpression(chainCallExpression.Arguments[1]), PeekTableFromDataSources())))
//                                {
//                                    Visit(chainCallExpression.Arguments[1]);
//                                }
//                            }
//                            else if (new[] { "Select" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
//                                continue;
//                            else if (new[] { "SelectMany" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
//                                continue;
//                            else if (new[] { "Any" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 1)
//                                continue;
//                        }
//                    }
//                }
//                return node;
//            }
//            else
//                return base.VisitMethodCall(node);
//        }

//        protected override Expression VisitBinary(BinaryExpression node)
//        {
//            var table = PeekTableFromDataSources().Table;
//            var condition = ExtractJoinCondition(table, node);
//            if (condition != null)
//                table.AddJoinCondition(condition);
//            return base.VisitBinary(node);
//        }

//        private ParameterExpression ExtractParameterVaribleFromFilterExpression(Expression filterExpression)
//        {
//            var unary = (UnaryExpression)filterExpression;
//            if (unary.NodeType != ExpressionType.Quote || unary.IsLifted || unary.IsLiftedToNull || unary.Method != null)
//                throw new NotSupportedException();
//            var lambda = (LambdaExpression)unary.Operand;
//            if (lambda.ReturnType != typeof(bool) || lambda.TailCall || !string.IsNullOrEmpty(lambda.Name) || lambda.Parameters.Count != 1)
//                throw new NotSupportedException();
//            return lambda.Parameters[0];
//        }

//        private TableDataSource PeekTableFromDataSources()
//        {
//            foreach (var dataSource in _dataSourceStack.Reverse())
//            {
//                if (dataSource is TableDataSource tableDataSource)
//                    return tableDataSource;
//            }
//            throw new NotSupportedException();
//        }

//        private StackPusher<string> PushContext(string contextItem)
//        {
//            var toReturn = new StackPusher<string>(_context, contextItem);
//            PrintContext();
//            return toReturn;
//        }

//        private void PrintContext()
//        {
//            if (_context.Count == 0)
//                Console.WriteLine("Context is empty");
//            else
//                Console.WriteLine(string.Join(" -> ", _context));
//        }


//        private JoinCondition ExtractJoinCondition(TableNode table, BinaryExpression filter)
//        {
//            if (filter.NodeType != ExpressionType.Equal)
//                return null;
//            var leftPart = ExtractTableField(filter.Left);
//            if (leftPart == null)
//                return null;
//            var rightPart = ExtractTableField(filter.Right);
//            if (rightPart == null)
//                return null;
//            if (leftPart.Table != table && rightPart.Table != table)
//                return null;
//            if (rightPart.Table == table)
//                (leftPart, rightPart) = (rightPart, leftPart);
//            return new JoinCondition(leftPart.Field, rightPart.Table, rightPart.Field);
//        }

//        private TableAndField ExtractTableField(Expression expression)
//        {
//            expression = Unwrap(expression);

//            if (expression is MemberExpression memberExpression && memberExpression.Member is PropertyInfo memberProperty)
//            {
//                if (memberExpression.Expression is ParameterExpression memberParameterExpression)
//                {
//                    var table = GetTableFromExpression(memberParameterExpression);
//                    if (table != null)
//                    {
//                        // Column access. Like User.Name
//                        if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
//                            return new TableAndField(table, memberProperty.Name);
//                        // Association access
//                        if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
//                        {
//                            var associationAttribute = memberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
//                            var nextTableName = memberProperty.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
//                                .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
//                            var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
//                            return new TableAndField(table, currentTableField);
//                        }
//                    }
//                }
//                if (memberExpression.Expression is MemberExpression innerMemberExpression && innerMemberExpression.Member is PropertyInfo innerMemberProperty)
//                {
//                    if (innerMemberExpression.Expression is ParameterExpression innerMemberParameterExpression)
//                    {
//                        var table = _variablesOnStack.Where(v => v.Parameter == innerMemberParameterExpression).Select(v => v.Table).SingleOrDefault();
//                        if (table != null)
//                        {
//                            if (innerMemberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
//                            {
//                                var associationAttribute = innerMemberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
//                                var nextTableName = innerMemberProperty.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
//                                    .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
//                                var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
//                                var nextTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
//                                if (nextTableField == memberProperty.Name)
//                                    return new TableAndField(table, currentTableField);
//                            }
//                        }
//                    }
//                }
//            }

//            if (expression is ParameterExpression parameterExpression)
//            {
//                var table = _variablesOnStack.Where(v => v.Parameter == parameterExpression).Select(v => v.Table).SingleOrDefault();
//                if (table != null)
//                    return new TableAndField(table, _columnTypeProvider.GetPKFields(table.TableName).Single());
//            }

//            return null;

//            TableNode GetTableFromExpression(ParameterExpression parameterExpression)
//            {
//                // Pickundelying table bypassin SelectTavble
//                var table = _variablesOnStack.Where(v => v.Parameter == memberParameterExpression).Select(v => v.Table).SingleOrDefault();
//            }

//            static Expression Unwrap(Expression expression)
//            {
//                if (expression.NodeType == ExpressionType.Convert)
//                {
//                    var unaryExpression = (UnaryExpression)expression;
//                    if (unaryExpression.IsLifted || unaryExpression.IsLiftedToNull || unaryExpression.Method != null)
//                        throw new NotSupportedException();
//                    return unaryExpression.Operand;
//                }
//                return expression;
//            }
//        }

//        private record TableAndField(TableNode Table, string Field);

//    }
//}


//public static class StackExtensions
//{
//    public static StackPusher<T> ScopePush<T>(this Stack<T> stack, T item)
//    {
//        return new StackPusher<T>(stack, item);
//    }
//}
