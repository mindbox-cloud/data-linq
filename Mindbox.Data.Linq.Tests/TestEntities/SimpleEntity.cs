using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	[Table(Name = "SimpleTable")]
	public sealed class SimpleEntity
	{
		[Column(IsPrimaryKey = true)]
		public int Id { get; set; }

		[Column]
		public string Discriminator { get; set; }

		[Column]
		public int X { get; set; }
	}
}
