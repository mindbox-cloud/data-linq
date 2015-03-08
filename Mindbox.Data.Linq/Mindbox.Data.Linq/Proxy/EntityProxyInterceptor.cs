using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		private static readonly MethodInfo entityProxyGetEntityRefMethodDefinition = ReflectionExpressions
			.GetMethodInfo<IEntityProxy>(aProxy => aProxy.GetEntityRef<object>(default(MemberInfo)))
			.GetGenericMethodDefinition();

		private static readonly MethodInfo entityProxySetEntityRefMethodDefinition = ReflectionExpressions
			.GetMethodInfo<IEntityProxy>(entityProxy =>
				entityProxy.SetEntityRef(default(MemberInfo), default(EntityRef<object>)))
			.GetGenericMethodDefinition();

		private static readonly MethodInfo handleEntityRefGetterMethodDefinition = ReflectionExpressions
			.GetMethodInfo<EntityProxyInterceptor>(interceptor =>
				interceptor.HandleEntityRefGetter<object>(default(IInvocation)))
			.GetGenericMethodDefinition();

		private static readonly MethodInfo handleEntityRefSetterMethodDefinition = ReflectionExpressions
			.GetMethodInfo<EntityProxyInterceptor>(interceptor =>
				interceptor.HandleEntityRefSetter<object>(default(IInvocation), default(PropertyInfo)))
			.GetGenericMethodDefinition();

		private static readonly MethodInfo setEntityRefMethodDefinition = ReflectionExpressions
			.GetMethodInfo<EntityProxyInterceptor>(interceptor =>
				interceptor.SetEntityRef(default(EntityRef<object>), default(MethodInfo), default(IEntityProxy)))
			.GetGenericMethodDefinition();

		private static readonly EventInfo notifyPropertyChangingPropertyChangingEvent = 
			typeof(INotifyPropertyChanging).GetEvent("PropertyChanging");

		private static readonly EventInfo notifyPropertyChangedPropertyChangedEvent =
			typeof(INotifyPropertyChanged).GetEvent("PropertyChanged");

	
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
		private PropertyChangingEventHandler propertyChangingEventHandler;
		private PropertyChangedEventHandler propertyChangedEventHandler;

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

			if (invocation.Method.DeclaringType == typeof(IEntityProxy))
			{
				InterceptEntityProxyMethods(invocation);
				return;
			}

			if (invocation.Method.DeclaringType == typeof(INotifyPropertyChanging))
			{
				InterceptNotifyPropertyChangingMethods(invocation);
				return;
			}

			if (invocation.Method.DeclaringType == typeof(INotifyPropertyChanged))
			{
				InterceptNotifyPropertyChangedMethods(invocation);
				return;
			}

			InterceptOverridenMethods(invocation);
		}


		private void InterceptOverridenMethods(IInvocation invocation)
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			var proxy = (IEntityProxy)invocation.Proxy;
			var property = TryGetPropertyInfoByGetter(invocation.Method) ?? TryGetPropertyInfoBySetter(invocation.Method);
			if (property != null)
			{
				var metaMember = model.GetMetaType(proxy.GetType()).GetDataMember(property) as AttributedMetaDataMember;
				if (metaMember != null)
				{
					if (metaMember.IsAssociation &&
						metaMember.Association.IsForeignKey &&
						metaMember.DoesRequireProxy)
					{
						if (invocation.Method == property.GetMethod)
						{
							handleEntityRefGetterMethodDefinition
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
							handleEntityRefSetterMethodDefinition
								.MakeGenericMethod(invocation.Method.GetParameters()[0].ParameterType)
								.Invoke(
									this,
									new object[]
									{
										invocation,
										property
									});
						}
						return;
					}

					if (metaMember.IsPersistent && (invocation.Method == property.SetMethod))
					{
						var oldValue = property.GetValue(invocation.Proxy);
						var newValue = invocation.Arguments[0];
						if (!Equals(oldValue, newValue))
						{
							NotifyPropertyChanging(invocation, property.Name);
							invocation.Proceed();
							NotifyPropertyChanged(invocation, property.Name);
						}
						return;
					}
				}
			}

			invocation.Proceed();
		}

		private void InterceptEntityProxyMethods(IInvocation invocation)
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			var proxy = (IEntityProxy)invocation.Proxy;

			if (invocation.Method.IsGenericMethod &&
				invocation.Method.GetGenericMethodDefinition() == entityProxySetEntityRefMethodDefinition)
			{
				var getMethod = (MethodInfo)invocation.Arguments[0];
				var entityRef = invocation.Arguments[1];
				setEntityRefMethodDefinition
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
				invocation.Method.GetGenericMethodDefinition() == entityProxyGetEntityRefMethodDefinition)
			{
				var getMethod = (MethodInfo)invocation.Arguments[0];
				object entityRef;
				invocation.ReturnValue = entityRefsByGetMethod.TryGetValue(getMethod, out entityRef)
					? entityRef
					: Activator.CreateInstance(typeof(EntityRef<>).MakeGenericType(getMethod.ReturnType));
				return;
			}

			throw new NotImplementedException();
		}

		private void InterceptNotifyPropertyChangingMethods(IInvocation invocation)
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			if (invocation.Method == notifyPropertyChangingPropertyChangingEvent.AddMethod)
			{
				propertyChangingEventHandler += (PropertyChangingEventHandler)invocation.Arguments[0];
				return;
			}

			if (invocation.Method == notifyPropertyChangingPropertyChangingEvent.RemoveMethod)
			{
				propertyChangingEventHandler -= (PropertyChangingEventHandler)invocation.Arguments[0];
				return;
			}

			throw new NotImplementedException();
		}

		private void InterceptNotifyPropertyChangedMethods(IInvocation invocation)
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			if (invocation.Method == notifyPropertyChangedPropertyChangedEvent.AddMethod)
			{
				propertyChangedEventHandler += (PropertyChangedEventHandler)invocation.Arguments[0];
				return;
			}

			if (invocation.Method == notifyPropertyChangedPropertyChangedEvent.RemoveMethod)
			{
				propertyChangedEventHandler -= (PropertyChangedEventHandler)invocation.Arguments[0];
				return;
			}

			throw new NotImplementedException();
		}

		private void NotifyPropertyChanging(IInvocation invocation, string propertyName)
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			var currentHandler = propertyChangingEventHandler;
			if (currentHandler != null)
				currentHandler(invocation.Proxy, new PropertyChangingEventArgs(propertyName));
		}

		private void NotifyPropertyChanged(IInvocation invocation, string propertyName)
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");

			var currentHandler = propertyChangedEventHandler;
			if (currentHandler != null)
				currentHandler(invocation.Proxy, new PropertyChangedEventArgs(propertyName));
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

		private void HandleEntityRefSetter<TEntity>(IInvocation invocation, PropertyInfo property)
			where TEntity : class
		{
			if (invocation == null)
				throw new ArgumentNullException("invocation");
			if (property == null)
				throw new ArgumentNullException("property");

			var shouldNotify = false;
			if (!isSettingAfterEntityRefLoad)
			{
				var entityRef = GetEntityRef<TEntity>(property.GetMethod);
				var newEntity = (TEntity)invocation.Arguments[0];
				if (!entityRef.HasLoadedOrAssignedValue || !Equals(entityRef.Entity, newEntity))
				{
					shouldNotify = true;
					NotifyPropertyChanging(invocation, property.Name);

					entityRef.Entity = newEntity;
					entityRefsByGetMethod[property.GetMethod] = entityRef;
				}
			}

			invocation.Proceed();

			if (shouldNotify)
				NotifyPropertyChanged(invocation, property.Name);
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
