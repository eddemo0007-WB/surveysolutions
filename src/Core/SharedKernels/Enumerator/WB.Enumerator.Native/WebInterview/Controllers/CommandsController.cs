﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web.Http;
using Main.Core.Entities.SubEntities;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Commands.Interview.Base;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Enumerator.Native.WebInterview.Models;

namespace WB.Enumerator.Native.WebInterview.Controllers
{
    public abstract class CommandsController : ApiController
    {
        protected readonly ICommandService commandService;
        protected readonly IImageFileStorage imageFileStorage;
        protected readonly IAudioFileStorage audioFileStorage;
        protected readonly IQuestionnaireStorage questionnaireRepository;
        protected readonly IStatefulInterviewRepository statefulInterviewRepository;
        protected readonly IWebInterviewNotificationService webInterviewNotificationService;


        public CommandsController(ICommandService commandService, IImageFileStorage imageFileStorage, IAudioFileStorage audioFileStorage,
            IQuestionnaireStorage questionnaireRepository, IStatefulInterviewRepository statefulInterviewRepository,
            IWebInterviewNotificationService webInterviewNotificationService)
        {
            this.commandService = commandService;
            this.imageFileStorage = imageFileStorage;
            this.audioFileStorage = audioFileStorage;
            this.questionnaireRepository = questionnaireRepository;
            this.statefulInterviewRepository = statefulInterviewRepository;
            this.webInterviewNotificationService = webInterviewNotificationService;
        }

        protected virtual Guid GetCommandResponsibleId(Guid interviewId)
        {
            var interview = statefulInterviewRepository.Get(interviewId.FormatGuid());
            return interview.CurrentResponsibleId;
        }

        public IHttpActionResult ChangeLanguage(ChangeLanguageRequest request)
        {
            this.commandService.Execute(new SwitchTranslation(request.InterviewId, request.Language, this.GetCommandResponsibleId(request.InterviewId)));
            return Ok();
        }

        public IHttpActionResult AnswerTextQuestion(Guid interviewId, string questionIdenty, string text)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerTextQuestionCommand(interviewId,
                this.GetCommandResponsibleId(interviewId), identity.Id, identity.RosterVector, text));
            return Ok();
        }

        public IHttpActionResult AnswerTextListQuestion(Guid interviewId, string questionIdenty, TextListAnswerRowDto[] rows)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerTextListQuestionCommand(interviewId,
                this.GetCommandResponsibleId(interviewId), identity.Id, identity.RosterVector,
                rows.Select(row => new Tuple<decimal, string>(row.Value, row.Text)).ToArray()));
            return Ok();
        }

        public IHttpActionResult AnswerGpsQuestion(Guid interviewId, string questionIdenty, GpsAnswer answer)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerGeoLocationQuestionCommand(interviewId,
                this.GetCommandResponsibleId(interviewId), identity.Id, identity.RosterVector, answer.Latitude, answer.Longitude,
                answer.Accuracy ?? 0, answer.Altitude ?? 0, DateTimeOffset.FromUnixTimeMilliseconds(answer.Timestamp ?? 0)));
            return Ok();
        }

        public IHttpActionResult AnswerDateQuestion(Guid interviewId, string questionIdenty, DateTime answer)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerDateTimeQuestionCommand(interviewId,
                this.GetCommandResponsibleId(interviewId), identity.Id, identity.RosterVector, answer));
            return Ok();
        }

        public IHttpActionResult AnswerSingleOptionQuestion(Guid interviewId, int answer, string questionId)
        {
            Identity identity = Identity.Parse(questionId);
            this.ExecuteQuestionCommand(new AnswerSingleOptionQuestionCommand(interviewId, GetCommandResponsibleId(interviewId),
                identity.Id, identity.RosterVector, answer));
            return Ok();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used by HqApp @store.actions.js")]
        public IHttpActionResult AnswerLinkedSingleOptionQuestion(Guid interviewId, string questionIdentity, decimal[] answer)
        {
            Identity identity = Identity.Parse(questionIdentity);
            this.ExecuteQuestionCommand(new AnswerSingleOptionLinkedQuestionCommand(interviewId, GetCommandResponsibleId(interviewId),
                identity.Id, identity.RosterVector, answer));
            return Ok();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used by HqApp @store.actions.js")]
        public IHttpActionResult AnswerLinkedMultiOptionQuestion(Guid interviewId, string questionIdentity, decimal[][] answer)
        {
            Identity identity = Identity.Parse(questionIdentity);
            this.ExecuteQuestionCommand(new AnswerMultipleOptionsLinkedQuestionCommand(interviewId, GetCommandResponsibleId(interviewId),
                identity.Id, identity.RosterVector, answer.Select(x => new RosterVector(x)).ToArray()));
            return Ok();
        }

        public IHttpActionResult AnswerMultiOptionQuestion(Guid interviewId, int[] answer, string questionId)
        {
            Identity identity = Identity.Parse(questionId);
            this.ExecuteQuestionCommand(new AnswerMultipleOptionsQuestionCommand(interviewId, GetCommandResponsibleId(interviewId),
                identity.Id, identity.RosterVector, answer));
            return Ok();
        }

        public IHttpActionResult AnswerYesNoQuestion(Guid interviewId, string questionId, InterviewYesNoAnswer[] answerDto)
        {
            Identity identity = Identity.Parse(questionId);
            var answer = answerDto.Select(a => new AnsweredYesNoOption(a.Value, a.Yes)).ToArray();
            this.ExecuteQuestionCommand(new AnswerYesNoQuestion(interviewId, GetCommandResponsibleId(interviewId),
                identity.Id, identity.RosterVector, answer));
            return Ok();
        }

        public IHttpActionResult AnswerIntegerQuestion(Guid interviewId, string questionIdenty, int answer)
        {
            Identity identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerNumericIntegerQuestionCommand(interviewId, this.GetCommandResponsibleId(interviewId), identity.Id, identity.RosterVector, answer));
            return Ok();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used by HqApp @store.actions.js")]
        public IHttpActionResult AnswerDoubleQuestion(Guid interviewId, string questionIdenty, double answer)
        {
            Identity identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerNumericRealQuestionCommand(interviewId, this.GetCommandResponsibleId(interviewId), identity.Id, identity.RosterVector, answer));
            return Ok();
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used by HqApp @store.actions.js")]
        public IHttpActionResult AnswerQRBarcodeQuestion(Guid interviewId, string questionIdenty, string text)
        {
            var identity = Identity.Parse(questionIdenty);
            this.ExecuteQuestionCommand(new AnswerQRBarcodeQuestionCommand(interviewId,
                this.GetCommandResponsibleId(interviewId), identity.Id, identity.RosterVector, text));
            return Ok();
        }

        [ObserverNotAllowed]
        public IHttpActionResult RemoveAnswer(Guid interviewId, string questionId)
        {
            Identity identity = Identity.Parse(questionId);

            try
            {
                var interview = statefulInterviewRepository.Get(interviewId.FormatGuid());
                var questionnaire = questionnaireRepository.GetQuestionnaire(interview.QuestionnaireIdentity, null);
                var questionType = questionnaire.GetQuestionType(identity.Id);

                if (questionType == QuestionType.Multimedia)
                {
                    var fileName = $@"{questionnaire.GetQuestionVariableName(identity.Id)}{string.Join(@"-", identity.RosterVector.Select(rv => rv))}.jpg";
                    this.imageFileStorage.RemoveInterviewBinaryData(interviewId, fileName);
                }
                else if (questionType == QuestionType.Audio)
                {
                    var fileName = $@"{questionnaire.GetQuestionVariableName(identity.Id)}__{identity.RosterVector}.m4a";
                    this.audioFileStorage.RemoveInterviewBinaryData(interviewId, fileName);
                }
            }
            catch (Exception e)
            {
                webInterviewNotificationService.MarkAnswerAsNotSaved(interviewId, identity, e);
            }

            this.ExecuteQuestionCommand(new RemoveAnswerCommand(interviewId, GetCommandResponsibleId(interviewId), identity));
            return Ok();
        }

        [ObserverNotAllowed]
        public abstract IHttpActionResult CompleteInterview(CompleteInterviewRequest completeInterviewRequest);

        [ObserverNotAllowed]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used by HqApp @store.actions.js")]
        public IHttpActionResult SendNewComment(Guid interviewId, string questionIdentity, string comment)
        {
            var identity = Identity.Parse(questionIdentity);
            var command = new CommentAnswerCommand(interviewId, this.GetCommandResponsibleId(interviewId), identity.Id, identity.RosterVector, comment);

            this.commandService.Execute(command);
            return Ok();
        }

        [ObserverNotAllowed]
        private void ExecuteQuestionCommand(QuestionCommand command)
        {
            try
            {
                InScopeExecutor.Current.Execute(sl =>
                {
                    sl.GetInstance<ICommandService>().Execute(command);
                });
            }
            catch (Exception e)
            {
                webInterviewNotificationService.MarkAnswerAsNotSaved(command.InterviewId, command.Question, e);
            }
        }
    }
}
