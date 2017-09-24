using System;
using System.Data;
using System.Data.Common;

namespace Mindbox.Data.Linq.Tests
{
	internal class DbConnectionStub : DbConnection
	{
		public override string ConnectionString {
			get => "Server=no;Persist Security Info=True;initial catalog=Api;Integrated Security=SSPI;";
			set => throw new NotImplementedException();
		}

		public override string Database => throw new NotImplementedException();

		public override string DataSource => throw new NotImplementedException();

		public override string ServerVersion => "100500";

		public override ConnectionState State => ConnectionState.Open;

		public override void ChangeDatabase(string databaseName)
		{
			throw new NotImplementedException();
		}

		public override void Close()
		{
			throw new NotImplementedException();
		}

		public override void Open()
		{
			throw new NotImplementedException();
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
			throw new NotImplementedException();
		}

		protected override DbCommand CreateDbCommand()
		{
			return new DbCommandStub(this);
		}
	}
}