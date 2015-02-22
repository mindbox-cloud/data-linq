using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.Data.Linq.SqlClient
{
	public static class ThisAssembly
	{
		public static string InformationalVersion
		{
			get { return Assembly.GetCallingAssembly().GetName().Version.ToString(); }
		}
	}
}
