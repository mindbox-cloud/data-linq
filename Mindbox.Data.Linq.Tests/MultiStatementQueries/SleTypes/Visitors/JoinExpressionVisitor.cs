using System;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes.Visitors;

class JoinExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public JoinChainPart JoinSle { get; }

    public JoinExpressionVisitor(VisitorContext context)
    {
        JoinSle = new JoinChainPart();
        _visitorContext = context;

    }

    public void Visit(MethodCallExpression methodCallExpression)
    {
        var innerVisitor = new ChainExpressionVisitor(JoinSle, _visitorContext);
        innerVisitor.Visit(methodCallExpression.Arguments[1]);
        JoinSle.Inner = innerVisitor.Chain;

        _visitorContext.ParameterToSle.Add(ExtractParameterVariableFromSelectExpression(methodCallExpression.Arguments[2]), GetLastRowSource(JoinSle.Chain));
        var outerKeyVisitor = new ChainExpressionVisitor(JoinSle, _visitorContext);
        outerKeyVisitor.Visit(ExtractLambdaBody(methodCallExpression.Arguments[2]));
        JoinSle.OuterKeySelectorSle = outerKeyVisitor.Chain;

        _visitorContext.ParameterToSle.Add(ExtractParameterVariableFromSelectExpression(methodCallExpression.Arguments[3]), GetLastRowSource(JoinSle.Inner));
        var innerKeyVisitor = new ChainExpressionVisitor(JoinSle, _visitorContext);
        innerKeyVisitor.Visit(ExtractLambdaBody(methodCallExpression.Arguments[3]));
        JoinSle.InnerKeySelectorSle = innerKeyVisitor.Chain;

        _visitorContext.ParameterToSle.Add(ExtractParameterVariableFromSelectExpression(methodCallExpression.Arguments[4], 0), GetLastRowSource(JoinSle.Chain));
        _visitorContext.ParameterToSle.Add(ExtractParameterVariableFromSelectExpression(methodCallExpression.Arguments[4], 1), GetLastRowSource(JoinSle.Inner));
        var resultSelector = ExtractLambdaBody(methodCallExpression.Arguments[4]);
        switch (resultSelector.NodeType)
        {
            case ExpressionType.New:
                JoinSle.ChainPartType = SelectChainPartType.Complex;
                var newExpression = (NewExpression)resultSelector;
                for (int i = 0; i < newExpression.Members.Count; i++)
                {
                    var member = newExpression.Members[i];
                    var arg = newExpression.Arguments[i];
                    var visitor = new ChainExpressionVisitor(JoinSle, _visitorContext);
                    visitor.Visit(arg);
                    JoinSle.NamedChains.Add(member.Name, visitor.Chain);
                }
                break;
            case ExpressionType.MemberInit:
                var memberInitExpression = (MemberInitExpression)resultSelector;
                for (int i = 0; i < memberInitExpression.Bindings.Count; i++)
                {
                    var binding = (MemberAssignment)memberInitExpression.Bindings[i];
                    var visitor = new ChainExpressionVisitor(JoinSle, _visitorContext);
                    visitor.Visit(binding.Expression);
                    JoinSle.NamedChains.Add(binding.Member.Name, visitor.Chain);
                }
                break;
            default:
                var simpleVisitor = new ChainExpressionVisitor(JoinSle, _visitorContext);
                simpleVisitor.Visit(resultSelector);
                JoinSle.NamedChains.Add(string.Empty, simpleVisitor.Chain);
                break;
        }
    }

    private IChainPart GetLastRowSource(ChainSle chain)
    {
        for (int i = chain.Items.Count - 1; i >= 0; i--)
        {
            if (chain.Items[i] == JoinSle)
                continue;
            if (chain.Items[i] is IRowSourceChainPart rowSource)
                return rowSource;
            if (chain.Items[i] is ReferenceRowSourceChainPart referenceRowSource)
                return referenceRowSource;
        }
        throw new NotSupportedException();
    }

    private ParameterExpression ExtractParameterVariableFromSelectExpression(Expression filterExpression, int parameterIndex = 0)
    {
        var unary = (UnaryExpression)filterExpression;
        if (unary.NodeType != ExpressionType.Quote || unary.IsLifted || unary.IsLiftedToNull || unary.Method != null)
            throw new NotSupportedException();
        var lambda = (LambdaExpression)unary.Operand;
        if (lambda.TailCall || !string.IsNullOrEmpty(lambda.Name) || lambda.Parameters.Count <= parameterIndex)
            throw new NotSupportedException();
        return lambda.Parameters[parameterIndex];
    }


    private Expression ExtractLambdaBody(Expression expression)
    {
        if (expression is UnaryExpression unary)
        {
            if (unary.Method != null)
                throw new InvalidOperationException();
            if (unary.IsLifted || unary.IsLiftedToNull)
                throw new InvalidOperationException();
            return ((LambdaExpression)unary.Operand).Body;
        }
        return ((LambdaExpression)expression).Body;
    }
}