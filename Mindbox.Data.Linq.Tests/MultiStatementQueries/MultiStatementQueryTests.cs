using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.RewriterTypes;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment.EntityTypes;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
using Snapshooter.MSTest;
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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
        var rewrittenExpression = new Rewriter().Rewrite(queryExpression);

        // Assert
        AssertTranslation(query.CommandText, queryExpression, rewrittenExpression);
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

    [TestMethod]
    public void Translate_TableJoinByJoinFollowedByWhere_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var orders = contextAndConnection.DataContext.GetTable<RetailOrder>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Join(orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })
            .Where(j => j.c.AreaId == 10)
            .Where(j => j.o.TotalSum > 100)
            .Expression;
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }

    [TestMethod]
    public void Translate_WithFunction_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextWithFunctionsAndConnection();

        // Act
        var orders = contextAndConnection.DataContext.GetTable<RetailOrder>();
        var history = contextAndConnection.DataContext.GetTable<RetailOrderHistoryItem>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>()
            .Where(c =>
                contextAndConnection.DataContext.ValueAsQueryableDecimal(
                        orders
                            .Where(o => o.CurrentCustomer == c)
                            .Join(
                                history,
                                order => order.Id,
                                history => history.RetailOrderId,
                                (order, history) => new RetailOrderCurrentHistoryItemData
                                {
                                    RetailOrder = order,
                                    RetailOrderHistoryItem = history
                                })
                            .Where(x => x.RetailOrderHistoryItem.IsCurrentOtherwiseNull != null)
                            .Where(
                                item => item.RetailOrderHistoryItem.Purchases.Where(item => item.Count != null).Any())
                        .Select(entity => (decimal?)entity.RetailOrderHistoryItem.Amount)
                        .Sum())
                    .Any(v => v.Value > 10)
            )
            .Expression;
        var query = SqlQueryTranslator.Translate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }


    private void AssertTranslation(string commandText, Expression expression, Expression<Func<ResultSet, bool>> rewrittenExpression)
    {
        StringBuilder sb = new();
        sb.AppendLine("****************************** Original expression **********************************");
        sb.AppendLine(expression.ToString());
        sb.AppendLine();
        sb.AppendLine("****************************** SQL result *******************************************");
        sb.AppendLine(commandText);
        sb.AppendLine();
        sb.AppendLine("****************************** Rewritten expression **********************************");
        sb.AppendLine(rewrittenExpression.ToString());
        sb.ToString().MatchSnapshot();

        rewrittenExpression.Compile();
    }
}

