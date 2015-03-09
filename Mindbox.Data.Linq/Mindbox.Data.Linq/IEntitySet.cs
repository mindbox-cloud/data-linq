using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq
{
	internal interface IEntitySet
	{
		event EventHandler ListChanging;
	}
}
