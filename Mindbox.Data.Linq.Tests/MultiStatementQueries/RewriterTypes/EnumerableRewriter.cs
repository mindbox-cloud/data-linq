using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.RewriterTypes;


internal class EnumerableRewriter : ExpressionVisitor
{
    public ReadOnlyDictionary<ParameterExpression, ParameterExpression> GetParameterReplacements()
        => parameterReplacements == null
            ? null
            : new ReadOnlyDictionary<ParameterExpression, ParameterExpression>(parameterReplacements);

    private Dictionary<ParameterExpression, ParameterExpression> parameterReplacements;
    protected override Expression VisitParameter(ParameterExpression par)
    {
        var type = par.Type;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
        {
            if (parameterReplacements == null)
                parameterReplacements = new Dictionary<ParameterExpression, ParameterExpression>();

            if (!parameterReplacements.TryGetValue(par, out var replacement))
            {
                var elementType = type.GetGenericArguments()[0];
                replacement = Expression.Parameter(
                    typeof(IEnumerable<>).MakeGenericType(elementType),
                    par.Name);
                parameterReplacements[par] = replacement;
            }

            return replacement;
        }
        return par;
    }

    // We must ensure that if a LabelTarget is rewritten that it is always rewritten to the same new target
    // or otherwise expressions using it won't match correctly.
    private Dictionary<LabelTarget, LabelTarget> _targetCache;
    // Finding equivalent types can be relatively expensive, and hitting with the same types repeatedly is quite likely.
    private Dictionary<Type, Type> _equivalentTypeCache;

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        var mInfo = m.Method;
        var typeArgs = (mInfo.IsGenericMethod) ? mInfo.GetGenericArguments() : null;
        if (mInfo.DeclaringType == typeof(Queryable))
        {
            var obj = Visit(m.Object);
            var args = Visit(m.Arguments);
            // convert Queryable method to Enumerable method
            var seqMethod = FindEnumerableMethod(mInfo.Name, args, typeArgs);
            args = FixupQuotedArgs(seqMethod, args);
            return Expression.Call(obj, seqMethod, args);
        }
        return m;
        /*
        var obj = Visit(m.Object);
        var args = Visit(m.Arguments);

        // check for args changed
        if (obj != m.Object || args != m.Arguments)
        {
            var mInfo = m.Method;
            var typeArgs = (mInfo.IsGenericMethod) ? mInfo.GetGenericArguments() : null;

            if ((mInfo.IsStatic || mInfo.DeclaringType.IsAssignableFrom(obj.Type))
                && ArgsMatch(mInfo, args, typeArgs))
            {
                // current method is still valid
                return Expression.Call(obj, mInfo, args);
            }
            else if (mInfo.DeclaringType == typeof(Queryable))
            {
                // convert Queryable method to Enumerable method
                var seqMethod = FindEnumerableMethod(mInfo.Name, args, typeArgs);
                args = FixupQuotedArgs(seqMethod, args);
                return Expression.Call(obj, seqMethod, args);
            }
            else
            {
                // rebind to new method
                var method = FindMethod(mInfo.DeclaringType, mInfo.Name, args, typeArgs);
                args = FixupQuotedArgs(method, args);
                return Expression.Call(obj, method, args);
            }
        }
        return m;
        */
    }

    private ReadOnlyCollection<Expression> FixupQuotedArgs(MethodInfo mi, ReadOnlyCollection<Expression> argList)
    {
        var pis = mi.GetParameters();
        if (pis.Length > 0)
        {
            List<Expression> newArgs = null;
            for (int i = 0, n = pis.Length; i < n; i++)
            {
                var arg = argList[i];
                var pi = pis[i];
                arg = FixupQuotedExpression(pi.ParameterType, arg);
                if (newArgs == null && arg != argList[i])
                {
                    newArgs = new List<Expression>(argList.Count);
                    for (var j = 0; j < i; j++)
                    {
                        newArgs.Add(argList[j]);
                    }
                }

                newArgs?.Add(arg);
            }
            if (newArgs != null)
                argList = newArgs.AsReadOnly();
        }
        return argList;
    }

    private Expression FixupQuotedExpression(Type type, Expression expression)
    {
        var expr = expression;
        while (true)
        {
            if (type.IsAssignableFrom(expr.Type))
                return expr;
            if (expr.NodeType != ExpressionType.Quote)
                break;
            expr = ((UnaryExpression)expr).Operand;
        }
        if (!type.IsAssignableFrom(expr.Type) && type.IsArray && expr.NodeType == ExpressionType.NewArrayInit)
        {
            var strippedType = StripExpression(expr.Type);
            if (type.IsAssignableFrom(strippedType))
            {
                var elementType = type.GetElementType();
                var na = (NewArrayExpression)expr;
                var exprs = new List<Expression>(na.Expressions.Count);
                for (int i = 0, n = na.Expressions.Count; i < n; i++)
                {
                    exprs.Add(FixupQuotedExpression(elementType, na.Expressions[i]));
                }
                expression = Expression.NewArrayInit(elementType, exprs);
            }
        }
        return expression;
    }

    protected override Expression VisitLambda<T>(Expression<T> node) => node;

    private static Type GetPublicType(Type t)
    {
        // If we create a constant explicitly typed to be a private nested type,
        // such as Lookup<,>.Grouping or a compiler-generated iterator class, then
        // we cannot use the expression tree in a context which has only execution
        // permissions.  We should endeavour to translate constants into
        // new constants which have public types.
        if (t.IsGenericType && t.GetGenericTypeDefinition().GetInterfaces().Contains(typeof(IGrouping<,>)))
            return typeof(IGrouping<,>).MakeGenericType(t.GetGenericArguments());
        if (!t.IsNestedPrivate)
            return t;
        foreach (var iType in t.GetInterfaces())
        {
            if (iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iType;
        }
        if (typeof(IEnumerable).IsAssignableFrom(t))
            return typeof(IEnumerable);
        return t;
    }

    private Type GetEquivalentType(Type type)
    {
        if (_equivalentTypeCache == null)
        {
            // Pre-loading with the non-generic IQueryable and IEnumerable not only covers this case
            // without any reflection-based introspection, but also means the slightly different
            // code needed to catch this case can be omitted safely.
            _equivalentTypeCache = new Dictionary<Type, Type>
                    {
                        { typeof(IQueryable), typeof(IEnumerable) },
                        { typeof(IEnumerable), typeof(IEnumerable) }
                    };
        }
        if (!_equivalentTypeCache.TryGetValue(type, out var equiv))
        {
            var pubType = GetPublicType(type);
            if (pubType.IsInterface && pubType.IsGenericType)
            {
                var genericType = pubType.GetGenericTypeDefinition();
                if (genericType == typeof(IOrderedEnumerable<>))
                    equiv = pubType;
                else if (genericType == typeof(IOrderedQueryable<>))
                    equiv = typeof(IOrderedEnumerable<>).MakeGenericType(pubType.GenericTypeArguments[0]);
                else if (genericType == typeof(IEnumerable<>))
                    equiv = pubType;
                else if (genericType == typeof(IQueryable<>))
                    equiv = typeof(IEnumerable<>).MakeGenericType(pubType.GenericTypeArguments[0]);
            }
            if (equiv == null)
            {
                var interfacesWithInfo = pubType.GetInterfaces().Select(IntrospectionExtensions.GetTypeInfo).ToArray();
                var singleTypeGenInterfacesWithGetType = interfacesWithInfo
                    .Where(i => i.IsGenericType && i.GenericTypeArguments.Length == 1)
                    .Select(i => new { Info = i, GenType = i.GetGenericTypeDefinition() })
                    .ToArray();
                var typeArg = singleTypeGenInterfacesWithGetType
                    .Where(i => i.GenType == typeof(IOrderedQueryable<>) || i.GenType == typeof(IOrderedEnumerable<>))
                    .Select(i => i.Info.GenericTypeArguments[0])
                    .Distinct()
                    .SingleOrDefault();
                if (typeArg != null)
                    equiv = typeof(IOrderedEnumerable<>).MakeGenericType(typeArg);
                else
                {
                    typeArg = singleTypeGenInterfacesWithGetType
                        .Where(i => i.GenType == typeof(IQueryable<>) || i.GenType == typeof(IEnumerable<>))
                        .Select(i => i.Info.GenericTypeArguments[0])
                        .Distinct()
                        .Single();
                    equiv = typeof(IEnumerable<>).MakeGenericType(typeArg);
                }
            }
            _equivalentTypeCache.Add(type, equiv);
        }
        return equiv;
    }



    private static ILookup<string, MethodInfo> s_seqMethods;
    private static MethodInfo FindEnumerableMethod(string name, ReadOnlyCollection<Expression> args, params Type[] typeArgs)
    {
        if (s_seqMethods == null)
        {
            s_seqMethods = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                             .ToLookup(m => m.Name);
        }
        var mi = s_seqMethods[name].FirstOrDefault(m => ArgsMatch(m, args, typeArgs));
        Debug.Assert(mi != null, "All static methods with arguments on Queryable have equivalents on Enumerable.");
        if (typeArgs != null)
            return mi.MakeGenericMethod(typeArgs);
        return mi;
    }

    private static MethodInfo FindMethod(Type type, string name, ReadOnlyCollection<Expression> args, Type[] typeArgs)
    {
        using (var en = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                            .Where(m => m.Name == name)
                            .GetEnumerator())
        {
            if (!en.MoveNext())
                throw new InvalidOperationException($"No method '{name}' on type '{type.FullName}'.");
            do
            {
                var mi = en.Current;
                if (ArgsMatch(mi, args, typeArgs))
                    return (typeArgs != null) ? mi.MakeGenericMethod(typeArgs) : mi;
            } while (en.MoveNext());
        }
        throw new InvalidOperationException($"No method '{name}{(typeArgs != null ? "<" + typeArgs + ">" : null)}' on type '{type.FullName}' matches arguments '{args}'.");
    }

    private static bool ArgsMatch(MethodInfo m, ReadOnlyCollection<Expression> args, Type[] typeArgs)
    {
        var mParams = m.GetParameters();
        if (mParams.Length != args.Count)
            return false;
        if (!m.IsGenericMethod && typeArgs != null && typeArgs.Length > 0)
        {
            return false;
        }
        if (!m.IsGenericMethodDefinition && m.IsGenericMethod && m.ContainsGenericParameters)
        {
            m = m.GetGenericMethodDefinition();
        }
        if (m.IsGenericMethodDefinition)
        {
            if (typeArgs == null || typeArgs.Length == 0)
                return false;
            if (m.GetGenericArguments().Length != typeArgs.Length)
                return false;
            m = m.MakeGenericMethod(typeArgs);
            mParams = m.GetParameters();
        }
        for (int i = 0, n = args.Count; i < n; i++)
        {
            var parameterType = mParams[i].ParameterType;
            if (parameterType == null)
                return false;
            if (parameterType.IsByRef)
                parameterType = parameterType.GetElementType();
            var arg = args[i];
            if (!parameterType.IsAssignableFrom(arg.Type))
            {
                if (arg.NodeType == ExpressionType.Quote)
                {
                    arg = ((UnaryExpression)arg).Operand;
                }
                if (!parameterType.IsAssignableFrom(arg.Type) &&
                    !parameterType.IsAssignableFrom(StripExpression(arg.Type)))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static Type StripExpression(Type type)
    {
        var isArray = type.IsArray;
        var tmp = isArray ? type.GetElementType() : type;
        var eType = GetExpressionType(tmp);
        if (eType != null)
            tmp = eType.GetGenericArguments()[0];
        if (isArray)
        {
            var rank = type.GetArrayRank();
            return (rank == 1) ? tmp.MakeArrayType() : tmp.MakeArrayType(rank);
        }
        return type;
    }

    private static Type GetExpressionType(Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Expression<>))
                return type;
            type = type.BaseType;
        }
        return null;
    }

    protected override Expression VisitConditional(ConditionalExpression c)
    {
        var type = c.Type;
        if (!typeof(IQueryable).IsAssignableFrom(type))
            return base.VisitConditional(c);
        var test = Visit(c.Test);
        var ifTrue = Visit(c.IfTrue);
        var ifFalse = Visit(c.IfFalse);
        var trueType = ifTrue.Type;
        var falseType = ifFalse.Type;
        if (trueType.IsAssignableFrom(falseType))
            return Expression.Condition(test, ifTrue, ifFalse, trueType);
        if (falseType.IsAssignableFrom(trueType))
            return Expression.Condition(test, ifTrue, ifFalse, falseType);
        return Expression.Condition(test, ifTrue, ifFalse, GetEquivalentType(type));
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        var type = node.Type;
        if (!typeof(IQueryable).IsAssignableFrom(type))
            return base.VisitBlock(node);
        var nodes = Visit(node.Expressions);
        var variables = VisitAndConvert(node.Variables, "EnumerableRewriter.VisitBlock");
        if (type == node.Expressions.Last().Type)
            return Expression.Block(variables, nodes);
        return Expression.Block(GetEquivalentType(type), variables, nodes);
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        var type = node.Value.Type;
        if (!typeof(IQueryable).IsAssignableFrom(type))
            return base.VisitGoto(node);
        var target = VisitLabelTarget(node.Target);
        var value = Visit(node.Value);
        return Expression.MakeGoto(node.Kind, target, value, GetEquivalentType(typeof(EnumerableQuery).IsAssignableFrom(type) ? value.Type : type));
    }

    protected override LabelTarget VisitLabelTarget(LabelTarget node)
    {
        LabelTarget newTarget;
        if (_targetCache == null)
            _targetCache = new Dictionary<LabelTarget, LabelTarget>();
        else if (_targetCache.TryGetValue(node, out newTarget))
            return newTarget;
        var type = node.Type;
        if (!typeof(IQueryable).IsAssignableFrom(type))
            newTarget = base.VisitLabelTarget(node);
        else
            newTarget = Expression.Label(GetEquivalentType(type), node.Name);
        _targetCache.Add(node, newTarget);
        return newTarget;
    }
}
