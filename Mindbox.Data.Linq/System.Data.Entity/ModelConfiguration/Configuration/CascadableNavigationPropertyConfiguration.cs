namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures a relationship that can support cascade on delete functionality.
	/// </summary>
	public abstract class CascadableNavigationPropertyConfiguration
	{
		/// <summary>
		/// Configures cascade delete to be on for the relationship.
		/// </summary>
		public void WillCascadeOnDelete()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Configures whether or not cascade delete is on for the relationship.
		/// </summary>
		/// <param name="value"> Value indicating if cascade delete is on or not. </param>
		public void WillCascadeOnDelete(bool value)
		{
			throw new NotImplementedException();
		}
	}
}
