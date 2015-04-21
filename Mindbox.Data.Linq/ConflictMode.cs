namespace System.Data.Linq
{
	/// <summary>
	/// Used to specify how a submit should behave when one
	/// or more updates fail due to optimistic concurrency
	/// conflicts.
	/// </summary>
	public enum ConflictMode {
		/// <summary>
		/// Fail immediately when the first change conflict is encountered.
		/// </summary>
		FailOnFirstConflict,
		/// <summary>
		/// Only fail after all changes have been attempted.
		/// </summary>
		ContinueOnConflict
	}
}