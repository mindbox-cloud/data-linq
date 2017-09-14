using System.Data.Linq;
using System.Data.Linq.Mapping;
using Mindbox.Data.Linq.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static System.Data.Linq.ChangeTracker;
using static System.Data.Linq.ChangeTracker.StandardChangeTracker;
using System.Data.Linq.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Moq;
using static System.Data.Linq.ChangeProcessor;

namespace Mindbox.Data.Linq.Tests
{
	[TestClass]
	public class TrackedObjectLofFormatterTests
	{
		[TestMethod]
		public void TrackedObject_ЛоггируетсяСНеобходимымиСвойствами()
		{
			var exception = new InvalidOperationException("CycleDetected");
			var expected = "TrackedObject.Current.Name = TestEntity1\r\nTrackedObject.Current:\r\nId = 0\r\nTrackedObject.IsInteresting = False\r\nTrackedObject.IsDeleted = False\r\nTrackedObject.IsModified = False\r\nTrackedObject.IsDead = False\r\nTrackedObject.IsWeaklyTracked = False\r\n";

			var trackedObjectMock = new Mock<TrackedObject>(MockBehavior.Strict);
			trackedObjectMock.Setup(tom => tom.Current).Returns(new TestEntity1());
			trackedObjectMock.Setup(tom => tom.IsInteresting).Returns(false);
			trackedObjectMock.Setup(tom => tom.IsDeleted).Returns(false);
			trackedObjectMock.Setup(tom => tom.IsModified).Returns(false);
			trackedObjectMock.Setup(tom => tom.IsDead).Returns(false);
			trackedObjectMock.Setup(tom => tom.IsWeaklyTracked).Returns(false);

			var trackedObjectLogFormatter = new TrackedObjectLogFormatter();
			var trackedObject = trackedObjectMock.Object;

			trackedObjectLogFormatter.LogTrackedObject(trackedObject, "TrackedObject", exception);

			var data = exception.Data["TrackedObject"].ToString();
			Assert.AreEqual(expected, data);
		}

		[TestMethod]
		public void ListTrackedObject_Логгируется()
		{
			var cycleException = new InvalidOperationException("CycleDetected");
			var expected = "TrackedObject.MetaType.Name = MetaTypeName\r\nTrackedObject.Current:\r\nId = 0\r\nTrackedObject.IsInteresting = False\r\nTrackedObject.IsDeleted = False\r\nTrackedObject.IsModified = False\r\nTrackedObject.IsDead = False\r\nTrackedObject.IsWeaklyTracked = False\r\n";

			var trackedList = new List<TrackedObject>();
			for (int i = 0; i < 10; i++)
			{
				var trackedObjectMock = new Mock<TrackedObject>(MockBehavior.Strict);
				trackedObjectMock.Setup(tom => tom.Current).Returns(new TestEntity1());
				trackedObjectMock.Setup(tom => tom.IsInteresting).Returns(false);
				trackedObjectMock.Setup(tom => tom.IsDeleted).Returns(false);
				trackedObjectMock.Setup(tom => tom.IsModified).Returns(false);
				trackedObjectMock.Setup(tom => tom.IsDead).Returns(false);
				trackedObjectMock.Setup(tom => tom.IsWeaklyTracked).Returns(false);

				trackedList.Add(trackedObjectMock.Object);
			}

			var trackedObjectLogFormatter = new TrackedObjectLogFormatter();
			trackedObjectLogFormatter.LogTrackedList(trackedList, "TrackedList", cycleException);


			for (int i = 0; i < 10; i++)
			{
				var data = cycleException.Data[$"{i} element in TrackedList"].ToString();

				Assert.AreEqual(expected, data);
			}
		}

		[TestMethod]
		public void DisctionaryTrackedObjectState_Логгируется()
		{
			var cycleException = new InvalidOperationException("CycleDetected");
			var expected = "Has state: After\r\nTrackedObject.MetaType.Name = MetaTypeName\r\nTrackedObject.Current:\r\nId = 0\r\nTrackedObject.IsInteresting = False\r\nTrackedObject.IsDeleted = False\r\nTrackedObject.IsModified = False\r\nTrackedObject.IsDead = False\r\nTrackedObject.IsWeaklyTracked = False\r\n";

			var trackedList = new Dictionary<TrackedObject, VisitState>();
			for (int i = 0; i < 10; i++)
			{
				var metaTypeMock = new Mock<MetaType>();
				metaTypeMock.Setup(mtm => mtm.Name).Returns("MetaTypeName");

				var trackedObjectMock = new Mock<TrackedObject>();
				trackedObjectMock.Setup(tom => tom.Type).Returns(metaTypeMock.Object);
				trackedObjectMock.Setup(tom => tom.Current).Returns(new TestEntity1());
				trackedObjectMock.Setup(tom => tom.IsInteresting).Returns(false);
				trackedObjectMock.Setup(tom => tom.IsDeleted).Returns(false);
				trackedObjectMock.Setup(tom => tom.IsModified).Returns(false);
				trackedObjectMock.Setup(tom => tom.IsDead).Returns(false);
				trackedObjectMock.Setup(tom => tom.IsWeaklyTracked).Returns(false);

				trackedList.Add(trackedObjectMock.Object, VisitState.After);
			}

			new TrackedObjectLogFormatter().LogTrackedObjectVisitState(trackedList, "Dictionary", cycleException);

			for (int i = 0; i < 10; i++)
			{
				var data = cycleException.Data[$"{i} element in Dictionary"].ToString();

				Assert.AreEqual(expected, data);
			}
		}
	}
}
