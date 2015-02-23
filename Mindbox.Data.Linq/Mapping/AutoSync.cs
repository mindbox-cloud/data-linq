namespace System.Data.Linq.Mapping
{
	/// <summary>
	/// Used to specify for during insert and update operations when
	/// a data member should be read back after the operation completes.
	/// </summary>
	public enum AutoSync 
	{
		Default = 0, // Automatically choose
		Always = 1,
		Never = 2,
		OnInsert = 3,
		OnUpdate = 4 
	}
}