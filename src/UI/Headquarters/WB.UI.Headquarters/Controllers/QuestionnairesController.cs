﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Implementation.Services;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.BoundedContexts.Headquarters.WebInterview;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.SurveyManagement.Web.Filters;
using WB.UI.Headquarters.Models;

namespace WB.UI.Headquarters.Controllers
{
    [LimitsFilter, Authorize(Roles = "Administrator, Headquarter, Supervisor")]
    [Localizable(false)]
    public class QuestionnairesController : Controller
    {
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly IQuestionnaireBrowseViewFactory browseViewFactory;
        private readonly IWebInterviewConfigProvider webInterviewConfigProvider;
        private readonly IPlainKeyValueStorage<QuestionnairePdf> pdfStorage;
        private readonly IUserViewFactory userViewFactory;
        private readonly IRestServiceSettings restServiceSettings;

        public QuestionnairesController(
            IQuestionnaireStorage questionnaireStorage,
            IQuestionnaireBrowseViewFactory browseViewFactory,
            IWebInterviewConfigProvider webInterviewConfigProvider,
            IRestServiceSettings restServiceSettings,
            IPlainKeyValueStorage<QuestionnairePdf> pdfStorage,
            IUserViewFactory userViewFactory)
        {
            this.questionnaireStorage = questionnaireStorage ?? throw new ArgumentNullException(nameof(questionnaireStorage));
            this.browseViewFactory = browseViewFactory ?? throw new ArgumentNullException(nameof(browseViewFactory));
            this.webInterviewConfigProvider = webInterviewConfigProvider ?? throw new ArgumentNullException(nameof(webInterviewConfigProvider));
            this.restServiceSettings = restServiceSettings ?? throw new ArgumentNullException(nameof(restServiceSettings));
            this.pdfStorage = pdfStorage ?? throw new ArgumentNullException(nameof(pdfStorage));
            this.userViewFactory = userViewFactory;
        }

        [OutputCache(Duration = 1200)]
        public ActionResult Details(string id)
        {
            var questionnaireIdentity = QuestionnaireIdentity.Parse(id);
            var questionnaire = this.questionnaireStorage.GetQuestionnaire(questionnaireIdentity, null);
            var browseItem = browseViewFactory.GetById(questionnaireIdentity);

            var model = new QuestionnaireDetailsModel
            {
                Title = questionnaire.Title,
                Version = questionnaire.Version,
                ImportDateUtc = browseItem.ImportDate.GetValueOrDefault(),
                LastEntryDateUtc = browseItem.LastEntryDate,
                CreationDateUtc = browseItem.CreationDate,
                WebMode = this.webInterviewConfigProvider.Get(questionnaireIdentity).Started,
                AudioAudit = browseItem.IsAudioRecordingEnabled,
                DesignerUrl = $"{this.restServiceSettings.Endpoint.TrimEnd('/')}/" +
                              $"questionnaire/details/{questionnaire.QuestionnaireId:N}${questionnaire.Revision}",
                Comment = browseItem.Comment
            };

            if (browseItem.ImportedBy.HasValue)
            {
                var user = this.userViewFactory.GetUser(browseItem.ImportedBy.Value);
                if (user != null)
                {
                    model.ImportedBy = new QuestionnaireDetailsModel.User
                    {
                        Role = Enum.GetName(typeof(UserRoles), user.Roles.First()),
                        Name = user.UserName
                    };
                }
            }

            var mainPdfFile = this.pdfStorage.HasNotEmptyValue(questionnaireIdentity.ToString());
            if (mainPdfFile)
            {
                model.MainPdfUrl = Url.Action("Pdf", new { id = id });
            }

            foreach (var translation in questionnaire.Translations)
            {
                if (this.pdfStorage.HasNotEmptyValue($"{translation.Id:N}_{questionnaireIdentity}"))
                {
                    var url = Url.Action("Pdf",
                        new
                        {
                            id = questionnaireIdentity.ToString(),
                            translation = translation.Id
                        });

                    var pdf = new TranslatedPdf(translation.Name, url);

                    model.TranslatedPdfVersions.Add(pdf);
                }
            }

            FillStats(questionnaireIdentity, model);

            return View(model);
        }

        [OutputCache(Duration = 1200, Location = OutputCacheLocation.Any)]
        public ActionResult Pdf(string id, Guid? translation = null)
        {
            var questionnaireIdentity = QuestionnaireIdentity.Parse(id);
            var questionnaire = this.questionnaireStorage.GetQuestionnaire(questionnaireIdentity, null);

            var pdf = translation != null
                ? this.pdfStorage.GetById($"{translation:N}_{id}")
                : this.pdfStorage.GetById(questionnaireIdentity.ToString());

            var fileName = Path.ChangeExtension(questionnaire.VariableName, ".pdf");
            if (translation != null)
            {
                var translationName = questionnaire.Translations.First(x => x.Id == translation).Name;
                fileName = translationName + " " + fileName;
            }

            return File(pdf.Content, "application/pdf", fileName);
        }

        private void FillStats(QuestionnaireIdentity questionnaireIdentity, QuestionnaireDetailsModel model)
        {
            var document = this.questionnaireStorage.GetQuestionnaireDocument(questionnaireIdentity);

            foreach (var questionnaireEntry in document.Children.TreeToEnumerable(x => x.Children))
            {
                if (questionnaireEntry is IGroup group)
                {
                    if (group.GetParent().PublicKey == questionnaireIdentity.QuestionnaireId)
                    {
                        model.SectionsCount++;
                    }
                    else if (group.IsRoster)
                    {
                        model.RostersCount++;
                    }
                    else
                    {
                        model.SubSectionsCount++;
                    }
                }
                else
                {
                    if (questionnaireEntry is IQuestion question)
                    {
                        model.QuestionsCount++;
                        if (!string.IsNullOrEmpty(question.ConditionExpression))
                        {
                            model.QuestionsWithConditionsCount++;
                        }
                    }
                }
            }
        }
    }
}