﻿using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using Main.Core.View;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.SharedKernels.QuestionnaireVerification.Services;
using WB.UI.Designer.Code;
using WB.UI.Designer.Filters;
using WB.UI.Designer.Models;

namespace WB.UI.Designer.Api
{
    [Authorize]
    public class QuestionnaireController : ApiController
    {
        private readonly IVerificationErrorsMapper verificationErrorsMapper;
        private readonly IQuestionnaireVerifier questionnaireVerifier;

        private readonly IViewFactory<QuestionnaireViewInputModel, QuestionnaireView> questionnaireViewFactory;
        private readonly IViewFactory<ChapterInfoViewInputModel, ChapterInfoView> chapterInfoViewFactory;
        private readonly IViewFactory<QuestionnaireInfoViewInputModel, QuestionnaireInfoView> questionnaireInfoViewFactory;

        public QuestionnaireController(IViewFactory<ChapterInfoViewInputModel, ChapterInfoView> chapterInfoViewFactory,
            IViewFactory<QuestionnaireInfoViewInputModel, QuestionnaireInfoView> questionnaireInfoViewFactory,
            IViewFactory<QuestionnaireViewInputModel, QuestionnaireView> questionnaireViewFactory, 
            IQuestionnaireVerifier questionnaireVerifier, 
            IVerificationErrorsMapper verificationErrorsMapper)
        {
            this.chapterInfoViewFactory = chapterInfoViewFactory;
            this.questionnaireInfoViewFactory = questionnaireInfoViewFactory;
            this.questionnaireViewFactory = questionnaireViewFactory;
            this.questionnaireVerifier = questionnaireVerifier;
            this.verificationErrorsMapper = verificationErrorsMapper;
        }

        public QuestionnaireInfoView Get(string id)
        {
            return questionnaireInfoViewFactory.Load(new QuestionnaireInfoViewInputModel() {QuestionnaireId = id});
        }

        [HttpGet]
        public ChapterInfoView Chapter(string id, string chapterId)
        {
            return
                chapterInfoViewFactory.Load(new ChapterInfoViewInputModel()
                {
                    QuestionnaireId = id,
                    ChapterId = chapterId
                });
        }

        [HttpGet]
        [CamelCase]
        public IHttpActionResult Verify(Guid id)
        {
            var questionnaireDocument = this.GetQuestionnaire(id).Source;

            var questoinnaireErrors = questionnaireVerifier.Verify(questionnaireDocument).ToArray();

            var verificationResult = new VerificationResult
            {
                Errors = questoinnaireErrors
            };
            return this.Ok(verificationResult);
        }

        private QuestionnaireView GetQuestionnaire(Guid id)
        {
            QuestionnaireView questionnaire = this.questionnaireViewFactory.Load(new QuestionnaireViewInputModel(id));

            if (questionnaire == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, string.Format("Questionnaire with id={0} cannot be found", id));
            }

            return questionnaire;
        }

    }
}
