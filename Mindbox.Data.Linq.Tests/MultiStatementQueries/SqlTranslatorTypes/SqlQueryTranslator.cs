using Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;

class SqlQueryTranslator
{
    public static SqlQueryTranslatorResult Translate(Expression node, IDbColumnTypeProvider columnTypeProvider)
    {
        var table = TranslateCore(node, columnTypeProvider);
        OptimizeTree(table);

        var command = SqlTreeCommandBuilder.Build(table, columnTypeProvider);

        return new SqlQueryTranslatorResult(command);
    }

    private static TableNode TranslateCore(Expression expression, IDbColumnTypeProvider columnTypeProvider)
    {
        var rootSle = TranslateToSimplifiedExpression(expression, columnTypeProvider);
        var context = new TranslationContext(columnTypeProvider);
        TranslateChain(context, rootSle);

        // Ensure that all tables are port of join
        foreach (var table in context.Node2ChainParts.Keys)
            if (!context.RootTable.GetAllTableNodes().Contains(table))
                throw new InvalidOperationException($"No connection condition was found for table {table.Name}.");

        return context.RootTable;
    }

    private static void TranslateChain(TranslationContext context, ChainSle chain)
    {
        ISelectChainPart currentSelectChainPart = null;
        TableNode currentTable = null!;
        foreach (var chainItem in chain.Items)
        {
            if (chainItem is TableChainPart tableChainPart)
            {
                currentTable = new TableNode(tableChainPart.Name);
                currentSelectChainPart = null;
                context.AddChainPartForNode(currentTable, tableChainPart);
                if (context.RootTable == null)
                    context.RootTable = currentTable;
            }
            else if (chainItem is SelectChainPart selectChainPart)
            {
                foreach (var innerChain in selectChainPart.NamedChains.Values)
                    TranslateChain(context, innerChain);
            }
            else if (chainItem is JoinChainPart joinChainPart)
            {
                TranslateChain(context, joinChainPart.Inner);
                foreach (var innerChain in joinChainPart.NamedChains.Values)
                    TranslateChain(context, innerChain);
                DetectConnections(context, joinChainPart);
            }
            else if (chainItem is AssociationChainPart associationChainPart)
            {
                if (currentSelectChainPart != null)
                    switch (currentSelectChainPart.ChainPartType)
                    {
                        case SelectChainPartType.Simple:
                            currentTable = context.GetTableNodeByTablePart(currentSelectChainPart.NamedChains[""].Items.Last());
                            if (currentTable == null)
                                throw new NotSupportedException();
                            currentSelectChainPart = null;
                            var associationTable = new TableNode(associationChainPart.NextTableName);
                            currentTable.AddConnection(new[] { associationChainPart.ColumnName }, associationTable, new string[] { associationChainPart.NextTableColumnName });
                            currentTable = associationTable;
                            currentSelectChainPart = null;
                            break;
                        case SelectChainPartType.Complex:
                            throw new NotSupportedException();
                        default:
                            throw new NotSupportedException();
                    }
                else
                {
                    if (currentTable == null)
                        throw new NotSupportedException();
                    var associationTable = new TableNode(associationChainPart.NextTableName);
                    currentTable.AddConnection(new[] { associationChainPart.ColumnName }, associationTable, new string[] { associationChainPart.NextTableColumnName });
                    currentTable = associationTable;
                    currentSelectChainPart = null;
                }
            }
            else if (chainItem is FilterChainPart filterChainPart)
            {
                if (filterChainPart.InnerExpression is ChainSle filterChainInnerExpressionAsChain)
                    TranslateChain(context, filterChainInnerExpressionAsChain);
                else
                    TranslateTree(context, (ITreeNodeSle)filterChainPart.InnerExpression);
            }
            else if (chainItem is ReferenceRowSourceChainPart referenceRowSourceChainPart)
            {
                if (referenceRowSourceChainPart.ReferenceRowSource is TableChainPart tableReferenceRowSource)
                {
                    if (currentTable != null)
                        throw new InvalidOperationException();
                    var tableNodeByReference = context.GetTableNodeByTablePart(tableReferenceRowSource);
                    if (tableNodeByReference == null)
                        throw new InvalidOperationException();
                    currentTable = tableNodeByReference;
                    currentSelectChainPart = null;
                }
                else if (referenceRowSourceChainPart.ReferenceRowSource is ISelectChainPart selectReferenceRowSource)
                {
                    currentTable = null;
                    currentSelectChainPart = selectReferenceRowSource;
                }
                else if (referenceRowSourceChainPart.ReferenceRowSource is AssociationChainPart associationReferenceRowSource)
                {
                    var tableNodeByReference = context.GetTableNodeByTablePart(associationReferenceRowSource);
                    if (tableNodeByReference == null)
                        throw new InvalidOperationException();
                    currentTable = tableNodeByReference;
                    currentSelectChainPart = null;
                }
                else if (referenceRowSourceChainPart.ReferenceRowSource is PropertyAccessChainPart propertyAccessChainPart)
                {
                    if (UnwrapReferenceSources(propertyAccessChainPart.GetPrevious()) is SelectChainPart propertyForSelect)
                    {
                        if (!propertyForSelect.NamedChains.ContainsKey(propertyAccessChainPart.PropertyName))
                            throw new NotSupportedException();
                        currentTable = context.GetTableNodeByTablePart(propertyForSelect.NamedChains[propertyAccessChainPart.PropertyName].Items.Last());
                        if (currentTable == null)
                            throw new NotSupportedException();
                        currentSelectChainPart = null;
                    }
                    else
                        throw new NotSupportedException();
                }
                else
                    throw new NotSupportedException();
                continue;
            }
            else if (chainItem is ColumnAccessChainPart columnAccessChainPart)
            {
                if (currentSelectChainPart != null)
                    switch (currentSelectChainPart.ChainPartType)
                    {
                        case SelectChainPartType.Simple:
                            currentTable = context.GetTableNodeByTablePart(currentSelectChainPart.NamedChains[""].Items.Last());
                            if (currentTable == null)
                                throw new NotSupportedException();
                            currentSelectChainPart = null;
                            currentTable.AddField(columnAccessChainPart.ColumnName);
                            break;
                        case SelectChainPartType.Complex:
                            throw new NotSupportedException();
                        default:
                            throw new NotSupportedException();
                    }
                else
                {
                    if (currentTable == null)
                        throw new NotSupportedException();
                    currentTable.AddField(columnAccessChainPart.ColumnName);
                }
            }
            else if (chainItem is PropertyAccessChainPart propertyAccessChainPart)
            {
                if (currentSelectChainPart != null && currentSelectChainPart.NamedChains.ContainsKey(propertyAccessChainPart.PropertyName))
                {
                    currentTable = context.GetTableNodeByTablePart(UnwrapReferenceSources(currentSelectChainPart.NamedChains[propertyAccessChainPart.PropertyName].Items.Last()));
                    if (currentTable == null)
                        throw new NotSupportedException();
                    currentSelectChainPart = null;
                }
                else
                    throw new NotSupportedException();
            }
            else
                throw new NotSupportedException();

            if (currentTable != null)
                context.AddChainPartForNode(currentTable, chainItem);
        }
    }

    static void DetectConnections(TranslationContext context, JoinChainPart joinPart)
    {
        var left = GetTableAndField(context, joinPart.InnerKeySelectorSle);
        var right = GetTableAndField(context, joinPart.OuterKeySelectorSle);

        if (!left.HasValue || !right.HasValue)
            throw new NotImplementedException();
        if (context.RootTable.GetAllTableNodes().Contains(left.Value.Table))
        {
            var existingConnection = left.Value.Table.Connections.FirstOrDefault(c => c.OtherTable == right.Value.Table);
            if (existingConnection != null)
                existingConnection.AddFields(left.Value.Field, right.Value.Field);
            else
                left.Value.Table.AddConnection(new[] { left.Value.Field }, right.Value.Table, new[] { right.Value.Field });
        }
        else if (context.RootTable.GetAllTableNodes().Contains(right.Value.Table))
        {
            var existingConnection = right.Value.Table.Connections.FirstOrDefault(c => c.OtherTable == left.Value.Table);
            if (existingConnection != null)
                existingConnection.AddFields(right.Value.Field, left.Value.Field);
            else
                right.Value.Table.AddConnection(new[] { right.Value.Field }, left.Value.Table, new[] { left.Value.Field });
        }
        else
        {
            throw new NotImplementedException();
        }

    }

    private static IChainPart UnwrapReferenceSources(IChainPart chainPart)
    {
        while (true)
        {
            if (chainPart is ReferenceRowSourceChainPart referenceRowSourceChainPart)
                chainPart = referenceRowSourceChainPart.ReferenceRowSource;
            else
                return chainPart;
        }
    }

    private static void TranslateTree(TranslationContext context, ITreeNodeSle sle)
    {
        if (sle is FilterBinarySle filterBinary)
        {
            if (IsOperatorAgainstConstant(filterBinary, out var nonConstantSle))
            {
                if (nonConstantSle is ChainSle nonConstantChainSle)
                    TranslateChain(context, nonConstantChainSle);
                else if (nonConstantSle is ITreeNodeSle treeNodeSle)
                    TranslateTree(context, treeNodeSle);
                else
                    throw new NotSupportedException();
            }
            else
            {
                TranslateCore(context, filterBinary.LeftExpression);
                TranslateCore(context, filterBinary.RightExpression);
            }
            DetectConnections(context, filterBinary);
        }
        else
            throw new NotSupportedException();



        static void DetectConnections(TranslationContext context, FilterBinarySle filterBinary)
        {
            // Processing only simple join conditions, like c.Id == b.CustomerId
            if (!filterBinary.IsTopLevelChainEqualityStatement())
                return;
            var left = GetTableAndField(context, filterBinary.LeftExpression);
            var right = GetTableAndField(context, filterBinary.RightExpression);

            if (!left.HasValue || !right.HasValue)
                return;
            if (context.RootTable.GetAllTableNodes().Contains(left.Value.Table))
            {
                var existingConnection = left.Value.Table.Connections.FirstOrDefault(c => c.OtherTable == right.Value.Table);
                if (existingConnection != null)
                    existingConnection.AddFields(left.Value.Field, right.Value.Field);
                else
                    left.Value.Table.AddConnection(new[] { left.Value.Field }, right.Value.Table, new[] { right.Value.Field });
            }
            else if (context.RootTable.GetAllTableNodes().Contains(right.Value.Table))
            {
                var existingConnection = right.Value.Table.Connections.FirstOrDefault(c => c.OtherTable == left.Value.Table);
                if (existingConnection != null)
                    existingConnection.AddFields(right.Value.Field, left.Value.Field);
                else
                    right.Value.Table.AddConnection(new[] { right.Value.Field }, left.Value.Table, new[] { left.Value.Field });
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        static void TranslateCore(TranslationContext context, ISimplifiedLinqExpression expression)
        {
            if (expression is FilterBinarySle filterBinary)
                TranslateTree(context, filterBinary);
            else
                TranslateChain(context, (ChainSle)expression);
        }
    }

    private static (TableNode Table, string Field)? GetTableAndField(TranslationContext context, ISimplifiedLinqExpression sle)
    {
        // So far only supported
        // - [variable] - assumes PK access
        // - [variable].[fieldName] 
        // - [variable].[association] 
        // - [variable].[association].[fieldName] - fieldName is PK
        if (sle is not ChainSle chain)
            return null;
        switch (chain.Items.Count)
        {
            case 1:
                if (chain.Items[0] is ReferenceRowSourceChainPart variableAsReference)
                    return (GetTableNode(context, variableAsReference), context.ColumnTypeProvider.GetPKFields(GetTableNode(context, variableAsReference).Name).Single());
                return null;
            case 2:
                {
                    if (chain.Items[0] is not ReferenceRowSourceChainPart referenceChainPart)
                        return null;
                    if (chain.Items[1] is ColumnAccessChainPart columnAccessChain)
                        return (GetTableNode(context, referenceChainPart), columnAccessChain.ColumnName);
                    else if (chain.Items[1] is AssociationChainPart associationChainPart)
                        return (GetTableNode(context, referenceChainPart), associationChainPart.ColumnName);
                    return null;
                }
            case 3:
                {
                    if (chain.Items[0] is not ReferenceRowSourceChainPart referenceChainPart)
                        return null;
                    if (chain.Items[1] is not AssociationChainPart associationChainPart)
                        return null;
                    if (chain.Items[2] is not ColumnAccessChainPart columnAccessChain)
                        return null;
                    if (columnAccessChain.ColumnName != context.ColumnTypeProvider.GetPKFields(associationChainPart.NextTableName).Single())
                        return null;
                    return (GetTableNode(context, referenceChainPart), associationChainPart.ColumnName);
                }
            default:
                return null;
        }

        static TableNode GetTableNode(TranslationContext context, ReferenceRowSourceChainPart referenceRowSourceChainPart)
        {
            if (referenceRowSourceChainPart.ReferenceRowSource is TableChainPart tableChainPart)
            {
                var tableNodeReference = context.GetTableNodeByTablePart(tableChainPart);
                if (tableNodeReference == null)
                    throw new InvalidOperationException();
                return tableNodeReference;
            }
            else if (referenceRowSourceChainPart.ReferenceRowSource is SelectChainPart selectChainPart)
            {
                switch (selectChainPart.ChainPartType)
                {
                    case SelectChainPartType.Simple:
                        var tableReferenceSimple = context.GetTableNodeByTablePart(selectChainPart.NamedChains[""].Items.Last());
                        if (tableReferenceSimple == null)
                            throw new InvalidOperationException();
                        return tableReferenceSimple;
                    case SelectChainPartType.Complex:
                        throw new NotSupportedException();
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (referenceRowSourceChainPart.ReferenceRowSource is AssociationChainPart associationChainPart)
            {
                var tableNodeReference = context.GetTableNodeByTablePart(associationChainPart);
                if (tableNodeReference == null)
                    throw new InvalidOperationException();
                return tableNodeReference;
            }
            else if (referenceRowSourceChainPart.ReferenceRowSource is PropertyAccessChainPart propertyAccessChainPart)
            {
                if (UnwrapReferenceSources(propertyAccessChainPart.GetPrevious()) is SelectChainPart propertyForSelect)
                {
                    if (!propertyForSelect.NamedChains.ContainsKey(propertyAccessChainPart.PropertyName))
                        throw new NotSupportedException();
                    var tableReferenceSimple = context.GetTableNodeByTablePart(propertyForSelect.NamedChains[propertyAccessChainPart.PropertyName].Items.Last());
                    if (tableReferenceSimple == null)
                        throw new InvalidOperationException();
                    return tableReferenceSimple;
                }
                else
                    throw new NotSupportedException();
                throw new NotSupportedException();

            }
            else
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Operator against constant.
    /// We can simply analysis only non constant part and drop constant sle.
    /// Examples: 'c.Password == "asdf"' or 'c.Age > 20' or 'c.CustomerActions.Where(c=>c.ActionTypeId == 12).Count() < 10'
    /// </summary>
    /// <param name="sle"></param>
    /// <param name="nonConstant"></param>
    /// <returns></returns>
    private static bool IsOperatorAgainstConstant(FilterBinarySle sle, out ISimplifiedLinqExpression nonConstant)
    {
        if (sle.LeftExpression is ChainSle leftChain && IsConstant(leftChain))
        {
            nonConstant = sle.RightExpression;
            return true;
        }

        if (sle.RightExpression is ChainSle rightChain && IsConstant(rightChain))
        {
            nonConstant = sle.LeftExpression;
            return true;
        }
        nonConstant = null;
        return false;

        static bool IsConstant(ChainSle chain) => chain.Items.Count == 1 && chain.Items[0] is FixedValueChainPart;
    }

    private static ChainSle TranslateToSimplifiedExpression(Expression expression, IDbColumnTypeProvider columnTypeProvider)
    {
        var visitorContext = new VisitorContext(columnTypeProvider);
        var visitor = new ChainExpressionVisitor(null, visitorContext);
        visitor.Visit(expression);
        return visitor.Chain;
    }

    private static void OptimizeTree(TableNode root)
    {
        while (true)
        {
            bool hasOptimization = false;
            foreach (var node in root.GetAllTableNodes())
            {
                hasOptimization = node.OptimizeConnections();
                if (hasOptimization)
                    break;
            }

            if (!hasOptimization)
                break;
        }

    }

    class TranslationContext
    {
        private Dictionary<ISimplifiedLinqExpression, string> _variableNames = new();

        public Dictionary<TableNode, List<IChainPart>> Node2ChainParts { get; } = new();
        public Dictionary<Connection, ChainSle> Connection2Chains { get; } = new();
        public IDbColumnTypeProvider ColumnTypeProvider { get; private set; }

        public TableNode RootTable { get; set; }

        public TranslationContext(IDbColumnTypeProvider columnTypeProvider)
        {
            ColumnTypeProvider = columnTypeProvider;
        }

        public void AddChainPartForNode(TableNode tableNode, IChainPart chainPart)
        {
            if (!Node2ChainParts.ContainsKey(tableNode))
                Node2ChainParts.Add(tableNode, new());
            if (Node2ChainParts[tableNode].Contains(chainPart))
                return;
            Node2ChainParts[tableNode].Add(chainPart);
        }

        public TableNode GetTableNodeByTablePart(IChainPart tableChainPart)
        {
            foreach (var (node, parts) in Node2ChainParts)
                foreach (var part in parts)
                    if (part == tableChainPart)
                        return node;
            return null;
        }

        public string GetVariableName(ISimplifiedLinqExpression sle, string tableName)
        {
            if (!_variableNames.TryGetValue(sle, out var name))
            {
                name = $"@table{tableName.Replace('.', '_')}";
                if (_variableNames.Values.Contains(name))
                {
                    int counter = 2;
                    while (true)
                    {
                        name = $"@table{tableName.Replace('.', '_')}_{counter++}";
                        if (!_variableNames.Values.Contains(name))
                            break;
                    }
                }
                _variableNames.Add(sle, name);
            }
            return name;
        }
    }
}