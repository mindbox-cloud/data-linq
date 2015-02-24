using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Mapping
{
	public class MindboxMappingConfiguration
	{
		private readonly Dictionary<Type, List<InheritanceMappingAttribute>> additionalInheritanceAttributesByRootType =
			new Dictionary<Type, List<InheritanceMappingAttribute>>();

		private readonly DbModelBuilder modelBuilder = new DbModelBuilder();

		private readonly Dictionary<Type, TableAttribute> additionalTableAttributesByRootType = 
			new Dictionary<Type, TableAttribute>();

		private EventHandler<EntityFrameworkIncompatibility> entityFrameworkIncompatibilityHandler;


		public bool IsFrozen { get; private set; }

		public DbModelBuilder ModelBuilder
		{
			get
			{
				CheckNotFrozen();
				return modelBuilder;
			}
		}

		public event EventHandler<EntityFrameworkIncompatibility> EntityFrameworkIncompatibility
		{
			add
			{
				CheckNotFrozen();
				entityFrameworkIncompatibilityHandler += value;
			}
			remove
			{
				CheckNotFrozen();
				entityFrameworkIncompatibilityHandler -= value;
			}
		}


		public void AddInheritance<TRoot, T>(object code)
			where TRoot : class
			where T : TRoot
		{
			if (code == null)
				throw new ArgumentNullException("code");
			if (typeof(T) == typeof(TRoot))
				throw new InvalidOperationException("typeof(T) == typeof(TRoot)");
			CheckNotFrozen();

			List<InheritanceMappingAttribute> additionalInheritanceAttributes;
			if (!additionalInheritanceAttributesByRootType.TryGetValue(typeof(TRoot), out additionalInheritanceAttributes))
			{
				additionalInheritanceAttributes = new List<InheritanceMappingAttribute>();
				additionalInheritanceAttributesByRootType.Add(typeof(TRoot), additionalInheritanceAttributes);
			}
			additionalInheritanceAttributes.Add(new InheritanceMappingAttribute
			{
				Code = code,
				Type = typeof(T)
			});
		}

		public void Freeze()
		{
			IsFrozen = true;

			modelBuilder.Validate();
			foreach (var tableAttributeByRootType in modelBuilder.GetTableAttributesByRootType())
				additionalTableAttributesByRootType.Add(tableAttributeByRootType.RootType, tableAttributeByRootType.Attribute);
		}


		internal ICollection<InheritanceMappingAttribute> GetAdditionalInheritanceAttributes(Type rootType)
		{
			if (rootType == null)
				throw new ArgumentNullException("rootType");

			List<InheritanceMappingAttribute> additionalInheritanceAttributes;
			return additionalInheritanceAttributesByRootType.TryGetValue(rootType, out additionalInheritanceAttributes) ? 
				additionalInheritanceAttributes.ToList() : 
				new List<InheritanceMappingAttribute>();
		}

		internal TableAttribute TryGetTableAttribute(Type rootType)
		{
			if (rootType == null)
				throw new ArgumentNullException("rootType");

			TableAttribute tableAttribute;
			return additionalTableAttributesByRootType.TryGetValue(rootType, out tableAttribute) ? tableAttribute : null;
		}

		internal void OnEntityFrameworkIncompatibility(EntityFrameworkIncompatibility entityFrameworkIncompatibility)
		{
			CheckFrozen();

			if (entityFrameworkIncompatibilityHandler != null)
				entityFrameworkIncompatibilityHandler(this, entityFrameworkIncompatibility);
		}


		private void CheckNotFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("IsFrozen");
		}

		private void CheckFrozen()
		{
			if (!IsFrozen)
				throw new InvalidOperationException("!IsFrozen");
		}
	}
}
