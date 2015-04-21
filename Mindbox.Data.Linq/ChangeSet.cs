using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Linq
{
	[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ChangeSet", Justification="The capitalization was deliberately chosen.")]
	public sealed class ChangeSet {
		ReadOnlyCollection<object> inserts;
		ReadOnlyCollection<object> deletes;
		ReadOnlyCollection<object> updates;

		internal ChangeSet(
			ReadOnlyCollection<object> inserts,
			ReadOnlyCollection<object> deletes,
			ReadOnlyCollection<object> updates
			) {
			this.inserts = inserts;
			this.deletes = deletes;
			this.updates = updates;
			}

		public IList<object> Inserts {
			get { return this.inserts; }
		}

		public IList<object> Deletes {
			get { return this.deletes; }
		}

		public IList<object> Updates {
			get { return this.updates; }
		}

		public override string ToString() {
			return "{" +
				string.Format(
					Globalization.CultureInfo.InvariantCulture,
					"Inserts: {0}, Deletes: {1}, Updates: {2}",
					this.Inserts.Count,
					this.Deletes.Count,
					this.Updates.Count
					) + "}";
		}
	}
}