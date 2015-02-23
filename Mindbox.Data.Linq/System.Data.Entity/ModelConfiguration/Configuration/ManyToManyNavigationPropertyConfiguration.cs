namespace System.Data.Entity.ModelConfiguration.Configuration
{
	/// <summary>
	/// Configures a many:many relationship.
	/// </summary>
	/// <typeparam name="TEntityType">The type of the parent entity of the navigation property specified in the HasMany call.</typeparam>
	/// <typeparam name="TTargetEntityType">The type of the parent entity of the navigation property specified in the WithMany call.</typeparam>
	public class ManyToManyNavigationPropertyConfiguration<TEntityType, TTargetEntityType>
		where TEntityType : class
		where TTargetEntityType : class
	{
		/// <summary>
		/// Configures the foreign key column(s) and table used to store the relationship.
		/// </summary>
		/// <param name="configurationAction"> Action that configures the foreign key column(s) and table. </param>
		/// <returns>The same <see cref="T:System.Data.Entity.ModelConfiguration.Configuration.ManyToManyNavigationPropertyConfiguration`2" /> instance so that multiple calls can be chained.</returns>
		public ManyToManyNavigationPropertyConfiguration<TEntityType, TTargetEntityType> Map(
			Action<ManyToManyAssociationMappingConfiguration> configurationAction)
		{
			if (configurationAction == null)
				throw new ArgumentNullException("configurationAction");

			throw new NotImplementedException();
		}
	}
}
