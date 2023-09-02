using System;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes.Visitors;

class SelectExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public SelectChainPart SelectSle { get; }

    public SelectExpressionVisitor(VisitorContext context)
    {
        SelectSle = new SelectChainPart();
        _visitorContext = context;

    }

    public void Visit(Expression expression)
    {
        expression = ExtractSelectLambdaBody(expression);
        switch (expression.NodeType)
        {
            case ExpressionType.New:
                SelectSle.ChainPartType = SelectChainPartType.Complex;
                var newExpression = (NewExpression)expression;
                for (int i = 0; i < newExpression.Members.Count; i++)
                {
                    var member = newExpression.Members[i];
                    var arg = newExpression.Arguments[i];
                    var visitor = new ChainExpressionVisitor(SelectSle, _visitorContext);
                    visitor.Visit(arg);
                    SelectSle.NamedChains.Add(member.Name, visitor.Chain);
                }
                break;
            case ExpressionType.MemberInit:
                var memberInitExpression = (MemberInitExpression)expression;
                for (int i = 0; i < memberInitExpression.Bindings.Count; i++)
                {
                    var binding = (MemberAssignment)memberInitExpression.Bindings[i];
                    var visitor = new ChainExpressionVisitor(SelectSle, _visitorContext);
                    visitor.Visit(binding.Expression);
                    SelectSle.NamedChains.Add(binding.Member.Name, visitor.Chain);
                }
                break;
            default:
                var simpleVisitor = new ChainExpressionVisitor(SelectSle, _visitorContext);
                simpleVisitor.Visit(expression);
                SelectSle.NamedChains.Add(string.Empty, simpleVisitor.Chain);
                break;
        }
    }

    private Expression ExtractSelectLambdaBody(Expression expression)
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
