using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Reflection;
using System.Text;
using System.Linq;
using Castle.DynamicProxy;
using Mindbox.Data.Linq.Proxy;

namespace Mindbox.Data.Linq.Mapping 
{
	internal class MindboxMetaModel : AttributedMetaModel 
	{
		private static readonly ProxyGenerator proxyGenerator = new ProxyGenerator();


        internal MindboxMetaModel(MappingSource mappingSource, Type contextType) 
			: base(mappingSource, contextType)
		{
        }


		internal override AttributedRootType CreateRootType(AttributedMetaTable table, Type type)
		{
			return new MindboxRootType(this, table, type);
		}

		internal override IReadOnlyCollection<TableAttribute> GetTableAttributes(Type type, bool shouldInherit)
		{
			var tableAttributes = base.GetTableAttributes(type, shouldInherit).ToList();

			var configuration = ((MindboxMappingSource)MappingSource).Configuration;
			if (tableAttributes.Any())
				configuration.OnEntityFrameworkIncompatibility(EntityFrameworkIncompatibility.TableAttribute);

			var additionalTableAttribute = configuration.TryGetTableAttribute(type);
			if (additionalTableAttribute != null)
				tableAttributes.Add(additionalTableAttribute);

			for (var currentType = type; shouldInherit && (currentType.BaseType != null); currentType = currentType.BaseType)
			{
				var currentTypeAdditionalTableAttribute = configuration.TryGetTableAttribute(currentType);
				if (currentTypeAdditionalTableAttribute != null)
					tableAttributes.Add(currentTypeAdditionalTableAttribute);
			}

			if (!shouldInherit && (tableAttributes.Count > 1))
				throw new InvalidOperationException("!shouldInherit && (tableAttributes.Count > 1)");

			return tableAttributes;
		}

		internal override ColumnAttribute TryGetColumnAttribute(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			var baseAttribute = base.TryGetColumnAttribute(member);

			var configuration = ((MindboxMappingSource)MappingSource).Configuration;
			if (baseAttribute != null)
				configuration.OnEntityFrameworkIncompatibility(EntityFrameworkIncompatibility.ColumnAttribute);

			var additionalAttribute = configuration.TryGetColumnAttribute(member);
			if ((baseAttribute != null) && (additionalAttribute != null))
				throw new InvalidOperationException("(baseAttribute != null) && (additionalAttribute != null)");

			return baseAttribute ?? additionalAttribute;
		}

		internal override AssociationAttribute TryGetAssociationAttribute(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			var baseAttribute = base.TryGetAssociationAttribute(member);

			var configuration = ((MindboxMappingSource)MappingSource).Configuration;
			if (baseAttribute != null)
				configuration.OnEntityFrameworkIncompatibility(EntityFrameworkIncompatibility.AssociationAttribute);

			var additionalAttribute = configuration.TryGetAssociationAttribute(member);
			if ((baseAttribute != null) && (additionalAttribute != null))
				throw new InvalidOperationException("(baseAttribute != null) && (additionalAttribute != null)");

			return baseAttribute ?? additionalAttribute;
		}

		internal override bool IsDeferredMember(MemberInfo member, Type storageType, AssociationAttribute associationAttribute)
		{
			return base.IsDeferredMember(member, storageType, associationAttribute) || 
				IsProxyDeferredMember(member, associationAttribute);
		}

		internal override bool ShouldEntityProxyBeCreated(Type entityType)
		{
			if (entityType == null)
				throw new ArgumentNullException("entityType");

			var entityMetaType = GetMetaType(entityType) as AttributedMetaType;
			return (entityMetaType != null) && entityMetaType.DoesRequireProxy;
		}

		internal override bool DoesMemberRequireProxy(
			MemberInfo member, 
			Type storageType, 
			AssociationAttribute associationAttribute)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			if (storageType == null)
				throw new ArgumentNullException("storageType");

			return !base.IsDeferredMember(member, storageType, associationAttribute) && 
				IsProxyDeferredMember(member, associationAttribute);
		}

		internal override object CreateEntityProxy(Type entityType)
		{
			if (entityType == null)
				throw new ArgumentNullException("entityType");

			var proxy = proxyGenerator.CreateClassProxy(
				entityType,
				new[]
				{
					typeof(IEntityProxy),
					typeof(INotifyPropertyChanging),
					typeof(INotifyPropertyChanged)
				},
				new EntityProxyInterceptor(this));
			foreach (var dataMember in GetMetaType(entityType).DataMembers.OfType<AttributedMetaDataMember>())
				if (dataMember.IsAssociation && dataMember.Association.IsMany && !dataMember.DoesRequireProxy)
					((IEntitySet)dataMember.StorageAccessor.GetBoxedValue(proxy)).ListChanging +=
						((IEntityProxy)proxy).HandleEntitySetChanging;
			return proxy;
		}

		internal override Type UnproxyType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			var currentType = type;
			while (typeof(IEntityProxy).IsAssignableFrom(currentType) && (currentType.BaseType != null))
				currentType = currentType.BaseType;
			return currentType;
		}


		private bool IsProxyDeferredMember(MemberInfo member, AssociationAttribute associationAttribute)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			if ((associationAttribute == null) || !associationAttribute.IsForeignKey)
				return false;

			var property = member as PropertyInfo;
			return (property != null) && property.GetMethod.IsVirtual && !property.GetMethod.IsFinal;
		}

		public static bool DatabaseIsMigrated = false;
	}
}
