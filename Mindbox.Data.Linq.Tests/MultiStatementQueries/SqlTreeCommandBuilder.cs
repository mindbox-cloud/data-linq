using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

internal class SqlTreeCommandBuilder
{
    public static string Build(TableNode2 query, IDbColumnTypeProvider columnTypeProvider)
    {
        var context = new BuilderContext();
        List<string> queryParts = new();
        List<string> variableDefinitions = new();

        var rootVariableName = context.CreateVariableName(query);
        variableDefinitions.Add(BuildTableVariableDefinition(rootVariableName, query, columnTypeProvider));
        queryParts.Add($"""
            INSERT INTO {rootVariableName}({string.Join(", ,", GetUsedColumns(query))})
                SELECT {string.Join(", ", GetUsedColumns(query).Order().Select(c => $"current.{c}"))}
                    FROM {query.Name} AS current
                        WHERE Id = @__id
            SELECT * FROM {rootVariableName}
            """);


        foreach (var connection in query.Connections)
            BuildCore(context, queryParts, variableDefinitions, connection, columnTypeProvider);

        var variableDefinition = string.Join("\r\n", variableDefinitions);
        var queries = string.Join("\r\n\r\n", queryParts);
        return string.Join("\r\n\r\n", new[] { variableDefinition, queries });

        static void BuildCore(BuilderContext context, List<string> queryParts, List<string> variableDefinitions, Connection connection, IDbColumnTypeProvider columnTypeProvider)
        {
            var otherTable = connection.OtherTable;
            var variableName = context.CreateVariableName(otherTable);
            variableDefinitions.Add(BuildTableVariableDefinition(variableName, otherTable, columnTypeProvider));

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
                previousSource = $"(SELECT DISTINCT {string.Join(", ", connection.TableFields)} FROM {context.GetVariableName(connection.Table)})";
            else
                previousSource = context.GetVariableName(connection.Table);

            var query = $"""
                INSERT INTO {variableName}({string.Join(", ,", GetUsedColumns(otherTable))})
                    SELECT {string.Join(", ", GetUsedColumns(otherTable).Order().Select(c => $"current.{c}"))}
                        FROM {otherTable.Name} AS current
                            INNER JOIN {previousSource} AS previous ON
                                {string.Join(" AND\r\n                ", joinConditionParts)}
                SELECT * FROM {variableName}
                """;
            queryParts.Add(query);

            foreach (var nextConnection in connection.OtherTable.Connections)
                BuildCore(context, queryParts, variableDefinitions, connection, columnTypeProvider);
        }
    }

    private static IEnumerable<string> GetUsedColumns(TableNode2 table)
    {
        if (table.Connections.Count() == 0)
            return table.UsedFields.Concat(new[] { "Id" }).Distinct().Order();
        return table.UsedFields.Order();
    }


    private static string BuildTableVariableDefinition(string variableName, TableNode2 table, IDbColumnTypeProvider columntTypeProvider)
    {
        var columnsWithTypes = GetUsedColumns(table).Order().Select(c => $"{c} {columntTypeProvider.GetSqlType(table.Name, c)}").ToArray();
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
        private Dictionary<TableNode2, string> _variableNames = new();
        public StringBuilder StringBuilder { get; } = new StringBuilder();

        public string GetVariableName(TableNode2 table)
        {
            if (!_variableNames.TryGetValue(table, out var name))
                throw new ArgumentException();
            return name;
        }

        public string CreateVariableName(TableNode2 table)
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
