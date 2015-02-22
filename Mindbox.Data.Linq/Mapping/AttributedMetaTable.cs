using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal sealed class AttributedMetaTable : MetaTable 
	{
		private readonly AttributedMetaModel model;
		private readonly string tableName;
		private readonly MetaType rowType;
		private bool areMethodsInitialized;
		private MethodInfo insertMethod;
		private MethodInfo updateMethod;
		private MethodInfo deleteMethod;


		internal AttributedMetaTable(AttributedMetaModel model, TableAttribute attr, Type rowType) 
		{
			this.model = model;
			tableName = string.IsNullOrEmpty(attr.Name) ? rowType.Name : attr.Name;
			this.rowType = model.CreateRootType(this, rowType);
		}


		public override MetaModel Model 
		{
			get { return model; }
		}

		public override string TableName 
		{
			get { return tableName; }
		}

		public override MetaType RowType 
		{
			get { return rowType; }
		}

		public override MethodInfo InsertMethod 
		{
			get 
			{
				InitMethods();
				return insertMethod;
			}
		}

		public override MethodInfo UpdateMethod 
		{
			get 
			{
				InitMethods();
				return updateMethod;
			}
		}

		public override MethodInfo DeleteMethod 
		{
			get 
			{
				InitMethods();
				return deleteMethod;
			}
		}


		private void InitMethods() 
		{
			if (areMethodsInitialized)
				return;

			insertMethod = MethodFinder.FindMethod(
				model.ContextType,
				"Insert" + rowType.Name,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				new[]
				{
					rowType.Type
				});
			updateMethod = MethodFinder.FindMethod(
				model.ContextType,
				"Update" + rowType.Name,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				new[]
				{
					rowType.Type
				});
			deleteMethod = MethodFinder.FindMethod(
				model.ContextType,
				"Delete" + rowType.Name,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				new[]
				{
					rowType.Type
				});
			areMethodsInitialized = true;
		}
	}
}