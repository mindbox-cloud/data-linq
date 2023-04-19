using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Mindbox.Data.Linq;

namespace System.Data.Linq 
{
    public sealed class EntitySet<TEntity> : IList, IList<TEntity>, IListSource, IEntitySet
        where TEntity : class 
	{
        private IEnumerable<TEntity> source;
		private ItemList<TEntity> entities;
		private ItemList<TEntity> removedEntities;
		private readonly Action<TEntity> onAdd;
		private readonly Action<TEntity> onRemove;
		private TEntity onAddEntity;
		private TEntity onRemoveEntity;
		private int version;
        private bool isModified;
        private bool isLoaded;
		private bool listChanged;
		private IBindingList cachedList;
	    private EventHandler listChanging;


	    [Obsolete("Non compatible API with EntityFramework")]
        public EntitySet() 
		{
        }

        [Obsolete("Non compatible API with EntityFramework")]
        public EntitySet(Action<TEntity> onAdd, Action<TEntity> onRemove) 
		{
            this.onAdd = onAdd;
            this.onRemove = onRemove;
        }

        internal static EntitySet<TEntity> Create(Action<TEntity> onAdd, Action<TEntity> onRemove) 
#pragma warning disable CS0618
	        => new(onAdd, onRemove);
#pragma warning restore CS0618
        
        internal static EntitySet<TEntity> Create() 
#pragma warning disable CS0618
	        => new();
#pragma warning restore CS0618
  
        internal EntitySet(EntitySet<TEntity> sourceEntitySet, bool copyNotifications) 
		{
            source = sourceEntitySet.source;
            foreach (var entity in sourceEntitySet.entities) 
				entities.Add(entity);
            foreach (var removedEntity in sourceEntitySet.removedEntities) 
				removedEntities.Add(removedEntity);
            version = sourceEntitySet.version;
            if (copyNotifications) 
			{
                onAdd = sourceEntitySet.onAdd;
                onRemove = sourceEntitySet.onRemove;
            }
        }


        public int Count 
		{
            get 
			{
                Load();
                return entities.Count;
            }
        }

		/// <summary>
		/// Returns true if this entity set has a deferred query
		/// that hasn't been executed yet.
		/// </summary>
		public bool IsDeferred
		{
			get { return HasSource; }
		}

		/// <summary>
		/// Returns true if the entity set has been modified in any way by the user or its items
		/// have been loaded from the database.
		/// </summary>
		public bool HasLoadedOrAssignedValues
		{
			get { return HasAssignedValues || HasLoadedValues; }
		}

        public TEntity this[int index] 
		{
            get 
			{
                Load();
                if (index < 0 || index >= entities.Count)
                    throw Error.ArgumentOutOfRange("index");
                return entities[index];
            }
            set 
			{
                Load();
                if (index < 0 || index >= entities.Count)
                    throw Error.ArgumentOutOfRange("index");
                if (value == null || IndexOf(value) >= 0)
                    throw Error.ArgumentOutOfRange("value");
                CheckModify();
                var old = entities[index];
                OnRemove(old);
                OnListChanged(ListChangedType.ItemDeleted, index);

                OnAdd(value);
                entities[index] = value;
                OnModified();
                OnListChanged(ListChangedType.ItemAdded, index);
            }
        }

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool IList.IsReadOnly
		{
			get { return false; }
		}

		object IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				var entity = value as TEntity;
				if (value == null) 
					throw Error.ArgumentOutOfRange("value");
				this[index] = entity;
			}
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { return this; }
		}

		bool ICollection<TEntity>.IsReadOnly
		{
			get { return false; }
		}

		bool IListSource.ContainsListCollection
		{
			get { return true; }
		}

		public event ListChangedEventHandler ListChanged;

		event EventHandler IEntitySet.ListChanging
		{
			add { listChanging += value; }
			remove { listChanging -= value; }
		}


		/// <summary>
		/// Returns true if values have been either assigned or loaded.
		/// </summary>
		internal bool HasValues
		{
			get { return source == null || HasAssignedValues || HasLoadedValues; }
		}

		/// <summary>   
		/// Returns true if the set has been modified in any way by the user.
		/// </summary>
		internal bool HasAssignedValues
		{
			get { return isModified; }
		}

		/// <summary>
		/// Returns true if the set has been loaded from the database.
		/// </summary>
		internal bool HasLoadedValues
		{
			get { return isLoaded; }
		}

		/// <summary>
		/// Returns true if the set has a deferred source query that hasn't been loaded yet.
		/// </summary>
		internal bool HasSource
		{
			get { return source != null && !HasLoadedValues; }
		}

		/// <summary>
		/// Returns true if the collection has been loaded.
		/// </summary>
		internal bool IsLoaded
		{
			get { return isLoaded; }
		}

		internal IEnumerable<TEntity> Source
		{
			get { return source; }
		}


        public void Add(TEntity entity)
        {
	        if (entity == null)
		        throw new ArgumentNullException("entity");

	        if (entity != onAddEntity) 
			{
                CheckModify();
                if (!entities.Contains(entity)) 
				{
                    OnAdd(entity);
                    if (HasSource) 
						removedEntities.Remove(entity);
                    entities.Add(entity);
                    OnListChanged(ListChangedType.ItemAdded, entities.IndexOf(entity));
                }
                OnModified();
            }
        }

	    public void AddRange(IEnumerable<TEntity> collection) 
		{
		    if (collection == null)
			    throw new ArgumentNullException("collection");

		    CheckModify();
            // convert to List in case adding elements here removes them from the 'collection' (ie entityset to entityset assignment)
            collection = collection.ToList();
            foreach (var entity in collection) 
			{
                if (!entities.Contains(entity)) 
				{
                    OnAdd(entity);
                    if (HasSource) 
						removedEntities.Remove(entity);
                    entities.Add(entity);
                    OnListChanged(ListChangedType.ItemAdded, entities.IndexOf(entity));
                }
            }
            OnModified();
        }

        public void Assign(IEnumerable<TEntity> entitySource) 
		{
            // No-op if assigning the same object to itself
	        if (ReferenceEquals(this, entitySource))
		        return;

	        Clear();
            if (entitySource != null)
                AddRange(entitySource);

            // When an entity set is assigned, it is considered loaded.
            // Since with defer loading enabled, a load is triggered
            // anyways, this is only necessary in cases where defer loading
            // is disabled.  In such cases, the materializer assigns a 
            // prefetched collection and we want IsLoaded to be true.
            isLoaded = true;
        }

        public void Clear() 
		{
            Load();
            CheckModify();
	        if (entities.Items != null)
		        foreach (var entity in entities.Items.ToList())
			        Remove(entity);
	        entities = default(ItemList<TEntity>);
            OnModified();
            OnListChanged(ListChangedType.Reset, 0);
        }

        public bool Contains(TEntity entity) 
		{
            return IndexOf(entity) >= 0;
        }

        public void CopyTo(TEntity[] array, int arrayIndex) 
		{
            Load();
            if (entities.Count > 0) 
				Array.Copy(entities.Items, 0, array, arrayIndex, entities.Count);
        }

        public IEnumerator<TEntity> GetEnumerator() 
		{
            Load();
            return new Enumerator(this);
        }

		public int IndexOf(TEntity entity)
		{
			Load();
			return entities.IndexOf(entity);
		}

		public void Insert(int index, TEntity entity)
		{
			Load();
			if (index < 0 || index > Count)
				throw Error.ArgumentOutOfRange("index");
			if (entity == null || IndexOf(entity) >= 0)
				throw Error.ArgumentOutOfRange("entity");
			CheckModify();
			entities.Insert(index, entity);
			OnListChanged(ListChangedType.ItemAdded, index);

			OnAdd(entity);
		}

		public void Load()
		{
			if (HasSource)
			{
				var addedEntities = entities;
				entities = default(ItemList<TEntity>);
				foreach (var entity in source) 
					entities.Add(entity);
				foreach (var addedEntity in addedEntities) 
					entities.Include(addedEntity);
				foreach (var removedEntity in removedEntities)
					entities.Remove(removedEntity);
				source = SourceState<TEntity>.Loaded;
				isLoaded = true;
				removedEntities = default(ItemList<TEntity>);
			}
		}

		public bool Remove(TEntity entity)
		{
			if (entity == null || entity == onRemoveEntity)
				return false;
			CheckModify();
			var index = -1;
			var removed = false;
			if (HasSource)
			{
				if (!removedEntities.Contains(entity))
				{
					OnRemove(entity);
					// check in entities in case it has been pre-added
					index = entities.IndexOf(entity);
					if (index == -1)
						removedEntities.Add(entity);
					else
						entities.RemoveAt(index);
					removed = true;
				}
			}
			else
			{
				index = entities.IndexOf(entity);
				if (index != -1)
				{
					OnRemove(entity);
					entities.RemoveAt(index);
					removed = true;
				}
			}
			if (removed)
			{
				OnModified();
				// If index == -1 here, that means that the entity was not in the list before Remove was called,
				// so we shouldn't fire the event since the list itself will not be changed, even though the Remove will still be tracked
				// on the removedEntities list in case a subsequent Load brings in this entity.
				if (index != -1)
					OnListChanged(ListChangedType.ItemDeleted, index);
			}
			return removed;
		}

		public void RemoveAt(int index)
		{
			Load();
			if (index < 0 || index >= Count)
				throw Error.ArgumentOutOfRange("index");
			CheckModify();
			var entity = entities[index];
			OnRemove(entity);
			entities.RemoveAt(index);
			OnModified();
			OnListChanged(ListChangedType.ItemDeleted, index);
		}

		public void SetSource(IEnumerable<TEntity> entitySource)
		{
			if (HasAssignedValues || HasLoadedValues)
				throw Error.EntitySetAlreadyLoaded();
			source = entitySource;
		}

		public IBindingList GetNewBindingList()
		{
			return new EntitySetBindingList<TEntity>(this.ToList(), this);
		}

		int IList.Add(object value)
		{
			var entity = value as TEntity;
			if (entity == null || IndexOf(entity) >= 0)
				throw Error.ArgumentOutOfRange("value");
			CheckModify();
			var index = entities.Count;
			entities.Add(entity);
			OnAdd(entity);
			return index;
		}

		bool IList.Contains(object value)
		{
			return Contains(value as TEntity);
		}

		int IList.IndexOf(object value)
		{
			return IndexOf(value as TEntity);
		}

		void IList.Insert(int index, object value)
		{
			var entity = value as TEntity;
			if (value == null)
				throw Error.ArgumentOutOfRange("value");
			Insert(index, entity);
		}

		void IList.Remove(object value)
		{
			Remove(value as TEntity);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			Load();
			if (entities.Count > 0) 
				Array.Copy(entities.Items, 0, array, index, entities.Count);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IList IListSource.GetList()
		{
			if (cachedList == null || listChanged)
			{
				cachedList = GetNewBindingList();
				listChanged = false;
			}
			return cachedList;
		}


        internal IEnumerable<TEntity> GetUnderlyingValues() 
		{
            return new UnderlyingValues(this);
        }


		private void OnModified()
		{
			isModified = true;
		}

		private void CheckModify()
		{
			if (onAddEntity != null || onRemoveEntity != null)
				throw Error.ModifyDuringAddOrRemove();
			version++;
		}

		private void OnAdd(TEntity entity)
		{
			OnListChanging();
			if (onAdd != null)
			{
				var previousOnAddEntity = onAddEntity;
				onAddEntity = entity;
				try
				{
					onAdd(entity);
				}
				finally
				{
					onAddEntity = previousOnAddEntity;
				}
			}
		}

		private void OnRemove(TEntity entity)
		{
			OnListChanging();
			if (onRemove != null)
			{
				var previousOnRemoveEntity = onRemoveEntity;
				onRemoveEntity = entity;
				try
				{
					onRemove(entity);
				}
				finally
				{
					onRemoveEntity = previousOnRemoveEntity;
				}
			}
		}

	    private void OnListChanging()
	    {
		    if (listChanging != null)
			    listChanging(this, EventArgs.Empty);
	    }

		private void OnListChanged(ListChangedType type, int index)
		{
			listChanged = true;
			if (ListChanged != null)
				ListChanged(this, new ListChangedEventArgs(type, index));
		}


        private class UnderlyingValues : IEnumerable<TEntity> 
		{
	        private readonly EntitySet<TEntity> entitySet;


	        public UnderlyingValues(EntitySet<TEntity> entitySet) 
			{
                this.entitySet = entitySet;
            }


            public IEnumerator<TEntity> GetEnumerator() 
			{
                return new Enumerator(entitySet);
            }

            IEnumerator IEnumerable.GetEnumerator() 
			{
                return GetEnumerator();
            }
        }


        private class Enumerator : IEnumerator<TEntity> 
		{
	        private readonly EntitySet<TEntity> entitySet;
			private readonly TEntity[] items;
			private int index;
			private readonly int endIndex;
			private readonly int version;


            public Enumerator(EntitySet<TEntity> entitySet) 
			{
                this.entitySet = entitySet;
                items = entitySet.entities.Items;
                index = -1;
                endIndex = entitySet.entities.Count - 1;
                version = entitySet.version;
            }


			public TEntity Current
			{
				get { return items[index]; }
			}

			object IEnumerator.Current
			{
				get { return items[index]; }
			}


            public void Dispose()
            {
            }

            public bool MoveNext() 
			{
                if (version != entitySet.version)
                    throw Error.EntitySetModifiedDuringEnumeration();

                if (index == endIndex) 
					return false;
                index++;
                return true;
            }

            void IEnumerator.Reset() 
			{
                if (version != entitySet.version)
                    throw Error.EntitySetModifiedDuringEnumeration();
                index = -1;
            }
        }
    }
}
