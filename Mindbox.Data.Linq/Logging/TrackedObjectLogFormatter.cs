using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using static System.Data.Linq.ChangeProcessor;

namespace System.Data.Linq.Logging
{
	internal class TrackedObjectLogFormatter
	{
		public void LogTrackedObject(TrackedObject trackedObject, string trackedObjectPrefix, Exception cycleException,
			VisitState? visitState = null)
		{
			var stringBuilder = new StringBuilder();

			if (visitState.HasValue)
			{
				stringBuilder.AppendLine($"Has state: {visitState}");
			}

			stringBuilder
				.AppendLine($"TrackedObject.Type:{Environment.NewLine}{FormatTypeProperties(trackedObject.Type)}")
				.AppendLine($"TrackedObject.Current:{Environment.NewLine}{FormatCurrentPropertiesWithColumnAttribute(trackedObject.Current)}")
				.AppendLine($"TrackedObject.{nameof(trackedObject.IsInteresting)} = {trackedObject.IsInteresting}")
				.AppendLine($"TrackedObject.{nameof(trackedObject.IsDeleted)} = {trackedObject.IsDeleted}")
				.AppendLine($"TrackedObject.{nameof(trackedObject.IsModified)} = {trackedObject.IsModified}")
				.AppendLine($"TrackedObject.{nameof(trackedObject.IsDead)} = {trackedObject.IsDead}")
				.AppendLine($"TrackedObject.{nameof(trackedObject.IsWeaklyTracked)} = {trackedObject.IsWeaklyTracked}");

			cycleException.Data[trackedObjectPrefix] = stringBuilder.ToString();
		}

		public string FormatCurrentPropertiesWithColumnAttribute(object current)
		{
			var currentType = current.GetType();
			var fieldPropertyDataList = currentType.GetProperties()
				.Where(p => p.GetCustomAttribute<ColumnAttribute>()?.IsPrimaryKey ?? false)
				.Select(p => $"{p.Name} = {p.GetValue(current)}");

			return string.Join(Environment.NewLine, fieldPropertyDataList);
		}

		public string FormatTypeProperties(MetaType type)
		{
			var stringBuilder = new StringBuilder();
			var fieldProperties = type.GetType().GetProperties();

			foreach (var property in fieldProperties)
			{
				var propertyName = property.Name;
				var propertyValue = property.GetValue(type);

				stringBuilder.AppendLine($"Type.{propertyName} = {propertyValue}");
			}

			return stringBuilder.ToString();
		}

		public void LogTrackedList(List<TrackedObject> trackedList, string trackedListPrefix, Exception cycleException)
		{
			for (int index = 0; index < trackedList.Count; index++)
			{
				LogTrackedObject(trackedList[index], $"{index} element in {trackedListPrefix}", cycleException);
			}
		}

		public void LogTrackedObjectVisitState(Dictionary<TrackedObject, VisitState> visitedTrackedObjectsWithStates,
			string visitedTrackedObjectsPrefix, Exception cycleException)
		{
			for (int index = 0; index < visitedTrackedObjectsWithStates.Count; index++)
			{
				var trackedObjectWithState = visitedTrackedObjectsWithStates.ElementAt(index);
				LogTrackedObject(trackedObjectWithState.Key, $"{index} element in {visitedTrackedObjectsPrefix}",
					cycleException, trackedObjectWithState.Value);
			}
		}
	}
}
