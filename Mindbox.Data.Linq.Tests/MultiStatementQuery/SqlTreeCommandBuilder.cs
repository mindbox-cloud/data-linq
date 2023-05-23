using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;

internal class SqlTreeCommandBuilder
{
    public static string Build(MultiStatementQuery query, IDbColumnTypeProvider columntTypeProvider)
    {
        var context = new BuilderContext();
        List<string> queryParts = new();
        List<string> variableDefinitionParts = new();

        foreach (var table in query.Tables)
        {
            var variableName = context.GetVariableName(table);
            variableDefinitionParts.Add(BuildTableVariableDefinition(variableName, table, columntTypeProvider));
            var columnNames = string.Join(", ", table.UsedColumns.Order().Select(c => $"current.{c}"));


            List<string> querySubparts = new()
            {
$$"""
INSERT INTO {{variableName}}
    SELECT {{columnNames}} 
        FROM {{table.TableName}} AS current
"""
            };

            // Joint to parent
            if (table.JoinConditions != null)
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


    private static string BuildTableVariableDefinition(string variableName, TableNode table, IDbColumnTypeProvider columntTypeProvider)
    {
        var columnsWithTypes = table.UsedColumns.Order().Select(c => $"{c} {columntTypeProvider.GetSqlType(table.TableName, c)}").ToArray();
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

    //private static IEnumerable<ColumnPath> CollectPaths(MultiStatementQuery query)
    //{
    //    var path = new TablePathItem();
    //    path.Table = query.Tables.First();
    //    yield return new ColumnPath(path, "Id");
    //    foreach (var item in query.Tables.Skip(1))
    //    {

    //    }
    //    foreach (var columnPath in CollectPathsCore(path, query))
    //        yield return columnPath;

    //    static IEnumerable<ColumnPath> CollectPathsCore(TablePathItem path, TableNode node)
    //    {
    //        foreach (var usedColumn in node.UsedColumns)
    //        {
    //            yield return new ColumnPath(path.GetAt(node), usedColumn);

    //            /*
    //            if (child is SqlQueryTranslator.SqlDataFieldNode fieldAccessNode)
    //                yield return new ColumnPath(path.GetAt(fieldAccessNode.TableOwner), fieldAccessNode.ColumnName);
    //            else if (child is SqlQueryTranslator.SqlAssociationFieldNode associationFieldNode)
    //            {
    //                yield return new ColumnPath(path.GetAt(associationFieldNode.PreviousTableOwner), associationFieldNode.PreviousColumnName);
    //                var associationConnection = new List<TablePathConnection>() { new TablePathConnection(associationFieldNode.PreviousColumnName, associationFieldNode.ColumnName) };
    //                var next = path.GetAt(associationFieldNode.PreviousTableOwner)
    //                    .AddNext(associationFieldNode.TableOwner, new() { new(associationFieldNode.PreviousColumnName, associationFieldNode.ColumnName) });
    //                foreach (var associationPath in CollectPathsCore(next, associationFieldNode))
    //                    yield return associationPath;
    //                yield return new ColumnPath(next, associationFieldNode.ColumnName);
    //            }
    //            else
    //                foreach (var childPath in CollectPathsCore(path, child))
    //                    yield return childPath;
    //            */
    //        }
    //        foreach (var joinedTable in node.JoinedTables)
    //        {
    //            foreach (var innerPath in CollectPathsCore(path.GetAt(node).AddNext(joinedTable.RighTable, ConnectionFromJoinCondition(joinedTable.Conditions)), joinedTable.RighTable))
    //                yield return innerPath;
    //        }
    //    }

    //    static IEnumerable<TablePathConnection> ConnectionFromJoinCondition(IEnumerable<JoinCondition> joinConditions)
    //    {
    //        foreach (var joinCondtion in joinConditions)
    //            yield return new TablePathConnection(joinCondtion.FieldLeft, joinCondtion.FieldRight);
    //    }
    //}

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
}
