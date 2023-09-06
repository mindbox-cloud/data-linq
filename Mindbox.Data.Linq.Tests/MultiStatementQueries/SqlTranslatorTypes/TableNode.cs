using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;

[DebuggerDisplay("{Name}")]
class TableNode
{
    private List<string> _usedFields = new();
    private List<Connection> _connections = new();

    public string Name { get; private set; }
    public IEnumerable<string> UsedFields => _usedFields;
    public IEnumerable<Connection> Connections => _connections;

    public TableNode(string name)
    {
        Name = name;
    }

    public void AddField(string name)
    {
        if (_usedFields.Contains(name))
            return;
        _usedFields.Add(name);
        _usedFields.Sort();
    }

    public Connection AddConnection(IEnumerable<string> fields, TableNode otherTable, IEnumerable<string> otherTableFields)
    {
        foreach (var connection in _connections)
        {
            if (connection.Table == this && connection.TableFields.SequenceEqual(fields) &&
                connection.OtherTable == otherTable && connection.OtherTableFields.SequenceEqual(otherTableFields) ||
                connection.Table == otherTable && connection.TableFields.SequenceEqual(otherTableFields) &&
                connection.OtherTable == this && connection.OtherTableFields.SequenceEqual(fields))
                return connection;
        }
        foreach (var field in fields)
            AddField(field);
        foreach (var otherField in otherTableFields)
            otherTable.AddField(otherField);

        var newConnection = new Connection(this, fields, otherTable, otherTableFields);
        _connections.Add(newConnection);
        return newConnection;
    }

    public bool OptimizeConnections()
    {
        // Merge duplicated connections
        bool hasChanges = false;
        for (int i = 0; i < _connections.Count - 1; i++)
            for (int j = i; j < _connections.Count; j++)
            {
                var connectionA = _connections[i];
                var connectionB = _connections[j];
                if (connectionA == connectionB)
                    continue;
                if (!connectionA.Equals(connectionB))
                    continue;
                _connections.Remove(connectionB);
                j--;
                hasChanges = true;
                foreach (var connectionBOtherTableConnection in connectionB.OtherTable.Connections)
                    connectionA.OtherTable.AddConnection(connectionBOtherTableConnection.TableFields, connectionBOtherTableConnection.OtherTable, connectionBOtherTableConnection.OtherTableFields);
            }

        // if we have connections like Customer->CustomerAction->Customer->SomeOtherTable
        // We can actually remove second Customer connection(move all its connections to top Customer) and have something like this
        // Customer -> CustomerAction
        //        \ -> SomeOtherTable
        bool lifted = false;
        foreach (var connection in _connections)
        {
            foreach (var subConnection in connection.OtherTable.Connections)
            {

                if (!subConnection.IsSame(connection))
                    continue;
                connection.OtherTable._connections.Remove(subConnection);
                foreach (var movedConnection in subConnection.OtherTable.Connections)
                    _connections.Add(movedConnection);
                lifted = true;
                break;
            }
            if (lifted)
                break;
        }
        hasChanges |= lifted;

        return hasChanges;
    }

    public IEnumerable<TableNode> GetAllTableNodes()
    {
        return GetAllTableNodes(this);

        static IEnumerable<TableNode> GetAllTableNodes(TableNode tableNode2)
        {
            yield return tableNode2;
            foreach (var connection in tableNode2._connections)
            {
                yield return connection.OtherTable;
                foreach (var table in GetAllTableNodes(connection.OtherTable))
                {
                    yield return table;

                }
            }
        }
    }

    internal void Merge(TableNode table)
    {
        if (Name != table.Name)
            throw new InvalidOperationException();

        foreach (var usedField in table.UsedFields)
            AddField(usedField);

        foreach (var otherConnection in table.Connections)
        {
            var matchingConnection = _connections.FirstOrDefault(c => c.Equals(otherConnection));
            if (matchingConnection == null)
                matchingConnection = AddConnection(otherConnection.TableFields, new TableNode(otherConnection.OtherTable.Name), otherConnection.OtherTableFields);
            matchingConnection.OtherTable.Merge(otherConnection.OtherTable);
        }
    }
}
