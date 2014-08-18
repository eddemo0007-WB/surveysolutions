﻿using Raven.Client;
using Raven.Client.Document;
using WB.Core.Infrastructure.Raven.Raven.PlainStorage;

namespace WB.Core.Infrastructure.Raven.Raven.Implementation.PlainStorage
{
    internal class RavenPlainStorageProvider : IRavenPlainStorageProvider
    {
        private readonly DocumentStore documentStore;

        public RavenPlainStorageProvider(DocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public IDocumentStore GetDocumentStore()
        {
            return this.documentStore;
        }
    }
}