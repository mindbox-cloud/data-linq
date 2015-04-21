using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Data.Linq
{
	[SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "[....]: Types are never compared to each other.  When comparisons happen it is against the entities that are represented by these constructs.")]
	public struct ModifiedMemberInfo {
		MemberInfo member;
		object current;
		object original;

		internal ModifiedMemberInfo(MemberInfo member, object current, object original) {
			this.member = member;
			this.current = current;
			this.original = original;
		}

		public MemberInfo Member {
			get { return this.member; }
		}

		public object CurrentValue {
			get { return this.current; }
		}

		public object OriginalValue {
			get { return this.original; }
		}
	}
}