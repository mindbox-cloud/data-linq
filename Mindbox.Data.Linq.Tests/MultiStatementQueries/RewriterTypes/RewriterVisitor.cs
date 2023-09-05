using Mindbox.Data.Linq.Tests.MultiStatementQueries.SleTypes;
using Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.RewriterTypes;


internal class RewriterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _resultSetParameter;
    private readonly MethodInfo _getTableMethodInfo;
    private readonly MethodInfo _getValueMethodInfo;
    private readonly MethodInfo _getReferencedRowsMethodInfo;
    private readonly MethodInfo _getReferencedRowMethodInfo;
    private Dictionary<ParameterExpression, ParameterExpression> _parameterMapping = new();
    private AssemblyName _dynamicAssemblyName;
    private AssemblyBuilder _dynamicAssembly;
    private ModuleBuilder _dynamicModule;
    private int _dynamicTypeCounter;
    private Dictionary<Type, Type> _anonToRewrittenType = new();

    public RewriterVisitor(ParameterExpression resultSetParameter)
    {
        _resultSetParameter = resultSetParameter;
        _getTableMethodInfo = typeof(ResultSet).GetMethod(nameof(ResultSet.GetTable));
        _getValueMethodInfo = typeof(ResultRow).GetMethod(nameof(ResultRow.GetValue));
        _getReferencedRowsMethodInfo = typeof(ResultRow).GetMethod(nameof(ResultRow.GetReferencedRows));
        _getReferencedRowMethodInfo = typeof(ResultRow).GetMethod(nameof(ResultRow.GetReferencedRow));
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        var tableName = ExpressionHelpers.GetTableName(node);
        if (!string.IsNullOrEmpty(tableName))
            return Expression.Call(_resultSetParameter, _getTableMethodInfo, Expression.Constant(tableName));
        return base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var tableName = ExpressionHelpers.GetTableName(node);
        if (!string.IsNullOrEmpty(tableName))
            return Expression.Call(_resultSetParameter, _getTableMethodInfo, Expression.Constant(tableName));

        var tableAttribute = node.Member.DeclaringType.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute));
        if (tableAttribute != null)
        {
            var objectExpression = Visit(node.Expression);
            if (node.Member is PropertyInfo property)
            {
                var associationAttribute = property.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(AssociationAttribute));
                if (associationAttribute != null)
                {
                    var otherTable = GetTypeOrElementType(property.PropertyType).CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute))
                        .NamedArguments.Single(a => a.MemberName == nameof(TableAttribute.Name)).TypedValue.Value.ToString();
                    var currentTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.ThisKey)).TypedValue.Value.ToString();
                    var otherTableField = associationAttribute.NamedArguments.Single(a => a.MemberName == nameof(AssociationAttribute.OtherKey)).TypedValue.Value.ToString();
                    if (IsCollection(property.PropertyType))
                        return Expression.Call(objectExpression, _getReferencedRowsMethodInfo, Expression.Constant(otherTable), Expression.Constant(currentTableField), Expression.Constant(otherTableField));
                    else
                        return Expression.Call(objectExpression, _getReferencedRowMethodInfo, Expression.Constant(otherTable), Expression.Constant(currentTableField), Expression.Constant(otherTableField));
                }
            }

            return Expression.Call(objectExpression, _getValueMethodInfo.MakeGenericMethod(node.Type), Expression.Constant(node.Member.Name));
        }
        else if (_anonToRewrittenType.ContainsKey(node.Member.DeclaringType))
        {
            var rewrittenType = _anonToRewrittenType[node.Member.DeclaringType];
            if (node.Member is not PropertyInfo property)
                throw new NotSupportedException();
            var objectExpression = Visit(node.Expression);
            return Expression.MakeMemberAccess(objectExpression, rewrittenType.GetProperty(property.Name));
        }
        return base.VisitMember(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (_parameterMapping.ContainsKey(node))
            return _parameterMapping[node];
        var tableAttribute = node.Type.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute));
        if (tableAttribute != null)
        {
            var mappedParameter = Expression.Parameter(typeof(ResultRow), node.Name);
            _parameterMapping.Add(node, mappedParameter);
            return mappedParameter;
        }
        else if (_anonToRewrittenType.ContainsKey(node.Type))
        {
            var mappedParameter = Expression.Parameter(_anonToRewrittenType[node.Type], node.Name);
            _parameterMapping.Add(node, mappedParameter);
            return mappedParameter;
        }

        return base.VisitParameter(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Enumerable))
        {
            List<Type> parameterTypes = new();
            foreach (var parameterInfo in node.Method.GetParameters())
            {
                var tableAttribute = parameterInfo.ParameterType.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute));
                if (tableAttribute != null)
                    parameterTypes.Add(typeof(ResultRow));
                else
                    parameterTypes.Add(parameterInfo.ParameterType);
            }
            List<Expression> parameters = new();
            foreach (var parameter in node.Arguments)
                parameters.Add(Visit(parameter));

            List<Type> genericArgs = new();
            foreach (var argType in node.Method.GetGenericArguments())
            {
                var tableAttribute = argType.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute));
                if (tableAttribute != null)
                    genericArgs.Add(typeof(ResultRow));
                else
                    genericArgs.Add(_anonToRewrittenType.TryGetValue(argType, out var rewrittenType) ? rewrittenType : argType);
            }

            var rewrittenMethodInfo = RewriteMethodDefinition(node.Method, genericArgs.ToArray());
            return Expression.Call(node.Object, rewrittenMethodInfo, parameters.ToArray());
        }
        return base.VisitMethodCall(node);
    }

    private MethodInfo RewriteMethodDefinition(MethodInfo method, IEnumerable<Type> genericArgs)
    {
        return method.GetGenericMethodDefinition().MakeGenericMethod(genericArgs.ToArray());
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        return base.VisitUnary(node);
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        return base.VisitMemberInit(node);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        bool hasMembersToRewrite = false;
        foreach (var item in node.Members)
        {
            if (item is not PropertyInfo property)
                continue;
            if (property.PropertyType.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute)) == null)
                continue;
            hasMembersToRewrite = true;
            break;
        }

        if (!hasMembersToRewrite)
            return node;

        if (!_anonToRewrittenType.ContainsKey(node.Type))
        {
            // Create new assembly
            _dynamicAssemblyName ??= new AssemblyName("MyAsm");
            _dynamicAssembly ??= AssemblyBuilder.DefineDynamicAssembly(_dynamicAssemblyName, AssemblyBuilderAccess.Run);
            _dynamicModule ??= _dynamicAssembly.DefineDynamicModule("MyAsm");

            // Create types
            TypeBuilder dynamicAnonymousType = _dynamicModule.DefineType($"MyAnon_{_dynamicTypeCounter++}", TypeAttributes.Public);
            List<Type> propertyTypes = new();
            List<FieldBuilder> fields = new();
            // Add fields and properties
            foreach (var item in node.Members)
            {
                if (item is not PropertyInfo property)
                    throw new NotSupportedException();
                Type fieldType;
                if (property.PropertyType.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(TableAttribute)) == null)
                    fieldType = property.PropertyType;
                else if (IsCollection(property.PropertyType))
                    fieldType = typeof(IEnumerable<ResultRow>);
                else
                    fieldType = typeof(ResultRow);

                propertyTypes.Add(fieldType);
                var field = dynamicAnonymousType.DefineField($"__{property.Name}", fieldType, FieldAttributes.Private);
                fields.Add(field);
                var propertyBuilder = dynamicAnonymousType.DefineProperty(property.Name, property.Attributes, fieldType, null);
                // The property set and property get methods require a special set of attributes.
                MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

                // Define the "get" accessor method for CustomerName.
                MethodBuilder getBuilder = dynamicAnonymousType.DefineMethod($"get_{property.Name}", getSetAttr, fieldType, Type.EmptyTypes);

                ILGenerator getBuilderIL = getBuilder.GetILGenerator();
                getBuilderIL.Emit(OpCodes.Ldarg_0);
                getBuilderIL.Emit(OpCodes.Ldfld, field);
                getBuilderIL.Emit(OpCodes.Ret);

                // Define the "set" accessor method for CustomerName.
                MethodBuilder setBuilder = dynamicAnonymousType.DefineMethod($"set_{property.Name}", getSetAttr, null, new Type[] { fieldType });

                ILGenerator setBuilderIL = setBuilder.GetILGenerator();
                setBuilderIL.Emit(OpCodes.Ldarg_0);
                setBuilderIL.Emit(OpCodes.Ldarg_1);
                setBuilderIL.Emit(OpCodes.Stfld, field);
                setBuilderIL.Emit(OpCodes.Ret);

                // Last, we must map the two methods created above to our PropertyBuilder to
                // their corresponding behaviors, "get" and "set" respectively.
                propertyBuilder.SetGetMethod(getBuilder);
                propertyBuilder.SetSetMethod(setBuilder);
            }
            var typeConstructor = dynamicAnonymousType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, propertyTypes.ToArray());

            var ctorIL = typeConstructor.GetILGenerator();
            for (var x = 0; x < fields.Count; x++)
            {
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Ldarg_S, x + 1);
                ctorIL.Emit(OpCodes.Stfld, fields[x]);
            }
            ctorIL.Emit(OpCodes.Ret);

            var createdType = dynamicAnonymousType.CreateType();
            _anonToRewrittenType.Add(node.Type, createdType);
        }
        var type = _anonToRewrittenType[node.Type];

        List<MemberInfo> members = new();
        foreach (var item in node.Members)
        {
            if (item is not PropertyInfo property)
                throw new NotSupportedException();
            members.Add(type.GetProperty(property.Name));
        }

        List<Expression> arguments = new();
        foreach (var arg in node.Arguments)
            arguments.Add(Visit(arg));

        // Return the type to the caller
        return Expression.New(type.GetConstructors().Single(), arguments.ToArray(), members.ToArray());
    }

    private static Type GetTypeOrElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        if (type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableTo(typeof(IEnumerable<>)))
            throw new NotImplementedException();
        return type;
    }

    private static bool IsCollection(Type type)
    {
        if (type.IsArray)
            return true;
        if (type.IsGenericType)
            return type.GetGenericTypeDefinition().IsAssignableTo(typeof(IEnumerable<>));
        return false;
    }

    [return: NotNullIfNotNull("node")]
    public override Expression Visit(Expression node)
    {
        if (node is LambdaExpression lambda)
        {
            List<ParameterExpression> parameters = new();
            foreach (var item in lambda.Parameters)
                parameters.Add((ParameterExpression)Visit(item));
            var body = Visit(lambda.Body);
            return Expression.Lambda(body, parameters);
        }
        return base.Visit(node);
    }
}
