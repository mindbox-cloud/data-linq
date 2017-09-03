using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Mindbox.Data.Linq.Tests
{
	internal sealed class DbCommandStub : DbCommand
	{
		private class DbParametersStub : DbParameterCollection
		{
			private readonly ICollection<DbParameter> dbParameters = new List<DbParameter>();
			public override int Count => dbParameters.Count;
			public override bool IsFixedSize { get; }
			public override bool IsReadOnly { get; }
			public override bool IsSynchronized { get; }

			public override object SyncRoot => throw new NotImplementedException();

			public override int Add(object value)
			{
				dbParameters.Add((DbParameter)value);
				return 0;
			}

			public override void AddRange(Array values)
			{
				var parameters = values.Cast<DbParameter>();

				foreach (var parameter in parameters)
				{
					dbParameters.Add(parameter);
				}
			}

			public override void Clear()
			{
				throw new NotImplementedException();
			}

			public override bool Contains(object value)
			{
				throw new NotImplementedException();
			}

			public override bool Contains(string value)
			{
				throw new NotImplementedException();
			}

			public override void CopyTo(Array array, int index)
			{
				throw new NotImplementedException();
			}

			public override IEnumerator GetEnumerator()
			{
				return dbParameters.GetEnumerator();
			}

			public override int IndexOf(object value)
			{
				throw new NotImplementedException();
			}

			public override int IndexOf(string parameterName)
			{
				throw new NotImplementedException();
			}

			public override void Insert(int index, object value)
			{
				throw new NotImplementedException();
			}

			public override void Remove(object value)
			{
				throw new NotImplementedException();
			}

			public override void RemoveAt(int index)
			{
				throw new NotImplementedException();
			}

			public override void RemoveAt(string parameterName)
			{
				throw new NotImplementedException();
			}

			protected override DbParameter GetParameter(int index)
			{
				throw new NotImplementedException();
			}

			protected override DbParameter GetParameter(string parameterName)
			{
				throw new NotImplementedException();
			}

			protected override void SetParameter(int index, DbParameter value)
			{
				throw new NotImplementedException();
			}

			protected override void SetParameter(string parameterName, DbParameter value)
			{
				throw new NotImplementedException();
			}
		}

		public DbCommandStub(DbConnection dbConnection)
		{
			this.dbConnection = dbConnection;

			dbParameterCollectionMock = new DbParametersStub();
		}

		public override void Prepare()
		{
			throw new NotImplementedException();
		}

		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }
		public override CommandType CommandType { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }

		protected override DbConnection DbConnection
		{
			get { return dbConnection; }
			set { throw new NotImplementedException(); }
		}

		protected override DbParameterCollection DbParameterCollection => dbParameterCollectionMock;

		protected override DbTransaction DbTransaction
		{
			get { return null; }
			set
			{
				// do nothing
			}
		}

		public override bool DesignTimeVisible { get; set; }

		public override void Cancel()
		{
			throw new NotImplementedException();
		}

		protected override DbParameter CreateDbParameter()
		{
			return new DbParameterStub();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			throw new NotImplementedException();
		}

		public override int ExecuteNonQuery()
		{
			return 0;
		}

		public override object ExecuteScalar()
		{
			throw new NotImplementedException();
		}

		private readonly DbParameterCollection dbParameterCollectionMock;
		private readonly ICollection<DbParameter> dbParameters = new List<DbParameter>();
		private readonly DbConnection dbConnection;
	}
}