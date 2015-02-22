using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace System.Data.Linq.SqlClient
{
	public static class Strings
	{
		public static string MaxSizeNotSupported(Expression expression)
		{
			return "MaxSizeNotSupported: " + expression + ".";
		}

		public static string TextNTextAndImageCannotOccurInDistinct(Expression select)
		{
			return "TextNTextAndImageCannotOccurInDistinct: " + select + ".";
		}

		public static string TextNTextAndImageCannotOccurInUnion(Expression select)
		{
			return "TextNTextAndImageCannotOccurInUnion: " + select + ".";
		}

		public static string LenOfTextOrNTextNotSupported(Expression expression)
		{
			return "LenOfTextOrNTextNotSupported: " + expression + ".";
		}

		public static string SourceExpressionAnnotation(Expression expression)
		{
			return "SourceExpressionAnnotation: " + expression + ".";
		}

		public static string LogGeneralInfoMessage(string source, string message)
		{
			return "LogGeneralInfoMessage: " + source + ", " + message + ".";
		}

		public static string LogAttemptingToDeleteDatabase(string dbName)
		{
			return "LogAttemptingToDeleteDatabase: " + dbName + ".";
		}
	}
}
