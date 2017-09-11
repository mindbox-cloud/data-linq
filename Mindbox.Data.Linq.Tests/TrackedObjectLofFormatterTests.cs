using System.Data.Linq;
using System.Data.Linq.Mapping;
using Mindbox.Data.Linq.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static System.Data.Linq.ChangeTracker;
using static System.Data.Linq.ChangeTracker.StandardChangeTracker;
using System.Data.Linq.Logging;
using System;

namespace Mindbox.Data.Linq.Tests
{
	[TestClass]
	public class TrackedObjectLofFormatterTests
	{
		[TestMethod]
		public void ТестовыйПример()
		{
			var dataContext = new DataContext(""); // TODO:
			var mindboxMappingConfiguration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(mindboxMappingConfiguration);
			var metaModel = new MindboxMetaModel(mappingSource, typeof(StandardChangeTracker));
			var commonDataServices = new CommonDataServices(dataContext, metaModel);
			var tracker = new StandardChangeTracker(commonDataServices);

			var attributedMetaModel = new AttributedMetaModel(mappingSource, dataContext.GetType());
			var tableAttribute = new TableAttribute();
			var attributedMetaTable = new AttributedMetaTable(attributedMetaModel, tableAttribute, typeof(string));
			var minboxRootType = new MindboxRootType(attributedMetaModel, attributedMetaTable, typeof(MappedRootType));

			var trackedObject = new StandardTrackedObject(tracker, minboxRootType, new TestEntity1(), new TestEntity12());

			var trackedObjectLogFormatter = new TrackedObjectLogFormatter();
			var exception = new InvalidOperationException("CycleDetected");

			trackedObjectLogFormatter.LogTrackedObject(trackedObject, "TrackedObject", exception);

			// TODO:
		}
	}
}
