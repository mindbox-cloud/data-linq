using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq.SqlClient;
using System.Linq;
using LinqToSqlShared.Mapping;

namespace System.Data.Linq.Mapping
{
	internal class AttributedRootType : AttributedMetaType 
	{
		private readonly Dictionary<Type, MetaType> types;
		private readonly ReadOnlyCollection<MetaType> inheritanceTypes;
		private readonly MetaType inheritanceDefault;


		internal AttributedRootType(AttributedMetaModel model, AttributedMetaTable table, Type type)
			: base(model, table, type, null) 
		{

			// check for inheritance and create all other types
			var inheritanceAttributes = GetInheritanceMappingAttributes(type, model);
			if (inheritanceAttributes.Count > 0) 
			{
				if (Discriminator == null)
					throw Error.NoDiscriminatorFound(type);
				if (!MappingSystem.IsSupportedDiscriminatorType(Discriminator.Type))
					throw Error.DiscriminatorClrTypeNotSupported(
						Discriminator.DeclaringType.Name,
						Discriminator.Name,
						Discriminator.Type);

				types = new Dictionary<Type, MetaType>();
				types.Add(type, this); // add self
				var codeMap = new Dictionary<object, MetaType>();

				// initialize inheritance types
				foreach (var inheritanceAttribute in inheritanceAttributes) 
				{
					if (!type.IsAssignableFrom(inheritanceAttribute.Type))
						throw Error.InheritanceTypeDoesNotDeriveFromRoot(inheritanceAttribute.Type, type);
					if (inheritanceAttribute.Type.IsAbstract)
						throw Error.AbstractClassAssignInheritanceDiscriminator(inheritanceAttribute.Type);

					var inheritedType = CreateInheritedType(type, inheritanceAttribute.Type);
					if (inheritanceAttribute.Code == null)
						throw Error.InheritanceCodeMayNotBeNull();
					if (inheritedType.inheritanceCode != null)
						throw Error.InheritanceTypeHasMultipleDiscriminators(inheritanceAttribute.Type);

					var codeValue = DBConvert.ChangeType(inheritanceAttribute.Code, Discriminator.Type);                
					foreach (var codeMapKey in codeMap.Keys) 
					{
						// if the keys are equal, or if they are both strings containing only spaces
						// they are considered equal
						if ((codeValue is string && 
									((string)codeValue).Trim().Length == 0 &&
									codeMapKey is string && 
									((string)codeMapKey).Trim().Length == 0) ||
								Equals(codeMapKey, codeValue))
							throw Error.InheritanceCodeUsedForMultipleTypes(codeValue);
					}
					inheritedType.inheritanceCode = codeValue;
					codeMap.Add(codeValue, inheritedType);

					if (inheritanceAttribute.IsDefault) 
					{
						if (inheritanceDefault != null)
							throw Error.InheritanceTypeHasMultipleDefaults(type);
						inheritanceDefault = inheritedType;
					}
				}

				if (inheritanceDefault == null)
					throw Error.InheritanceHierarchyDoesNotDefineDefault(type);
			}

			inheritanceTypes = types == null ? 
				new MetaType[]
				{
					this
				}
					.ToList()
					.AsReadOnly() : 
				types.Values.ToList().AsReadOnly();

			Validate();
		}


		public override bool HasInheritance 
		{
			get { return types != null; }
		}

		public override ReadOnlyCollection<MetaType> InheritanceTypes
		{
			get { return inheritanceTypes; }
		}

		public override MetaType InheritanceDefault
		{
			get { return inheritanceDefault; }
		}


		public override MetaType GetInheritanceType(Type type)
		{
			var nonProxyType = Model.UnproxyType(type);

			if (nonProxyType == Type)
				return this;

			MetaType metaType = null;
			if (types != null)
				types.TryGetValue(nonProxyType, out metaType);
			return metaType;
		}


		protected virtual ICollection<InheritanceMappingAttribute> GetInheritanceMappingAttributes(
			Type type,
			AttributedMetaModel model)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (model == null)
				throw new ArgumentNullException("model");

			return (InheritanceMappingAttribute[])type.GetCustomAttributes(typeof(InheritanceMappingAttribute), true);
		}


		private void Validate()
		{
			var memberToColumn = new Dictionary<object, string>();
			foreach (var inheritanceType in InheritanceTypes)
			{
				if (inheritanceType != this)
				{
					var attrs = ((AttributedMetaModel)Model).GetTableAttributes(inheritanceType.Type, false);
					if (attrs.Any())
						throw Error.InheritanceSubTypeIsAlsoRoot(inheritanceType.Type);
				}

				foreach (var persistentDataMember in inheritanceType.PersistentDataMembers)
				{
					if (!persistentDataMember.IsDeclaredBy(inheritanceType))
						continue;

					if (persistentDataMember.IsDiscriminator && !HasInheritance)
						throw Error.NonInheritanceClassHasDiscriminator(inheritanceType);

					if (persistentDataMember.IsAssociation)
						continue;

					// validate that no database column is mapped twice
					if (string.IsNullOrEmpty(persistentDataMember.MappedName))
						continue;

					string column;
					var distinguishedMemberName = InheritanceRules.DistinguishedMemberName(persistentDataMember.Member);
					if (memberToColumn.TryGetValue(distinguishedMemberName, out column))
					{
						if (column != persistentDataMember.MappedName)
							throw Error.MemberMappedMoreThanOnce(persistentDataMember.Member.Name);
					}
					else
					{
						memberToColumn.Add(distinguishedMemberName, persistentDataMember.MappedName);
					}
				}
			}
		}

		private AttributedMetaType CreateInheritedType(Type root, Type type) 
		{
			MetaType metaType;
			if (!types.TryGetValue(type, out metaType)) 
			{
				metaType = new AttributedMetaType(Model, Table, type, this);
				types.Add(type, metaType);
				if (type != root && type.BaseType != typeof(object))
					CreateInheritedType(root, type.BaseType);
			}
			return (AttributedMetaType)metaType;
		}
	}
}