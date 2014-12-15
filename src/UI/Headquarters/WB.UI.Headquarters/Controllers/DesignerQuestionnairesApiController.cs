﻿using System;
using System.Linq;
using System.Web;
using System.Web.Http;
using Main.Core.Documents;

using WB.Core.GenericSubdomains.Logging;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.GenericSubdomains.Utils.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernel.Structures.Synchronization.Designer;
using WB.Core.SharedKernel.Utils.Implementation.Services;
using WB.Core.SharedKernel.Utils.Services.Rest;
using WB.Core.SharedKernels.DataCollection.Commands.Questionnaire;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.SharedKernels.SurveyManagement.Views.Template;
using WB.Core.SharedKernels.SurveyManagement.Web.Controllers;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.Core.SharedKernels.SurveyManagement.Web.Utils.Membership;
using WB.UI.Shared.Web.Filters;

namespace WB.UI.Headquarters.Controllers
{
    [Authorize(Roles = "Headquarter")]
    [ApiValidationAntiForgeryToken]
    public class DesignerQuestionnairesApiController : BaseApiController
    {
        internal RestCredentials designerUserCredentials
        {
            get { return this.getDesignerUserCredentials(this.GlobalInfo); }
            set { SetDesignerUserCredentials(this.GlobalInfo, value); }
        }

        private readonly IRestService restService;
        private readonly IStringCompressor zipUtils;
        private readonly ISupportedVersionProvider supportedVersionProvider;
        private readonly Func<IGlobalInfoProvider, RestCredentials> getDesignerUserCredentials;

        public DesignerQuestionnairesApiController(
            ISupportedVersionProvider supportedVersionProvider,
            ICommandService commandService, IGlobalInfoProvider globalInfo, IStringCompressor zipUtils, ILogger logger, IRestService restService)
            : this(supportedVersionProvider, commandService, globalInfo, zipUtils, logger, GetDesignerUserCredentials, restService)
        {
        }

        internal DesignerQuestionnairesApiController(ISupportedVersionProvider supportedVersionProvider,
            ICommandService commandService, IGlobalInfoProvider globalInfo, IStringCompressor zipUtils, ILogger logger,
            Func<IGlobalInfoProvider, RestCredentials> getDesignerUserCredentials, IRestService restService)
            : base(commandService, globalInfo, logger)
        {
            this.zipUtils = zipUtils;
            this.getDesignerUserCredentials = getDesignerUserCredentials;
            this.supportedVersionProvider = supportedVersionProvider;
            this.restService = restService;
        }

        private static RestCredentials GetDesignerUserCredentials(IGlobalInfoProvider globalInfoProvider)
        {
            return (RestCredentials)HttpContext.Current.Session[globalInfoProvider.GetCurrentUser().Name];
        }

        private static void SetDesignerUserCredentials(IGlobalInfoProvider globalInfoProvider, RestCredentials designerUserCredentials)
        {
            HttpContext.Current.Session[globalInfoProvider.GetCurrentUser().Name] = designerUserCredentials;
        }

        public DesignerQuestionnairesView QuestionnairesList(DesignerQuestionnairesListViewModel data)
        {
            var list = this.restService.PostAsync<PagedQuestionnaireCommunicationPackage>(
                url: "pagedquestionnairelist",
                credentials: this.designerUserCredentials, 
                requestQueryString: new QuestionnaireListRequest()
                {
                    Filter = data.Request.Filter,
                    PageIndex = data.Pager.Page,
                    PageSize = data.Pager.PageSize,
                    SortOrder = data.SortOrder.GetOrderRequestString()
                }).Result;

            return new DesignerQuestionnairesView()
                {
                    Items = list.Items.Select(x => new DesignerQuestionnaireListViewItem() { Id = x.Id, Title = x.Title }),
                    TotalCount = list.TotalCount,
                    ItemsSummary = null
                };
        }

        [HttpPost]
        public QuestionnaireVerificationResponse GetQuestionnaire(ImportQuestionnaireRequest request)
        {
            try
            {
                var supportedVersion = this.supportedVersionProvider.GetSupportedQuestionnaireVersion();

                var docSource = this.restService.PostAsync<QuestionnaireCommunicationPackage>(
                    url: "questionnaire",
                    credentials: designerUserCredentials,
                    requestBody: new DownloadQuestionnaireRequest()
                    {
                        QuestionnaireId = request.QuestionnaireId,
                        SupportedQuestionnaireVersion =
                            new QuestionnaireVersion()
                            {
                                Major = supportedVersion.SupportedQuestionnaireVersionMajor,
                                Minor = supportedVersion.SupportedQuestionnaireVersionMinor,
                                Patch = supportedVersion.SupportedQuestionnaireVersionPatch
                            }
                    }).Result;

                var document = this.zipUtils.DecompressString<QuestionnaireDocument>(docSource.Questionnaire);

                var supportingAssembly = docSource.QuestionnaireAssembly;

                this.CommandService.Execute(new ImportFromDesigner(this.GlobalInfo.GetCurrentUser().Id, document,
                    request.AllowCensusMode, supportingAssembly));

                return new QuestionnaireVerificationResponse();
            }
            catch (Exception ex)
            {
                var domainEx = ex.GetSelfOrInnerAs<QuestionnaireException>();
                if (domainEx != null) return new QuestionnaireVerificationResponse() {ImportError = domainEx.Message};

                var restEx = ex.GetSelfOrInnerAs<RestException>();
                if (restEx != null) return new QuestionnaireVerificationResponse() {ImportError = restEx.Message};

                this.Logger.Error(
                    string.Format("Designer: error when importing template #{0}", request.QuestionnaireId), ex);
                throw;
            }
        }
    }
}