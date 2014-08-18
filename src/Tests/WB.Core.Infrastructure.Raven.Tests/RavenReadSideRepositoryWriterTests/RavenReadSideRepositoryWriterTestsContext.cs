﻿using Machine.Specifications;

using Moq;

using Raven.Client.Document;
using WB.Core.Infrastructure.Raven.Raven.Implementation.ReadSide;
using WB.Core.Infrastructure.Raven.Raven.Implementation.ReadSide.RepositoryAccessors;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.Infrastructure.ReadSide.Repository;

using it = Moq.It;

namespace WB.Core.Infrastructure.Raven.Tests.RavenReadSideRepositoryWriterTests
{
    [Subject(typeof(RavenReadSideRepositoryWriter<>))]
    internal class RavenReadSideRepositoryWriterTestsContext
    {
        internal static RavenReadSideRepositoryWriter<TEntity> CreateRavenReadSideRepositoryWriter<TEntity>(
            DocumentStore ravenStore, IRavenReadSideRepositoryWriterRegistry writerRegistry)
            where TEntity : class, IReadSideRepositoryEntity
        {
            return new RavenReadSideRepositoryWriter<TEntity>(
                ravenStore ?? new DocumentStore(),
                writerRegistry ?? Mock.Of<IRavenReadSideRepositoryWriterRegistry>());
        }
    }
}