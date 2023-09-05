using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;

internal class SqlTreeCommandBuilder
{
    private const string _customerParameterName = "@__id";

    public static SqlTreeCommandBuilderResult Build(TableNode query, IDbColumnTypeProvider columnTypeProvider, bool fastCustomerSelect)
    {
        // Unify columns across same tables
        UnifySameTableFields(query);

        query.AddField("Id");

        var context = new BuilderContext();
        List<string> queryParts = new();
        List<string> variableDefinitions = new();
        List<string> tableReadOrder = new();

        var rootVariableName = context.CreateVariableName(query);
        tableReadOrder.Add(query.Name);
        variableDefinitions.Add(BuildTableVariableDefinition(rootVariableName, query, columnTypeProvider, true));
        queryParts.Add($"""
            INSERT INTO {rootVariableName}({string.Join(", ", GetUsedColumns(query, true))})
                SELECT {string.Join(", ", GetUsedColumns(query).Order().Select(c => $"current.{c}"))}
                    FROM {query.Name} AS current
                        WHERE Id = {_customerParameterName}
            SELECT * FROM {rootVariableName}
            """);


        foreach (var connection in query.Connections)
            BuildCore(context, queryParts, variableDefinitions, connection, columnTypeProvider, tableReadOrder, fastCustomerSelect);

        var variableDefinition = string.Join("\r\n", variableDefinitions);
        var queries = string.Join("\r\n\r\n", queryParts);
        return new(string.Join("\r\n\r\n", new[] { variableDefinition, queries }), tableReadOrder);

        static void BuildCore(BuilderContext context, List<string> queryParts, List<string> variableDefinitions, Connection connection, IDbColumnTypeProvider columnTypeProvider, List<string> tableReadOrder, bool fastCustomerSelect)
        {
            var otherTable = connection.OtherTable;
            tableReadOrder.Add(otherTable.Name);
            var variableName = context.CreateVariableName(otherTable);
            variableDefinitions.Add(BuildTableVariableDefinition(variableName, otherTable, columnTypeProvider));

            if (columnTypeProvider.HasField(otherTable.Name, "CustomerId") && fastCustomerSelect)
            {
                var query = $"""
                    INSERT INTO {variableName}({string.Join(", ", GetUsedColumns(otherTable))})
                        SELECT {string.Join(", ", GetUsedColumns(otherTable).Order().Select(c => $"current.{c}"))}
                            FROM {otherTable.Name} AS current
                            WHERE current.CustomerId = {_customerParameterName}
                    SELECT * FROM {variableName}
                    """;
                queryParts.Add(query);
            }
            else
            {
                var joinConditionParts = connection.MappedFields
                    .Select(c => $"previous.{c.Field} = current.{c.OtherField}")
                    .ToArray();
                if (!IsPKJoin(otherTable.Name, connection.TableFields, columnTypeProvider) &&
                    !IsPKJoin(otherTable.Name, connection.OtherTableFields, columnTypeProvider))
                {
                    throw new NotSupportedException("At least one part of join must point to PK.");
                }
                string previousSource;
                if (IsPKJoin(connection.Table.Name, connection.TableFields, columnTypeProvider))
                    previousSource = context.GetVariableName(connection.Table);
                else
                    previousSource = $"(SELECT DISTINCT {string.Join(", ", connection.TableFields)} FROM {context.GetVariableName(connection.Table)})";
                var query = $"""
                    INSERT INTO {variableName}({string.Join(", ", GetUsedColumns(otherTable))})
                        SELECT {string.Join(", ", GetUsedColumns(otherTable).Order().Select(c => $"current.{c}"))}
                            FROM {otherTable.Name} AS current
                                INNER JOIN {previousSource} AS previous ON
                                    {string.Join(" AND\r\n                ", joinConditionParts)}
                    SELECT * FROM {variableName}
                    """;
                queryParts.Add(query);
            }

            foreach (var nextConnection in connection.OtherTable.Connections)
                BuildCore(context, queryParts, variableDefinitions, nextConnection, columnTypeProvider, tableReadOrder, fastCustomerSelect);
        }
    }

    private static void UnifySameTableFields(TableNode query)
    {
        Dictionary<string, HashSet<string>> table2Fields = new();
        foreach (var table in query.GetAllTableNodes())
        {
            if (!table2Fields.ContainsKey(table.Name))
                table2Fields.Add(table.Name, new HashSet<string>());
            foreach (var usedField in table.UsedFields)
                table2Fields[table.Name].Add(usedField);
        }

        foreach (var table in query.GetAllTableNodes())
            foreach (var usedField in table2Fields[table.Name])
                table.AddField(usedField);
    }

    private static IEnumerable<string> GetUsedColumns(TableNode table, bool addIdColumn = false)
    {
        if (addIdColumn)
            return table.UsedFields.Concat(new[] { "Id" }).Distinct().Order();
        return table.UsedFields.Order();
    }


    private static string BuildTableVariableDefinition(string variableName, TableNode table, IDbColumnTypeProvider columnTypeProvider, bool addIdColumn = false)
    {
        var columnsWithTypes = GetUsedColumns(table, addIdColumn).Order().Select(c => $"{c} {columnTypeProvider.GetSqlType(table.Name, c)}").ToArray();
        return
            $"""
            DECLARE {variableName} TABLE(
                {string.Join(",\r\n    ", columnsWithTypes)}
            )
            """;
    }

    private static bool IsPKJoin(string tableName, IEnumerable<string> joinFields, IDbColumnTypeProvider columntTypeProvider)
    {
        var pkFields = columntTypeProvider.GetPKFields(tableName);
        return joinFields.Intersect(pkFields).Count() == pkFields.Length;
    }

    private class BuilderContext
    {
        private Dictionary<TableNode, string> _variableNames = new();
        public StringBuilder StringBuilder { get; } = new StringBuilder();

        public string GetVariableName(TableNode table)
        {
            if (!_variableNames.TryGetValue(table, out var name))
                throw new ArgumentException();
            return name;
        }

        public string CreateVariableName(TableNode table)
        {
            if (_variableNames.ContainsKey(table))
                throw new ArgumentException();
            var name = $"@table{table.Name.Replace('.', '_')}";
            if (_variableNames.Values.Contains(name))
            {
                int counter = 2;
                while (true)
                {
                    name = $"@table{table.Name.Replace('.', '_')}_{counter++}";
                    if (!_variableNames.Values.Contains(name))
                        break;
                }
            }
            _variableNames.Add(table, name);
            return name;
        }
    }
}

record SqlTreeCommandBuilderResult(string CommandText, IReadOnlyList<string> TableReadOrder);
