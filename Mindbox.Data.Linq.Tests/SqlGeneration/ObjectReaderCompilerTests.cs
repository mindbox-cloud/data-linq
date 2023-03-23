using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace Mindbox.Data.Linq.Tests.SqlGeneration;

[TestClass]
public class ObjectReaderCompilerTests
{
    [TestMethod]
    public void DBInt_PropertyIntAndNullableInt_Success()
    {
        // Arrange
        var reader = CreateReader(new[]
        {
            new Row(1, 10),
            new Row(2, null),
        });
        using var connection = CreateConnection(reader);
        using var context = new DataContext(connection);

        // Act
        var rows = context.GetTable<CustomRowWithIntKey>().ToArray();

        // Assert
        Assert.AreEqual(2, rows.Length);
        Assert.AreEqual(1, rows[0].Id);
        Assert.AreEqual(10, rows[0].ValueNullable);
        Assert.AreEqual(2, rows[1].Id);
        Assert.IsNull(rows[1].ValueNullable);
    }

    [TestMethod]
    public void DBBigint_PropertyLongAndNullableLong_Success()
    {
        // Arrange
        var reader = CreateReader(new[]
        {
            new Row(1L, 10L),
            new Row(2L, null),
        });
        using var connection = CreateConnection(reader);
        using var context = new DataContext(connection);

        // Act
        var rows = context.GetTable<CustomRowWithBigintKey>().ToArray();

        // Assert
        Assert.AreEqual(2, rows.Length);
        Assert.AreEqual(1L, rows[0].Id);
        Assert.AreEqual(10, rows[0].ValueNullable);
        Assert.AreEqual(2L, rows[1].Id);
        Assert.IsNull(rows[1].ValueNullable);
    }

    [TestMethod]
    public void DBHasInt_PropertyLong_Success()
    {
        // Arrange
        var reader = CreateReader(new[]
        {
            new Row(1, 10L), 
            new Row(2, null),
        });
        using var connection = CreateConnection(reader);
        using var context = new DataContext(connection);

        // Act
        var rows = context.GetTable<CustomRowWithBigintKey>().ToArray();

        // Assert
        Assert.AreEqual(2, rows.Length);
        Assert.AreEqual(1L, rows[0].Id);
        Assert.AreEqual(10, rows[0].ValueNullable);
        Assert.AreEqual(2L, rows[1].Id);
        Assert.IsNull(rows[1].ValueNullable);
    }

    [TestMethod]
    public void DBHasInt_PropertyLongAndNullableLong_Success()
    {
        // Arrange
        var reader = CreateReader(new[]
        {
            new Row(1, 10),
            new Row(2, null),
        });
        using var connection = CreateConnection(reader);
        using var context = new DataContext(connection);

        // Act
        var rows = context.GetTable<CustomRowWithBigintKey>().ToArray();

        // Assert
        Assert.AreEqual(2, rows.Length);
        Assert.AreEqual(1L, rows[0].Id);
        Assert.AreEqual(10, rows[0].ValueNullable);
        Assert.AreEqual(2L, rows[1].Id);
        Assert.IsNull(rows[1].ValueNullable);
    }

    private DbDataReader CreateReader(IEnumerable<Row> rows)
    {
        var rowsArray = rows.ToArray();
        var currentIndex = -1;
        var readerMock = new Mock<DbDataReader>();

        readerMock.Setup(r => r.IsDBNull(It.IsAny<int>()))
            .Returns<int>(i => rowsArray[currentIndex].FieldValues[i] == null);

        readerMock.Setup(r => r.GetFieldType(It.IsAny<int>()))
            .Returns<int>(i => rowsArray[currentIndex].FieldValues[i] == null
                ? typeof(object)
                : rowsArray[currentIndex].FieldValues[i].GetType());

        readerMock.Setup(r => r.GetInt32(It.IsAny<int>()))
            .Returns<int>(i => readerMock.Object.GetFieldType(i) == typeof(int)
                ? (int)rowsArray[currentIndex].FieldValues[i]
                : throw new InvalidOperationException("Value is not of type Int32."));

        readerMock.Setup(r => r.GetInt64(It.IsAny<int>()))
            .Returns<int>(i => readerMock.Object.GetFieldType(i) == typeof(long)
                ? (long)rowsArray[currentIndex].FieldValues[i]
                : throw new InvalidOperationException("Value is not of type Int64."));

        readerMock.Setup(r => r.IsDBNull(It.IsAny<int>()))
            .Returns<int>(i => rowsArray[currentIndex].FieldValues[i] == null);

        readerMock.Setup(r => r.Read()).Returns(() => ++currentIndex < rowsArray.Length);

        readerMock.Setup(r => r.NextResult()).Returns(false);

        return readerMock.Object;
    }

    private DbConnection CreateConnection(DbDataReader reader)
    {
        var commandMock = new Mock<DbCommand>();
        commandMock.Protected()
            .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>()).Returns(reader);

        var connectionMock = new Mock<DbConnection>();
        connectionMock.SetupGet(c => c.ConnectionString).Returns("Server=somehost;Initial catalog=someCatalog;Integrated Security=True");
        connectionMock.SetupGet(c => c.ServerVersion).Returns("100500");
        connectionMock.SetupGet(c => c.State).Returns(ConnectionState.Open);
        connectionMock.Protected()
            .Setup<DbCommand>("CreateDbCommand").Returns(commandMock.Object);
        return connectionMock.Object;
    }


    public record Row(params object[] FieldValues);


    [Table(Name = "CustomRowWithBigintKey")]
    public sealed class CustomRowWithBigintKey
    {
        [Column(IsPrimaryKey = true)]
        public long Id { get; set; }

        [Column]
        public long? ValueNullable { get; set; }
    }

    [Table(Name = "CustomRowWithIntKey")]
    public sealed class CustomRowWithIntKey
    {
        [Column(IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column]
        public int? ValueNullable { get; set; }
    }
}