using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.Infrastructure.PlainStorage;

namespace WB.Core.Infrastructure.Implementation
{
    public class InMemoryPlainStorageAccessor<TEntity> : IPlainStorageAccessor<TEntity>, IPlainKeyValueStorage<TEntity>
        where TEntity : class
    {
        private readonly Dictionary<object,TEntity> inMemoryStorage = new Dictionary<object, TEntity>(); 

        public TEntity GetById(object id)
        {
            if (this.inMemoryStorage.ContainsKey(id))
                return this.inMemoryStorage[id];
            return null;
        }

        public void Remove(object id)
        {
            if (this.inMemoryStorage.ContainsKey(id))
                this.inMemoryStorage.Remove(id);
        }

        public void Remove(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                var itemToRemove = this.inMemoryStorage.SingleOrDefault(x => x.Value.Equals(entity));
                this.inMemoryStorage.Remove(itemToRemove.Key);
            }
        }

        public void Store(TEntity entity, object id)
        {
            this.inMemoryStorage[id] = entity;
        }

        public void Store(IEnumerable<Tuple<TEntity, object>> entities)
        {
            foreach (var entity in entities)
            {
                this.Store(entity.Item1,entity.Item2);
            }
        }

        public void Flush()
        {
        }

        public TResult Query<TResult>(Func<IQueryable<TEntity>, TResult> query)
        {
            return query.Invoke(this.inMemoryStorage.Values.AsQueryable());
        }

        public TEntity GetById(string id)
        {
            return GetById((object) id);
        }

        public bool HasNotEmptyValue(string id)
        {
            return this.inMemoryStorage.ContainsKey(id) && this.inMemoryStorage[id] != null;
        }

        public void Remove(string id)
        {
            Remove((object)id);
        }

        public void Store(TEntity view, string id)
        {
            Store(view, (object)id);
        }
    }
}
