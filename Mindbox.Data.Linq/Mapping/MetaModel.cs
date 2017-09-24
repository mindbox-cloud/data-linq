using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

namespace System.Data.Linq.Mapping 
{
    /// <summary>
    /// A MetaModel is an abstraction representing the mapping between a database and domain objects
    /// </summary>
    public abstract class MetaModel 
	{
		/// <summary>
		/// Internal value used to determine a reference identity for comparing meta models
		/// without needing to keep track of the actual meta model reference.
		/// </summary>
		private readonly object identity = new object();


        /// <summary>
        /// The mapping source that originated this model.
        /// </summary>
        public abstract MappingSource MappingSource { get; }

        /// <summary>
        /// The type of DataContext type this model describes.
        /// </summary>
        public abstract Type ContextType { get; }

        /// <summary>
        /// The name of the database.
        /// </summary>
        public abstract string DatabaseName { get; }

        /// <summary>
        /// The CLR type that implements IProvider to use as a provider.
        /// </summary>
        public abstract Type ProviderType { get; }


		internal object Identity
		{
			get { return identity; }
		}


        /// <summary>
        /// Gets the MetaTable associated with a given type.
        /// </summary>
        /// <param name="rowType">The CLR row type.</param>
        /// <returns>The MetaTable if one exists, otherwise null.</returns>
        public abstract MetaTable GetTable(Type rowType);

        /// <summary>
        /// Gets the MetaFunction corresponding to a database function: user-defined function, 
        /// table-valued function or stored-procedure.
        /// </summary>
        /// <param name="method">The method defined on the DataContext or 
        /// subordinate class that represents the database function.</param>
        /// <returns>The MetaFunction if one exists, otherwise null.</returns>
        public abstract MetaFunction GetFunction(MethodInfo method);

        /// <summary>
        /// Get an enumeration of all tables.
        /// </summary>
        /// <returns>An enumeration of all the MetaTables</returns>
        public abstract IEnumerable<MetaTable> GetTables();

        /// <summary>
        /// This method discovers the MetaType for the given Type.
        /// </summary>
        public abstract MetaType GetMetaType(Type type);


	    internal virtual bool ShouldEntityProxyBeCreated(Type entityType)
	    {
		    if (entityType == null)
			    throw new ArgumentNullException("entityType");

		    return false;
	    }

		internal virtual object CreateEntityProxy(Type entityType)
	    {
		    if (entityType == null)
			    throw new ArgumentNullException("entityType");

		    throw new NotSupportedException();
	    }

		internal virtual Type UnproxyType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return type;
		}

	    internal T CreateObject<T>()
	    {
		    if (ShouldEntityProxyBeCreated(typeof(T)))
			    return (T)CreateEntityProxy(typeof(T));
		    return Activator.CreateInstance<T>();
	    }
	}
}
