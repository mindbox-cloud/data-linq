using System.Data.Linq.SqlClient;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Data.Linq.Mapping
{
	internal sealed class AttributedMetaDataMember : MetaDataMember 
	{
		private static MetaAccessor CreateAccessor(Type accessorType, params object[] args)
		{
			return (MetaAccessor)Activator.CreateInstance(
				accessorType,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				args,
				null);
		}

		private static MetaAccessor MakeMemberAccessor(Type accessorType, MemberInfo member, MetaAccessor storageAccessor)
		{
			var field = member as FieldInfo;
			if (field != null)
				return FieldAccessor.Create(accessorType, field);

			var property = (PropertyInfo)member;
			return PropertyAccessor.Create(accessorType, property, storageAccessor);
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static void MakeDeferredAccessors(
			Type objectDeclaringType, 
			MetaAccessor accessor,
			out MetaAccessor accessorValue, 
			out MetaAccessor accessorDeferredValue, 
			out MetaAccessor accessorDeferredSource)
		{
			if (accessor.Type.IsGenericType)
			{
				var genericTypeDefinition = accessor.Type.GetGenericTypeDefinition();
				var itemType = accessor.Type.GetGenericArguments()[0];
				if (genericTypeDefinition == typeof(Link<>))
				{
					accessorValue = CreateAccessor(
						typeof(LinkValueAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					accessorDeferredValue = CreateAccessor(
						typeof(LinkDefValueAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					accessorDeferredSource = CreateAccessor(
						typeof(LinkDefSourceAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					return;
				}

				if (typeof(EntityRef<>).IsAssignableFrom(genericTypeDefinition))
				{
					accessorValue = CreateAccessor(
						typeof(EntityRefValueAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					accessorDeferredValue = CreateAccessor(
						typeof(EntityRefDefValueAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					accessorDeferredSource = CreateAccessor(
						typeof(EntityRefDefSourceAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					return;
				}

				if (typeof(EntitySet<>).IsAssignableFrom(genericTypeDefinition))
				{
					accessorValue = CreateAccessor(
						typeof(EntitySetValueAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					accessorDeferredValue = CreateAccessor(
						typeof(EntitySetDefValueAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					accessorDeferredSource = CreateAccessor(
						typeof(EntitySetDefSourceAccessor<,>).MakeGenericType(objectDeclaringType, itemType),
						accessor);
					return;
				}
			}

			throw Error.UnhandledDeferredStorageType(accessor.Type);
		}

		private static bool IsDeferredType(Type entityType)
		{
			if (entityType == null || entityType == typeof(object))
				return false;

			if (!entityType.IsGenericType)
				return false;

			var genericTypeDefinition = entityType.GetGenericTypeDefinition();
			return genericTypeDefinition == typeof(Link<>) ||
				typeof(EntitySet<>).IsAssignableFrom(genericTypeDefinition) ||
				typeof(EntityRef<>).IsAssignableFrom(genericTypeDefinition) ||
				IsDeferredType(entityType.BaseType);
		} 


		private readonly AttributedMetaType metaType;
		private readonly MemberInfo member;
		private readonly MemberInfo storageMember;
		private readonly int ordinal;
		private readonly Type type;
		private readonly Type declaringType;
		private bool areAccessorsInitialized;
		private MetaAccessor memberAccessor;
		private MetaAccessor storageAccessor;
		private MetaAccessor deferredValueAccessor;
		private MetaAccessor deferredSourceAccessor;
		private readonly ColumnAttribute columnAttribute;
		private readonly AssociationAttribute associationAttribute;
		private AttributedMetaAssociation assoc;
		private readonly bool isNullableType;
		private readonly bool isDeferred;
		private readonly object metaDataMemberLock = new object(); // Hold locks on private object rather than public MetaType.
		private bool hasLoadMethod;
		private MethodInfo loadMethod;
        

		internal AttributedMetaDataMember(AttributedMetaType metaType, MemberInfo member, int ordinal) 
		{
			declaringType = member.DeclaringType;
			this.metaType = metaType;
			this.member = member;
			this.ordinal = ordinal;
			type = TypeSystem.GetMemberType(member);
			isNullableType = TypeSystem.IsNullableType(type);
			columnAttribute = ((AttributedMetaModel)metaType.Model).TryGetColumnAttribute(member);
			associationAttribute = (AssociationAttribute)Attribute.GetCustomAttribute(member, typeof(AssociationAttribute));
			var attr = (columnAttribute == null) ? associationAttribute : (DataAttribute)columnAttribute;
			if (attr != null && attr.Storage != null) 
			{
				var mis = member.DeclaringType.GetMember(attr.Storage, BindingFlags.Instance | BindingFlags.NonPublic);
				if (mis.Length != 1)
					throw Error.BadStorageProperty(attr.Storage, member.DeclaringType, member.Name);
				storageMember = mis[0];
			}
			var storageType = storageMember == null ? type : TypeSystem.GetMemberType(storageMember);
			isDeferred = IsDeferredType(storageType);

			// auto-gen identities must be synced on insert
			if ((columnAttribute != null) && 
					columnAttribute.IsDbGenerated && 
					columnAttribute.IsPrimaryKey &&
					(columnAttribute.AutoSync != AutoSync.Default) && 
					(columnAttribute.AutoSync != AutoSync.OnInsert))
				throw Error.IncorrectAutoSyncSpecification(member.Name);
		}


		public override MetaType DeclaringType
		{
			get { return metaType; }
		}

		public override MemberInfo Member
		{
			get { return member; }
		}

		public override MemberInfo StorageMember
		{
			get { return storageMember; }
		}

		public override string Name
		{
			get { return member.Name; }
		}

		public override int Ordinal
		{
			get { return ordinal; }
		}

		public override Type Type
		{
			get { return type; }
		}

		public override MetaAccessor MemberAccessor
		{
			get
			{
				InitAccessors();
				return memberAccessor;
			}
		}

		public override MetaAccessor StorageAccessor
		{
			get
			{
				InitAccessors();
				return storageAccessor;
			}
		}

		public override MetaAccessor DeferredValueAccessor
		{
			get
			{
				InitAccessors();
				return deferredValueAccessor;
			}
		}

		public override MetaAccessor DeferredSourceAccessor
		{
			get
			{
				InitAccessors();
				return deferredSourceAccessor;
			}
		}

		public override bool IsDeferred
		{
			get { return isDeferred; }
		}

		public override bool IsPersistent
		{
			get { return (columnAttribute != null) || (associationAttribute != null); }
		}

		public override bool IsAssociation
		{
			get { return associationAttribute != null; }
		}

		public override bool IsPrimaryKey
		{
			get { return (columnAttribute != null) && columnAttribute.IsPrimaryKey; }
		}

		/// <summary>
		/// Returns true if the member is explicitly marked as auto gen, or if the
		/// member is computed or generated by the database server.
		/// </summary>
		public override bool IsDbGenerated
		{
			get
			{
				return (columnAttribute != null) &&
					(columnAttribute.IsDbGenerated || !string.IsNullOrEmpty(columnAttribute.Expression)) || IsVersion;
			}
		}

		public override bool IsVersion
		{
			get { return columnAttribute != null && columnAttribute.IsVersion; }
		}

		public override bool IsDiscriminator
		{
			get { return (columnAttribute != null) && columnAttribute.IsDiscriminator; }
		}

		public override bool CanBeNull
		{
			get
			{
				if (columnAttribute == null)
					return true;
				if (columnAttribute.CanBeNullSet)
					return columnAttribute.CanBeNull;
				return isNullableType || !type.IsValueType;
			}
		}

		public override string DbType
		{
			get { return columnAttribute == null ? null : columnAttribute.DbType; }
		}

		public override string Expression
		{
			get { return columnAttribute == null ? null : columnAttribute.Expression; }
		}

		public override string MappedName
		{
			get
			{
				if (columnAttribute != null && columnAttribute.Name != null)
					return columnAttribute.Name;
				if (associationAttribute != null && associationAttribute.Name != null)
					return associationAttribute.Name;
				return member.Name;
			}
		}

		public override UpdateCheck UpdateCheck
		{
			get
			{
				return columnAttribute == null ? UpdateCheck.Never : columnAttribute.UpdateCheck;
			}
		}

		public override AutoSync AutoSync
		{
			get
			{
				if (columnAttribute == null)
					return AutoSync.Never;

				// auto-gen keys are always and only synced on insert
				if (IsDbGenerated && IsPrimaryKey)
					return AutoSync.OnInsert;

				// if the user has explicitly set it, use their value
				if (columnAttribute.AutoSync != AutoSync.Default)
					return columnAttribute.AutoSync;

				// database generated members default to always
				return IsDbGenerated ? AutoSync.Always : AutoSync.Never;
			}
		}

		public override MetaAssociation Association
		{
			get
			{
				if (IsAssociation)
				{
					// LOCKING: This deferral isn't an optimization. It can't be done in the constructor
					// because there may be loops in the association graph.
					if (assoc == null)
					{
						lock (metaDataMemberLock)
						{
							if (assoc == null)
								assoc = new AttributedMetaAssociation(this, associationAttribute);
						}
					}
				}
				return assoc;
			}
		}

		public override MethodInfo LoadMethod
		{
			get
			{
				if (!hasLoadMethod && (IsDeferred || IsAssociation))
				{
					// defer searching for this access method until we really need to know
					loadMethod = MethodFinder.FindMethod(
						metaType.Model.ContextType,
						"Load" + member.Name,
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
						new[]
						{
							DeclaringType.Type
						});
					hasLoadMethod = true;
				}
				return loadMethod;
			}
		}


		public override bool IsDeclaredBy(MetaType declaringMetaType)
		{
			if (declaringMetaType == null)
				throw new ArgumentNullException("declaringMetaType");

			return declaringMetaType.Type == declaringType;
		}

		public override string ToString()
		{
			return DeclaringType + ":" + Member;
		}


		private void InitAccessors()
		{
			if (areAccessorsInitialized)
				return;

			lock (metaDataMemberLock) 
			{
				if (areAccessorsInitialized)
					return;

				if (storageMember == null)
				{
					memberAccessor = storageAccessor = MakeMemberAccessor(member.ReflectedType, member, null);
					if (isDeferred)
						MakeDeferredAccessors(
							member.ReflectedType,
							storageAccessor,
							out storageAccessor,
							out deferredValueAccessor,
							out deferredSourceAccessor);
				}
				else
				{
					storageAccessor = MakeMemberAccessor(member.ReflectedType, storageMember, null);
					if (isDeferred)
						MakeDeferredAccessors(
							member.ReflectedType,
							storageAccessor,
							out storageAccessor,
							out deferredValueAccessor,
							out deferredSourceAccessor);
					memberAccessor = MakeMemberAccessor(member.ReflectedType, member, storageAccessor);
				}

				areAccessorsInitialized = true;
			}
		}
	}
}