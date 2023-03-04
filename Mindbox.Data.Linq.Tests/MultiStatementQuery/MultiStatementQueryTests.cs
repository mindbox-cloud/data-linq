using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Castle.Core.Resource;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using static Mindbox.Data.Linq.Tests.MultiStatementQuery.MultiStatementQueryTests;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;

[TestClass]
public class MultiStatementQueryTests
{
    [TestMethod]
    public void Analyze_NoFilter_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().AsQueryable().Expression;
        var query = SqlQueryConverter.Analyze(queryExpression);

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Analyze_TopLevelFilterAgainstConstant_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == "123").Expression;
        var query = SqlQueryConverter.Analyze(queryExpression);

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, TempPasswordEmail FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Analyze_TopLevelFilterAgainstVariable_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail).Expression;
        var query = SqlQueryConverter.Analyze(queryExpression);

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, TempPasswordEmail FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Analyze_TopLevelFilterAgainstBooleanField_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.IsDeleted).Expression;
        var query = SqlQueryConverter.Analyze(queryExpression);

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, IsDeleted FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Analyze_TopLevelFilterAgainstNotBooleanField_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => !c.IsDeleted).Expression;
        var query = SqlQueryConverter.Analyze(queryExpression);

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, IsDeleted FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Analyze_TopLevelMultipleSimleFilters_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail && c.Id > 10 && !c.IsDeleted).Expression;
        var query = SqlQueryConverter.Analyze(queryExpression);

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, TempPasswordEmail, IsDeleted FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }

    [TestMethod]
    public void Analyze_TopLevelMultipleSimleFiltersOnSeveralWhereBlocks_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var someEmail = "123";
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.TempPasswordEmail == someEmail).Where(c => c.Id > 10).Where(c => c.Id > 10 && !c.IsDeleted).Expression;
        var query = SqlQueryConverter.Analyze(queryExpression);

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT Id, TempPasswordEmail, IsDeleted FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }


    [TestMethod]
    public void Analyze_TableLinkedViaReferenceAssociation_Success()
    {
        // Arrange
        using var contextAndConnection = new DataContextAndConnection();

        // Act
        var customerActions = contextAndConnection.DataContext.GetTable<CustomerAction>();
        var queryExpression = contextAndConnection.DataContext
            .GetTable<Customer>().Where(c => c.Area.Name == "SomeArea").Expression;
        var query = SqlQueryConverter.Analyze(queryExpression);

        // Assert
        Assert.AreEqual("""
            INSERT INTO @tabledirectcrm_Customers_1
                SELECT  FROM directcrm.Customers WHERE Id = @pKeyId
            SELECT * FROM @tabledirectcrm_Customers_1
            """, query.CommandText);
    }
}