using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq.Mapping;
using System.Diagnostics;
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


        var visitor = new QueryExpressionVisitor(new DbColumnTypeProvider());
        visitor.Visit(queryExpression);

        Console.WriteLine();
        Console.WriteLine("Query:");
        Console.WriteLine(visitor.Query.Dump());
    }

    class DataSource { }

    class TableDataSource : DataSource
    {
        public TableNode Table { get; private set; }

        public TableDataSource(TableNode table)
        {
            Table=table;
        }
    }

    class SelectDataSource : DataSource
    {
        public Dictionary<PropertyInfo, Expression> Mapping { get; } = new Dictionary<PropertyInfo, Expression>();
    }



    class QueryExpressionVisitor : ExpressionVisitor
    {
        private Stack<DataSource> _dataSourceStack = new();
        private Stack<(ParameterExpression Parameter, DataSource DataSource)> _variablesOnStack = new();
        private Stack<string> _context = new();
        private readonly IDbColumnTypeProvider _columnTypeProvider;

        public MultiStatementQuery Query { get; private set; } = new MultiStatementQuery();

        public QueryExpressionVisitor(IDbColumnTypeProvider columnTypeProvider)
        {
            _columnTypeProvider= columnTypeProvider;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var tableName = ExpressionHelpers.GetTableName(node);
            if (!string.IsNullOrEmpty(tableName))
            {
                throw new InvalidOperationException("All tables should be extracted from chain calls.");
            }

            return base.VisitConstant(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ConstantExpression)
            {
                var memberConstantValue = Expression.Lambda(node).Compile().DynamicInvoke();
                var memberTableName = ExpressionHelpers.GetTableNameFromObject(memberConstantValue);
                if (!string.IsNullOrEmpty(memberTableName))
                {
                    Console.WriteLine($"Table: {memberTableName}");
                }
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var chainCalls = ExpressionOrderFixer.GetReorderedChainCall(node).ToArray();
            var tableName = ExpressionHelpers.GetTableName(chainCalls[0]);
            if (!string.IsNullOrEmpty(tableName))
                chainCalls = chainCalls.Skip(1).ToArray();
            else if (string.IsNullOrEmpty(tableName) && chainCalls.Length > 1)
            {
                tableName = ExpressionHelpers.GetTableName(chainCalls[1]);
                if (!string.IsNullOrEmpty(tableName))
                    chainCalls  = chainCalls.Skip(2).ToArray();
            }
            if (!string.IsNullOrEmpty(tableName))
            {
                using (_dataSourceStack.ScopePush(new TableDataSource(Query.AddTable(tableName))))
                using (PushContext(tableName))
                {
                    foreach (var chainItemExpression in chainCalls)
                    {
                        if (chainItemExpression is MethodCallExpression chainCallExpression &&
                            (chainCallExpression.Method.DeclaringType == typeof(Queryable) || chainCallExpression.Method.DeclaringType == typeof(Enumerable)))
                        {
                            if (new[] { "Where", "Any" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
                            {
                                using (PushContext(chainCallExpression.Method.Name))
                                using (_variablesOnStack.ScopePush((ExtractParameterVaribleFromFilterExpression(chainCallExpression.Arguments[1]), PeekTableFromDataSources())))
                                {
                                    Visit(chainCallExpression.Arguments[1]);
                                }
                            }
                            else if (new[] { "Select" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
                                continue;
                            else if (new[] { "SelectMany" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 2)
                                continue;
                            else if (new[] { "Any" }.Contains(chainCallExpression.Method.Name) && chainCallExpression.Arguments.Count == 1)
                                continue;
                        }
                    }
                }
                return node;
            }
            else
                return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var table = PeekTableFromDataSources().Table;
            var condition = ExtractJoinCondition(table, node);
            if (condition != null)
                table.AddJoinCondition(condition);
            return base.VisitBinary(node);
        }

        private ParameterExpression ExtractParameterVaribleFromFilterExpression(Expression filterExpression)
        {
            var unary = (UnaryExpression)filterExpression;
            if (unary.NodeType != ExpressionType.Quote || unary.IsLifted || unary.IsLiftedToNull || unary.Method != null)
                throw new NotSupportedException();
            var lambda = (LambdaExpression)unary.Operand;
            if (lambda.ReturnType != typeof(bool) || lambda.TailCall || !string.IsNullOrEmpty(lambda.Name) || lambda.Parameters.Count != 1)
                throw new NotSupportedException();
            return lambda.Parameters[0];
        }

        private TableDataSource PeekTableFromDataSources()
        {
            foreach (var dataSource in _dataSourceStack.Reverse())
            {
                if (dataSource is TableDataSource tableDataSource)
                    return tableDataSource;
            }
            throw new NotSupportedException();
        }

        private StackPusher<string> PushContext(string contextItem)
        {
            var toReturn = new StackPusher<string>(_context, contextItem);
            PrintContext();
            return toReturn;
        }

        private void PrintContext()
        {
            if (_context.Count == 0)
                Console.WriteLine("Context is empty");
            else
                Console.WriteLine(string.Join(" -> ", _context));
        }


        private JoinCondition ExtractJoinCondition(TableNode table, BinaryExpression filter)
        {
            if (filter.NodeType != ExpressionType.Equal)
                return null;
            var leftPart = ExtractTableField(filter.Left);
            if (leftPart == null)
                return null;
            var rightPart = ExtractTableField(filter.Right);
            if (rightPart == null)
                return null;
            if (leftPart.Table != table && rightPart.Table != table)
                return null;
            if (rightPart.Table == table)
                (leftPart, rightPart) = (rightPart, leftPart);
            return new JoinCondition(leftPart.Field, rightPart.Table, rightPart.Field);
        }

        private TableAndField ExtractTableField(Expression expression)
        {
            expression = Unwrap(expression);

            if (expression is MemberExpression memberExpression && memberExpression.Member is PropertyInfo memberProperty)
            {
                if (memberExpression.Expression is ParameterExpression memberParameterExpression)
                {
                    var table = GetTableFromExpression(memberParameterExpression);
                    if (table != null)
                    {
                        // Column access. Like User.Name
                        if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(ColumnAttribute)))
                            return new TableAndField(table, memberProperty.Name);
                        // Association access
                        if (memberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                        {
                            var associationAttribute = memberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                            var nextTableName = memberProperty.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                                .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                            var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                            return new TableAndField(table, currentTableField);
                        }
                    }
                }
                if (memberExpression.Expression is MemberExpression innerMemberExpression && innerMemberExpression.Member is PropertyInfo innerMemberProperty)
                {
                    if (innerMemberExpression.Expression is ParameterExpression innerMemberParameterExpression)
                    {
                        var table = _variablesOnStack.Where(v => v.Parameter == innerMemberParameterExpression).Select(v => v.Table).SingleOrDefault();
                        if (table != null)
                        {
                            if (innerMemberProperty.CustomAttributes.Any(p => p.AttributeType == typeof(AssociationAttribute)))
                            {
                                var associationAttribute = innerMemberProperty.CustomAttributes.SingleOrDefault(p => p.AttributeType == typeof(AssociationAttribute));
                                var nextTableName = innerMemberProperty.PropertyType.CustomAttributes.Single(c => c.AttributeType == typeof(TableAttribute)).NamedArguments
                                    .Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                                var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                                var nextTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
                                if (nextTableField == memberProperty.Name)
                                    return new TableAndField(table, currentTableField);
                            }
                        }
                    }
                }
            }

            if (expression is ParameterExpression parameterExpression)
            {
                var table = _variablesOnStack.Where(v => v.Parameter == parameterExpression).Select(v => v.Table).SingleOrDefault();
                if (table != null)
                    return new TableAndField(table, _columnTypeProvider.GetPKFields(table.TableName).Single());
            }

            return null;

            TableNode GetTableFromExpression(ParameterExpression parameterExpression)
            {
                // Pickundelying table bypassin SelectTavble
                var table = _variablesOnStack.Where(v => v.Parameter == memberParameterExpression).Select(v => v.Table).SingleOrDefault();
            }

            static Expression Unwrap(Expression expression)
            {
                if (expression.NodeType == ExpressionType.Convert)
                {
                    var unaryExpression = (UnaryExpression)expression;
                    if (unaryExpression.IsLifted || unaryExpression.IsLiftedToNull || unaryExpression.Method != null)
                        throw new NotSupportedException();
                    return unaryExpression.Operand;
                }
                return expression;
            }
        }

        private record TableAndField(TableNode Table, string Field);

    }
}


public static class StackExtensions
{
    public static StackPusher<T> ScopePush<T>(this Stack<T> stack, T item)
    {
        return new StackPusher<T>(stack, item);
    }
}
