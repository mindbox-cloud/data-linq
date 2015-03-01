using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Data.Linq.SqlClient.Implementation
{
	/// <summary>
	/// Internal interface type defining the operations dynamic materialization functions need to perform when
	/// materializing objects, without reflecting/invoking privates.
	/// <remarks>This interface is required because our anonymously hosted materialization delegates 
	/// run under partial trust and cannot access non-public members of types in the fully trusted 
	/// framework assemblies.</remarks>
	/// </summary>
	public abstract class ObjectMaterializer<TDataReader>
		where TDataReader : DbDataReader
	{
		public static IEnumerable<TOutput> Convert<TOutput>(IEnumerable source)
		{
			foreach (var value in source)
				yield return DBConvert.ChangeType<TOutput>(value);
		}

		public static IGrouping<TKey, TElement> CreateGroup<TKey, TElement>(TKey key, IEnumerable<TElement> items)
		{
			return new ObjectReaderCompiler.Group<TKey, TElement>(key, items);
		}

		public static IOrderedEnumerable<TElement> CreateOrderedEnumerable<TElement>(IEnumerable<TElement> items)
		{
			return new ObjectReaderCompiler.OrderedResults<TElement>(items);
		}

		public static Exception ErrorAssignmentToNull(Type type)
		{
			return Error.CannotAssignNull(type);
		}


		// These are public fields rather than properties for access speed
		public int[] Ordinals;
		public object[] Globals;
		public object[] Locals;
		public object[] Arguments;
		public TDataReader DataReader;
		public DbDataReader BufferReader;


		public abstract bool CanDeferLoad { get; }


		public abstract object InsertLookup(int globalMetaType, object instance);

		public abstract void SendEntityMaterialized(int globalMetaType, object instance);

		public abstract IEnumerable ExecuteSubQuery(int iSubQuery, object[] args);

		public abstract IEnumerable<T> GetLinkSource<T>(int globalLink, int localFactory, object[] keyValues);

		public abstract IEnumerable<T> GetNestedLinkSource<T>(int globalLink, int localFactory, object instance);

		public abstract bool Read();

		public virtual T CreateEntityProxy<T>()
			where T : class, new()
		{
			throw new NotSupportedException();
		}
	}
}
