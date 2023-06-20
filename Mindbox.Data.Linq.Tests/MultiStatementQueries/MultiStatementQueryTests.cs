﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
            .GetTable<Customer>().Where(c2 => c2.TempPasswordEmail == "123").Expression;
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

    [TestMethod]
    public void Translate_TableLinkedViaReferenceChainAssociation_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.Area.SubArea.Name == "SomeSubArea").Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableLinkedViaReferenceChainAssociationAndFilterOnAllAssociations_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Where(c => c.Area.Name == "SomeArea")
            .Where(c => c.Area.SubArea.Name == "SomeSubArea").Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableJoinByDataFieldViaWhere_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Where(c => customerActions.Where(ca => ca.CustomerId == c.Id).Any(ca => ca.ActionTemplateId == 10))
            .Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableJoinByAssociationFieldViaWhere_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Where(c => customerActions.Where(ca => ca.ActionTemplateId == 10).Where(ca => ca.Customer == c).Any())
            .Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableJoinByAssociationFieldPlusDataViaWhere_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Where(c => customerActions.Where(ca => ca.ActionTemplateId == 10).Where(ca => ca.Customer.Id == c.Id).Any())
            .Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

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
    }

    // Several neested joins
    // Querable join
    // Select with anonympus types
    // SelectMany

    // See sample for more cases


    [TestMethod]
    public void Translate_TableJoinReversedByAssociationFollowedBySelectManyWithExpressionVisitor_Success()
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



        var visitorContext = new VisitorContext(new DbColumnTypeProvider());
        var visitor = new ChainExpressionVisitor(visitorContext);
        visitor.Visit(queryExpression);

        //Console.WriteLine();
        //Console.WriteLine("Query:");
        //Console.WriteLine(visitor.Query.Dump());
    }
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
interface IChainPartSle : ISimplifiedLinqExpression
{
    IChainPartSle PreviousChainExpression { get; set; }
    IChainPartSle NextChainExpression { get; set; }
}

/// <summary>
/// Tree like statement from tree statements. 
/// Example:
///     Tree statement: (USer.Id == 10) || (User.Name == "asdf") 
///     ITreePartSle parts:
///                                 (USer.Id == 10)     ||       (User.Name == "asdf")
///                             User.Id          10           User.Name          "asdf"
/// </summary>
interface ITreePartSle : ISimplifiedLinqExpression
{
    ISimplifiedLinqExpression ParentExpression { get; set; }
}

/// <summary>
/// Chain with tree sle.
/// </summary>
interface IChainWithTreePartSle : IChainPartSle, ITreePartSle
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
interface IRowSourceSle : IChainPartSle
{
}

class TableSle : IRowSourceSle
{
    public string Name { get; private set; }

    public IChainPartSle PreviousChainExpression { get; set; }
    public IChainPartSle NextChainExpression { get; set; }

    public TableSle(string name)
    {
        Name = name;
    }
}

class ReferenceRowSourceSle : IChainPartSle
{
    public IChainPartSle ReferenceRowSource { get; set; }
    public IChainPartSle PreviousChainExpression { get; set; }
    public IChainPartSle NextChainExpression { get; set; }
}

class ColumnAccessSle : IChainPartSle
{
    public string ColumnName { get; set; }
    public IChainPartSle PreviousChainExpression { get; set; }
    public IChainPartSle NextChainExpression { get; set; }
}

class FixedValueSle : IChainPartSle
{
    public IChainPartSle PreviousChainExpression { get; set; }
    public IChainPartSle NextChainExpression { get; set; }
}

class AssociationSle : IRowSourceSle
{
    public string ColumnName { get; set; }
    public string NextTableName { get; set; }
    public string NextTableColumnName { get; set; }
    public IChainPartSle PreviousChainExpression { get; set; }
    public IChainPartSle NextChainExpression { get; set; }
}

class FilterSle : IChainWithTreePartSle
{
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public IChainPartSle PreviousChainExpression { get; set; }
    public ISimplifiedLinqExpression InnerExpression { get; set; }
    public IChainPartSle NextChainExpression { get; set; }
}

class FilterBinarySle : ITreePartSle
{
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public ISimplifiedLinqExpression LeftExpression { get; set; }
    public ISimplifiedLinqExpression RightExpression { get; set; }
}

class SelectSle : IRowSourceSle, IChainWithTreePartSle
{
    public IChainPartSle PreviousChainExpression { get; set; }
    public IChainPartSle NextChainExpression { get; set; }
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public ISimplifiedLinqExpression InnerExpression { get; set; }

}

delegate void SetChildDelegate(ISimplifiedLinqExpression parent, ISimplifiedLinqExpression child);

class VisitorContext
{
    public Dictionary<ParameterExpression, IChainPartSle> ParameterToSle { get; private set; } = new();
    public ISimplifiedLinqExpression Root { get; set; }
    public IDbColumnTypeProvider ColumnTypeProvider { get; private set; }

    public IChainPartSle CurrentChainSle { get; private set; }
    public ITreePartSle CurrentTreeSle { get; private set; }
    public SetChildDelegate CurrentTreeSleSetChildFunc { get; private set; }

    public VisitorContext(IDbColumnTypeProvider columnTypeProvider)
    {
        ColumnTypeProvider = columnTypeProvider;
    }

    /// <summary>
    /// Adds chain sle.
    /// </summary>
    /// <param name="chainSle">Chain.</param>
    public void AddChain(IChainPartSle newChainSle)
    {
        if (newChainSle is ITreePartSle)
            throw new InvalidOperationException();
        if (Root == null)
            Root = newChainSle;

        if (CurrentChainSle != null)
        {
            CurrentChainSle.NextChainExpression = newChainSle;
            newChainSle.PreviousChainExpression = CurrentChainSle;
        }


        // Maintain linking from parent-> child for trees
        if (CurrentTreeSleSetChildFunc != null)
            CurrentTreeSleSetChildFunc(CurrentTreeSle, newChainSle);

        CurrentTreeSleSetChildFunc = null;
        CurrentTreeSle = null;
        CurrentChainSle = newChainSle;
    }

    /// <summary>
    /// Adds tree node.
    /// </summary>
    /// <param name="newTreeNodeSle">New tree node sle.</param>
    /// <param name="childSetFunc">Child set func.</param>
    public void AddTreeNode(ITreePartSle newTreeNodeSle, SetChildDelegate childSetFunc)
    {
        if (newTreeNodeSle is IChainPartSle)
            throw new InvalidOperationException();
        if (Root == null)
            throw new InvalidOperationException();

        CurrentChainSle = null;
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
    public void AddChainWithRootTree(IChainWithTreePartSle newChainWithTree, SetChildDelegate childSetFunc)
    {
        if (Root == null)
            throw new InvalidOperationException();

        // Set link in chain
        if (CurrentChainSle != null)
        {
            CurrentChainSle.NextChainExpression = newChainWithTree;
            newChainWithTree.PreviousChainExpression = CurrentChainSle;
        }

        // This tree is always root of tree, we expect to traverse it as next step
        CurrentChainSle = null;
        CurrentTreeSle = newChainWithTree;
        CurrentTreeSleSetChildFunc = childSetFunc;
    }

    /// <summary>
    /// Moves to existing chain sle.
    /// </summary>
    /// <param name="existingSle">Existing sle.</param>
    public void MoveToChainSle(IChainPartSle existingSle)
    {
        CurrentChainSle = existingSle;
        CurrentTreeSle = null;
        CurrentTreeSleSetChildFunc = null;
    }

    /// <summary>
    /// Moves to existing sle.
    /// </summary>
    /// <param name="existingSle">Existing sle.</param>
    /// <param name="childSetFunc">Child set funnc.</param>
    public void MoveToTreeSle(ITreePartSle existingSle, SetChildDelegate childSetFunc)
    {
        if (existingSle is IChainPartSle)
            throw new InvalidOperationException();

        CurrentChainSle = null;
        CurrentTreeSle = existingSle;
        CurrentTreeSleSetChildFunc = childSetFunc;
    }
}

class ChainExpressionVisitor : ExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public ChainExpressionVisitor(VisitorContext context)
    {
        _visitorContext = context;
    }

    [return: NotNullIfNotNull("node")]
    public override Expression Visit(Expression node)
    {
        if (UnwrpaNode(node) is ConstantExpression)
        {
            _visitorContext.AddChain(new FixedValueSle());
            return node;
        }

        return VisitChain(node);
    }

    private Expression UnwrpaNode(Expression node)
    {
        // Removes all convers.
        while (true)
        {
            if (node is not UnaryExpression unaryExpression)
                return node;
            if (unaryExpression.NodeType != ExpressionType.Convert)
                return node;
            node = unaryExpression.Operand;
        }
    }

    private Expression VisitChain(Expression node)
    {
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

        IChainPartSle lastRowSourceSle;
        if (!string.IsNullOrEmpty(tableName))
            lastRowSourceSle = new TableSle(tableName);
        else
        {
            // May be we are accessing table via parameter 
            if (chainCalls[0] is ParameterExpression parameterExpression && _visitorContext.ParameterToSle.TryGetValue(parameterExpression, out var parameterSle))
            {
                lastRowSourceSle = new ReferenceRowSourceSle() { ReferenceRowSource = parameterSle };
                chainCalls = chainCalls.Skip(1).ToArray();
            }
            else
                throw new InvalidOperationException();
        }

        _visitorContext.AddChain(lastRowSourceSle);

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
                    _visitorContext.MoveToChainSle(filterVisitor.FilterSle);
                }
                else if ((new[] { "Select" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2) ||
                    (new[] { "SelectMany" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2))
                {
                    var filterParameter = ExtractParameterVaribleFromSelectExpression(chainCallExpression.Arguments[1]);
                    _visitorContext.ParameterToSle.Add(filterParameter, lastRowSourceSle);
                    var selectSle = new SelectSle();
                    _visitorContext.AddChainWithRootTree(selectSle, (p, c) => ((SelectSle)p).InnerExpression = c);
                    lastRowSourceSle = selectSle;
                    new ChainExpressionVisitor(_visitorContext).Visit(ExtractSelectLambdaBody(chainCallExpression.Arguments[1]));
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
                        _visitorContext.AddChain(new ColumnAccessSle() { ColumnName = memberProperty.Name });
                    else if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                    {
                        var associationAttribute = memberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                        var nextTableName = GetMetaTypeFromAssociation(memberProperty.PropertyType).CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                        var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                        var otherTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
                        var associationSle = new AssociationSle() { ColumnName = currentTableField, NextTableName = nextTableName, NextTableColumnName = otherTableField };
                        lastRowSourceSle = associationSle;
                        _visitorContext.AddChain(associationSle);
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
    public FilterSle FilterSle { get; private set; }

    public FilterExpressionVisitor(VisitorContext context)
    {
        _visitorContext = context;
        FilterSle = new FilterSle();
        _visitorContext.AddChainWithRootTree(FilterSle, (p, c) => ((FilterSle)p).InnerExpression = c);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var binarySle = new FilterBinarySle();
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
