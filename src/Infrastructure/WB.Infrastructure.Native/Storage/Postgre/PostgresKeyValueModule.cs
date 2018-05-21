﻿using WB.Core.Infrastructure.Modularity;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;

namespace WB.Infrastructure.Native.Storage.Postgre
{
    public class PostgresKeyValueModule : PostgresModuleWithCache
    {
        public PostgresKeyValueModule(ReadSideCacheSettings cacheSettings)
            : base(cacheSettings) {}

        protected override IReadSideStorage<TEntity> GetPostgresReadSideStorage<TEntity>(IModuleContext context)
            => (IReadSideStorage<TEntity>) context.GetServiceWithGenericType(typeof(PostgresReadSideKeyValueStorage<>), typeof(TEntity));

        public override void Load(IIocRegistry registry)
        {
            base.Load(registry);

            registry.BindToMethodInSingletonScope(typeof(IReadSideKeyValueStorage<>), this.GetReadSideStorageWrappedWithCache);

            registry.BindAsSingleton(typeof(IPlainKeyValueStorage<>), typeof(PostgresPlainKeyValueStorage<>));

            registry.BindAsSingleton(typeof(IEntitySerializer<>), typeof(EntitySerializer<>));
        }
    }
}
