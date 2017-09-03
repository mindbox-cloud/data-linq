using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Data.Linq.SqlClient;
using System.Threading;

namespace System.Data.Linq.Mapping 
{
	internal class AttributedMetaModel : MetaModel 
	{
		internal static bool IsDeferredType(Type entityType)
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


		private static bool IsUserFunction(MethodInfo method)
		{
			return Attribute.GetCustomAttribute(method, typeof(FunctionAttribute), false) != null;
		}


		private readonly ReaderWriterLock metaModelLock = new ReaderWriterLock();
		private readonly MappingSource mappingSource;
		private readonly Type contextType;
		private readonly Type providerType;
		private readonly ConcurrentDictionary<Type, MetaType> metaTypes;
		private readonly Dictionary<Type, MetaTable> metaTables;
		private ReadOnlyCollection<MetaTable> staticTables;
		private readonly ConcurrentDictionary<MetaPosition, MetaFunction> metaFunctions;
		private readonly string dbName;
		private bool areStaticTablesInitialized;

        internal AttributedMetaModel(MappingSource mappingSource, Type contextType) 
		{
            this.mappingSource = mappingSource;
            this.contextType = contextType;
            metaTypes = new ConcurrentDictionary<Type, MetaType>();
            metaTables = new Dictionary<Type, MetaTable>();
            metaFunctions = new ConcurrentDictionary<MetaPosition, MetaFunction>();

            // Provider type
            var attrs = (ProviderAttribute[])this.contextType.GetCustomAttributes(typeof(ProviderAttribute), true);
			// Provider attribute is !AllowMultiple
            providerType = attrs.Length == 1 ? attrs[0].Type : typeof(SqlProvider);

            // Database name 
            var das = (DatabaseAttribute[])this.contextType.GetCustomAttributes(typeof(DatabaseAttribute), false);
            dbName = das.Length > 0 ? das[0].Name : this.contextType.Name;
        }


        public override MappingSource MappingSource 
		{
            get { return mappingSource; }
        }

        public override Type ContextType 
		{
            get { return contextType; }
        }

        public override string DatabaseName 
		{
            get { return dbName; }
        }

        public override Type ProviderType 
		{
            get { return providerType; }
        }


        public override IEnumerable<MetaTable> GetTables() 
		{
            InitStaticTables();
	        if (staticTables.Count > 0)
		        return staticTables;

	        metaModelLock.AcquireReaderLock(Timeout.Infinite);
	        try 
			{
		        return metaTables.Values.Where(metaTable => metaTable != null).Distinct();
	        }
	        finally 
			{
		        metaModelLock.ReleaseReaderLock();
	        }
		}

        public override MetaTable GetTable(Type rowType) 
		{
	        if (rowType == null)
		        throw Error.ArgumentNull("rowType");

	        var nonProxyRowType = UnproxyType(rowType);

	        MetaTable table;
            metaModelLock.AcquireReaderLock(Timeout.Infinite);
            try 
			{
				if (metaTables.TryGetValue(nonProxyRowType, out table))
					return table;
			}
            finally 
			{
                metaModelLock.ReleaseReaderLock();
            }

            metaModelLock.AcquireWriterLock(Timeout.Infinite);
            try 
			{
                table = GetTableNoLocks(nonProxyRowType);
            }
            finally 
			{
                metaModelLock.ReleaseWriterLock();
            }
            return table;
        }

        public override MetaType GetMetaType(Type type) 
		{
	        if (type == null)
		        throw Error.ArgumentNull("type");

	        var nonProxyType = UnproxyType(type);

	        MetaType mtype;
            metaModelLock.AcquireReaderLock(Timeout.Infinite);
            try 
			{
				if (metaTypes.TryGetValue(nonProxyType, out mtype))
					return mtype;
			}
            finally 
			{
                metaModelLock.ReleaseReaderLock();
            }

            // Attributed meta model allows us to learn about tables we did not
            // statically know about
            var tab = GetTable(nonProxyType);
	        if (tab != null)
		        return tab.RowType.GetInheritanceType(nonProxyType);

            metaModelLock.AcquireWriterLock(Timeout.Infinite);
            try 
			{
                if (!metaTypes.TryGetValue(nonProxyType, out mtype)) 
				{
                    mtype = new UnmappedType(this, nonProxyType);
                    metaTypes.TryAdd(nonProxyType, mtype);
                }
            }
            finally 
			{
                metaModelLock.ReleaseWriterLock();
            }
            return mtype;
        }

        public override MetaFunction GetFunction(MethodInfo method) 
		{
	        if (method == null)
		        throw Error.ArgumentNull("method");

			var key = new MetaPosition(method);

			var function = metaFunctions.GetOrAdd(key, mp =>
			{
				if (IsUserFunction(method))
				{
					// Added this constraint because XML mapping model didn't support 
					// mapping sprocs to generic method.
					// The attribute mapping model was, however, able to support it. This check is for parity between 
					// the two models.
					if (method.IsGenericMethodDefinition)
						throw Error.InvalidUseOfGenericMethodAsMappedFunction(method.Name);

					var metaFunction = new AttributedMetaFunction(this, method);

					// pre-set all known function result types into metaType map
					foreach (var resultRowType in metaFunction.ResultRowTypes)
					foreach (var inheritanceType in resultRowType.InheritanceTypes)
						metaTypes.TryAdd(inheritanceType.Type, inheritanceType);

					return metaFunction;
				}

				return null;
			});

            return function;
        }

		internal MetaTable GetTableNoLocks(Type rowType)
		{
			MetaTable table;
			if (metaTables.TryGetValue(rowType, out table))
				return table;

			var root = GetRoot(rowType) ?? rowType;
			var attrs = GetTableAttributes(root, true);
			if (!attrs.Any())
			{
				metaTables.Add(rowType, null);
				return null;
			}

			if (!metaTables.TryGetValue(root, out table))
			{
				table = new AttributedMetaTable(this, attrs.First(), root);
				foreach (var inheritanceType in table.RowType.InheritanceTypes)
					metaTables.Add(inheritanceType.Type, table);
			}

			// catch case of derived type that is not part of inheritance
			if (table.RowType.GetInheritanceType(rowType) == null)
			{
				metaTables.Add(rowType, null);
				return null;
			}

			return table;
		}

		internal virtual AttributedRootType CreateRootType(AttributedMetaTable table, Type type)
		{
			return new AttributedRootType(this, table, type);
		}

		internal virtual IReadOnlyCollection<TableAttribute> GetTableAttributes(Type type, bool shouldInherit)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return (TableAttribute[])type.GetCustomAttributes(typeof(TableAttribute), shouldInherit);
		}

		internal virtual ColumnAttribute TryGetColumnAttribute(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			return (ColumnAttribute)Attribute.GetCustomAttribute(member, typeof(ColumnAttribute));
		}

		internal virtual AssociationAttribute TryGetAssociationAttribute(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");

			return (AssociationAttribute)Attribute.GetCustomAttribute(member, typeof(AssociationAttribute));
		}

		internal virtual bool IsDeferredMember(MemberInfo member, Type storageType, AssociationAttribute associationAttribute)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			if (storageType == null)
				throw new ArgumentNullException("storageType");

			return IsDeferredType(storageType);
		}

		internal virtual bool DoesMemberRequireProxy(
			MemberInfo member, 
			Type storageType, 
			AssociationAttribute associationAttribute)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			if (storageType == null)
				throw new ArgumentNullException("storageType");

			return false;
		}


		private void InitStaticTables()
		{
			if (areStaticTablesInitialized)
				return;

			metaModelLock.AcquireWriterLock(Timeout.Infinite);
			try
			{
				if (areStaticTablesInitialized)
					return;

				var tables = new HashSet<MetaTable>();
				for (var type = contextType; type != typeof(DataContext); type = type.BaseType)
				{
					var fields = type.GetFields(
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					foreach (var field in fields)
					{
						var fieldType = field.FieldType;
						if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Table<>))
						{
							var rowType = fieldType.GetGenericArguments()[0];
							tables.Add(GetTableNoLocks(rowType));
						}
					}
					var properties = type.GetProperties(
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					foreach (var property in properties)
					{
						var propertyType = property.PropertyType;
						if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Table<>))
						{
							var rowType = propertyType.GetGenericArguments()[0];
							tables.Add(GetTableNoLocks(rowType));
						}
					}
				}
				staticTables = new List<MetaTable>(tables).AsReadOnly();
				areStaticTablesInitialized = true;
			}
			finally
			{
				metaModelLock.ReleaseWriterLock();
			}
		}

		private Type GetRoot(Type derivedType)
		{
			while (derivedType != null && derivedType != typeof(object))
			{
				var attrs = GetTableAttributes(derivedType, false);
				if (attrs.Any())
					return derivedType;
				derivedType = derivedType.BaseType;
			}
			return null;
		}
	}
}
