namespace System.Data.Linq
{
	/// <summary>
	/// Used to specify a value synchronization strategy. 
	/// </summary>
	public enum RefreshMode {
		/// <summary>
		/// Keep the current values.
		/// </summary>
		KeepCurrentValues,
		/// <summary>
		/// Current values that have been changed are not modified, but
		/// any unchanged values are updated with the current database
		/// values.  No changes are lost in this merge.
		/// </summary>
		KeepChanges,
		/// <summary>
		/// All current values are overwritten with current database values,
		/// regardless of whether they have been changed.
		/// </summary>
		OverwriteCurrentValues
	}
}