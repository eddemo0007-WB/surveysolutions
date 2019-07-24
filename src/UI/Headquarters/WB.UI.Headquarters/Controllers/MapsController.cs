﻿using System.Web.Mvc;
using WB.Core.BoundedContexts.Headquarters;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Maps;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models.Maps;
using WB.Core.BoundedContexts.Headquarters.Implementation.Services.Export;
using WB.Core.BoundedContexts.Headquarters.Maps;

namespace WB.UI.Headquarters.Controllers
{
    [LimitsFilter]
    [AuthorizeOr403(Roles = "Administrator, Headquarter")]
    public class MapsController : BaseController
    {
        private readonly IAuthorizedUser authorizedUser;

        private readonly IPlainStorageAccessor<MapBrowseItem> mapPlainStorageAccessor;

        public MapsController(ICommandService commandService, ILogger logger,
            IPlainStorageAccessor<MapBrowseItem> mapPlainStorageAccessor, IAuthorizedUser authorizedUser) : base(commandService, logger)
        {
            this.mapPlainStorageAccessor = mapPlainStorageAccessor;
            this.authorizedUser = authorizedUser;
        }

        public ActionResult Index()
        {
            this.ViewBag.ActivePage = MenuItem.Maps;

            var model = new MapsModel()
            {
                DataUrl = Url.RouteUrl("DefaultApiWithAction",
                    new
                    {
                        httproute = "",
                        controller = "MapsApi",
                        action = "MapList"
                    }),

                UploadMapsFileUrl = Url.RouteUrl("DefaultApiWithAction",
                    new {httproute = "", controller = "MapsApi", action = "Upload"}),
                UserMapsUrl =
                    Url.RouteUrl("Default", new { httproute = "", controller = "Maps", action = "UserMaps" }),
                UserMapLinkingUrl =
                    Url.RouteUrl("Default", new {httproute = "", controller = "Maps", action = "UserMapsLink"}),
                DeleteMapLinkUrl = Url.RouteUrl("DefaultApiWithAction",
                    new {httproute = "", controller = "MapsApi", action = "DeleteMap"}),
                IsObserver = authorizedUser.IsObserver,
                IsObserving = authorizedUser.IsObserving,
            };
            return View(model);
        }

        public ActionResult UserMapsLink()
        {
            this.ViewBag.ActivePage = MenuItem.Maps;
            var model = new UserMapLinkModel()
            {
                DownloadAllUrl = Url.RouteUrl("DefaultApiWithAction",
                    new {httproute = "", controller = "MapsApi", action = "MappingDownload"}),
                UploadUrl = Url.RouteUrl("DefaultApiWithAction",
                    new { httproute = "", controller = "MapsApi", action = "UploadMappings" }),
                MapsUrl = Url.RouteUrl("Default", new {httproute = "", controller = "Maps", action = "Index"}),
                IsObserver = authorizedUser.IsObserver,
                IsObserving = authorizedUser.IsObserving,
                FileExtension = TabExportFile.Extention,

                UserMapsUrl = Url.RouteUrl("Default", new { httproute = "", controller = "Maps", action = "UserMaps" })
            };
            return View(model);
        }

        [HttpGet]
        [ActivePage(MenuItem.Maps)]
        public ActionResult UserMaps()
        {
            this.ViewBag.ActivePage = MenuItem.Maps;
            var model = new UserMapModel()
            {
                DataUrl = Url.RouteUrl("DefaultApiWithAction",
                    new
                    {
                        httproute = "",
                        controller = "MapsApi",
                        action = "UserMaps"
                    }),
                MapsUrl = Url.RouteUrl("Default", new { httproute = "", controller = "Maps", action = "Index" }),
                UserMapLinkingUrl = Url.RouteUrl("Default", new { httproute = "", controller = "Maps", action = "UserMapsLink" })
            };
            return View(model);
        }
    }
}
