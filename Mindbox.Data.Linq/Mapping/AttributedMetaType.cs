using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq.SqlClient;
using System.Linq;
using System.Reflection;
using LinqToSqlShared.Mapping;

namespace System.Data.Linq.Mapping
{
	internal class AttributedMetaType : MetaType 
	{
		internal object inheritanceCode;

	
		private readonly MetaModel model;
		private readonly MetaTable table;
		private readonly Type type;
		private Dictionary<MetaPosition, MetaDataMember> dataMemberMap;
		private ReadOnlyCollection<MetaDataMember> dataMembers;
		private readonly ReadOnlyCollection<MetaDataMember> persistentMembers;
		private readonly ReadOnlyCollection<MetaDataMember> identities;
		private MetaDataMember dbGeneratedIdentity;
		private MetaDataMember version;
		private MetaDataMember discriminator;
		private readonly MetaType inheritanceRoot;
		private bool inheritanceBaseSet;
		private MetaType inheritanceBase;
		private ReadOnlyCollection<MetaType> derivedTypes;
		private ReadOnlyCollection<MetaAssociation> associations;
		private bool areMethodsInitialized;
		private bool hasAnyLoadMethod;
		private bool hasAnyValidateMethod;
		private MethodInfo onLoadedMethod;
		private MethodInfo onValidateMethod;

		private readonly object metaTypeLock = new object(); // Hold locks on private object rather than public MetaType.


		internal AttributedMetaType(MetaModel model, MetaTable table, Type type, MetaType inheritanceRoot) 
		{
			this.model = model;
			this.table = table;
			this.type = type;
			this.inheritanceRoot = inheritanceRoot ?? this;

			// Not lazy-loading to simplify locking and enhance performance 
			// (because no lock will be required for the common read scenario).
			InitDataMembers();
			identities = dataMembers.Where(dataMember => dataMember.IsPrimaryKey).ToList().AsReadOnly();
			persistentMembers = dataMembers.Where(dataMember => dataMember.IsPersistent).ToList().AsReadOnly();
		}


		public override MetaModel Model 
		{
			get { return model; }
		}

		public override MetaTable Table 
		{
			get { return table; }
		}

		public override Type Type 
		{
			get { return type; }
		}

		public override string Name 
		{
			get { return type.Name; }
		}

		public override bool IsEntity 
		{
			get { return table != null && table.RowType.IdentityMembers.Count > 0; }
		}

		public override bool CanInstantiate 
		{
			get { return !type.IsAbstract && (this == InheritanceRoot || HasInheritanceCode); }
		}

		public override MetaDataMember DBGeneratedIdentityMember 
		{
			get { return dbGeneratedIdentity; }
		}

		public override MetaDataMember VersionMember 
		{
			get { return version; }
		}

		public override MetaDataMember Discriminator 
		{
			get { return discriminator; }
		}

		public override bool HasUpdateCheck 
		{
			get 
			{
				foreach (var member in PersistentDataMembers)
					if (member.UpdateCheck != UpdateCheck.Never)
						return true;
				return false;
			}
		}

		public override bool HasInheritance 
		{
			get { return inheritanceRoot.HasInheritance; }
		}

		public override bool HasInheritanceCode 
		{
			get { return inheritanceCode != null; }
		}

		public override object InheritanceCode 
		{
			get { return inheritanceCode; }
		}

		public override MetaType InheritanceRoot 
		{
			get { return inheritanceRoot; }
		}

		public override MetaType InheritanceBase 
		{
			get 
			{
				// LOCKING: Cannot initialize at construction
				if (!inheritanceBaseSet && inheritanceBase == null) 
				{
					lock (metaTypeLock) 
					{
						if (inheritanceBase == null) 
						{
							inheritanceBase = InheritanceBaseFinder.FindBase(this);
							inheritanceBaseSet = true;
						}
					}
				}
				return inheritanceBase;
			}
		}

		public override MetaType InheritanceDefault 
		{
			get { return InheritanceRoot.InheritanceDefault; }
		}

		public override bool IsInheritanceDefault 
		{
			get { return InheritanceDefault == this; }
		}

		public override ReadOnlyCollection<MetaType> InheritanceTypes 
		{
			get { return inheritanceRoot.InheritanceTypes; }
		}

		public override ReadOnlyCollection<MetaType> DerivedTypes 
		{
			get 
			{
				// LOCKING: Cannot initialize at construction because derived types
				// won't exist yet.
				if (derivedTypes == null) 
				{
					lock (metaTypeLock) 
					{
						if (derivedTypes == null) 
						{
							var derivedTypeList = new List<MetaType>();
							foreach (var inheritanceType in InheritanceTypes)
								if (inheritanceType.Type.BaseType == type)
									derivedTypeList.Add(inheritanceType);
							derivedTypes = derivedTypeList.AsReadOnly();
						}
					}
				}
				return derivedTypes;
			}
		}

		public override ReadOnlyCollection<MetaDataMember> DataMembers 
		{
			get { return dataMembers; }
		}

		public override ReadOnlyCollection<MetaDataMember> PersistentDataMembers 
		{
			get { return persistentMembers; }
		}

		public override ReadOnlyCollection<MetaDataMember> IdentityMembers 
		{
			get { return identities; }
		}

		public override ReadOnlyCollection<MetaAssociation> Associations 
		{
			get 
			{
				// LOCKING: Associations are late-expanded so that cycles are broken.
				if (associations == null)
					lock (metaTypeLock)
						if (associations == null)
							associations = dataMembers
								.Where(dataMember => dataMember.IsAssociation)
								.Select(dataMember => dataMember.Association)
								.ToList()
								.AsReadOnly();
				return associations;
			}
		}

		public override MethodInfo OnLoadedMethod 
		{
			get 
			{
				InitMethods();
				return onLoadedMethod;
			}
		}

		public override MethodInfo OnValidateMethod 
		{
			get 
			{
				InitMethods();
				return onValidateMethod;
			}
		}

		public override bool HasAnyValidateMethod 
		{
			get 
			{
				InitMethods();
				return hasAnyValidateMethod;
			}
		}

		public override bool HasAnyLoadMethod 
		{
			get 
			{
				InitMethods();
				return hasAnyLoadMethod;
			}
		}


		public override MetaDataMember GetDataMember(MemberInfo mi)
		{
			if (mi == null)
				throw Error.ArgumentNull("mi");

			MetaDataMember mm;
			if (dataMemberMap.TryGetValue(new MetaPosition(mi), out mm))
				return mm;

			// DON'T look to see if we are trying to get a member from an inherited type.
			// The calling code should know to look in the inherited type.
			if (mi.DeclaringType.IsInterface)
				throw Error.MappingOfInterfacesMemberIsNotSupported(mi.DeclaringType.Name, mi.Name);

			// the member is not mapped in the base class
			throw Error.UnmappedClassMember(mi.DeclaringType.Name, mi.Name);
		}

		public override MetaType GetTypeForInheritanceCode(object key)
		{
			if (InheritanceRoot.Discriminator.Type == typeof(string))
			{
				var stringKey = (string)key;
				foreach (var inheritanceType in InheritanceRoot.InheritanceTypes)
					if (string.Compare(
							(string)inheritanceType.InheritanceCode,
							stringKey,
							StringComparison.OrdinalIgnoreCase) == 0)
						return inheritanceType;
			}
			else
			{
				foreach (var inheritanceType in InheritanceRoot.InheritanceTypes)
					if (Equals(inheritanceType.InheritanceCode, key))
						return inheritanceType;
			}
			return null;
		}

		public override MetaType GetInheritanceType(Type inheritanceType)
		{
			return inheritanceType == type ? this : inheritanceRoot.GetInheritanceType(inheritanceType);
		}

		public override string ToString()
		{
			return Name;
		}


		private void ValidatePrimaryKeyMember(MetaDataMember metaDataMember)
		{
			// if the type is a sub-type, no member declared in the type can be primary key
			if (metaDataMember.IsPrimaryKey && inheritanceRoot != this && metaDataMember.Member.DeclaringType == type)
				throw Error.PrimaryKeyInSubTypeNotSupported(type.Name, metaDataMember.Name);
		}

		private void InitMethods()
		{
			if (areMethodsInitialized)
				return;

			onLoadedMethod = MethodFinder.FindMethod(
				Type,
				"OnLoaded",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				Type.EmptyTypes,
				false);
			onValidateMethod = MethodFinder.FindMethod(
				Type,
				"OnValidate",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				new[]
				{
					typeof(ChangeAction)
				},
				false);

			hasAnyLoadMethod = (onLoadedMethod != null) || (InheritanceBase != null && InheritanceBase.HasAnyLoadMethod);
			hasAnyValidateMethod = (onValidateMethod != null) || 
				(InheritanceBase != null && InheritanceBase.HasAnyValidateMethod);

			areMethodsInitialized = true;
		}

		private void InitDataMembers()
		{
			if (dataMembers == null)
			{
				dataMemberMap = new Dictionary<MetaPosition, MetaDataMember>();

				var ordinal = 0;
				const BindingFlags flags = 
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

				var fields = TypeSystem.GetAllFields(type, flags).ToArray();
				for (int i = 0, n = fields.Length; i < n; i++)
				{
					var fi = fields[i];
					var dataMember = new AttributedMetaDataMember(this, fi, ordinal);
					ValidatePrimaryKeyMember(dataMember);

					// must be public or persistent
					if (dataMember.IsPersistent || fi.IsPublic)
					{
						dataMemberMap.Add(new MetaPosition(fi), dataMember);
						ordinal++;

						// must be persistent for the rest
						if (dataMember.IsPersistent)
							InitSpecialMember(dataMember);
					}
				}

				var properties = TypeSystem.GetAllProperties(type, flags).ToArray();
				for (int i = 0, n = properties.Length; i < n; i++)
				{
					var property = properties[i];
					var dataMember = new AttributedMetaDataMember(this, property, ordinal);
					ValidatePrimaryKeyMember(dataMember);

					// must be public or persistent
					var isPublic = (property.CanRead && property.GetGetMethod(false) != null)
						&& (!property.CanWrite || property.GetSetMethod(false) != null);
					if (dataMember.IsPersistent || isPublic)
					{
						dataMemberMap.Add(new MetaPosition(property), dataMember);
						ordinal++;

						// must be persistent for the rest
						if (dataMember.IsPersistent)
							InitSpecialMember(dataMember);
					}
				}

				dataMembers = new List<MetaDataMember>(dataMemberMap.Values).AsReadOnly();
			}
		}

		private void InitSpecialMember(MetaDataMember metaDataMember)
		{
			// Can only have one auto gen member that is also an identity member,
			// except if that member is a computed column (since they are implicitly auto gen)
			if (metaDataMember.IsDbGenerated && metaDataMember.IsPrimaryKey && string.IsNullOrEmpty(metaDataMember.Expression))
			{
				if (dbGeneratedIdentity != null)
					throw Error.TwoMembersMarkedAsPrimaryKeyAndDBGenerated(metaDataMember.Member, dbGeneratedIdentity.Member);
				dbGeneratedIdentity = metaDataMember;
			}

			if (metaDataMember.IsPrimaryKey && !MappingSystem.IsSupportedIdentityType(metaDataMember.Type))
				throw Error.IdentityClrTypeNotSupported(metaDataMember.DeclaringType, metaDataMember.Name, metaDataMember.Type);

			if (metaDataMember.IsVersion)
			{
				if (version != null)
					throw Error.TwoMembersMarkedAsRowVersion(metaDataMember.Member, this.version.Member);
				version = metaDataMember;
			}

			if (metaDataMember.IsDiscriminator)
			{
				if (discriminator != null)
					throw Error.TwoMembersMarkedAsInheritanceDiscriminator(metaDataMember.Member, discriminator.Member);
				discriminator = metaDataMember;
			}
		}
	}
}