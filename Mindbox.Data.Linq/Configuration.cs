using System;

namespace Mindbox.Data.Linq
{
	public static class Configuration
	{
		public static Func<bool> UseExpressionFunctions { get; set; } = () => false;
	}
}