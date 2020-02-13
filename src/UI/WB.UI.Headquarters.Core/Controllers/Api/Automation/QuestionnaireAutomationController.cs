﻿
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.Implementation.Services;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.GenericSubdomains.Portable.Implementation;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.UI.Headquarters.Services;
using WB.UI.Headquarters.Services.Impl;

namespace WB.UI.Headquarters.API.Automation
{
    [Authorize(Roles = "Administrator")]
    [Route("api/QuestionnaireAutomation")]
    public class QuestionnaireAutomationController : ControllerBase
    {
        private readonly IDesignerUserCredentials designerUserCredentials;
        private readonly IQuestionnaireImportService importService;
        private readonly IAssignmentsUpgradeService upgradeService;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly IAuthorizedUser user;

        public QuestionnaireAutomationController(
            IDesignerUserCredentials designerUserCredentials,
            IQuestionnaireImportService importService,
            IAssignmentsUpgradeService upgradeService,
            IQuestionnaireStorage questionnaireStorage,

            IAuthorizedUser user)
        {
            this.designerUserCredentials = designerUserCredentials;
            this.importService = importService;
            this.upgradeService = upgradeService;
            this.questionnaireStorage = questionnaireStorage;
            this.user = user;
        }

        [HttpPost]
        [Route("ImportQuestionnaire")]
        public async Task<QuestionnaireIdentity> ImportQuestionnaire([FromBody] ImportQuestionnaireApiRequest request)
        {
            this.designerUserCredentials.Set(new RestCredentials
            {
                Login = request.DesignerUsername,
                Password = request.DesignerPassword
            });

            var result = await this.importService.Import(request.QuestionnaireId, null, false, request.Comment,
                Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(this.Request)
                , includePdf: false);

            if (result.Status == QuestionnaireImportStatus.Finished)
            {
                if (request.ShouldUpgradeAssignments)
                {
                    long version = request.MigrateFromVersion;
                    Guid questionnaireId = request.MigrateFrom;

                    var processId = Guid.NewGuid();
                    var sourceQuestionnaireId = new QuestionnaireIdentity(questionnaireId, version);
                    this.upgradeService.EnqueueUpgrade(processId, this.user.Id, sourceQuestionnaireId, result.Identity);
                }

                return result.Identity;
            }

            return null;
        }

        [HttpGet]
        [Route("IsQuestionnaireImported")]
        public bool IsQuestionnaireImported(Guid questionnaireId, long version)
        {
            return questionnaireStorage.GetQuestionnaire(new QuestionnaireIdentity(questionnaireId, version), null)
                   != null;
        }
    }

    public class ImportQuestionnaireApiRequest
    {
        public Guid QuestionnaireId { get; set; }
        public string DesignerUsername { get; set; }
        public string DesignerPassword { get; set; }
        public bool ShouldUpgradeAssignments { get; set; }
        public Guid MigrateFrom { get; set; }
        public long MigrateFromVersion { get; set; }
        public string Comment { get; set; }
    }
}
