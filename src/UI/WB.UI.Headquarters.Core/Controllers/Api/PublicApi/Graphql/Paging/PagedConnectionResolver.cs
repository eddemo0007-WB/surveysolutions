#nullable enable
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;
using NHibernate.Linq;

namespace WB.UI.Headquarters.Controllers.Api.PublicApi.Graphql.Paging
{
    internal sealed class PagedConnectionResolver<TClrType, TSchemaType>
        where TClrType : class
        where TSchemaType : class, IType
    {
        private readonly IQueryable<TClrType>? unfilteredQuery;
        private readonly IQueryable<TClrType> source;
        private readonly PageRequestInfo pageRequestInfo;

        public PagedConnectionResolver(IQueryable<TClrType>? unfilteredQuery, IQueryable<TClrType> source, PageRequestInfo pageRequestInfo)
        {
            this.unfilteredQuery = unfilteredQuery;
            this.source = source;
            this.pageRequestInfo = pageRequestInfo;
        }

        public async Task<IPagedConnection> ResolveAsync(CancellationToken cancellationToken)
        {
            var filteredCount = pageRequestInfo.HasFilteredCount ? await this.source.CountAsync(cancellationToken) : 0;
            var totalCount = pageRequestInfo.HasTotalCount ? await this.unfilteredQuery.CountAsync(cancellationToken) : 0;

            var data = await this.source
                .Skip(this.pageRequestInfo.Skip)
                .Take(this.pageRequestInfo.Take)
                .ToListAsync(cancellationToken);

            return new PagedConnection<TSchemaType>(totalCount, filteredCount, data);
        }
    }
}