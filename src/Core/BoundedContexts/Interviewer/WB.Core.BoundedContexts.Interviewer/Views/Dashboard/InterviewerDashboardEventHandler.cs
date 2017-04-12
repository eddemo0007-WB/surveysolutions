using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Utils;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;

namespace WB.Core.BoundedContexts.Interviewer.Views.Dashboard
{
    public class InterviewerDashboardEventHandler : BaseDenormalizer,
                                         ILitePublishedEventHandler<SynchronizationMetadataApplied>,
                                         ILitePublishedEventHandler<InterviewSynchronized>,
                                         ILitePublishedEventHandler<InterviewStatusChanged>,
                                         ILitePublishedEventHandler<InterviewHardDeleted>,

                                         ILitePublishedEventHandler<TextQuestionAnswered>,
                                         ILitePublishedEventHandler<MultipleOptionsQuestionAnswered>,
                                         ILitePublishedEventHandler<SingleOptionQuestionAnswered>,
                                         ILitePublishedEventHandler<NumericRealQuestionAnswered>,
                                         ILitePublishedEventHandler<NumericIntegerQuestionAnswered>,
                                         ILitePublishedEventHandler<DateTimeQuestionAnswered>,
                                         ILitePublishedEventHandler<GeoLocationQuestionAnswered>,
                                         ILitePublishedEventHandler<QRBarcodeQuestionAnswered>,
                                         ILitePublishedEventHandler<YesNoQuestionAnswered>,
                                         ILitePublishedEventHandler<TextListQuestionAnswered>,
                                         ILitePublishedEventHandler<PictureQuestionAnswered>,

                                         ILitePublishedEventHandler<AnswersRemoved>,

                                         ILitePublishedEventHandler<InterviewOnClientCreated>,
                                         ILitePublishedEventHandler<AnswerRemoved>,

                                         ILitePublishedEventHandler<TranslationSwitched>,
                                         ILitePublishedEventHandler<MultipleOptionsLinkedQuestionAnswered>,
                                         ILitePublishedEventHandler<SingleOptionLinkedQuestionAnswered>,
                                         ILitePublishedEventHandler<AreaQuestionAnswered>
    {
        private readonly IPlainStorage<InterviewView> interviewViewRepository;
        private readonly IPlainStorage<PrefilledQuestionView> prefilledQuestions;
        private readonly IQuestionnaireStorage questionnaireRepository;

        public InterviewerDashboardEventHandler(IPlainStorage<InterviewView> interviewViewRepository, 
            IPlainStorage<PrefilledQuestionView> prefilledQuestions,
            IQuestionnaireStorage questionnaireRepository,
            ILiteEventRegistry liteEventRegistry)
        {
            this.interviewViewRepository = interviewViewRepository;
            this.prefilledQuestions = prefilledQuestions;
            this.questionnaireRepository = questionnaireRepository;

            liteEventRegistry.Subscribe(this);
        }

        public override object[] Writers => new object[] { this.interviewViewRepository };
        public override object[] Readers => new object[] { this.interviewViewRepository};

        public void Handle(IPublishedEvent<SynchronizationMetadataApplied> evnt)
        {
            this.AddOrUpdateInterviewToDashboard(evnt.Payload.QuestionnaireId,
                evnt.Payload.QuestionnaireVersion,
                evnt.EventSourceId,
                evnt.Payload.UserId,
                evnt.Payload.Status,
                evnt.Payload.Comments,
                evnt.Payload.FeaturedQuestionsMeta,
                evnt.Payload.CreatedOnClient,
                false,
                evnt.Payload.InterviewerAssignedDateTime,
                null,
                evnt.Payload.RejectedDateTime);
        }


        public void Handle(IPublishedEvent<InterviewOnClientCreated> evnt)
        {
            this.AddOrUpdateInterviewToDashboard(evnt.Payload.QuestionnaireId,
                evnt.Payload.QuestionnaireVersion,
                evnt.EventSourceId,
                evnt.Payload.UserId,
                InterviewStatus.InterviewerAssigned,
                null,
                new AnsweredQuestionSynchronizationDto[0],
                true,
                true,
                evnt.EventTimeStamp,
                evnt.EventTimeStamp,
                null);
        }


        private void AddOrUpdateInterviewToDashboard(
            Guid questionnaireId, long questionnaireVersion, Guid interviewId, Guid responsibleId, InterviewStatus status,
            string comments, IEnumerable<AnsweredQuestionSynchronizationDto> answeredQuestions,
            bool createdOnClient, bool canBeDeleted,
            DateTime? assignedDateTime, DateTime? startedDateTime, DateTime? rejectedDateTime)
        {
            var questionnaireIdentity = new QuestionnaireIdentity(questionnaireId, questionnaireVersion);
            var questionnaireDocumentView = this.questionnaireRepository.GetQuestionnaireDocument(questionnaireIdentity);
            
            if (questionnaireDocumentView == null)
                return;

            var storageInterviewId = interviewId.FormatGuid();
            var interviewView = this.interviewViewRepository.GetById(storageInterviewId) ?? new InterviewView
            {
                Id = storageInterviewId,
                InterviewId = interviewId,
                ResponsibleId = responsibleId,
                QuestionnaireId = questionnaireIdentity.ToString(),
                Census = createdOnClient,
            };

            var questionnaire = this.questionnaireRepository.GetQuestionnaire(questionnaireIdentity, interviewView.Language);

            var prefilledQuestions = new List<PrefilledQuestionView>();
            var featuredQuestions = questionnaireDocumentView.Children
                                                             .TreeToEnumerableDepthFirst(x => x.Children)
                                                             .OfType<IQuestion>()
                                                             .Where(q => q.Featured).ToList();

            InterviewGpsCoordinatesView gpsCoordinates = null;
            Guid? prefilledGpsQuestionId = null;

            int prefilledQuestionSortIndex = 0;
            foreach (var featuredQuestion in featuredQuestions)
            {
                var item = answeredQuestions.FirstOrDefault(q => q.Id == featuredQuestion.PublicKey);

                if (featuredQuestion.QuestionType != QuestionType.GpsCoordinates)
                {
                    var answerOnPrefilledQuestion = this.GetAnswerOnPrefilledQuestion(featuredQuestion.PublicKey, questionnaire, item?.Answer, interviewId);
                    answerOnPrefilledQuestion.SortIndex = prefilledQuestionSortIndex;
                    prefilledQuestionSortIndex++;
                    prefilledQuestions.Add(answerOnPrefilledQuestion);
                }
                else
                {
                    prefilledGpsQuestionId = featuredQuestion.PublicKey;

                    var answerOnPrefilledGeolocationQuestion = GetGeoPositionAnswer(item);
                    if (answerOnPrefilledGeolocationQuestion != null)
                    {
                        gpsCoordinates = new InterviewGpsCoordinatesView
                        {
                            Latitude = answerOnPrefilledGeolocationQuestion.Latitude,
                            Longitude = answerOnPrefilledGeolocationQuestion.Longitude
                        };
                    }
                }
            }

            interviewView.Status = status;

            var existingPrefilledForInterview = this.prefilledQuestions.Where(x => x.InterviewId == interviewId).ToList();
            this.prefilledQuestions.Remove(existingPrefilledForInterview);
            this.prefilledQuestions.Store(prefilledQuestions);

            interviewView.StartedDateTime = startedDateTime;
            interviewView.InterviewerAssignedDateTime = assignedDateTime;
            interviewView.RejectedDateTime = rejectedDateTime;
            interviewView.CanBeDeleted = canBeDeleted;
            interviewView.LastInterviewerOrSupervisorComment = comments;
            interviewView.LocationQuestionId = prefilledGpsQuestionId;

            if (gpsCoordinates != null)
            {
                interviewView.LocationLatitude = gpsCoordinates.Latitude;
                interviewView.LocationLongitude = gpsCoordinates.Longitude;
            }

            this.interviewViewRepository.Store(interviewView);
        }

        private static GeoPosition GetGeoPositionAnswer(AnsweredQuestionSynchronizationDto item)
        {
            if (item == null)
                return null;

            var geoPositionAnswer = item.Answer as GeoPosition;
            if (geoPositionAnswer != null)
                return geoPositionAnswer;

            var geoPositionString = item.Answer as string;
            if (geoPositionString != null)
                return GeoPosition.FromString(geoPositionString);

            return null;
        }

        private PrefilledQuestionView GetAnswerOnPrefilledQuestion(Guid prefilledQuestion, IQuestionnaire questionnaire, object answer, Guid interviewId)
        {
            Func<decimal, string> getCategoricalOptionText = null;

            var questionType = questionnaire.GetQuestionType(prefilledQuestion);

            if (answer != null)
            {
                switch (questionType)
                {
                    case QuestionType.DateTime:
                        DateTime dateTimeAnswer;
                        if (answer is string)
                            dateTimeAnswer = DateTime.Parse((string) answer);
                        else
                            dateTimeAnswer = (DateTime) answer;

                        var isTimestamp = questionnaire.IsTimestampQuestion(prefilledQuestion);
                        var localTime = dateTimeAnswer.ToLocalTime();
                        answer = isTimestamp 
                            ? localTime.ToString(CultureInfo.CurrentCulture)
                            : localTime.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                        break;
                    case QuestionType.MultyOption:
                    case QuestionType.SingleOption:
                        if (answer.GetType().IsArray)
                        {
                            answer =
                                (answer as object[]).Select(x => Convert.ToDecimal(x, CultureInfo.InvariantCulture))
                                    .ToArray();
                        }
                        else
                        {
                            answer = Convert.ToDecimal(answer, CultureInfo.InvariantCulture);
                        }
                        getCategoricalOptionText = (option) => questionnaire.GetOptionForQuestionByOptionValue(prefilledQuestion, option).Title;
                        break;
                    case QuestionType.Numeric:
                        decimal answerTyped = answer is string ? decimal.Parse((string)answer, CultureInfo.InvariantCulture) : Convert.ToDecimal(answer);
                        if (questionnaire.ShouldUseFormatting(prefilledQuestion))
                        {
                            answer = answerTyped.FormatDecimal();
                        }
                        else
                        {
                            answer = answerTyped.ToString(CultureInfo.CurrentCulture);
                        }
                        break;
                }
            }

            return new PrefilledQuestionView
            {
                Id = $"{interviewId:N}${prefilledQuestion:N}",
                InterviewId = interviewId,
                QuestionId = prefilledQuestion,
                QuestionText = questionnaire.GetQuestionTitle(prefilledQuestion),
                Answer = answer == null ? null : AnswerUtils.AnswerToString(answer, getCategoricalOptionText)
            };
        }

        public void Handle(IPublishedEvent<InterviewSynchronized> evnt)
        {
            this.AddOrUpdateInterviewToDashboard(evnt.Payload.InterviewData.QuestionnaireId,
                evnt.Payload.InterviewData.QuestionnaireVersion,
                evnt.EventSourceId,
                evnt.Payload.UserId,
                evnt.Payload.InterviewData.Status,
                evnt.Payload.InterviewData.Comments,
                evnt.Payload.InterviewData.Answers,
                evnt.Payload.InterviewData.CreatedOnClient,
                canBeDeleted: false,
                assignedDateTime: evnt.Payload.InterviewData.InterviewerAssignedDateTime,
                startedDateTime: null,
                rejectedDateTime: evnt.Payload.InterviewData.RejectDateTime);
        }

        public void Handle(IPublishedEvent<InterviewHardDeleted> evnt)
        {
            this.interviewViewRepository.Remove(evnt.EventSourceId.FormatGuid());
        }

        public void Handle(IPublishedEvent<InterviewStatusChanged> evnt)
        {
            if(!this.IsInterviewCompletedOrRestarted(evnt.Payload.Status))
                return;

            InterviewView interviewView = this.interviewViewRepository.GetById(evnt.EventSourceId.FormatGuid());
            if (interviewView == null)
                return;

            if (evnt.Payload.Status == InterviewStatus.Completed)
                interviewView.CompletedDateTime = evnt.EventTimeStamp;

            if (evnt.Payload.Status == InterviewStatus.RejectedBySupervisor)
                interviewView.RejectedDateTime = evnt.EventTimeStamp;

            interviewView.Status = evnt.Payload.Status;
            interviewView.LastInterviewerOrSupervisorComment = evnt.Payload.Comment;

            this.interviewViewRepository.Store(interviewView);
        }

        private bool IsInterviewCompletedOrRestarted(InterviewStatus status)
        {
            return status == InterviewStatus.Completed || status == InterviewStatus.Restarted;
        }

        private void AnswerQuestion(Guid interviewId, Guid questionId, object answer, DateTime answerTimeUtc)
        {
            this.AnswerOnPrefilledQuestion(interviewId, questionId, answer);
            this.SetStartedDateTimeOnFirstAnswer(interviewId, questionId, answerTimeUtc);
        }

        private readonly HashSet<Guid> interviewsWithExistedStartedDateTime = new HashSet<Guid>();

        private void SetStartedDateTimeOnFirstAnswer(Guid interviewId, Guid questionId, DateTime answerTimeUtc)
        {
            if (this.interviewsWithExistedStartedDateTime.Contains(interviewId))
                return;

            var interviewView = this.interviewViewRepository.GetById(interviewId.FormatGuid());
            if (interviewView == null) return;

            var questionnaireIdentity = QuestionnaireIdentity.Parse(interviewView.QuestionnaireId);
            var questionnaire = this.questionnaireRepository.GetQuestionnaire(questionnaireIdentity, interviewView.Language);
            if (questionnaire.IsPrefilled(questionId))
                return;

            var questionScope = questionnaire.GetQuestionScope(questionId);
            if (questionScope != QuestionScope.Interviewer)
                return;

            this.interviewsWithExistedStartedDateTime.Add(interviewId);

            if (!interviewView.StartedDateTime.HasValue)
            {
                interviewView.StartedDateTime = answerTimeUtc;
            }

            this.interviewViewRepository.Store(interviewView);
        }

        private void AnswerOnPrefilledQuestion(Guid interviewId, Guid questionId, object answer)
        {
            var interviewView = this.interviewViewRepository.GetById(interviewId.FormatGuid());
            if (interviewView == null) return;

            var questionnaireIdentity = QuestionnaireIdentity.Parse(interviewView.QuestionnaireId);

            var questionnaire = this.questionnaireRepository.GetQuestionnaire(questionnaireIdentity, interviewView.Language);
            if (!questionnaire.IsPrefilled(questionId))
                return;

            if (questionId == interviewView.LocationQuestionId)
            {
                var gpsCoordinates = (GeoPosition) answer;

                if (gpsCoordinates == null)
                {
                    interviewView.LocationLongitude = interviewView.LocationLatitude = null;
                }
                else
                {
                    interviewView.LocationLongitude = gpsCoordinates.Longitude;
                    interviewView.LocationLatitude = gpsCoordinates.Latitude;
                }
            }
            else
            {
                var newPrefilledQuestionToStore = this.GetAnswerOnPrefilledQuestion(questionId, questionnaire, answer, interviewId);

                var interviewPrefilledQuestion = this.prefilledQuestions.Where(question => question.QuestionId == questionId && question.InterviewId == interviewId).FirstOrDefault()
                       ?? newPrefilledQuestionToStore;
                if (interviewPrefilledQuestion != null)
                {
                    interviewPrefilledQuestion.Answer = newPrefilledQuestionToStore.Answer;
                }

                this.prefilledQuestions.Store(interviewPrefilledQuestion);
            }

            this.interviewViewRepository.Store(interviewView);
        }

        public void Handle(IPublishedEvent<TextQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.Answer, evnt.Payload.AnswerTimeUtc);
        }
        
        public void Handle(IPublishedEvent<MultipleOptionsQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.SelectedValues, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<SingleOptionQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.SelectedValue, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<NumericRealQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.Answer, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<NumericIntegerQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.Answer, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<DateTimeQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.Answer, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<YesNoQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.AnsweredOptions, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<GeoLocationQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId,
                new GeoPosition(latitude: evnt.Payload.Latitude, longitude: evnt.Payload.Longitude,
                    accuracy: evnt.Payload.Accuracy, altitude:evnt.Payload.Altitude,
                    timestamp: evnt.Payload.Timestamp), evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<QRBarcodeQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.Answer, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<TextListQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.Answers, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<PictureQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.PictureFileName, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<MultipleOptionsLinkedQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.SelectedRosterVectors, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<SingleOptionLinkedQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.SelectedRosterVector, evnt.Payload.AnswerTimeUtc);
        }

        public void Handle(IPublishedEvent<AnswersRemoved> evnt)
        {
            foreach (var question in evnt.Payload.Questions)
            {
                this.AnswerQuestion(evnt.EventSourceId, question.Id, null, evnt.EventTimeStamp);
            }
        }

        public void Handle(IPublishedEvent<AnswerRemoved> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, null, evnt.EventTimeStamp);
        }

        public void Handle(IPublishedEvent<TranslationSwitched> @event)
        {
            var interviewView = this.interviewViewRepository.GetById(@event.EventSourceId.FormatGuid());
            if (interviewView == null) return;

            interviewView.Language = @event.Payload.Language;

            this.interviewViewRepository.Store(interviewView);
        }

        public void Handle(IPublishedEvent<AreaQuestionAnswered> evnt)
        {
            this.AnswerQuestion(evnt.EventSourceId, evnt.Payload.QuestionId, evnt.Payload.Answer, evnt.Payload.AnswerTimeUtc);
        }
    }
}