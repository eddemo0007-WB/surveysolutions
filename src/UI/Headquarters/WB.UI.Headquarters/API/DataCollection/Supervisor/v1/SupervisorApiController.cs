﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using Main.Core.Entities.SubEntities;
using Microsoft.AspNet.Identity;
using WB.Core.BoundedContexts.Headquarters.DataExport.Security;
using WB.Core.BoundedContexts.Headquarters.Implementation;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.BoundedContexts.Headquarters.Views.SynchronizationLog;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.Versions;
using WB.Core.SharedKernels.DataCollection;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Resources;
using WB.UI.Headquarters.Utils;
using WB.UI.Shared.Web.Extensions;
using WB.UI.Shared.Web.Filters;

namespace WB.UI.Headquarters.API.DataCollection.Supervisor.v1
{
    public class SupervisorApiController : AppApiControllerBase
    {
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly IAndroidPackageReader androidPackageReader;
        private readonly ITabletInformationService tabletInformationService;
        private readonly HqSignInManager signInManager;
        private readonly ISupervisorSyncProtocolVersionProvider syncVersionProvider;
        private readonly IProductVersion productVersion;
        private readonly IUserViewFactory userViewFactory;

        public SupervisorApiController(
            IFileSystemAccessor fileSystemAccessor,
            IAndroidPackageReader androidPackageReader, 
            ITabletInformationService tabletInformationService, 
            ISupervisorSyncProtocolVersionProvider syncVersionProvider,
            IProductVersion productVersion,
            IUserViewFactory userViewFactory, 
            HqSignInManager signInManager,
            IPlainKeyValueStorage<InterviewerSettings> settingsStorage,
            IPlainKeyValueStorage<TenantSettings> tenantSettings)
            : base(settingsStorage, tenantSettings)
        {
            this.fileSystemAccessor = fileSystemAccessor;
            this.androidPackageReader = androidPackageReader;
            this.tabletInformationService = tabletInformationService;
            this.syncVersionProvider = syncVersionProvider;
            this.productVersion = productVersion;
            this.userViewFactory = userViewFactory;
            this.signInManager = signInManager;
        }

        [HttpGet]
        [WriteToSyncLog(SynchronizationLogType.GetSupervisorApk)]
        public virtual HttpResponseMessage GetSupervisor() =>
            this.HttpResponseMessage(ClientApkInfo.SupervisorFileName, ClientApkInfo.SupervisorFileName);

        [HttpGet]
        [WriteToSyncLog(SynchronizationLogType.GetApk)]
        public virtual HttpResponseMessage GetInterviewer() =>
            this.HttpResponseMessage(ClientApkInfo.InterviewerFileName, ClientApkInfo.InterviewerFileName);

        [HttpGet]
        [WriteToSyncLog(SynchronizationLogType.GetExtendedApk)]
        public virtual HttpResponseMessage GetInterviewerWithMaps() =>
            this.HttpResponseMessage(ClientApkInfo.InterviewerExtendedFileName, ClientApkInfo.InterviewerFileName);

        private HttpResponseMessage HttpResponseMessage(string appName, string responseFileName)
        {
            string pathToSupervisorApp = this.fileSystemAccessor.CombinePath(HostingEnvironment.MapPath(ClientApkInfo.Directory), appName);

            if (!this.fileSystemAccessor.IsFileExists(pathToSupervisorApp))
                return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, TabletSyncMessages.FileWasNotFound);

            var fileHash = this.fileSystemAccessor.ReadHash(pathToSupervisorApp);

            if (this.RequestHasMatchingFileHash(fileHash))
            {
                return Request.CreateResponse(HttpStatusCode.NotModified);
            }
            
            Stream fileStream = new FileStream(pathToSupervisorApp, FileMode.Open, FileAccess.Read);
                       
            return this.AsProgressiveDownload(fileStream, @"application/vnd.android.package-archive", 
                responseFileName, fileHash);
        }

        [HttpGet]
        public virtual int? GetLatestVersion()
        {
            string pathToSupervisorApp =
                this.fileSystemAccessor.CombinePath(HostingEnvironment.MapPath(ClientApkInfo.Directory),
                    ClientApkInfo.SupervisorFileName);

            return !this.fileSystemAccessor.IsFileExists(pathToSupervisorApp)
                ? null
                : this.androidPackageReader.Read(pathToSupervisorApp).Version;
        }

        [ApiBasicAuth(UserRoles.Supervisor)]
        [WriteToSyncLog(SynchronizationLogType.CanSynchronize)]
        [HttpGet]
        [ApiNoCache]
        public virtual HttpResponseMessage CheckCompatibility(string deviceId, int deviceSyncProtocolVersion, string tenantId = null)
        {
            int serverSyncProtocolVersion = this.syncVersionProvider.GetProtocolVersion();
            int lastNonUpdatableSyncProtocolVersion = this.syncVersionProvider.GetLastNonUpdatableVersion();
            if (deviceSyncProtocolVersion < lastNonUpdatableSyncProtocolVersion)
                return this.Request.CreateResponse(HttpStatusCode.UpgradeRequired);

            if (!UserIsFromThisTenant(tenantId))
            {
                return this.Request.CreateResponse(HttpStatusCode.Conflict);
            }

            var currentVersion = new Version(this.productVersion.ToString().Split(' ')[0]);
            var supervisorVersion = GetSupervisorVersionFromUserAgent(this.Request);

            if (IsNeedUpdateAppBySettings(supervisorVersion, currentVersion))
            {
                return this.Request.CreateResponse(HttpStatusCode.UpgradeRequired);
            }

            if (supervisorVersion != null && supervisorVersion > currentVersion)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotAcceptable);
            }

            if (deviceSyncProtocolVersion != serverSyncProtocolVersion)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotAcceptable);
            }

            return this.Request.CreateResponse(HttpStatusCode.OK, @"158329303");
        }

        private Version GetSupervisorVersionFromUserAgent(HttpRequestMessage request)
        {
            foreach (var product in request.Headers?.UserAgent)
            {
                if ((product.Product?.Name.Equals(@"org.worldbank.solutions.supervisor",
                         StringComparison.OrdinalIgnoreCase) ?? false)
                    && Version.TryParse(product.Product.Version, out Version version))
                {
                    return version;
                }
            }

            return null;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> PostTabletInformation()
        {
            HttpRequestMessage request = this.Request;
            if (!request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var authHeader = request.Headers.Authorization?.ToString();

            if (authHeader != null)
            {
                await signInManager.SignInWithAuthTokenAsync(authHeader, false, UserRoles.Supervisor);
            }

            var multipartMemoryStreamProvider = await request.Content.ReadAsMultipartAsync();
            var httpContent = multipartMemoryStreamProvider.Contents.Single();
            var fileContent = await httpContent.ReadAsByteArrayAsync();

            var deviceId = this.Request.Headers.GetValues(@"DeviceId").Single();
            var userId = User.Identity.GetUserId();

            var user = userId != null
                ? this.userViewFactory.GetUser(new UserViewInputModel(Guid.Parse(userId)))
                : null;

            this.tabletInformationService.SaveTabletInformation(
                content: fileContent,
                androidId: deviceId,
                user: user);

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        private HttpResponseMessage GetPatchFile(string fileName)
        {
            string pathToInterviewerPatch = this.fileSystemAccessor.CombinePath(
                HostingEnvironment.MapPath(ClientApkInfo.Directory), fileName);

            if (!this.fileSystemAccessor.IsFileExists(pathToInterviewerPatch))
                return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, TabletSyncMessages.FileWasNotFound);

            Stream fileStream = new FileStream(pathToInterviewerPatch, FileMode.Open, FileAccess.Read);
            return this.AsProgressiveDownload(fileStream, @"application/octet-stream", 
                hash: this.fileSystemAccessor.ReadHash(pathToInterviewerPatch));
        }

        [HttpGet]
        [WriteToSyncLog(SynchronizationLogType.GetSupervisorApkPatch)]
        public virtual HttpResponseMessage Patch(int deviceVersion)
        {
            return GetPatchFile($@"Supervisor.{deviceVersion}.delta");
        }
    }
}
