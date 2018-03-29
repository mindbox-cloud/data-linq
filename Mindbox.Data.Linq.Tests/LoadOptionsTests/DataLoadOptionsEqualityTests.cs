using System.Data.Linq;
using 
	Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindbox.Data.Linq.Tests.SqlGeneration;

namespace Mindbox.Data.Linq.Tests
{
	[TestClass]
	public class DataLoadOptionsEqualityTests
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
		public void DataLoadOptions_EmptyInstances_AreEqual()
		{
			var dlo1 = new DataLoadOptions(dataContext.Mapping);
			var dlo2 = new DataLoadOptions(dataContext.Mapping);

			Assert.AreEqual(dlo1, dlo2);
			Assert.AreEqual(dlo1.GetHashCode(), dlo2.GetHashCode());
		}
		
		[TestMethod]
		public void DataLoadOptions_SameLoadWithFields_AreEqual()
		{
			var dlo1 = new DataLoadOptions(dataContext.Mapping);
			dlo1.LoadWith<TestEntity25>(e => e.Other1);
			dlo1.LoadWith<TestEntity25>(e => e.Other2);
			dlo1.LoadWith<TestEntity26>(e => e.Other1);
			
			var dlo2 = new DataLoadOptions(dataContext.Mapping);
			dlo2.LoadWith<TestEntity25>(e => e.Other2);
			dlo2.LoadWith<TestEntity25>(e => e.Other1);
			dlo2.LoadWith<TestEntity26>(e => e.Other1);

			Assert.AreEqual(dlo1, dlo2);
			Assert.AreEqual(dlo1.GetHashCode(), dlo2.GetHashCode());
		}

		[TestMethod]
		public void DataLoadOptions_SameAssociateWithFields_AreEqual()
		{
			var dlo1 = new DataLoadOptions(dataContext.Mapping);
			dlo1.AssociateWith<TestEntity25>(e => e.Values);

			var dlo2 = new DataLoadOptions(dataContext.Mapping);
			dlo2.AssociateWith<TestEntity25>(e => e.Values);

			Assert.AreEqual(dlo1, dlo2);
			Assert.AreEqual(dlo1.GetHashCode(), dlo2.GetHashCode());
		}

		[TestMethod]
		public void DataLoadOptions_DifferentLoadWithFields_AreNotEqual()
		{
			var dlo1 = new DataLoadOptions(dataContext.Mapping);
			dlo1.LoadWith<TestEntity25>(e => e.Other2);

			var dlo2 = new DataLoadOptions(dataContext.Mapping);
			dlo2.LoadWith<TestEntity25>(e => e.Other1);

			Assert.AreNotEqual(dlo1, dlo2);
		}

		[TestMethod]
		public void DataLoadOptions_DifferentAssociateWithFields_AreNotEqual()
		{
			var dlo1 = new DataLoadOptions(dataContext.Mapping);
			dlo1.AssociateWith<TestEntity25>(e => e.Values);

			var dlo2 = new DataLoadOptions(dataContext.Mapping);

			Assert.AreNotEqual(dlo1, dlo2);
		}
	}
}
