using System.Collections.Generic;

namespace System.Data.Linq
{
	internal static class SourceState<T> 
	{
		internal static readonly IEnumerable<T> Loaded = new T[0];
		internal static readonly IEnumerable<T> Assigned = new T[0];
	}
}