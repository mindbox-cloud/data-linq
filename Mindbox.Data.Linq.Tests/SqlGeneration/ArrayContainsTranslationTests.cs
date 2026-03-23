using System;
using System.Data.Linq;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mindbox.Data.Linq.Tests.SqlGeneration
{
	/// <summary>
	/// Tests that array.Contains(value) in LINQ queries is correctly translated to SQL IN clause.
	/// In .NET 10, the C# compiler generates MemoryExtensions.Contains(ReadOnlySpan, value)
	/// for this pattern instead of Enumerable.Contains — this must produce the same SQL.
	/// </summary>
	[TestClass]
	public class ArrayContainsTranslationTests
	{
		[TestMethod]
		public void ArrayContains_TranslatesToSqlIn()
		{
			var ids = new[] { 1, 2, 3 };

			using var connection = new DbConnectionStub();
			using var context = new DataContext(connection);

			var query = context.GetTable<SimpleEntity>().Where(t => ids.Contains(t.Id));

			using var command = context.GetCommand(query);

			Assert.AreEqual(
				"SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
				"FROM [SimpleTable] AS [t0]" + Environment.NewLine +
				"WHERE [t0].[Id] IN (@p0, @p1, @p2)",
				command.CommandText);
		}

		[TestMethod]
		public void ArrayContains_EmptyArray_TranslatesToFalse()
		{
			var ids = Array.Empty<int>();

			using var connection = new DbConnectionStub();
			using var context = new DataContext(connection);

			var query = context.GetTable<SimpleEntity>().Where(t => ids.Contains(t.Id));

			using var command = context.GetCommand(query);

			Assert.AreEqual(
				"SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
				"FROM [SimpleTable] AS [t0]" + Environment.NewLine +
				"WHERE 0 = 1",
				command.CommandText);
		}
	}
}
