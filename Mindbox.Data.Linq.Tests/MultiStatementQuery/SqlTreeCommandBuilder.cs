using System;
using System.Linq;
using System.Text;

namespace Mindbox.Data.Linq.Tests.MultiStatementQuery;

internal class SqlTreeCommandBuilder
{
    public static string Build(SqlQueryTranslator2.SqlTree sqlTree)
    {
        var context = new BuilderContext();

        var tableVariable = context.GenerateIntermediateTableVariableName(sqlTree.Table.Name);
        var fields = string.Join(", ", sqlTree.Table.Fields);
        context.StringBuilder.Append($$"""
            INSERT INTO {{tableVariable}}
                SELECT {{fields}} FROM {{sqlTree.Table.Name}} WHERE Id = @pKeyId
            SELECT * FROM {{tableVariable}}
            """);

        foreach (var linkedTable in sqlTree.Table.LinkedTables)
            BuildCore(context, tableVariable, linkedTable);


        return context.StringBuilder.ToString();
    }

    private static void BuildCore(BuilderContext context, string previosTableVariable, SqlQueryTranslator2.SqlTableLink tableLink)
    {
        var table = tableLink.RightTable;
        var tableVariable = context.GenerateIntermediateTableVariableName(table.Name);
        var fields = string.Join(", ", table.Fields.Select(f => $"right.{f}"));
        var joinCondition = string.Join(
            $"{Environment.NewLine}            ",
            tableLink.Connections.Select(c => $"left.{c.LeftFieldName} == right.{c.RightFieldName}"));
        context.StringBuilder.AppendLine();
        context.StringBuilder.AppendLine();
        context.StringBuilder.Append($$"""
            INSERT INTO {{tableVariable}}
                SELECT {{fields}} FROM {{table.Name}} AS right
                    INNER JOIN {{previosTableVariable}} AS left ON
                        {{joinCondition}}
            SELECT * FROM {{tableVariable}}
            """);


        foreach (var linkedTable in table.LinkedTables)
            BuildCore(context, tableVariable, linkedTable);
    }

    private class BuilderContext
    {
        private int _counter;
        public StringBuilder StringBuilder { get; } = new StringBuilder();

        public string GenerateIntermediateTableVariableName(string namePart)
        {
            return $"@table{namePart.Replace('.', '_')}_{++_counter}";
        }
    }


}
