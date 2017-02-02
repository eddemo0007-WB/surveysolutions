using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Recaptcha.Web;
using Recaptcha.Web.Mvc;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.BoundedContexts.Headquarters.WebInterview;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.UI.Headquarters.Controllers;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models.WebInterview;
using WB.UI.Headquarters.Resources;
using WB.UI.Shared.Web.Settings;

namespace WB.Core.SharedKernels.SurveyManagement.Web.Controllers
{
    [WebInterviewEnabled]
    public class WebInterviewController : BaseController
    {
        private readonly ICommandService commandService;
        private readonly IWebInterviewConfigProvider configProvider;
        private readonly IUserViewFactory usersRepository;
        private readonly IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory;
        private readonly IPlainInterviewFileStorage plainInterviewFileStorage;
        private readonly IStatefulInterviewRepository statefulInterviewRepository;
        private readonly IWebInterviewConfigProvider webInterviewConfigProvider;


        public WebInterviewController(ICommandService commandService, 
            IWebInterviewConfigProvider configProvider,
            IGlobalInfoProvider globalInfo, 
            IQuestionnaireBrowseViewFactory questionnaireBrowseViewFactory,
            IPlainInterviewFileStorage plainInterviewFileStorage,
            IStatefulInterviewRepository statefulInterviewRepository,
            IWebInterviewConfigProvider webInterviewConfigProvider,
            ILogger logger, IUserViewFactory usersRepository)
            : base(commandService, globalInfo, logger)
        {
            this.commandService = commandService;
            this.configProvider = configProvider;
            this.questionnaireBrowseViewFactory = questionnaireBrowseViewFactory;
            this.plainInterviewFileStorage = plainInterviewFileStorage;
            this.statefulInterviewRepository = statefulInterviewRepository;
            this.webInterviewConfigProvider = webInterviewConfigProvider;
            this.usersRepository = usersRepository;
        }

        public ActionResult Start(string id)
        {
            var questionnaireIdentity = QuestionnaireIdentity.Parse(id);
            var webInterviewConfig = this.configProvider.Get(questionnaireIdentity);
            if (!webInterviewConfig.Started)
            {
                return this.HttpNotFound();
            }

            var model = this.GetModel(questionnaireIdentity, webInterviewConfig);

            return this.View(model);
        }

        [HttpPost]
        [ActionName("Start")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> StartPost(string id)
        {
            var questionnaireIdentity = QuestionnaireIdentity.Parse(id);
            var webInterviewConfig = this.configProvider.Get(questionnaireIdentity);
            if (!webInterviewConfig.Started)
            {
                return this.HttpNotFound();
            }

            if (webInterviewConfig.UseCaptcha)
            {
                var helper = this.GetRecaptchaVerificationHelper();
                var verifyResult = await helper.VerifyRecaptchaResponseTaskAsync();
                if (verifyResult != RecaptchaVerificationResult.Success)
                {
                    var model = this.GetModel(questionnaireIdentity, webInterviewConfig);
                    this.ModelState.AddModelError("InvalidCaptcha", WebInterview.PleaseFillCaptcha);
                    return this.View(model);
                }
            }

            string interviewId = this.CreateInterview(questionnaireIdentity);
            return this.Redirect("~/WebInterview/" + interviewId + "/Cover");
        }

        private StartWebInterview GetModel(QuestionnaireIdentity questionnaireIdentity, WebInterviewConfig webInterviewConfig)
        {
            var questionnaireBrowseItem = this.questionnaireBrowseViewFactory.GetById(questionnaireIdentity);

            var model = new StartWebInterview();
            model.QuestionnaireTitle = questionnaireBrowseItem.Title;
            model.UseCaptcha = webInterviewConfig.UseCaptcha;
            return model;
        }

        public ActionResult Index(string id)
        {
            return this.View();
        }

        private string CreateInterview(QuestionnaireIdentity questionnaireId)
        {
            var webInterviewConfig = this.configProvider.Get(questionnaireId);
            if (!webInterviewConfig.Started)
            {
                throw new InvalidOperationException(@"Web interview is not started for this questionnaire");
            }
            var responsibleId = webInterviewConfig.ResponsibleId;
            var interviewer = this.usersRepository.Load(new UserViewInputModel(publicKey: responsibleId));

            var interviewId = Guid.NewGuid();
            var createInterviewOnClientCommand = new CreateInterviewOnClientCommand(interviewId,
                interviewer.PublicKey, questionnaireId, DateTime.UtcNow,
                interviewer.Supervisor.Id);

            this.commandService.Execute(createInterviewOnClientCommand);
            return interviewId.FormatGuid();
        }

        [HttpGet]
        public ActionResult Image(string interviewId, string questionId, string filename)
        {
            var interview = this.statefulInterviewRepository.Get(interviewId);
            
            if(interview.Status != InterviewStatus.InterviewerAssigned && interview.GetMultimediaQuestion(Identity.Parse(questionId)) != null)
            {
                return this.FileNotFound();
            }

            var file = plainInterviewFileStorage.GetInterviewBinaryData(interview.Id, filename);
            if (file == null || file.Length == 0)
                return this.FileNotFound();

            return this.File(file, "image/jpeg", filename);
        }

        [HttpPost]
        public async Task<ActionResult> Image(string interviewId, string questionId, HttpPostedFileBase file)
        {
            var interview = this.statefulInterviewRepository.Get(interviewId);
            var questionIdentity = Identity.Parse(questionId);
            var question = interview.GetQuestion(questionIdentity);

            if (interview.Status != InterviewStatus.InterviewerAssigned && question?.AsMultimedia != null)
            {
                return this.Json("fail");
            }

            using (var ms = new MemoryStream())
            {
                await file.InputStream.CopyToAsync(ms);

                var filename = $@"{question.VariableName}{string.Join(@"-", questionIdentity.RosterVector.Select(rv => (int)rv))}{DateTime.UtcNow.GetHashCode().ToString()}.jpg";
                var responsibleId = this.webInterviewConfigProvider.Get(interview.QuestionnaireIdentity).ResponsibleId;

                this.plainInterviewFileStorage.StoreInterviewBinaryData(interview.Id, filename, ms.ToArray());
                this.commandService.Execute(new AnswerPictureQuestionCommand(interview.Id,
                    responsibleId, questionIdentity.Id, questionIdentity.RosterVector, DateTime.UtcNow, filename));
            }

            return this.Json("ok");
        }

        private ActionResult FileNotFound()
        {
            return
                   this.File(
                       Assembly.GetExecutingAssembly()
                           .GetManifestResourceStream("WB.UI.Headquarters.Content.img.no_image_found.jpg"),
                       "image/jpeg", "no_image_found.jpg");
        }


        protected override void OnException(ExceptionContext filterContext)
        {
           HandleInDebugMode(filterContext);
        }

        [Conditional("DEBUG")]
        private void HandleInDebugMode(ExceptionContext filterContext)
        {
            filterContext.ExceptionHandled = true;
            
            filterContext.Result = new ContentResult
            {
                Content = $@"
<h1>No <b>Index.cshtml<b> found in ~Views\WebInterview folder.</h1>
<p>Index.cshtml is generated by 'WB.UI.Headquarters.Interview' application build. </p>
<p>Please navigate to WB.UI.Headquarters.Interview folder and run following commands that will install all nodejs deps and run dev server</p>
<pre>
npm install 
npm run dev
</pre>
Exception details:<br />
<pre>{filterContext.Exception.ToString()}</pre>"
            };
        }
    }
}