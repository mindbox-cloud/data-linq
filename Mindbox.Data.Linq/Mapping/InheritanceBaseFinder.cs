namespace System.Data.Linq.Mapping
{
	internal static class InheritanceBaseFinder {
		internal static MetaType FindBase(MetaType derivedType) {
			if (derivedType.Type == typeof(object)) {
				return null;
			}

			var clrType = derivedType.Type; // start
			var rootClrType = derivedType.InheritanceRoot.Type; // end
			var metaTable = derivedType.Table;
			MetaType metaType = null;

			while (true) {
				if (clrType == typeof(object) || clrType == rootClrType) {
					return null;
				}

				clrType = clrType.BaseType;
				metaType = derivedType.InheritanceRoot.GetInheritanceType(clrType);

				if (metaType != null) {
					return metaType;
				}
			}
		}
	}
}