using Moq;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System;
using Moq.Protected;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SampleEnvironment;

class DataContextWithFunctionsAndConnection : IDisposable
{
    private bool _isDisposed;
    private DbConnection _connection;

    public DataContextWithFunctions DataContext { get; private set; }

    public DataContextWithFunctionsAndConnection()
    {
        _connection = CreateConnection();
        DataContext = new DataContextWithFunctions(_connection);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        _connection.Dispose();
        DataContext.Dispose();
    }

    private static DbConnection CreateConnection()
    {
        var connectionMock = new Mock<DbConnection>();
        connectionMock.SetupGet(c => c.ConnectionString).Returns("Server=somehost;Initial catalog=someCatalog;Integrated Security=True");
        connectionMock.SetupGet(c => c.ServerVersion).Returns("100500");
        connectionMock.SetupGet(c => c.State).Returns(ConnectionState.Open);
        connectionMock.Protected()
            .Setup<DbCommand>("CreateDbCommand").Returns(CreateCommand());
        return connectionMock.Object;
    }

    private static DbCommand CreateCommand()
    {
        List<DbParameter> parameters = new List<DbParameter>();
        string commandText = null;

        var dbParameterCollection = new Mock<DbParameterCollection>();
        dbParameterCollection.Setup(c => c.GetEnumerator()).Returns(parameters.GetEnumerator());

        var commandMock = new Mock<DbCommand>();
        commandMock.Protected()
            .Setup<DbDataReader>("ExecuteDbDataReader", It.IsAny<CommandBehavior>()).Returns(new Mock<DbDataReader>().Object);
        commandMock.Protected().Setup<DbParameter>("CreateDbParameter").Returns(() =>
        {
            var toReturn = new Mock<DbParameter>();
            return toReturn.Object;
        });
        commandMock.Protected()
            .SetupGet<DbParameterCollection>("DbParameterCollection").Returns(() =>
            {
                return dbParameterCollection.Object;
            });

        commandMock.SetupGet(c => c.CommandText).Returns(() =>
        {
            return commandText;
        });
#pragma warning disable CS0618 // Type or member is obsolete
        commandMock.SetupSet(c => c.CommandText).Callback(t =>
        {
            commandText = t;
        });
#pragma warning restore CS0618 // Type or member is obsolete

        return commandMock.Object;
    }
}

