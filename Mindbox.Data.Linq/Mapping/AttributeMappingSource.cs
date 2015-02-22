namespace System.Data.Linq.Mapping
{
	/// <summary>
	/// A mapping source that uses attributes on the context to create the mapping model.
	/// </summary>
	public class AttributeMappingSource : MappingSource 
	{
		protected override MetaModel CreateModel(Type dataContextType) 
		{
			if (dataContextType == null)
				throw Error.ArgumentNull("dataContextType");

			return new AttributedMetaModel(this, dataContextType);
		}
	}
}