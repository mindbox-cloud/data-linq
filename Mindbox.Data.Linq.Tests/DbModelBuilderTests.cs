using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindbox.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests
{
	[TestClass]
	public class DbModelBuilderTests
	{
		[TestMethod]
		public void TableAttributeViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity4));

			Assert.IsNotNull(metaTable);
			Assert.AreEqual("administration.Staff", metaTable.TableName);
		}

		[TestMethod]
		public void TableAttributeViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity6));

			Assert.IsNotNull(metaTable);
			Assert.AreEqual("administration.Staff", metaTable.TableName);
		}
	}
}
