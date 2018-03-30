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
			var loadOptions = new DataLoadOptions();

			loadOptions.LoadWith<EntityWithoutInheritanceMapping>(e => e.Staff);

			dataContext.LoadOptions = loadOptions;

			Assert.AreEqual(loadOptions, dataContext.LoadOptions);
		}

		[TestMethod]
		public void RootTypeWithInheritanceMapping_Success()
		{
			var loadOptions = new DataLoadOptions();

			loadOptions.LoadWith<RootEntityWithInheritanceMapping>(e => e.Creator1);

			dataContext.LoadOptions = loadOptions;

			Assert.AreEqual(loadOptions, dataContext.LoadOptions);
		}

		[TestMethod]
		public void DescendantEntityWithInheritanceMapping_BaseClassProperty_Exception()
		{
			var loadOptions = new DataLoadOptions();

			loadOptions.LoadWith<DescendantEntityWithInheritanceMapping>(e => e.Creator1);

			AssertException.Throws<InvalidOperationException>(
				() => dataContext.LoadOptions = loadOptions,
				$"Type {typeof(DescendantEntityWithInheritanceMapping)} is not the root type of the inheritance mapping hierarchy," +
				$" so it can't be used for automatic loading.");
		}

		[TestMethod]
		public void DescendantEntityWithInheritanceMapping_DescendantClassProperty_Exception()
		{
			var loadOptions = new DataLoadOptions();

			loadOptions.LoadWith<DescendantEntityWithInheritanceMapping>(e => e.Creator2);

			AssertException.Throws<InvalidOperationException>(
				() => dataContext.LoadOptions = loadOptions,
				$"Type {typeof(DescendantEntityWithInheritanceMapping)} is not the root type of the inheritance mapping hierarchy," +
					$" so it can't be used for automatic loading.");
		}
	}
}