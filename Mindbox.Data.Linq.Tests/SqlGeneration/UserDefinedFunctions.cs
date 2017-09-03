using System;
using System.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests.SqlGeneration
{
	public static class UserDefinedFunctions
	{
		[Function(Name = "NEWID", IsComposable = true)]
		public static Guid Random()
		{
			return Guid.NewGuid();
		}
	}
}