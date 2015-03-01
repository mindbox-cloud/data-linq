using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Mindbox.Data.Linq.Mapping;
using Mindbox.Expressions;

namespace Mindbox.Data.Linq.Proxy
{
	internal class EntityProxyInterceptor : IInterceptor
	{
		private static PropertyInfo TryGetPropertyInfoByGetter(MethodInfo getter)
		{
			if (getter == null)
				throw new ArgumentNullException("getter");

			var properties = getter.DeclaringType.GetTypeInfo().DeclaredProperties;
			return properties.SingleOrDefault(property => property.GetMethod == getter);
		}

		private static PropertyInfo TryGetPropertyInfoBySetter(MethodInfo setter)
		{
			if (setter == null)
				throw new ArgumentNullException("setter");

			var properties = setter.DeclaringType.GetTypeInfo().DeclaredProperties;
			return properties.SingleOrDefault(property => property.SetMethod == setter);
		}


		private readonly MindboxMetaModel model;
		private bool isSettingAfterEntityRefLoad;

		private readonly Dictionary<MethodInfo, object> entityRefsByGetMethod =
			new Dictionary<MethodInfo, object>();


		public EntityProxyInterceptor(MindboxMetaModel model)
		{
			if (model == null)
				throw new ArgumentNullException("model");

			this.model = model;
		}


		public void Intercept(IInvocation invocation)
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			var proxy = (IEntityProxy)invocation.Proxy;

			if (invocation.Method.DeclaringType == typeof(IEntityProxy))
			{
				if (invocation.Method.IsGenericMethod &&
					invocation.Method.GetGenericMethodDefinition() ==
						ReflectionExpressions
							.GetMethodInfo<IEntityProxy>(aProxy =>
								aProxy.SetEntityRef(default(MemberInfo), default(EntityRef<object>)))
							.GetGenericMethodDefinition())
				{
					var getMethod = (MethodInfo)invocation.Arguments[0];
					var entityRef = invocation.Arguments[1];
					ReflectionExpressions
						.GetMethodInfo<EntityProxyInterceptor>(interceptor =>
							interceptor.SetEntityRef(default(EntityRef<object>), default(MethodInfo), default(IEntityProxy)))
						.GetGenericMethodDefinition()
						.MakeGenericMethod(getMethod.ReturnType)
						.Invoke(
							this,
							new[]
							{
								entityRef, 
								getMethod, 
								proxy
							});
					return;
				}

				if (invocation.Method.IsGenericMethod &&
					invocation.Method.GetGenericMethodDefinition() ==
						ReflectionExpressions
							.GetMethodInfo<IEntityProxy>(aProxy => aProxy.GetEntityRef<object>(default(MemberInfo)))
							.GetGenericMethodDefinition())
				{
					var getMethod = (MethodInfo)invocation.Arguments[0];
					object entityRef;
					invocation.ReturnValue = entityRefsByGetMethod.TryGetValue(getMethod, out entityRef) ? 
						entityRef : 
						Activator.CreateInstance(typeof(EntityRef<>).MakeGenericType(getMethod.ReturnType));
					return;
				}
			}
			else
			{
				var property = TryGetPropertyInfoByGetter(invocation.Method) ?? TryGetPropertyInfoBySetter(invocation.Method);
				if (property != null)
				{
					var metaMember = (AttributedMetaDataMember)model.GetMetaType(proxy.GetType()).GetDataMember(property);
					if (metaMember.IsAssociation && metaMember.Association.IsForeignKey && metaMember.DoesRequireProxy)
					{
						if (invocation.Method == property.GetMethod)
						{
							ReflectionExpressions
								.GetMethodInfo<EntityProxyInterceptor>(interceptor =>
									interceptor.HandleEntityRefGetter<object>(default(IInvocation)))
								.GetGenericMethodDefinition()
								.MakeGenericMethod(invocation.Method.ReturnType)
								.Invoke(
									this, 
									new object[]
									{
										invocation
									});
						}
						else
						{
							ReflectionExpressions
								.GetMethodInfo<EntityProxyInterceptor>(interceptor =>
									interceptor.HandleEntityRefSetter<object>(default(IInvocation), default(MethodInfo)))
								.GetGenericMethodDefinition()
								.MakeGenericMethod(invocation.Method.GetParameters()[0].ParameterType)
								.Invoke(
									this,
									new object[]
									{
										invocation, 
										property.GetMethod
									});
						}
						return;
					}
				}
			}

			invocation.Proceed();
		}


		private EntityRef<TEntity> GetEntityRef<TEntity>(MethodInfo getMethod)
			where TEntity : class
		{
			if (getMethod == null)
				throw new ArgumentNullException("getMethod");

			object entityRef;
			return entityRefsByGetMethod.TryGetValue(getMethod, out entityRef) ? 
				(EntityRef<TEntity>)entityRef : 
				default(EntityRef<TEntity>);
		}

		private void HandleEntityRefGetter<TEntity>(IInvocation invocation)
			where TEntity : class
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			var entityRef = GetEntityRef<TEntity>(invocation.Method);
			if (!entityRef.HasLoadedOrAssignedValue)
			{
				var value = entityRef.Entity;
				entityRefsByGetMethod[invocation.Method] = entityRef;
				if (!isSettingAfterEntityRefLoad)
				{
					isSettingAfterEntityRefLoad = true;
					try
					{
						TryGetPropertyInfoByGetter(invocation.Method).SetValue(invocation.Proxy, value);
					}
					finally
					{
						isSettingAfterEntityRefLoad = false;
					}
				}
			}

			invocation.Proceed();
		}

		private void HandleEntityRefSetter<TEntity>(IInvocation invocation, MethodInfo getMethod)
			where TEntity : class
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			if (!isSettingAfterEntityRefLoad)
			{
				var entityRef = GetEntityRef<TEntity>(getMethod);
				entityRef.Entity = (TEntity)invocation.Arguments[0];
				entityRefsByGetMethod[getMethod] = entityRef;
			}

			invocation.Proceed();
		}

		private void SetEntityRef<TEntity>(EntityRef<TEntity> entityRef, MethodInfo getMethod, IEntityProxy proxy)
			where TEntity : class
		{
			if (getMethod == null)
				throw new ArgumentNullException("getMethod");
			if (proxy == null)
				throw new ArgumentNullException("proxy");

			var setMethod = TryGetPropertyInfoByGetter(getMethod).GetSetMethod(nonPublic: true);
			if (entityRef.HasLoadedOrAssignedValue)
				setMethod.Invoke(
					proxy, 
					new object[]
					{
						entityRef.Entity
					});

			entityRefsByGetMethod[getMethod] = entityRef;
		}
	}
}
