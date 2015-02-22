using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.Data.Linq.Mapping 
{
    /// <summary>
    /// Represents a source for mapping information.
    /// </summary>
    public abstract class MappingSource 
	{
        private MetaModel primaryModel;
        private ReaderWriterLock rwlock;
        private Dictionary<Type, MetaModel> secondaryModels;


        /// <summary>
        /// Gets the MetaModel representing a DataContext and all it's 
        /// accessible tables, functions and entities.
        /// </summary>
        public MetaModel GetModel(Type dataContextType) 
		{
	        if (dataContextType == null)
		        throw Error.ArgumentNull("dataContextType");

	        MetaModel model = null;
            if (primaryModel == null) 
			{
                model = CreateModel(dataContextType);
                Interlocked.CompareExchange(ref primaryModel, model, null);
            }

            // if the primary one matches, use it!
	        if (primaryModel.ContextType == dataContextType)
		        return primaryModel;

	        // the rest of this only happens if you are using the mapping source for
            // more than one context type

            // build a map if one is not already defined
	        if (secondaryModels == null)
		        Interlocked.CompareExchange(ref secondaryModels, new Dictionary<Type, MetaModel>(), null);

	        // if we haven't created a read/writer lock, make one now
	        if (rwlock == null)
		        Interlocked.CompareExchange(ref rwlock, new ReaderWriterLock(), null);

	        // lock the map and look inside
            MetaModel foundModel;
            rwlock.AcquireReaderLock(Timeout.Infinite);
            try 
			{
				if (secondaryModels.TryGetValue(dataContextType, out foundModel))
					return foundModel;
			}
            finally 
			{
                rwlock.ReleaseReaderLock();
            }

            // if it wasn't found, lock for write and try again
            rwlock.AcquireWriterLock(Timeout.Infinite);
            try 
			{
				if (secondaryModels.TryGetValue(dataContextType, out foundModel))
					return foundModel;
				if (model == null)
					model = CreateModel(dataContextType);
				secondaryModels.Add(dataContextType, model);
            }
            finally 
			{
                rwlock.ReleaseWriterLock();
            }
            return model;
        }


        /// <summary>
        /// Creates a new instance of a MetaModel.  This method is called by GetModel().
        /// Override this method when defining a new type of MappingSource.
        /// </summary>
        protected abstract MetaModel CreateModel(Type dataContextType);
    }
}
