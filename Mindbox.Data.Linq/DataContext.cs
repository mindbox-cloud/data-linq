using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Data.Linq.Mapping;
using System.Data.Linq.Provider;

namespace System.Data.Linq 
{
	/// <summary>
    /// The DataContext is the source of all entities mapped over a database connection.
    /// It tracks changes made to all retrieved entities and maintains an 'identity cache' 
    /// that guarantees that entities retrieved more than once are represented using the 
    /// same object instance.
    /// </summary>
    public class DataContext : IDisposable 
	{
		private static MethodInfo _miExecuteQuery;


		private static void ValidateTable(MetaTable metaTable)
		{
			// Associations can only be between entities - verify both that both ends of all
			// associations are entities.
			foreach (var assoc in metaTable.RowType.Associations)
			{
				if (!assoc.ThisMember.DeclaringType.IsEntity)
					throw Error.NonEntityAssociationMapping(
						assoc.ThisMember.DeclaringType.Type,
						assoc.ThisMember.Name,
						assoc.ThisMember.DeclaringType.Type);
				if (!assoc.OtherType.IsEntity)
					throw Error.NonEntityAssociationMapping(
						assoc.ThisMember.DeclaringType.Type,
						assoc.ThisMember.Name,
						assoc.OtherType.Type);
			}
		}


        private CommonDataServices services;
		private IProvider provider;
		private Dictionary<MetaTable, ITable> tables;
		private bool objectTrackingEnabled = true;
		private bool deferredLoadingEnabled = true;
		private bool disposed;
		private bool isInSubmitChanges;
		private DataLoadOptions loadOptions;
		private string statementsLabel;
		private ChangeConflictCollection conflicts;

		public DataContext(string fileOrServerOrConnection) 
		{
			if (fileOrServerOrConnection == null)
				throw Error.ArgumentNull("fileOrServerOrConnection");
			InitWithDefaultMapping(fileOrServerOrConnection);
        }

		public DataContext(string fileOrServerOrConnection, MappingSource mapping) 
		{
			if (fileOrServerOrConnection == null)
				throw Error.ArgumentNull("fileOrServerOrConnection");
			if (mapping == null)
				throw Error.ArgumentNull("mapping");
			Init(fileOrServerOrConnection, mapping);
        }

		public DataContext(IDbConnection connection) 
		{
			if (connection == null)
				throw Error.ArgumentNull("connection");
			InitWithDefaultMapping(connection);
        }

		public DataContext(IDbConnection connection, MappingSource mapping) 
		{
			if (connection == null)
				throw Error.ArgumentNull("connection");
			if (mapping == null)
				throw Error.ArgumentNull("mapping");
			Init(connection, mapping);
        }


		internal DataContext(DataContext context) {
            if (context == null) {
                throw Error.ArgumentNull("context");
            }
            this.Init(context.Connection, context.Mapping.MappingSource);
            this.LoadOptions = context.LoadOptions;
            this.Transaction = context.Transaction;
            this.Log = context.Log;
            this.CommandTimeout = context.CommandTimeout;
        }


		private DataContext() 
		{
		}


		/// <summary>
		/// The connection object used by this DataContext when executing queries and commands.
		/// </summary>
		public DbConnection Connection
		{
			get
			{
				CheckDispose();
				return provider.Connection;
			}
		}

		/// <summary>
		/// The transaction object used by this DataContext when executing queries and commands.
		/// </summary>
		public DbTransaction Transaction
		{
			get
			{
				CheckDispose();
				return provider.Transaction;
			}
			set
			{
				CheckDispose();
				provider.Transaction = value;
			}
		}
		
		/// <summary>
		/// Label that is added to sql statement to identify its source
		/// </summary>
		public string StatementsLabel
		{
			get
			{
				CheckDispose();
				return provider.StatementLabel;
			}
			set
			{
				CheckDispose();
				provider.StatementLabel = value;
			}
		}

		/// <summary>
		/// The command timeout to use when executing commands.
		/// </summary>
		public int CommandTimeout
		{
			get
			{
				CheckDispose();
				return provider.CommandTimeout;
			}
			set
			{
				CheckDispose();
				provider.CommandTimeout = value;
			}
		}

		/// <summary>
		/// A text writer used by this DataContext to output information such as query and commands
		/// being executed.
		/// </summary>
		public TextWriter Log
		{
			get
			{
				CheckDispose();
				return provider.Log;
			}
			set
			{
				CheckDispose();
				provider.Log = value;
			}
		}

		/// <summary>
		/// True if object tracking is enabled, false otherwise.  Object tracking
		/// includes identity caching and change tracking.  If tracking is turned off, 
		/// SubmitChanges and related functionality is disabled.  DeferredLoading is
		/// also disabled when object tracking is disabled.
		/// </summary>
		public bool ObjectTrackingEnabled
		{
			get
			{
				CheckDispose();
				return objectTrackingEnabled;
			}
			set
			{
				CheckDispose();
				if (Services.HasCachedObjects)
				{
					throw Error.OptionsCannotBeModifiedAfterQuery();
				}
				objectTrackingEnabled = value;
				if (!objectTrackingEnabled)
				{
					deferredLoadingEnabled = false;
				}
				// force reinitialization of cache/tracking objects
				services.ResetServices();
			}
		}

		/// <summary>
		/// True if deferred loading is enabled, false otherwise.  With deferred
		/// loading disabled, association members return default values and are 
		/// not defer loaded.
		/// </summary>
		public bool DeferredLoadingEnabled
		{
			get
			{
				CheckDispose();
				return deferredLoadingEnabled;
			}
			set
			{
				CheckDispose();
				if (Services.HasCachedObjects)
				{
					throw Error.OptionsCannotBeModifiedAfterQuery();
				}
				// can't have tracking disabled and deferred loading enabled
				if (!ObjectTrackingEnabled && value)
				{
					throw Error.DeferredLoadingRequiresObjectTracking();
				}
				deferredLoadingEnabled = value;
			}
		}

		/// <summary>
		/// The mapping model used to describe the entities
		/// </summary>
		public MetaModel Mapping
		{
			get
			{
				CheckDispose();
				return services.Model;
			}
		}

		/// <summary>
		/// The DataLoadOptions used to define prefetch behavior for defer loaded members
		/// and membership of related collections.
		/// </summary>
		public DataLoadOptions LoadOptions
		{
			get
			{
				CheckDispose();
				return loadOptions;
			}
			set
			{
				CheckDispose();
				if (services.HasCachedObjects && value != loadOptions)
					throw Error.LoadOptionsChangeNotAllowedAfterQuery();
				if (value != null)
					value.Freeze(Mapping);
				loadOptions = value;
			}
		}

		/// <summary>
		/// This list of change conflicts produced by the last call to SubmitChanges.  Use this collection
		/// to resolve conflicts after catching a ChangeConflictException and before calling SubmitChanges again.
		/// </summary>
		public ChangeConflictCollection ChangeConflicts
		{
			get
			{
				CheckDispose();
				return conflicts;
			}
		}


		internal CommonDataServices Services
		{
			get
			{
				CheckDispose();
				return services;
			}
		}

		/// <summary>
		/// Internal method that can be accessed by tests to retrieve the provider
		/// The IProvider result can then be cast to the actual provider to call debug methods like
		///   CheckQueries, QueryCount, EnableCacheLookup
		/// </summary>
		internal IProvider Provider
		{
			get
			{
				CheckDispose();
				return provider;
			}
		}

		public bool ShouldThrowReaderRowsPresenceMismatchException { get; set; }

		public void Dispose() 
		{            
            this.disposed = true;
            Dispose(true);
            // Technically, calling GC.SuppressFinalize is not required because the class does not
            // have a finalizer, but it does no harm, protects against the case where a finalizer is added
            // in the future, and prevents an FxCop warning.
            GC.SuppressFinalize(this);
        }

		/// <summary>
		/// Returns the strongly-typed Table object representing a collection of persistent entities.  
		/// Use this collection as the starting point for queries.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity objects. In case of a persistent hierarchy
		/// the entity specified must be the base type of the hierarchy.</typeparam>
		/// <returns></returns>
		public Table<TEntity> GetTable<TEntity>() 
			where TEntity : class
		{
			CheckDispose();
			var metaTable = services.Model.GetTable(typeof(TEntity));
			if (metaTable == null)
				throw Error.TypeIsNotMarkedAsTable(typeof(TEntity));
			var table = GetTable(metaTable);
			if (table.ElementType != typeof(TEntity))
				throw Error.CouldNotGetTableForSubtype(typeof(TEntity), metaTable.RowType.Type);
			return (Table<TEntity>)table;
		}

		/// <summary>
		/// Returns the weakly-typed ITable object representing a collection of persistent entities. 
		/// Use this collection as the starting point for dynamic/runtime-computed queries.
		/// </summary>
		/// <param name="type">The type of the entity objects. In case of a persistent hierarchy
		/// the entity specified must be the base type of the hierarchy.</param>
		/// <returns></returns>
		public ITable GetTable(Type type)
		{
			CheckDispose();
			if (type == null)
				throw Error.ArgumentNull("type");
			var metaTable = services.Model.GetTable(type);
			if (metaTable == null)
				throw Error.TypeIsNotMarkedAsTable(type);
			if (metaTable.RowType.Type != type)
				throw Error.CouldNotGetTableForSubtype(type, metaTable.RowType.Type);
			return GetTable(metaTable);
		}

		/// <summary>
		/// Returns true if the database specified by the connection object exists.
		/// </summary>
		/// <returns></returns>
		public bool DatabaseExists()
		{
			CheckDispose();
			return provider.DatabaseExists();
		}

		/// <summary>
		/// Creates a new database instance (catalog or file) at the location specified by the connection
		/// using the metadata encoded within the entities or mapping file.
		/// </summary>
		public void CreateDatabase()
		{
			CheckDispose();
			provider.CreateDatabase();
		}

		/// <summary>
		/// Deletes the database instance at the location specified by the connection.
		/// </summary>
		public void DeleteDatabase()
		{
			CheckDispose();
			provider.DeleteDatabase();
		}

		/// <summary>
		/// Submits one or more commands to the database reflecting the changes made to the retreived entities.
		/// If a transaction is not already specified one will be created for the duration of this operation.
		/// If a change conflict is encountered a ChangeConflictException will be thrown.
		/// </summary>
		public void SubmitChanges(IMeasureProvider measureProvider = null)
		{
			CheckDispose();
			SubmitChanges(ConflictMode.FailOnFirstConflict, measureProvider);
		}

		/// <summary>
		/// Submits one or more commands to the database reflecting the changes made to the retreived entities.
		/// If a transaction is not already specified one will be created for the duration of this operation.
		/// If a change conflict is encountered a ChangeConflictException will be thrown.  
		/// You can override this method to implement common conflict resolution behaviors.
		/// </summary>
		/// <param name="failureMode">Determines how SubmitChanges handles conflicts.</param>
		public virtual void SubmitChanges(ConflictMode failureMode, IMeasureProvider measureProvider = null)
		{
			CheckDispose();
			CheckNotInSubmitChanges();
			VerifyTrackingEnabled();
			conflicts.Clear();

			try
			{
				isInSubmitChanges = true;

				if (Transactions.Transaction.Current == null && provider.Transaction == null)
				{
					var openedConnection = false;
					DbTransaction transaction = null;
					try
					{
						if (provider.Connection.State == ConnectionState.Open)
							provider.ClearConnection();
						if (provider.Connection.State == ConnectionState.Closed)
						{
							provider.Connection.Open();
							openedConnection = true;
						}
						transaction = provider.Connection.BeginTransaction(IsolationLevel.ReadCommitted);
						provider.Transaction = transaction;
						new ChangeProcessor(services, this).SubmitChanges(failureMode, measureProvider);
						AcceptChanges();

						// to commit a transaction, there can be no open readers
						// on the connection.
						provider.ClearConnection();

						transaction.Commit();
					}
					catch
					{
						if (transaction != null)
						{
							transaction.Rollback();
						}
						throw;
					}
					finally
					{
						provider.Transaction = null;
						if (openedConnection)
							provider.Connection.Close();
					}
				}
				else
				{
					new ChangeProcessor(services, this).SubmitChanges(failureMode, measureProvider);
					AcceptChanges();
				}
			}
			finally
			{
				isInSubmitChanges = false;
			}
		}

		/// <summary>
		/// Refresh the specified object using the mode specified.  If the refresh
		/// cannot be performed (for example if the object no longer exists in the
		/// database) an InvalidOperationException is thrown.
		/// </summary>
		/// <param name="mode">How the refresh should be performed.</param>
		/// <param name="entity">The object to refresh.  The object must be
		/// the result of a previous query.</param>
		public void Refresh(RefreshMode mode, object entity)
		{
			CheckDispose();
			CheckNotInSubmitChanges();
			VerifyTrackingEnabled();
			if (entity == null)
				throw Error.ArgumentNull("entity");
			var items = Array.CreateInstance(entity.GetType(), 1);
			items.SetValue(entity, 0);
			Refresh(mode, items);
		}

		/// <summary>
		/// Refresh a set of objects using the mode specified.  If the refresh
		/// cannot be performed (for example if the object no longer exists in the
		/// database) an InvalidOperationException is thrown.
		/// </summary>
		/// <param name="mode">How the refresh should be performed.</param>
		/// <param name="entities">The objects to refresh.</param>
		public void Refresh(RefreshMode mode, params object[] entities)
		{
			CheckDispose(); // code hygeine requirement

			if (entities == null)
				throw Error.ArgumentNull("entities");

			Refresh(mode, (IEnumerable)entities);
		}

		/// <summary>
		/// Refresh a collection of objects using the mode specified.  If the refresh
		/// cannot be performed (for example if the object no longer exists in the
		/// database) an InvalidOperationException is thrown.
		/// </summary>
		/// <param name="mode">How the refresh should be performed.</param>
		/// <param name="entities">The collection of objects to refresh.</param>
		public void Refresh(RefreshMode mode, IEnumerable entities)
		{
			CheckDispose();
			CheckNotInSubmitChanges();
			VerifyTrackingEnabled();

			if (entities == null)
				throw Error.ArgumentNull("entities");

			// if the collection is a query, we need to execute and buffer,
			// since below we will be issuing additional queries and can only
			// have a single reader open.
			var list = entities.Cast<object>().ToList();

			// create a fresh context to fetch new state from
			var refreshContext = CreateRefreshContext();

			foreach (var o in list)
			{
				// verify that each object in the list is an entity
				var inheritanceRoot = services.Model.GetMetaType(o.GetType()).InheritanceRoot;
				GetTable(inheritanceRoot.Type);

				var trackedObject = services.ChangeTracker.GetTrackedObject(o);
				if (trackedObject == null)
					throw Error.UnrecognizedRefreshObject();

				if (trackedObject.IsNew)
					throw Error.RefreshOfNewObject();

				// query to get the current database values
				var keyValues = CommonDataServices.GetKeyValues(trackedObject.Type, trackedObject.Original);
				var freshInstance = refreshContext.Services.GetObjectByKey(trackedObject.Type, keyValues);
				if (freshInstance == null)
					throw Error.RefreshOfDeletedObject();

				// refresh the tracked object using the new values and
				// the mode specified.
				trackedObject.Refresh(mode, freshInstance);
			}
		}

		/// <summary>
		/// Returns an IDbCommand object representing the query in the database server's
		/// native query language.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public DbCommand GetCommand(IQueryable query)
		{
			CheckDispose();
			if (query == null)
				throw Error.ArgumentNull("query");
			return provider.GetCommand(query.Expression);
		}

		/// <summary>
		/// Computes the un-ordered set of objects that have changed
		/// </summary>
		/// <returns></returns>
		public ChangeSet GetChangeSet()
		{
			CheckDispose();
			return new ChangeProcessor(services, this).GetChangeSet();
		}

		/// <summary>
		/// Execute a command against the database server that does not return a sequence of objects.
		/// The command is specified using the server's native query language, such as SQL.
		/// </summary>
		/// <param name="command">The command specified in the server's native query language.</param>
		/// <param name="parameters">The parameter values to use for the query.</param>
		/// <returns>A single integer return value</returns>
		public int ExecuteCommand(string command, params object[] parameters)
		{
			CheckDispose();
			if (command == null)
				throw Error.ArgumentNull("command");
			if (parameters == null)
				throw Error.ArgumentNull("parameters");
			return (int)ExecuteMethodCall(this, (MethodInfo)MethodBase.GetCurrentMethod(), command, parameters).ReturnValue;
		}

		/// <summary>
		/// Execute the sequence returning query against the database server. 
		/// The query is specified using the server's native query language, such as SQL.
		/// </summary>
		/// <typeparam name="TResult">The element type of the result sequence.</typeparam>
		/// <param name="query">The query specified in the server's native query language.</param>
		/// <param name="parameters">The parameter values to use for the query.</param>
		/// <returns>An IEnumerable sequence of objects.</returns>
		public IEnumerable<TResult> ExecuteQuery<TResult>(string query, params object[] parameters)
		{
			CheckDispose();
			if (query == null)
				throw Error.ArgumentNull("query");
			if (parameters == null)
				throw Error.ArgumentNull("parameters");
			return (IEnumerable<TResult>)ExecuteMethodCall(
					this, 
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(TResult)), 
					query, 
					parameters)
				.ReturnValue;
		}

		/// <summary>
		/// Execute the sequence returning query against the database server. 
		/// The query is specified using the server's native query language, such as SQL.
		/// </summary>
		/// <param name="elementType">The element type of the result sequence.</param>
		/// <param name="query">The query specified in the server's native query language.</param>
		/// <param name="parameters">The parameter values to use for the query.</param>
		/// <returns></returns>
		public IEnumerable ExecuteQuery(Type elementType, string query, params object[] parameters)
		{
			CheckDispose();
			if (elementType == null)
				throw Error.ArgumentNull("elementType");
			if (query == null)
				throw Error.ArgumentNull("query");
			if (parameters == null)
				throw Error.ArgumentNull("parameters");
			if (_miExecuteQuery == null)
				_miExecuteQuery =
					typeof(DataContext).GetMethods().Single(m => m.Name == "ExecuteQuery" && m.GetParameters().Length == 2);
			return (IEnumerable)ExecuteMethodCall(this, _miExecuteQuery.MakeGenericMethod(elementType), query, parameters)
				.ReturnValue;
		}

		/// <summary>
		/// Translates the data from a DbDataReader into sequence of objects.
		/// </summary>
		/// <typeparam name="TResult">The element type of the resulting sequence</typeparam>
		/// <param name="reader">The DbDataReader to translate</param>
		/// <returns>The translated sequence of objects</returns>
		public IEnumerable<TResult> Translate<TResult>(DbDataReader reader)
		{
			CheckDispose();
			return (IEnumerable<TResult>)Translate(typeof(TResult), reader);
		}

		/// <summary>
		/// Translates the data from a DbDataReader into sequence of objects.
		/// </summary>
		/// <param name="elementType">The element type of the resulting sequence</param>
		/// <param name="reader">The DbDataReader to translate</param>
		/// <returns>The translated sequence of objects</returns>
		public IEnumerable Translate(Type elementType, DbDataReader reader)
		{
			CheckDispose();
			if (elementType == null)
				throw Error.ArgumentNull("elementType");
			if (reader == null)
				throw Error.ArgumentNull("reader");
			return provider.Translate(elementType, reader);
		}

		/// <summary>
		/// Translates the data from a DbDataReader into IMultipleResults.
		/// </summary>
		/// <param name="reader">The DbDataReader to translate</param>
		/// <returns>The translated sequence of objects</returns>
		public IMultipleResults Translate(DbDataReader reader)
		{
			CheckDispose();
			if (reader == null)
				throw Error.ArgumentNull("reader");
			return provider.Translate(reader);
		}

		public T CreateObject<T>()
		{
			return Services.Model.CreateObject<T>();
		}


		/// <summary>
		/// Executes the equivalent of the specified method call on the database server.
		/// </summary>
		/// <param name="instance">The instance the method is being called on.</param>
		/// <param name="methodInfo">The reflection MethodInfo for the method to invoke.</param>
		/// <param name="parameters">The parameters for the method call.</param>
		/// <returns>The result of the method call. 
		/// Use this type's ReturnValue property to access the actual return value.</returns>
		internal protected IExecuteResult ExecuteMethodCall(
			object instance,
			MethodInfo methodInfo,
			params object[] parameters)
		{
			CheckDispose();
			if (instance == null)
				throw Error.ArgumentNull("instance");
			if (methodInfo == null)
				throw Error.ArgumentNull("methodInfo");
			if (parameters == null)
				throw Error.ArgumentNull("parameters");
			return provider.Execute(GetMethodCall(instance, methodInfo, parameters));
		}

		/// <summary>
		/// Create a query object for the specified method call.
		/// </summary>
		/// <typeparam name="TResult">The element type of the query.</typeparam>
		/// <param name="instance">The instance the method is being called on.</param>
		/// <param name="methodInfo">The reflection MethodInfo for the method to invoke.</param>
		/// <param name="parameters">The parameters for the method call.</param>
		/// <returns>The returned query object</returns>
		internal protected IQueryable<TResult> CreateMethodCallQuery<TResult>(
			object instance,
			MethodInfo methodInfo,
			params object[] parameters)
		{
			CheckDispose();
			if (instance == null)
				throw Error.ArgumentNull("instance");
			if (methodInfo == null)
				throw Error.ArgumentNull("methodInfo");
			if (parameters == null)
				throw Error.ArgumentNull("parameters");
			if (!typeof(IQueryable<TResult>).IsAssignableFrom(methodInfo.ReturnType))
				throw Error.ExpectedQueryableArgument("methodInfo", typeof(IQueryable<TResult>));
			return new DataQuery<TResult>(this, this.GetMethodCall(instance, methodInfo, parameters));
		}

		/// <summary>
		/// Execute a dynamic insert
		/// </summary>
		/// <param name="entity"></param>
		internal protected void ExecuteDynamicInsert(object entity)
		{
			CheckDispose();
			if (entity == null)
				throw Error.ArgumentNull("entity");
			CheckInSubmitChanges();
			var tracked = services.ChangeTracker.GetTrackedObject(entity);
			if (tracked == null)
				throw Error.CannotPerformOperationForUntrackedObject();
			services.ChangeDirector.DynamicInsert(tracked);
		}

		/// <summary>
		/// Execute a dynamic update
		/// </summary>
		/// <param name="entity"></param>
		internal protected void ExecuteDynamicUpdate(object entity)
		{
			CheckDispose();
			if (entity == null)
				throw Error.ArgumentNull("entity");
			CheckInSubmitChanges();
			var tracked = services.ChangeTracker.GetTrackedObject(entity);
			if (tracked == null)
				throw Error.CannotPerformOperationForUntrackedObject();
			var result = services.ChangeDirector.DynamicUpdate(tracked);
			if (result == 0)
				throw new ChangeConflictException();
		}

		/// <summary>
		/// Execute a dynamic delete
		/// </summary>
		/// <param name="entity"></param>
		internal protected void ExecuteDynamicDelete(object entity)
		{
			CheckDispose();
			if (entity == null)
				throw Error.ArgumentNull("entity");
			CheckInSubmitChanges();
			var tracked = services.ChangeTracker.GetTrackedObject(entity);
			if (tracked == null)
				throw Error.CannotPerformOperationForUntrackedObject();
			var result = services.ChangeDirector.DynamicDelete(tracked);
			if (result == 0)
				throw new ChangeConflictException();
		}


        // Not implementing finalizer here because there are no unmanaged resources
        // to release. See http://msdnwiki.microsoft.com/en-us/mtpswiki/12afb1ea-3a17-4a3f-a1f0-fcdb853e2359.aspx
        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing) 
		{
            // Implemented but empty so that derived contexts can implement
            // a finalizer that potentially cleans up unmanaged resources.
            if (disposing) 
			{
                if (provider != null) 
				{
                    provider.Dispose();
                    provider = null;
                }
                services = null;
                tables = null;
                loadOptions = null;
            }
        }


        internal void CheckDispose() 
		{
	        if (disposed)
		        throw Error.DataContextCannotBeUsedAfterDispose();
		}

		internal object Clone()
		{
			CheckDispose();
			return Activator.CreateInstance(GetType(), new object[] { Connection, Mapping.MappingSource });
		}

		internal void ClearCache()
		{
			CheckDispose();
			services.ResetServices();
		}

		/// <summary>
		/// Verify that change tracking is enabled, and throw an exception
		/// if it is not.
		/// </summary>
		internal void VerifyTrackingEnabled()
		{
			CheckDispose();
			if (!ObjectTrackingEnabled)
				throw Error.ObjectTrackingRequired();
		}

		/// <summary>
		/// Verify that submit changes is not occurring
		/// </summary>
		internal void CheckNotInSubmitChanges()
		{
			CheckDispose();
			if (isInSubmitChanges)
				throw Error.CannotPerformOperationDuringSubmitChanges();
		}

		/// <summary>
		/// Verify that submit changes is occurring
		/// </summary>
		internal void CheckInSubmitChanges()
		{
			CheckDispose();
			if (!isInSubmitChanges)
				throw Error.CannotPerformOperationOutsideSubmitChanges();
		}

		internal DataContext CreateRefreshContext()
		{
			CheckDispose();
			return new DataContext(this);
		}

		/// <summary>
		/// Returns the query text in the database server's native query language
		/// that would need to be executed to perform the specified query.
		/// </summary>
		/// <param name="query">The query</param>
		/// <returns></returns>
		internal string GetQueryText(IQueryable query)
		{
			CheckDispose();
			if (query == null)
				throw Error.ArgumentNull("query");
			return provider.GetQueryText(query.Expression);
		}

		/// <summary>
		/// Returns the command text in the database server's native query langauge
		/// that would need to be executed in order to persist the changes made to the
		/// objects back into the database.
		/// </summary>
		/// <returns></returns>
		internal string GetChangeText()
		{
			CheckDispose();
			VerifyTrackingEnabled();
			return new ChangeProcessor(services, this).GetChangeText();
		}

		/// <summary>
		/// Remove all Include\Subquery LoadOptions settings.
		/// </summary>
		internal void ResetLoadOptions()
		{
			CheckDispose();
			loadOptions = null;
		}


        private void InitWithDefaultMapping(object connection) 
		{
            Init(connection, new AttributeMappingSource());
        }

        private void Init(object connection, MappingSource mapping) 
		{
            var model = mapping.GetModel(GetType());
            services = new CommonDataServices(this, model);
            conflicts = new ChangeConflictCollection();

            // determine provider
	        if (model.ProviderType == null)
		        throw Error.ProviderTypeNull();
	        var providerType = model.ProviderType;

	        if (!typeof(IProvider).IsAssignableFrom(providerType))
		        throw Error.ProviderDoesNotImplementRequiredInterface(providerType, typeof(IProvider));

	        provider = (IProvider)Activator.CreateInstance(providerType);
            provider.Initialize(services, connection);

            tables = new Dictionary<MetaTable, ITable>();
            InitTables(this);
        }

        private ITable GetTable(MetaTable metaTable) 
		{
            Debug.Assert(metaTable != null);
            ITable tb;
            if (!tables.TryGetValue(metaTable, out tb)) 
			{
                ValidateTable(metaTable);
                var tbType = typeof(Table<>).MakeGenericType(metaTable.RowType.Type);
                tb = (ITable)Activator.CreateInstance(
					tbType, 
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
					null, 
					new object[] { this, metaTable }, 
					null);
                tables.Add(metaTable, tb);
            }
            return tb;
        }

        private void InitTables(object schema) 
		{
            var fields = schema.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var fi in fields) {
                var ft = fi.FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Table<>)) 
				{
                    var tb = (ITable)fi.GetValue(schema);
                    if (tb == null) 
					{
                        var rowType = ft.GetGenericArguments()[0];
                        tb = GetTable(rowType);
                        fi.SetValue(schema, tb);
                    }
                }     
            }
        }

        private void AcceptChanges() 
		{
            CheckDispose();
            VerifyTrackingEnabled();
            services.ChangeTracker.AcceptChanges();
        }

        private Expression GetMethodCall(object instance, MethodInfo methodInfo, params object[] parameters) 
		{
            CheckDispose();
            if (parameters.Length > 0) 
			{
                var pis = methodInfo.GetParameters();
                var args = new List<Expression>(parameters.Length);
                for (var i = 0; i < parameters.Length; i++) 
				{
                    var pType = pis[i].ParameterType;
					if (pType.IsByRef)
						pType = pType.GetElementType();
					args.Add(Expression.Constant(parameters[i], pType));
                }
                return Expression.Call(Expression.Constant(instance), methodInfo, args);
            }
            return Expression.Call(Expression.Constant(instance), methodInfo);
        }
    }
}
