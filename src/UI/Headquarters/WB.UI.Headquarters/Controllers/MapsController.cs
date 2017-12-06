﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Resources;
using WB.Core.BoundedContexts.Headquarters;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Repositories;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Maps;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.SurveyManagement.Web.Code;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models.Maps;
using WB.UI.Headquarters.Resources;
using WB.Core.BoundedContexts.Headquarters.Implementation.Services.Export;
using WB.Core.BoundedContexts.Headquarters.Maps;

namespace WB.UI.Headquarters.Controllers
{
    [LimitsFilter]
    [AuthorizeOr403(Roles = "Administrator, Headquarter")]
    public class MapsController : BaseController
    {
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly IMapStorageService mapStorageService;
        private readonly IExportFactory exportFactory;
        private readonly IAuthorizedUser authorizedUser;
        private readonly IMapPropertiesProvider mapPropertiesProvider;

        private readonly IPlainStorageAccessor<MapBrowseItem> mapPlainStorageAccessor;

        private readonly IRecordsAccessorFactory recordsAccessorFactory;

        public MapsController(ICommandService commandService, ILogger logger,
            IFileSystemAccessor fileSystemAccessor, IMapStorageService mapRepository,
            IExportFactory exportFactory, IRecordsAccessorFactory recordsAccessorFactory,
            IPlainStorageAccessor<MapBrowseItem> mapPlainStorageAccessor,
            IMapPropertiesProvider mapPropertiesProvider, IAuthorizedUser authorizedUser) : base(commandService, logger)
        {
            this.fileSystemAccessor = fileSystemAccessor;
            this.mapStorageService = mapRepository;
            this.exportFactory = exportFactory;
            this.recordsAccessorFactory = recordsAccessorFactory;
            this.mapPlainStorageAccessor = mapPlainStorageAccessor;
            this.authorizedUser = authorizedUser;
            this.mapPropertiesProvider = mapPropertiesProvider;
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
                    new {httproute = "", controller = "MapsApi", action = "UploadMapsFile"}),
                UserMapLinkingUrl =
                    Url.RouteUrl("Default", new {httproute = "", controller = "Maps", action = "UserMapsLink"}),
                DeleteMapLinkUrl = Url.RouteUrl("DefaultApiWithAction",
                    new {httproute = "", controller = "MapsApi", action = "DeleteMap"}),
                IsObserver = authorizedUser.IsObserver,
                IsObserving = authorizedUser.IsObserving,
            };
            return View(model);
        }

        [HttpDelete]
        [ObserverNotAllowed]
        public ActionResult DeleteMap(string mapName)
        {
            this.mapStorageService.DeleteMap(mapName);
            return this.Content("ok");
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
                FileExtension = TabExportFile.Extention
            };
            return View(model);
        }

        [HttpGet]
        [ActivePage(MenuItem.Maps)]
        public ActionResult Details(string mapName)
        {
            if (mapName == null)
                return HttpNotFound();

            MapBrowseItem map = mapPlainStorageAccessor.GetById(mapName);
            if (map == null)
                return HttpNotFound();

            return this.View("Details",
                new MapDetailsModel
                {

                    DataUrl = Url.RouteUrl("DefaultApiWithAction",
                        new
                        {
                            httproute = "",
                            controller = "MapsApi",
                            action = "MapUserList"
                        }),
                    MapPreviewUrl = Url.RouteUrl("Default",
                        new {httproute = "", controller = "Maps", action = "MapPreview", mapName = map.Id}),
                    MapsUrl = Url.RouteUrl("Default", new {httproute = "", controller = "Maps", action = "Index"}),
                    FileName = mapName,
                    Size = FileSizeUtils.SizeInMegabytes(map.Size),
                    Wkid = map.Wkid,
                    ImportDate = map.ImportDate.HasValue ? map.ImportDate.Value.FormatDateWithTime() : "",
                    MaxScale = map.MaxScale,
                    MinScale = map.MinScale,
                    DeleteMapUserLinkUrl = Url.RouteUrl("DefaultApiWithAction",
                        new {httproute = "", controller = "MapsApi", action = "DeleteMapUser"})
                });
        }

        [HttpDelete]
        [ObserverNotAllowed]
        public ActionResult DeleteMapUserLink(string mapName, string userName)
        {
            this.mapStorageService.DeleteMapUserLink(mapName, userName);
            return this.Content("ok");
        }


        [HttpGet]
        public ActionResult MapPreview(string mapName)
        {
            this.ViewBag.ActivePage = MenuItem.Maps;

            MapBrowseItem map = mapPlainStorageAccessor.GetById(mapName);
            if (map == null)
                return HttpNotFound();

            return View(map);
        }
    }
}