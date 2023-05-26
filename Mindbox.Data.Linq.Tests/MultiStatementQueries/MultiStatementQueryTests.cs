using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualBasic;
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

        var visitor = new Visitor();
        visitor.Visit(queryExpression);

        // var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        // query.CommandText.MatchSnapshot();
    }


    class Visitor : ExpressionVisitor
    {
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (!string.IsNullOrEmpty(ExpressionHelpers.GetTableName(node)))
                Console.WriteLine($"Visited table: {ExpressionHelpers.GetTableName(node)}");
            return base.VisitConstant(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var baseVisit = base.VisitMember(node);
            if (node.Expression is ConstantExpression)// Invocation of constant
            {
                var memberConstantValue = Expression.Lambda(node).Compile().DynamicInvoke();
                var memberTableName = ExpressionHelpers.GetTableName(memberConstantValue);
                if (!string.IsNullOrEmpty(memberTableName))
                {
                    Console.WriteLine($"Visited table: {memberTableName}");
                }
            }
            return baseVisit;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var baseVisit = base.VisitMethodCall(node);
            if (node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(Enumerable))
                Console.WriteLine($"Visiting querable or enumerable method: {node.Method.Name}");
            return baseVisit;
        }


        [return: NotNullIfNotNull("node")]
        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }
    }

    // Several neested joins
    // Querable join
    // Select with anonympus types
    // SelectMany

    // See sample for more cases

}