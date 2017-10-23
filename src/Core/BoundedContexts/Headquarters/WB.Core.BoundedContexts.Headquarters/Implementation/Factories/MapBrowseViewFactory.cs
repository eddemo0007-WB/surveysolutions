﻿using System.Linq;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Maps;
using WB.Core.BoundedContexts.Headquarters.Views.Maps;
using WB.Core.Infrastructure.PlainStorage;
using WB.Infrastructure.Native.Utils;

namespace WB.Core.BoundedContexts.Headquarters.Implementation.Factories
{
    public class MapBrowseViewFactory : IMapBrowseViewFactory
    {
        private readonly IPlainStorageAccessor<MapBrowseItem> mapBrowseItemeRader;
        private readonly IPlainStorageAccessor<UserMap> userMapReader;

        public MapBrowseViewFactory(IPlainStorageAccessor<MapBrowseItem> mapBrowseItemeRader, 
            IPlainStorageAccessor<UserMap> userMapReader)
        {
            this.mapBrowseItemeRader = mapBrowseItemeRader;
            this.userMapReader = userMapReader;
        }

        public MapsView Load(MapsInputModel input)
        {
            return this.mapBrowseItemeRader.Query(queryable =>
            {
                IQueryable<MapBrowseItem> query = queryable;

                
                if (!string.IsNullOrEmpty(input.SearchBy))
                {
                    var filterLowerCase = input.SearchBy.ToLower();
                    query = query.Where(x => x.FileName.ToLower().Contains(filterLowerCase));
                }

                
                var queryResult = query.OrderUsingSortExpression(input.Order);

                IQueryable<MapBrowseItem> pagedResults = queryResult;

                if (input.PageSize > 0)
                {
                    pagedResults = queryResult.Skip((input.Page - 1) * input.PageSize).Take(input.PageSize);
                }

                var itemIds = pagedResults.Select(x => x.Id).ToArray();
                var actualItems = queryable.Where(x => itemIds.Contains(x.Id))
                                           .OrderUsingSortExpression(input.Order)
                                           .ToList();

                return new MapsView(){Page = input.Page , PageSize = input.PageSize , TotalCount = queryResult.Count() ,Items = actualItems };
            });
            
        }

        public MapUsersView Load(MapUsersInputModel input)
        {
            return this.userMapReader.Query(queryable =>
            {
                IQueryable<UserMap> query = queryable;


                if (!string.IsNullOrEmpty(input.SearchBy))
                {
                    var filterLowerCase = input.SearchBy.ToLower();
                    query = query.Where(x => x.UserName.ToLower().Contains(filterLowerCase));
                }


                var queryResult = query.OrderUsingSortExpression(input.Order);

                IQueryable<UserMap> pagedResults = queryResult;

                if (input.PageSize > 0)
                {
                    pagedResults = queryResult.Skip((input.Page - 1) * input.PageSize).Take(input.PageSize);
                }

                var itemIds = pagedResults.Select(x => x.Id).ToArray();
                var actualItems = queryable.Where(x => itemIds.Contains(x.Id))
                    .OrderUsingSortExpression(input.Order)
                    .ToList();

                return new MapUsersView() { Page = input.Page, PageSize = input.PageSize, TotalCount = queryResult.Count(), Items = actualItems.Select(x => x.UserName).ToList() };
            });
        }
    }
}
