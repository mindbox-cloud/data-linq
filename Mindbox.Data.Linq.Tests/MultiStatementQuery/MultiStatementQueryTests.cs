﻿using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Snapshooter.MSTest;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;

[TestClass]
public class MultiStatementQueryTests
{
    [TestMethod]
    public void Translate_NoFilter_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().AsQueryable().Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        Assert.AreEqual("""
            DECLARE @tabledirectcrm_Customers_1 TABLE(
                Id int not null
            )

            INSERT INTO @tabledirectcrm_Customers_1
                SELECT current.Id 
                    FROM directcrm.Customers AS current
                    WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Translate_TopLevelFilterAgainstConstant_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c2 => c2.TempPasswordEmail == "123").Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        Assert.AreEqual("""
            DECLARE @tabledirectcrm_Customers_1 TABLE(
                Id int not null,
                TempPasswordEmail nvarchar(256) not null
            )

            INSERT INTO @tabledirectcrm_Customers_1
                SELECT current.Id, current.TempPasswordEmail 
                    FROM directcrm.Customers AS current
                    WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Translate_ChainedWhere_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c2 => c2.TempPasswordEmail == "123").Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        Assert.AreEqual("""
            DECLARE @tabledirectcrm_Customers_1 TABLE(
                Id int not null,
                TempPasswordEmail nvarchar(256) not null
            )

            INSERT INTO @tabledirectcrm_Customers_1
                SELECT current.Id, current.TempPasswordEmail 
                    FROM directcrm.Customers AS current
                    WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Translate_TopLevelFilterAgainstVariable_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, TempPasswordEmail FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Translate_TopLevelFilterAgainstBooleanField_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.IsDeleted).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        Assert.AreEqual("""
            DECLARE @tabledirectcrm_Customers_1 TABLE(
                Id int not null,
                IsDeleted bit not null
            )

            INSERT INTO @tabledirectcrm_Customers_1
                SELECT current.Id, current.IsDeleted 
                    FROM directcrm.Customers AS current
                    WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Translate_TopLevelFilterAgainstNotBooleanField_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => !c.IsDeleted).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, IsDeleted FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Translate_TopLevelMultipleSimleFilters_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail && c.Id > 10 && !c.IsDeleted).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, TempPasswordEmail, IsDeleted FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Translate_TopLevelMultipleSimleFiltersOnSeveralWhereBlocks_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail).Where(c => c.Id > 10).Where(c => c.Id > 10 && !c.IsDeleted).Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, TempPasswordEmail, IsDeleted FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Translate_TableLinkedViaReferenceAssociation_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.Area.Name == "SomeArea").Expression;
        var query = SqlQueryTranslator.Transalate(queryExpression, new DbColumnTypeProvider());

        // Assert
        query.CommandText.MatchSnapshot();
    }
}