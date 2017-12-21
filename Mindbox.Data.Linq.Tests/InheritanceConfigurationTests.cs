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
	public class InheritanceConfigurationTests
	{
		[TestMethod]
		public void AttributeInheritanceWorks()
		{
			var mappingSource = new MindboxMappingSource(new MindboxMappingConfiguration(), false);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity1));

			Assert.IsTrue(metaTable.RowType.IsInheritanceDefault);
			Assert.AreEqual(2, metaTable.RowType.InheritanceTypes.Count);
		}

		[TestMethod]
		public void ConfigurationInheritanceWorks()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.AddInheritance<TestEntity1, TestEntity3>("3");
			var mappingSource = new MindboxMappingSource(configuration, false);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity1));

			Assert.IsTrue(metaTable.RowType.IsInheritanceDefault);
			Assert.AreEqual(3, metaTable.RowType.InheritanceTypes.Count);
		}
	}
}
