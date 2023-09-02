using System;
using System.Linq.Expressions;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes.Visitors;

class FilterBinaryExpressionVisitor
{
    private readonly VisitorContext _visitorContext;

    public FilterBinarySle FilterBinarySle { get; private set; }

    public FilterBinaryExpressionVisitor(ISimplifiedLinqExpression parentSle, VisitorContext context)
    {
        _visitorContext = context;
        FilterBinarySle = new FilterBinarySle();
        FilterBinarySle.ParentExpression = parentSle;
    }

    public void Visit(BinaryExpression binaryExpression)
    {
        var binarySle = new FilterBinarySle();
        switch (binaryExpression.NodeType)
        {
            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Divide:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.Multiply:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
                binarySle.Operator = FilterBinaryOperator.ChainOther;
                break;
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                binarySle.Operator = FilterBinaryOperator.FilterBinaryAnd;
                break;
            case ExpressionType.Equal:
                binarySle.Operator = FilterBinaryOperator.ChainsEqual;
                break;
            case ExpressionType.NotEqual:
                binarySle.Operator = FilterBinaryOperator.FilterBinaryOr;
                break;
            case ExpressionType.Or:
            case ExpressionType.OrElse:
                binarySle.Operator = FilterBinaryOperator.FilterBinaryOr;
                break;
            default:
                throw new NotSupportedException();
        }

        if (binaryExpression.Left is BinaryExpression leftBinaryExpression)
        {
            var leftVisitor = new FilterBinaryExpressionVisitor(FilterBinarySle, _visitorContext);
            FilterBinarySle.LeftExpression = leftVisitor.FilterBinarySle;
            leftVisitor.Visit(leftBinaryExpression);
        }
        else
        {
            var leftVisitor = new ChainExpressionVisitor(FilterBinarySle, _visitorContext);
            FilterBinarySle.LeftExpression = leftVisitor.Chain;
            leftVisitor.Visit(binaryExpression.Left);
        }

        if (binaryExpression.Right is BinaryExpression rightBinaryExpression)
        {
            var rightVisitor = new FilterBinaryExpressionVisitor(FilterBinarySle, _visitorContext);
            FilterBinarySle.RightExpression = rightVisitor.FilterBinarySle;
            rightVisitor.Visit(rightBinaryExpression);
        }
        else
        {
            var rightVisitor = new ChainExpressionVisitor(FilterBinarySle, _visitorContext);
            FilterBinarySle.RightExpression = rightVisitor.Chain;
            rightVisitor.Visit(binaryExpression.Right);
        }
    }
}
