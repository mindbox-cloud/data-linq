using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

internal class SqlTreeCommandBuilder
{
    /*
    public static string Build(MultiStatementQuery query, IDbColumnTypeProvider columntTypeProvider)
    {
        var context = new BuilderContext();
        List<string> queryParts = new();
        List<string> variableDefinitionParts = new();

        foreach (var table in query.Tables)
        {
            var variableName = context.GetVariableName(table);
            variableDefinitionParts.Add(BuildTableVariableDefinition(variableName, table, columntTypeProvider));
            var columnNames = string.Join(", ", GetUsedColumns(table).Order().Select(c => $"current.{c}"));

            List<string> querySubparts = new()
            {
$$"""
INSERT INTO {{variableName}}
    SELECT {{columnNames}} 
        FROM {{table.TableName}} AS current
"""
            };

            // Joined table
            if (table.JoinConditions.Count != 0)
            {
                var parentTable = table.JoinConditions.Select(c => c.LeftTable).Distinct().Single();
                var connections = table.JoinConditions;
                var joinConditionParts = connections
                    .Select(c => $"previous.{c.FieldLeft} = current.{c.FieldRight}")
                    .ToArray();
                if (!IsPKJoin(parentTable.TableName, connections.Select(c => c.FieldLeft), columntTypeProvider) &&
                    !IsPKJoin(table.TableName, connections.Select(c => c.FieldRight), columntTypeProvider))
                {
                    throw new NotSupportedException("At least one part of join must point to PK.");
                }
                string previousSource;
                if (IsPKJoin(table.TableName, connections.Select(c => c.FieldRight), columntTypeProvider))
                    previousSource = $"(SELECT DISTINCT {string.Join(", ", connections.Select(c => c.FieldLeft))} FROM {context.GetVariableName(parentTable)})";
                else
                    previousSource = context.GetVariableName(parentTable);
                var joinPart =
$$"""
            INNER JOIN {{previousSource}} AS previous ON
                {{string.Join(" AND\r\n                ", joinConditionParts)}}
""";
                querySubparts.Add(joinPart);
            }
            else
                querySubparts.Add(
"""
        WHERE Id = @pKeyId
""");

            querySubparts.Add(
$$"""
SELECT * FROM {{variableName}}
""");

            var constructedQuery = string.Join("\r\n", querySubparts);
            queryParts.Add(constructedQuery);
        }

        var variableDefinitions = string.Join("\r\n", variableDefinitionParts);
        var queries = string.Join("\r\n\r\n", queryParts);
        return string.Join("\r\n\r\n", new[] { variableDefinitions, queries });
    }

    private static IEnumerable<string> GetUsedColumns(TableNode table)
    {
        if (table.JoinConditions.Count == 0)
            return table.UsedColumns.Concat(new[] { "Id" }).Distinct();
        return table.UsedColumns;
    }


    private static string BuildTableVariableDefinition(string variableName, TableNode table, IDbColumnTypeProvider columntTypeProvider)
    {
        var columnsWithTypes = GetUsedColumns(table).Order().Select(c => $"{c} {columntTypeProvider.GetSqlType(table.TableName, c)}").ToArray();
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

    private static IEnumerable<ColumpPathSet> GroupPaths(IEnumerable<ColumnPath> paths)
    {
        Dictionary<TablePathItem, HashSet<string>> dic = new();
        foreach (var path in paths)
        {
            if (!dic.TryGetValue(path.Path, out var fields))
            {
                fields = new HashSet<string>();
                dic.Add(path.Path, fields);
            }
            dic[path.Path].Add(path.ColumnName);
        }

        return dic.Select(p => new ColumpPathSet(p.Key, p.Value.Order().ToList()));
    }

    class TablePathItem
    {
        public TablePathItem Parent { get; set; }
        public TableNode Table { get; set; }
        public List<TablePathItemChild> Children { get; } = new List<TablePathItemChild>();

        public int Index
        {
            get
            {
                int counter = 0;
                var item = this;
                while (item != null)
                {
                    counter++;
                    item = item.Parent;
                }
                return counter;
            }
        }

        public string AsString
        {
            get
            {
                List<string> paths = new();
                var item = this;
                while (item != null)
                {
                    paths.Add(item.Table.TableName);
                    item = item.Parent;
                }
                paths.Reverse();
                return string.Join(" -> ", paths);
            }
        }

        public TablePathItem GetAt(TableNode table)
        {
            var top = this;
            while (top != null)
            {
                if (top.Table == table)
                    return top;
                top = top.Parent;
            }

            throw new NotSupportedException();
        }

        public TablePathItem AddNext(TableNode table, IEnumerable<TablePathConnection> connection)
        {
            var toReturn = new TablePathItem();
            toReturn.Table = table;
            toReturn.Parent = this;
            var child = new TablePathItemChild(toReturn, connection.ToList());
            Children.Add(child);
            return toReturn;
        }
    }

    private record TablePathItemChild(TablePathItem Item, List<TablePathConnection> NextConnections);

    private record TablePathConnection(string PreviousColumnName, string NextColumnName);

    [DebuggerDisplay("{Path.AsString,nq}, {ColumnName,nq}")]
    private record ColumnPath(TablePathItem Path, string ColumnName) : IComparable<ColumnPath>
    {
        public int CompareTo(ColumnPath other)
        {
            var result = Path.Index.CompareTo(other.Path.Index);
            if (result != 0)
                return result;
            result = Path.Table.TableName.CompareTo(other.Path.Table.TableName);
            if (result != 0)
                return result;
            return ColumnName.CompareTo(other.ColumnName);
        }
    }

    [DebuggerDisplay("{Path.AsString,nq}: {ColumnNamesAsString,nq}")]
    private record ColumpPathSet(TablePathItem Path, IReadOnlyList<string> ColumnNames) : IComparable<ColumpPathSet>
    {
        public string ColumnNamesAsString => string.Join(", ", ColumnNames);

        public int CompareTo(ColumpPathSet other)
        {
            var result = Path.Index.CompareTo(other.Path.Index);
            if (result != 0)
                return result;
            result = Path.Table.TableName.CompareTo(other.Path.Table.TableName);
            if (result != 0)
                return result;
            return 0;
        }
    }

    private class BuilderContext
    {
        private Dictionary<TableNode, string> _variableNames = new();
        public StringBuilder StringBuilder { get; } = new StringBuilder();

        public string GetVariableName(TableNode table)
        {
            if (!_variableNames.TryGetValue(table, out var name))
            {
                name = $"@table{table.TableName.Replace('.', '_')}";
                if (_variableNames.Values.Contains(name))
                {
                    int counter = 2;
                    while (true)
                    {
                        name = $"@table{table.TableName.Replace('.', '_')}_{counter++}";
                        if (!_variableNames.Values.Contains(name))
                            break;
                    }
                }
                _variableNames.Add(table, name);
            }
            return name;
        }
    }
    */
}
