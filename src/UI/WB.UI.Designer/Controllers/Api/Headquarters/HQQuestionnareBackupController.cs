using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WB.Core.BoundedContexts.Designer;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.ImportExport;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.UI.Designer.Code;
using WB.UI.Designer.Code.ImportExport;
using WB.UI.Designer.Models;
using WB.UI.Designer.Resources;

namespace WB.UI.Designer.Controllers.Api.Headquarters
{
    [Route("api/hq/backup")]
    [Authorize]
    public class HQQuestionnareBackupController : HQControllerBase
    {
        private readonly IQuestionnaireViewFactory questionnaireViewFactory;
        private readonly IVerificationErrorsMapper verificationErrorsMapper;
        private readonly IQuestionnaireHelper questionnaireExportService;
        public HQQuestionnareBackupController(IQuestionnaireHelper questionnaireExportService, 
            IQuestionnaireViewFactory questionnaireViewFactory,
            IVerificationErrorsMapper verificationErrorsMapper)
        {
            this.questionnaireExportService = questionnaireExportService;
            this.questionnaireViewFactory = questionnaireViewFactory;
            this.verificationErrorsMapper = verificationErrorsMapper;
        }

        private const int MaxVerificationErrors = 20;

        [HttpGet]
        [Route("verify/{questionnaireId}")]
        public IActionResult Verify(Guid questionnaireId)
        {
            var questionnaireView = this.questionnaireViewFactory.Load(new QuestionnaireViewInputModel(questionnaireId));
            if (questionnaireView == null)
            {
                return this.ErrorWithReasonPhraseForHQ(StatusCodes.Status404NotFound, string.Format(ErrorMessages.TemplateNotFound, questionnaireId));
            }

            if (!this.ValidateAccessPermissions(questionnaireView))
            {
                return this.ErrorWithReasonPhraseForHQ(StatusCodes.Status403Forbidden, ErrorMessages.NoAccessToQuestionnaire);
            }

            var result = VerifyQuestionnaire(questionnaireView);

            var verificationErrors = result
                .Where(x => x.MessageLevel > VerificationMessageLevel.Warning)
                .Take(MaxVerificationErrors)
                .ToArray();

            var verificationWarnings = result
                .Where(x => x.MessageLevel == VerificationMessageLevel.Warning)
                .Take(MaxVerificationErrors - verificationErrors.Length)
                .ToArray();

            var readOnlyQuestionnaire = questionnaireView.Source.AsReadOnly();
            VerificationMessage[] errors = this.verificationErrorsMapper.EnrichVerificationErrors(verificationErrors, readOnlyQuestionnaire);
            VerificationMessage[] warnings = this.verificationErrorsMapper.EnrichVerificationErrors(verificationWarnings, readOnlyQuestionnaire);

            return Ok(new VerificationResult
            (
                errors : errors,
                warnings : warnings
            ));
        }

        private static List<QuestionnaireVerificationMessage> VerifyQuestionnaire(QuestionnaireView questionnaireView)
        {
            var questionnaireDocument = questionnaireView.Source;
            var readOnlyQuestionnaireDocument = questionnaireDocument.AsReadOnly();
            var multiLanguageQuestionnaireDocument = new MultiLanguageQuestionnaireDocument(
                readOnlyQuestionnaireDocument,
                Enumerable.Empty<ReadOnlyQuestionnaireDocument>(),
                questionnaireView.SharedPersons);

            var verifier = new ExportQuestionnaireVerifier();
            var result = verifier.Verify(multiLanguageQuestionnaireDocument).ToList();
            return result;
        }

        [HttpGet]
        [Route("{questionnaireId}")]
        public IActionResult Get(Guid questionnaireId)
        {
            var questionnaireView = this.questionnaireViewFactory.Load(new QuestionnaireViewInputModel(questionnaireId));
            if (questionnaireView == null)
            {
                return this.ErrorWithReasonPhraseForHQ(StatusCodes.Status404NotFound, string.Format(ErrorMessages.TemplateNotFound, questionnaireId));
            }

            if (!this.ValidateAccessPermissions(questionnaireView))
            {
                return this.ErrorWithReasonPhraseForHQ(StatusCodes.Status403Forbidden, ErrorMessages.NoAccessToQuestionnaire);
            }

            var result = VerifyQuestionnaire(questionnaireView);
            if (result.Any(v => v.MessageLevel > VerificationMessageLevel.Warning))
            {
                return Forbid();
            }

            var stream = this.questionnaireExportService.GetBackupQuestionnaire(questionnaireId, out string questionnaireFileName);
            if (stream == null) return NotFound();

            return File(stream, "application/zip", $"{questionnaireFileName}.zip");
        }
    }
}
