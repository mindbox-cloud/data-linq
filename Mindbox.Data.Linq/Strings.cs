using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace System.Data.Linq
{
	internal static class Strings
	{
		public const string InsertCallbackComment = "--Callback into user code for insert.";
		public const string UpdateCallbackComment = "--Callback into user code for update.";
		public const string DeleteCallbackComment = "--Callback into user code for delete.";
		public const string DatabaseGeneratedAlreadyExistingKey = "The database generated a key that is already in use.";
		public const string RowNotFoundOrChanged = "Row not found or changed.";
		public const string CantAddAlreadyExistingKey = "Cannot add an entity with a key that is already in use.";


		public static string UpdatesFailedMessage(int failedUpdates, int totalUpdatesAttempted)
		{
			return string.Format("{0} of {1} updates failed.", failedUpdates, totalUpdatesAttempted);
		}
	}
}
