﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snapshooter.MSTest;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Policy;

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableJoinWithSelectFollowedByWhere_SelectSwitchesType_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Select(c => customerActions.Where(ca => ca.Customer == c).FirstOrDefault().ActionTemplate)
            .Where(c => c.Name == "dummy")
            .Expression;
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableJoinWithSelectFollowedByWhere_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Select(c => customerActions.Where(ca => ca.Customer == c).FirstOrDefault())
            .Where(c => c.ActionTemplate.Name == "dummy")
            .Expression;
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableJoinWithSelectAnonymousFollowedByWhere_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Select(c => new
            {
                CA = customerActions.Where(ca => ca.Customer == c).FirstOrDefault(),
                CustomerArea = c.Area
            })
            .Where(c => c.CA.ActionTemplate.Name == "dummy" && c.CA.StaffId == 10)
            .Where(c => c.CustomerArea.Id == 20)
            .Expression;
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableJoinByAssociationFollowedBySelectMany_Success()
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
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_TableJoinByAssociationFollowedBySelectWithAnonymousType_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var orders = contextAndConnection.DataContext.GetTable<RetailOrder>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Where(c =>
                orders.Where(o => o.CurrentCustomer == c)
                   .Select(o =>
                       new
                       {
                           o.History.Single(hi => hi.IsCurrentOtherwiseNull != null).Purchases,
                           Order = o
                       })
                   .Where(p => p.Purchases.Any(pur => pur.PriceForCustomerOfLine > 0))
                   .Where(p => p.Order.Id > 100)
                   .Any()
            )
            .Expression;
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    // Several nested joins
    // Querable join
    // See sample for more cases
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
    ChainSle Chain { get; set; }
}

static class ChainPartSleExtensions
{
    public static IChainPart GetNext(this IChainPart chainPart)
    {
        for (int i = 0; i < chainPart.Chain.Items.Count; i++)
        {
            if (chainPart == chainPart.Chain.Items[i])
                return i == 0 ? null : chainPart.Chain.Items[i - 1];
        }
        throw new InvalidOperationException();
    }

    public static IChainPart GetPrevious(this IChainPart chainPart)
    {
        var index = -1;
        for (int i = 0; i < chainPart.Chain.Items.Count; i++)
        {
            if (chainPart.Chain.Items[i] == chainPart)
            {
                index = i;
                break;
            }
        }
        if (index == -1)
            throw new InvalidOperationException();
        if (index == 0)
            return null;
        return chainPart.Chain.Items[index - 1];
    }

    public static bool IsLast(this IChainPart chainPart)
        => chainPart.Chain.Items.Last() == chainPart;
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

static class TreeNodeSleExtensions
{
    public static ChainSle GetChain(this ITreeNodeSle node)
    {
        if (node is IChainPart chainPart)
            return chainPart.Chain;
        while (true)
        {
            if (node.ParentExpression == null)
                throw new InvalidOperationException("Top of each node should be chain part");
            if (node.ParentExpression is IChainPart parentChainPart)
                return parentChainPart.Chain;
            if (node.ParentExpression is not ITreeNodeSle parentNode)
                throw new InvalidOperationException("Parent should be chain part or tree node");
            node = parentNode;
        }
    }

    /// <summary>
    /// Shows that node is FilterBinarySle and represents top level equality statement
    /// </summary>
    /// <param name="node">Node.</param>
    /// <returns>True - yes, false - not.</returns>
    public static bool IsTopLevelChainEqualityStatement(this ITreeNodeSle node)
    {
        if (node is not FilterBinarySle filter)
            return false;
        if (filter.Operator != FilterBinaryOperator.ChainsEqual)
            return false;

        var parent = node.ParentExpression as FilterBinarySle;
        while (parent != null)
        {
            if (parent.Operator != FilterBinaryOperator.FilterBinaryAnd)
                return false;
            parent = parent.ParentExpression as FilterBinarySle;
        }

        return true;
    }

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

class ChainSle : ITreeNodeSle
{
    private List<IChainPart> _items = new();

    public IReadOnlyList<IChainPart> Items => _items;

    public ISimplifiedLinqExpression ParentExpression { get; set; }

    public bool IsNegated { get; set; }

    public void AddChainPart(IChainPart chainPart)
    {
        if (chainPart.Chain != null)
            throw new InvalidOperationException();
        _items.Add(chainPart);
        chainPart.Chain = this;
    }
}

class TableChainPart : IRowSourceChainPart
{
    public string Name { get; private set; }

    public ChainSle Chain { get; set; }

    public TableChainPart(string name)
    {
        Name = name;
    }
}

class ReferenceRowSourceChainPart : IChainPart
{
    public ChainSle Chain { get; set; }
    public IChainPart ReferenceRowSource { get; set; }
}

class PropertyAccessChainPart : IRowSourceChainPart
{
    public ChainSle Chain { get; set; }
    public string PropertyName { get; set; }
}

class ColumnAccessChainPart : IChainPart
{
    public ChainSle Chain { get; set; }
    public string ColumnName { get; set; }
}

class FixedValueChainPart : IChainPart
{
    public ChainSle Chain { get; set; }
}

class AssociationChainPart : IRowSourceChainPart
{
    public string ColumnName { get; set; }
    public string NextTableName { get; set; }
    public string NextTableColumnName { get; set; }
    public ChainSle Chain { get; set; }
}

class FilterChainPart : IChainPartAndTreeNodeSle
{
    public ChainSle Chain { get; set; }
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
    public ChainSle Chain { get; set; }
    public ISimplifiedLinqExpression ParentExpression { get; set; }
    public SelectChainPartType ChainPartType { get; set; } = SelectChainPartType.Simple;
    public Dictionary<string, ChainSle> NamedChains { get; } = new Dictionary<string, ChainSle>();
}

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

delegate void SetTreeChildDelegate(ISimplifiedLinqExpression parent, ISimplifiedLinqExpression child);

class VisitorContext
{
    public Dictionary<ParameterExpression, IChainPart> ParameterToSle { get; private set; } = new();
    public IDbColumnTypeProvider ColumnTypeProvider { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columnTypeProvider">Column provider.</param>
    public VisitorContext(IDbColumnTypeProvider columnTypeProvider)
    {
        ColumnTypeProvider = columnTypeProvider;
    }
}

class ChainExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public ChainSle Chain { get; private set; }

    public ChainExpressionVisitor(ISimplifiedLinqExpression parentExpression, VisitorContext context)
    {
        _visitorContext = context;
        Chain = new ChainSle();
        Chain.ParentExpression = parentExpression;
    }

    private Expression UnwrapNode(Expression node)
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
        while (true)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    isNegated = !isNegated;
                    expression = (expression as UnaryExpression).Operand;
                    continue;
                case ExpressionType.Convert:
                    expression = (expression as UnaryExpression).Operand;
                    continue;
            }
            return expression;
        }
    }

    public void Visit(Expression node)
    {
        node = UnwrapNot(node, out var isNegated);
        Chain.IsNegated = isNegated;
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
                chainCalls = chainCalls.Skip(2).ToArray();
        }

        if (string.IsNullOrEmpty(tableName) && UnwrapNode(chainCalls[0]) is ConstantExpression) // plain constant or variable
        {
            Chain.AddChainPart(new FixedValueChainPart());
            return;
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

        Chain.AddChainPart(lastRowSourceSle);

        // Visit all chain parts
        foreach (var chainItemExpression in chainCalls)
        {
            if (chainItemExpression is MethodCallExpression chainCallExpression &&
                (chainCallExpression.Method.DeclaringType == typeof(Queryable) || chainCallExpression.Method.DeclaringType == typeof(Enumerable)))
            {
                if (new[] { "Where", "Any", "Single" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
                {
                    var filterParameter = ExtractParameterVariableFromFilterExpression(chainCallExpression.Arguments[1]);
                    _visitorContext.ParameterToSle.Add(filterParameter, lastRowSourceSle);
                    var filter = ExtractFilterLambdaBody(chainCallExpression.Arguments[1]);
                    var filterVisitor = new FilterExpressionVisitor(_visitorContext);
                    filterVisitor.Visit(filter);
                    Chain.AddChainPart(filterVisitor.FilterSle);
                    filterVisitor.FilterSle.ParentExpression = Chain;
                }
                else if ((new[] { "Select" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2) ||
                    (new[] { "SelectMany" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2))
                {
                    var filterParameter = ExtractParameterVariableFromSelectExpression(chainCallExpression.Arguments[1]);
                    _visitorContext.ParameterToSle.Add(filterParameter, lastRowSourceSle);
                    var selectVisitor = new SelectExpressionVisitor(_visitorContext);
                    selectVisitor.Visit(chainCallExpression.Arguments[1]);
                    lastRowSourceSle = selectVisitor.SelectSle;
                    Chain.AddChainPart(selectVisitor.SelectSle);
                    selectVisitor.SelectSle.ParentExpression = Chain;
                    continue;
                }
                else if (new[] { "Any", "Single", "SingleOrDefault", "First", "FirstOrDefault" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 1)
                    continue;
                else
                    throw new NotSupportedException();
            }
            else if (chainItemExpression is MemberExpression memberExpression)
            {
                if (memberExpression.Member is PropertyInfo memberProperty)
                {
                    if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                        Chain.AddChainPart(new ColumnAccessChainPart() { ColumnName = memberProperty.Name });
                    else if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                    {
                        var associationAttribute = memberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                        var nextTableName = GetMetaTypeFromAssociation(memberProperty.PropertyType).CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                            .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                        var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                        var otherTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
                        var associationSle = new AssociationChainPart() { ColumnName = currentTableField, NextTableName = nextTableName, NextTableColumnName = otherTableField };
                        lastRowSourceSle = associationSle;
                        Chain.AddChainPart(associationSle);
                    }
                    else
                    {
                        var propertyAccessChainPart = new PropertyAccessChainPart() { PropertyName = memberProperty.Name };
                        Chain.AddChainPart(propertyAccessChainPart);
                        lastRowSourceSle = propertyAccessChainPart;
                    }
                }
                else
                    throw new InvalidOperationException();
            }
            else
                throw new InvalidOperationException();
        }
    }

    private Type GetMetaTypeFromAssociation(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        return type;
    }

    private ParameterExpression ExtractParameterVariableFromFilterExpression(Expression filterExpression)
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

    private ParameterExpression ExtractParameterVariableFromSelectExpression(Expression filterExpression)
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
}

class SelectExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public SelectChainPart SelectSle { get; }

    public SelectExpressionVisitor(VisitorContext context)
    {
        SelectSle = new SelectChainPart();
        _visitorContext = context;

    }

    public void Visit(Expression expression)
    {
        expression = ExtractSelectLambdaBody(expression);
        switch (expression.NodeType)
        {
            case ExpressionType.New:
                SelectSle.ChainPartType = SelectChainPartType.Complex;
                var newExpression = (NewExpression)expression;
                for (int i = 0; i < newExpression.Members.Count; i++)
                {
                    var member = newExpression.Members[i];
                    var arg = newExpression.Arguments[i];
                    var visitor = new ChainExpressionVisitor(SelectSle, _visitorContext);
                    visitor.Visit(arg);
                    SelectSle.NamedChains.Add(member.Name, visitor.Chain);
                }
                break;
            default:
                var simpleVisitor = new ChainExpressionVisitor(SelectSle, _visitorContext);
                simpleVisitor.Visit(expression);
                SelectSle.NamedChains.Add(string.Empty, simpleVisitor.Chain);
                break;
        }
    }

    private Expression ExtractSelectLambdaBody(Expression expression)
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
}

class FilterBinaryExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public FilterBinarySle FilterBinarySle { get; private set; }

    public FilterBinaryExpressionVisitor(ISimplifiedLinqExpression parentSle, VisitorContext context)
    {
        _visitorContext = context;
        FilterBinarySle = new FilterBinarySle();
        FilterBinarySle.ParentExpression = parentSle;
    }

    public void Visit(BinaryExpression binaryExpression)
    {
        var binarySle = new FilterBinarySle();
        switch (binaryExpression.NodeType)
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

        if (binaryExpression.Left is BinaryExpression leftBinaryExpression)
        {
            var leftVisitor = new FilterBinaryExpressionVisitor(FilterBinarySle, _visitorContext);
            FilterBinarySle.LeftExpression = leftVisitor.FilterBinarySle;
            leftVisitor.Visit(leftBinaryExpression);
        }
        else
        {
            var leftVisitor = new ChainExpressionVisitor(FilterBinarySle, _visitorContext);
            FilterBinarySle.LeftExpression = leftVisitor.Chain;
            leftVisitor.Visit(binaryExpression.Left);
        }

        if (binaryExpression.Right is BinaryExpression rightBinaryExpression)
        {
            var rightVisitor = new FilterBinaryExpressionVisitor(FilterBinarySle, _visitorContext);
            FilterBinarySle.RightExpression = rightVisitor.FilterBinarySle;
            rightVisitor.Visit(rightBinaryExpression);
        }
        else
        {
            var rightVisitor = new ChainExpressionVisitor(FilterBinarySle, _visitorContext);
            FilterBinarySle.RightExpression = rightVisitor.Chain;
            rightVisitor.Visit(binaryExpression.Right);
        }
    }
}

class FilterExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    /// <summary>
    /// Filter sle.
    /// </summary>
    public FilterChainPart FilterSle { get; private set; }

    public FilterExpressionVisitor(VisitorContext context)
    {
        _visitorContext = context;
        FilterSle = new FilterChainPart();
    }

    public void Visit(Expression node)
    {
        if (node is BinaryExpression binaryExpression)
        {
            var visitor = new FilterBinaryExpressionVisitor(FilterSle, _visitorContext);
            FilterSle.InnerExpression = visitor.FilterBinarySle;
            visitor.Visit(binaryExpression);
        }
        else
        {
            var visitor = new ChainExpressionVisitor(FilterSle, _visitorContext);
            FilterSle.InnerExpression = visitor.Chain;
            visitor.Visit(node);
            FilterSle.InnerExpression = visitor.Chain;
        }
    }
}
