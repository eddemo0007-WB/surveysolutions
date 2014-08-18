﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Linq;
using WB.Core.Infrastructure.ReadSide.Repository;

namespace WB.Core.Infrastructure.Raven.Raven.Implementation.ReadSide.RepositoryAccessors
{
    #warning TLK: make string identifiers here after switch to new storage
    public abstract class RavenReadSideRepositoryAccessor<TEntity>
        where TEntity : class, IReadSideRepositoryEntity
    {
        private readonly DocumentStore ravenStore;

        protected RavenReadSideRepositoryAccessor(DocumentStore ravenStore)
        {
            this.ravenStore = ravenStore;
        }

        protected abstract TResult QueryImpl<TResult>(Func<IRavenQueryable<TEntity>, TResult> query);

        private static string ViewName
        {
            get
            {
                var viewType = typeof (TEntity);
                if(!viewType.IsGenericType)
                    return viewType.Name;
                return viewType.GetGenericArguments()[0].Name;
            }
        }

        protected IDocumentSession OpenSession()
        {
            return this.ravenStore.OpenSession();
        }

        protected static string ToRavenId(string id)
        {
            return string.Format("{0}${1}", ViewName, id);
        }

        public TResult Query<TResult>(Func<IQueryable<TEntity>, TResult> query)
        {
            return this.QueryImpl(query);
        }

        public IEnumerable<TEntity> QueryAll(Expression<Func<TEntity, bool>> condition)
        {
            var retval = new List<TEntity>();
            foreach (var docBulk in this.GetAllDocuments(condition))
            {
                retval.AddRange(docBulk);
            }
            return retval;
        }

        private int MaxNumberOfRequestsPerSession
        {
            get { return this.ravenStore.Conventions.MaxNumberOfRequestsPerSession; }
        }

        private IEnumerable<IQueryable<TEntity>> GetAllDocuments(Expression<Func<TEntity, bool>> condition)
        {
            int skipResults = 0;

            while (true)
            {
                var nextGroupOfPoints = this.GetPagedDocuments(condition, skipResults, this.MaxNumberOfRequestsPerSession);
                if (!nextGroupOfPoints.Any())
                    yield break;
                skipResults += this.MaxNumberOfRequestsPerSession;
                yield return nextGroupOfPoints;
            }
        }

        private IQueryable<TEntity> GetPagedDocuments(Expression<Func<TEntity, bool>> condition, int start, int pageSize)
        {
            return condition != null
                ? this.QueryImpl(queryable => Queryable.Skip(queryable.Where(condition), start).Take(pageSize))
                : this.QueryImpl(queryable => Queryable.Skip(queryable, start).Take(pageSize));
        }
    }
}