using System;
using System.Data.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindbox.Data.Linq.Tests.SqlGeneration;

namespace Mindbox.Data.Linq.Tests
{
	[TestClass]
	public class DataLoadOptionsLoadWithTests
	{
		private DataContext dataContext;

		[TestInitialize]
		public void TestInitialize()
		{
			dataContext = new SomeDataContext(new DbConnectionStub());
		}

		[TestCleanup]
		public void TestCleanup()
		{
			dataContext.Dispose();
		}

		[TestMethod]
		public void TypeWithoutInheritanceMapping_Success()
		{
			var loadOptions = new DataLoadOptions(dataContext.Mapping);

			loadOptions.LoadWith<EntityWithoutInheritanceMapping>(e => e.Staff);

			Assert.IsTrue(loadOptions.IsPreloaded(
				typeof(EntityWithoutInheritanceMapping)
					.GetProperty(nameof(EntityWithoutInheritanceMapping.Staff))));
		}

		[TestMethod]
		public void RootTypeWithInheritanceMapping_Success()
		{
			var loadOptions = new DataLoadOptions(dataContext.Mapping);

			loadOptions.LoadWith<RootEntityWithInheritanceMapping>(e => e.Creator1);

			Assert.IsTrue(loadOptions.IsPreloaded(
				typeof(RootEntityWithInheritanceMapping)
					.GetProperty(nameof(RootEntityWithInheritanceMapping.Creator1))));
		}

		[TestMethod]
		public void DescendantEntityWithInheritanceMapping_BaseClassProperty_Exception()
		{
			var loadOptions = new DataLoadOptions(dataContext.Mapping);

			AssertException.Throws<InvalidOperationException>(
				() => loadOptions.LoadWith<DescendantEntityWithInheritanceMapping>(e => e.Creator1),
				$"Type {typeof(DescendantEntityWithInheritanceMapping)} is not the root type of the inheritance mapping hierarchy," +
				$" so it can't be used for automatic loading.");
		}

		[TestMethod]
		public void DescendantEntityWithInheritanceMapping_DescendantClassProperty_Exception()
		{
			var loadOptions = new DataLoadOptions(dataContext.Mapping);

			AssertException.Throws<InvalidOperationException>(
				() => loadOptions.LoadWith<DescendantEntityWithInheritanceMapping>(e => e.Creator2),
				$"Type {typeof(DescendantEntityWithInheritanceMapping)} is not root type in the inheritance mapping hierarchy," +
				$" so it can't be used for automatic loading.");
		}
	}
}