using System;
using System.Collections.Generic;
using System.Text;

namespace System.Data.Linq.Mapping 
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ProviderAttribute : Attribute 
	{
		public ProviderAttribute() 
		{
        }

        public ProviderAttribute(Type type) 
		{
            Type = type;
        }


		public Type Type { get; private set; }
	}
}
