using System.Reflection;

namespace System.Data.Linq.Mapping
{
	internal sealed class AttributedMetaParameter : MetaParameter {
		private ParameterInfo parameterInfo;
		private ParameterAttribute paramAttrib;

		public AttributedMetaParameter(ParameterInfo parameterInfo) {
			this.parameterInfo = parameterInfo;
			this.paramAttrib = Attribute.GetCustomAttribute(parameterInfo, typeof(ParameterAttribute), false) as ParameterAttribute;
		}
		public override ParameterInfo Parameter {
			get { return this.parameterInfo; }
		}
		public override string Name {
			get { return this.parameterInfo.Name; }
		}
		public override string MappedName {
			get {
				if (this.paramAttrib != null && this.paramAttrib.Name != null)
					return this.paramAttrib.Name;
				return this.parameterInfo.Name;
			}
		}
		public override Type ParameterType {
			get { return this.parameterInfo.ParameterType; }
		}
		public override string DbType {
			get {
				if (this.paramAttrib != null && this.paramAttrib.DbType != null)
					return this.paramAttrib.DbType;
				return null;
			}
		}
	}
}