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

namespace Mindbox.Data.Linq.Tests
{
	[TestClass]
	public class TrackedObjectLofFormatterTests
	{
		internal class DummyTrackedObject : TrackedObject
		{
			internal override MetaType Type
			{
				get
				{
					var mappingSource = new MindboxMappingSource(new MindboxMappingConfiguration());
					var attributedMetaModel = new AttributedMetaModel(mappingSource, typeof(TestEntity1));
					var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity1));
					return new AttributedMetaType(attributedMetaModel, metaTable, typeof(TestEntity1), null);
				}
			}
			internal override object Current => new TestEntity1();
			internal override bool IsInteresting => false;
			internal override bool IsDeleted => false;
			internal override bool IsModified => false;
			internal override bool IsDead => false;
			internal override bool IsWeaklyTracked => false;

			#region NotTested
			internal override object Original => throw new NotImplementedException();
			internal override bool IsNew => throw new NotImplementedException();
			internal override bool IsUnmodified => throw new NotImplementedException();
			internal override bool IsPossiblyModified => throw new NotImplementedException();
			internal override bool IsRemoved => throw new NotImplementedException();
			internal override bool HasDeferredLoaders => throw new NotImplementedException();


			internal override void AcceptChanges()
			{
			}

			internal override bool CanInferDelete()
			{
				throw new NotImplementedException();
			}

			internal override void ConvertToDead()
			{
				throw new NotImplementedException();
			}

			internal override void ConvertToDeleted()
			{
				throw new NotImplementedException();
			}

			internal override void ConvertToModified()
			{
				throw new NotImplementedException();
			}

			internal override void ConvertToNew()
			{
				throw new NotImplementedException();
			}

			internal override void ConvertToPossiblyModified()
			{
				throw new NotImplementedException();
			}

			internal override void ConvertToPossiblyModified(object original)
			{
				throw new NotImplementedException();
			}

			internal override void ConvertToRemoved()
			{
				throw new NotImplementedException();
			}

			internal override void ConvertToUnmodified()
			{
				throw new NotImplementedException();
			}

			internal override object CreateDataCopy(object instance)
			{
				throw new NotImplementedException();
			}

			internal override IEnumerable<ModifiedMemberInfo> GetModifiedMembers()
			{
				throw new NotImplementedException();
			}

			internal override bool HasChangedValue(MetaDataMember mm)
			{
				throw new NotImplementedException();
			}

			internal override bool HasChangedValues()
			{
				throw new NotImplementedException();
			}

			internal override void InitializeDeferredLoaders()
			{
				throw new NotImplementedException();
			}

			internal override bool IsMemberPendingGeneration(MetaDataMember keyMember)
			{
				throw new NotImplementedException();
			}

			internal override bool IsPendingGeneration(IEnumerable<MetaDataMember> keyMembers)
			{
				throw new NotImplementedException();
			}

			internal override void Refresh(RefreshMode mode, object freshInstance)
			{
				throw new NotImplementedException();
			}

			internal override void RefreshMember(MetaDataMember member, RefreshMode mode, object freshValue)
			{
				throw new NotImplementedException();
			}

			internal override bool SynchDependentData()
			{
				throw new NotImplementedException();
			}
#endregion
		}

		[TestMethod]
		public void ТестовыйПример()
		{
			var exception = new InvalidOperationException("CycleDetected");
			var trackedObjectLogFormatter = new TrackedObjectLogFormatter();
			var trackedObject = new DummyTrackedObject();

			trackedObjectLogFormatter.LogTrackedObject(trackedObject, "TrackedObject", exception);

			var data = exception.Data["TrackedObject"].ToString();
			Assert.IsTrue(data.Contains("TrackedObject.IsInteresting = False"));
			Assert.IsTrue(data.Contains("TrackedObject.IsDeleted = False"));
			Assert.IsTrue(data.Contains("TrackedObject.IsModified = False"));
			Assert.IsTrue(data.Contains("TrackedObject.IsDead = False"));
			Assert.IsTrue(data.Contains("TrackedObject.IsWeaklyTracked = False"));
		}
	}
}
