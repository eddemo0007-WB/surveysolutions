﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Microsoft.AspNet.Identity;
using Ncqrs.Eventing.ServiceModel.Bus;
using Nito.AsyncEx;
using Nito.AsyncEx.Synchronous;
using WB.Core.BoundedContexts.Headquarters.Authentication;
using WB.Core.BoundedContexts.Headquarters.Authentication.Models;
using WB.Core.BoundedContexts.Headquarters.Interview.Views;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.Infrastructure.FunctionalDenormalization.EventHandlers;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.ReadSide;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;

namespace WB.Core.BoundedContexts.Headquarters.Interview.EventHandlers
{
    internal class InterviewSummaryEventHandlerFunctional : AbstractFunctionalEventHandler<InterviewSummary>, 
        ICreateHandler<InterviewSummary, InterviewCreated>,
        IUpdateHandler<InterviewSummary, InterviewStatusChanged>,
        IUpdateHandler<InterviewSummary, SupervisorAssigned>,
        IUpdateHandler<InterviewSummary, TextQuestionAnswered>,
        IUpdateHandler<InterviewSummary, MultipleOptionsQuestionAnswered>,
        IUpdateHandler<InterviewSummary, SingleOptionQuestionAnswered>,
        IUpdateHandler<InterviewSummary, NumericRealQuestionAnswered>,
        IUpdateHandler<InterviewSummary, NumericQuestionAnswered>,
        IUpdateHandler<InterviewSummary, NumericIntegerQuestionAnswered>,
        IUpdateHandler<InterviewSummary, DateTimeQuestionAnswered>,
        IUpdateHandler<InterviewSummary, GeoLocationQuestionAnswered>,
        IUpdateHandler<InterviewSummary, QRBarcodeQuestionAnswered>,
        IUpdateHandler<InterviewSummary, AnswerRemoved>,
        IUpdateHandler<InterviewSummary, AnswersRemoved>,
        IUpdateHandler<InterviewSummary, InterviewerAssigned>,
        IUpdateHandler<InterviewSummary, InterviewDeleted>,
        IUpdateHandler<InterviewSummary, InterviewRestored>,
        IUpdateHandler<InterviewSummary, InterviewDeclaredInvalid>,
        IUpdateHandler<InterviewSummary, InterviewDeclaredValid>,
        ICreateHandler<InterviewSummary, InterviewOnClientCreated>
    {
        private readonly IVersionedReadSideRepositoryWriter<QuestionnaireDocumentVersioned> questionnaires;
        private readonly IReadSideRepositoryWriter<UserDocument> users;
        private readonly UserManager<ApplicationUser> userManager;

        public override Type[] UsesViews
        {
            get { return new Type[] { typeof(UserDocument), typeof(QuestionnaireBrowseItem) }; }
        }

        public InterviewSummaryEventHandlerFunctional(IReadSideRepositoryWriter<InterviewSummary> interviewSummary,
            IVersionedReadSideRepositoryWriter<QuestionnaireDocumentVersioned> questionnaires, 
            IReadSideRepositoryWriter<UserDocument> users,
            UserManager<ApplicationUser> userManager)
            : base(interviewSummary)
        {
            this.questionnaires = questionnaires;
            this.users = users;
            this.userManager = userManager;
        }


        private InterviewSummary UpdateInterviewSummary(InterviewSummary interviewSummary, DateTime updateDateTime, Action<InterviewSummary> update)
        {
            update(interviewSummary);
            interviewSummary.UpdateDate = updateDateTime;
            return interviewSummary;
        }

        private InterviewSummary AnswerQuestion(InterviewSummary interviewSummary, Guid questionId, string answer, DateTime updateDate)
        {
           return this.UpdateInterviewSummary(interviewSummary, updateDate, interview =>
            {
                if (interview.AnswersToFeaturedQuestions.ContainsKey(questionId))
                {
                    interview.AnswersToFeaturedQuestions[questionId].Answer = answer;
                }
            });
        }

        private InterviewSummary AnswerFeaturedQuestionWithOptions(InterviewSummary interviewSummary, Guid questionId, DateTime updateDate,
            params decimal[] answers)
        {
            return this.UpdateInterviewSummary(interviewSummary, updateDate, interview =>
            {
                if (interview.AnswersToFeaturedQuestions.ContainsKey(questionId))
                {
                    var featuredQuestion = interview.AnswersToFeaturedQuestions[questionId] as QuestionAnswerWithOptions;
                    if (featuredQuestion == null)
                        return;

                    featuredQuestion.SetAnswerAsAnswerValues(answers);
                }
            });
        }

        private InterviewSummary CreateInterviewSummary(Guid userId, Guid questionnaireId, long questionnaireVersion,
            Guid eventSourceId, DateTime eventTimeStamp)
        {
            ApplicationUser responsible = userManager.FindByUserIdAsync(userId);

            var questionnarie = this.questionnaires.GetById(questionnaireId,
                questionnaireVersion);
            return
                new InterviewSummary(questionnarie.Questionnaire)
                {
                    InterviewId = eventSourceId,
                    UpdateDate = eventTimeStamp,
                    QuestionnaireId = questionnaireId,
                    QuestionnaireVersion = questionnaireVersion,
                    QuestionnaireTitle = questionnarie.Questionnaire.Title,
                    ResponsibleId = userId, // Creator is responsible
                    ResponsibleName = responsible.UserName,
                    ResponsibleRole = UserRoles.Headquarter
                };
        }

        public InterviewSummary Create(IPublishedEvent<InterviewCreated> evnt)
        {
            return this.CreateInterviewSummary(evnt.Payload.UserId, evnt.Payload.QuestionnaireId,
                evnt.Payload.QuestionnaireVersion, evnt.EventSourceId, evnt.EventTimeStamp);
        }

        public InterviewSummary Create(IPublishedEvent<InterviewOnClientCreated> evnt)
        {
            return this.CreateInterviewSummary(evnt.Payload.UserId, evnt.Payload.QuestionnaireId,
             evnt.Payload.QuestionnaireVersion, evnt.EventSourceId, evnt.EventTimeStamp);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<InterviewStatusChanged> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                interview.Status = evnt.Payload.Status;

                interview.CommentedStatusesHistory.Add(new InterviewCommentedStatus
                {
                    Status = interview.Status,
                    Date = evnt.EventTimeStamp,
                    Comment = evnt.Payload.Comment
                });
            });
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<SupervisorAssigned> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                var supervisorName = this.users.GetById(evnt.Payload.SupervisorId).UserName;

                interview.ResponsibleId = evnt.Payload.SupervisorId;
                interview.ResponsibleName = supervisorName;
                interview.ResponsibleRole = UserRoles.Supervisor;
                interview.TeamLeadId = evnt.Payload.SupervisorId;
                interview.TeamLeadName = supervisorName;
            });
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<TextQuestionAnswered> evnt)
        {
            return this.AnswerQuestion(currentState, evnt.Payload.QuestionId, evnt.Payload.Answer, evnt.EventTimeStamp);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<MultipleOptionsQuestionAnswered> evnt)
        {
            return this.AnswerFeaturedQuestionWithOptions(currentState, evnt.Payload.QuestionId, evnt.EventTimeStamp, evnt.Payload.SelectedValues);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<SingleOptionQuestionAnswered> evnt)
        {
            return this.AnswerFeaturedQuestionWithOptions(currentState, evnt.Payload.QuestionId, evnt.EventTimeStamp,evnt.Payload.SelectedValue);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<NumericRealQuestionAnswered> evnt)
        {
            return this.AnswerQuestion(currentState, evnt.Payload.QuestionId, evnt.Payload.Answer.ToString(CultureInfo.InvariantCulture), evnt.EventTimeStamp);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<NumericQuestionAnswered> evnt)
        {
            return this.AnswerQuestion(currentState, evnt.Payload.QuestionId, evnt.Payload.Answer.ToString(CultureInfo.InvariantCulture), evnt.EventTimeStamp);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<NumericIntegerQuestionAnswered> evnt)
        {
            return this.AnswerQuestion(currentState, evnt.Payload.QuestionId, evnt.Payload.Answer.ToString(CultureInfo.InvariantCulture), evnt.EventTimeStamp);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<DateTimeQuestionAnswered> evnt)
        {
            return this.AnswerQuestion(currentState, evnt.Payload.QuestionId, evnt.Payload.Answer.ToString("d", CultureInfo.InvariantCulture), evnt.EventTimeStamp);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<GeoLocationQuestionAnswered> evnt)
        {
            return this.AnswerQuestion(currentState, evnt.Payload.QuestionId,
                            string.Format("{0},{1}[{2}]", evnt.Payload.Latitude, evnt.Payload.Longitude, evnt.Payload.Accuracy), evnt.EventTimeStamp);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<QRBarcodeQuestionAnswered> evnt)
        {
            return this.AnswerQuestion(currentState, evnt.Payload.QuestionId, evnt.Payload.Answer, evnt.EventTimeStamp);
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<AnswerRemoved> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                if (interview.AnswersToFeaturedQuestions.ContainsKey(evnt.Payload.QuestionId))
                {
                    interview.AnswersToFeaturedQuestions[evnt.Payload.QuestionId].Answer = string.Empty;
                }
            });
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<AnswersRemoved> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                foreach (var question in evnt.Payload.Questions)
                {
                    if (interview.AnswersToFeaturedQuestions.ContainsKey(question.Id))
                    {
                        interview.AnswersToFeaturedQuestions[question.Id].Answer = string.Empty;
                    }
                }
            });
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<InterviewerAssigned> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                var interviewerName = this.users.GetById(evnt.Payload.InterviewerId).UserName;

                interview.ResponsibleId = evnt.Payload.InterviewerId;
                interview.ResponsibleName = interviewerName;
                interview.ResponsibleRole = UserRoles.Operator;
            });
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<InterviewDeleted> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                interview.IsDeleted = true;
            });
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<InterviewRestored> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                interview.IsDeleted = false;
            });
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<InterviewDeclaredInvalid> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                interview.HasErrors = true;
            });
        }

        public InterviewSummary Update(InterviewSummary currentState, IPublishedEvent<InterviewDeclaredValid> evnt)
        {
            return this.UpdateInterviewSummary(currentState, evnt.EventTimeStamp, interview =>
            {
                interview.HasErrors = false;
            });
        }
    }
}
