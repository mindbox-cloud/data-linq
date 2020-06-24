﻿using System.Collections.Generic;
using System.Data.Linq;
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
			var mappingSource = new MindboxMappingSource(new MindboxMappingConfiguration(), new Dictionary<string, bool>());
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(RootEntityWithInheritanceMapping));

			Assert.IsTrue(metaTable.RowType.IsInheritanceDefault);
			Assert.AreEqual(2, metaTable.RowType.InheritanceTypes.Count);
		}

		[TestMethod]
		public void ConfigurationInheritanceWorks()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.AddInheritance<RootEntityWithInheritanceMapping, TestEntity3>("3");
			var mappingSource = new MindboxMappingSource(configuration, new Dictionary<string, bool>());
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(RootEntityWithInheritanceMapping));

			Assert.IsTrue(metaTable.RowType.IsInheritanceDefault);
			Assert.AreEqual(3, metaTable.RowType.InheritanceTypes.Count);
		}
	}
}
