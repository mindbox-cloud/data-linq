using System;
using System.Data.Linq;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mindbox.Data.Linq.Tests.SqlGeneration
{
	[TestClass]
	public class UserDefinedFunctionTranslationTests
	{
		[TestMethod]
		public void SipmleQueryTranslation()
		{
			using (var connection = new DbConnectionStub())
			{
				using (var context = new DataContext(connection))
				{
					var query = context.GetTable<SimpleEntity>().Where(t => t.Id > 1);
					using (var command = context.GetCommand(query))
					{
						Assert.AreEqual(
"SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
"FROM [SimpleTable] AS [t0]" + Environment.NewLine +
"WHERE [t0].[Id] > @p0", 
							command.CommandText);
					}
				}
			}
		}

		[TestMethod]
		public void QueryWithUserDefinedFunctionInDataContextTranslationTest()
		{
			using (var connection = new DbConnectionStub())
			{
				using (var context = new SomeDataContext(connection))
				{
					var query = context.GetTable<SimpleEntity>().OrderBy(t => context.Random());
					using (var command = context.GetCommand(query))
					{
						Assert.AreEqual(
"SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
"FROM [SimpleTable] AS [t0]" + Environment.NewLine +
"ORDER BY NEWID()",
							command.CommandText);
					}
				}
			}
		}

		[TestMethod]
		public void QueryWithUserDefinedFunctionNotInDataContextTranslationTest()
		{
			using (var connection = new DbConnectionStub())
			{
				using (var context = new DataContext(connection))
				{
					var query = context.GetTable<SimpleEntity>().OrderBy(t => UserDefinedFunctions.Random());
					using (var command = context.GetCommand(query))
					{
						Assert.AreEqual(
"SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
"FROM [SimpleTable] AS [t0]" + Environment.NewLine +
"ORDER BY NEWID()",
							command.CommandText);
					}
				}
			}
		}
	}
}