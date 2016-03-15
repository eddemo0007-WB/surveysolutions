using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MvvmCross.Platform.Core;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Entities.Interview;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Utils;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions
{
    public class SingleOptionLinkedQuestionViewModel : MvxNotifyPropertyChanged, 
        IInterviewEntityViewModel,
        ILiteEventHandler<AnswersRemoved>,
        ILiteEventHandler<AnswerRemoved>,
        ILiteEventHandler<LinkedOptionsChanged>,
        IDisposable
    {
        private readonly Guid userId;
        private readonly IPlainQuestionnaireRepository questionnaireRepository;
        private readonly IPlainQuestionnaireRepository questionnaireStorage;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IAnswerToStringService answerToStringService;
        private readonly ILiteEventRegistry eventRegistry;
        private readonly IMvxMainThreadDispatcher mainThreadDispatcher;

        public SingleOptionLinkedQuestionViewModel(
            IPrincipal principal,
            IPlainQuestionnaireRepository questionnaireStorage,
            IStatefulInterviewRepository interviewRepository,
            IAnswerToStringService answerToStringService,
            ILiteEventRegistry eventRegistry,
            IMvxMainThreadDispatcher mainThreadDispatcher,
            QuestionStateViewModel<SingleOptionLinkedQuestionAnswered> questionStateViewModel,
            AnsweringViewModel answering, 
            IPlainQuestionnaireRepository questionnaireRepository)
        {
            if (principal == null) throw new ArgumentNullException("principal");
            if (questionnaireStorage == null) throw new ArgumentNullException("questionnaireStorage");
            if (interviewRepository == null) throw new ArgumentNullException("interviewRepository");
            if (answerToStringService == null) throw new ArgumentNullException("answerToStringService");
            if (eventRegistry == null) throw new ArgumentNullException("eventRegistry");

            this.userId = principal.CurrentUserIdentity.UserId;
            this.questionnaireStorage = questionnaireStorage;
            this.interviewRepository = interviewRepository;
            this.answerToStringService = answerToStringService;
            this.eventRegistry = eventRegistry;
            this.mainThreadDispatcher = mainThreadDispatcher ?? MvxMainThreadDispatcher.Instance;

            this.QuestionState = questionStateViewModel;
            this.Answering = answering;
            this.questionnaireRepository = questionnaireRepository;
        }

        private Guid interviewId;
        private Guid referencedQuestionId;
        private ObservableCollection<SingleOptionLinkedQuestionOptionViewModel> options;

        public ObservableCollection<SingleOptionLinkedQuestionOptionViewModel> Options
        {
            get { return this.options; }
            private set { this.options = value; this.RaisePropertyChanged(() => this.HasOptions);}
        }

        public bool HasOptions
        {
            get { return this.Options.Any(); }
        }

        public QuestionStateViewModel<SingleOptionLinkedQuestionAnswered> QuestionState { get; private set; }
        public AnsweringViewModel Answering { get; private set; }

        public Identity Identity { get; private set; }

        public void Init(string interviewId, Identity questionIdentity, NavigationState navigationState)
        {
            if (interviewId == null) throw new ArgumentNullException(nameof(interviewId));
            if (questionIdentity == null) throw new ArgumentNullException(nameof(questionIdentity));

            this.QuestionState.Init(interviewId, questionIdentity, navigationState);

            var interview = this.interviewRepository.Get(interviewId);
            var questionnaire = this.questionnaireStorage.GetQuestionnaire(interview.QuestionnaireIdentity);

            this.Identity = questionIdentity;
            this.interviewId = interview.Id;

            this.referencedQuestionId = questionnaire.GetQuestionReferencedByLinkedQuestion(this.Identity.Id);

            var options = this.GenerateOptionsFromModel(interview, this.questionnaireRepository.GetQuestionnaire(interview.QuestionnaireIdentity));
            this.Options = new ObservableCollection<SingleOptionLinkedQuestionOptionViewModel>(options);

            this.eventRegistry.Subscribe(this, interviewId);
        }

        public void Dispose()
        {
            this.eventRegistry.Unsubscribe(this, interviewId.FormatGuid());
            this.QuestionState.Dispose();

            foreach (var option in Options)
            {
                option.BeforeSelected -= this.OptionSelected;
                option.AnswerRemoved -= this.RemoveAnswer;
            }
        }

        private List<SingleOptionLinkedQuestionOptionViewModel> GenerateOptionsFromModel(IStatefulInterview interview, IQuestionnaire questionnaire)
        {
            var linkedAnswerModel = interview.GetLinkedSingleOptionAnswer(this.Identity);

            IEnumerable<BaseInterviewAnswer> referencedQuestionAnswers =
                interview.FindAnswersOfReferencedQuestionForLinkedQuestion(this.referencedQuestionId, this.Identity);

            var options = referencedQuestionAnswers
                .Select(referencedAnswer => this.GenerateOptionViewModelOrNull(referencedAnswer, linkedAnswerModel, interview, questionnaire))
                .Where(optionOrNull => optionOrNull != null)
                .ToList();

            return options;
        }

        private async void OptionSelected(object sender, EventArgs eventArgs)
        {
            await this.OptionSelectedAsync(sender);
        }

        private async void RemoveAnswer(object sender, EventArgs e)
        {
            try
            {
                await this.Answering.SendRemoveAnswerCommandAsync(
                    new RemoveAnswerCommand(this.interviewId,
                        this.userId,
                        this.Identity,
                        DateTime.UtcNow));
                this.QuestionState.Validity.ExecutedWithoutExceptions();
            }
            catch (InterviewException exception)
            {
                this.QuestionState.Validity.ProcessException(exception);
            }
        }

        internal async Task OptionSelectedAsync(object sender)
        {
            var selectedOption = (SingleOptionLinkedQuestionOptionViewModel) sender;
            var previousOption = this.Options.SingleOrDefault(option => option.Selected && option != selectedOption);

            var command = new AnswerSingleOptionLinkedQuestionCommand(
                this.interviewId,
                this.userId,
                this.Identity.Id,
                this.Identity.RosterVector,
                DateTime.UtcNow,
                selectedOption.RosterVector);

            try
            {
                if (previousOption != null)
                {
                    previousOption.Selected = false;
                }

                await this.Answering.SendAnswerQuestionCommandAsync(command);

                this.QuestionState.Validity.ExecutedWithoutExceptions();
            }
            catch (InterviewException ex)
            {
                selectedOption.Selected = false;

                if (previousOption != null)
                {
                    previousOption.Selected = true;
                }

                this.QuestionState.Validity.ProcessException(ex);
            }
        }


        public void Handle(AnswersRemoved @event)
        {
            foreach (var question in @event.Questions)
            {
                if (this.Identity.Equals(question.Id, question.RosterVector))
                {
                    foreach (var option in this.Options.Where(option => option.Selected))
                    {
                        option.Selected = false;
                    }
                }
            }
        }

        public void Handle(AnswerRemoved @event)
        {
            if (this.Identity.Equals(@event.QuestionId, @event.RosterVector))
            {
                foreach (var option in this.Options.Where(option => option.Selected))
                {
                    option.Selected = false;
                }
            }
        }

        public void Handle(LinkedOptionsChanged @event)
        {
            ChangedLinkedOptions changedLinkedQuestion = @event.ChangedLinkedQuestions.SingleOrDefault(x => x.QuestionId == this.Identity);

            if (changedLinkedQuestion != null)
            {
                IStatefulInterview interview = this.interviewRepository.Get(this.interviewId.FormatGuid());
                IQuestionnaire questionnaire = this.questionnaireRepository.GetQuestionnaire(interview.QuestionnaireIdentity);

                this.mainThreadDispatcher.RequestMainThreadAction(() =>
                {
                    var newOptions = this.GenerateOptionsFromModel(interview, questionnaire);
                    var removedItems = this.Options.SynchronizeWith(newOptions, (s, t) => s.RosterVector.Identical(t.RosterVector));
                    removedItems.ForEach(option =>
                    {
                        option.BeforeSelected -= this.OptionSelected;
                        option.AnswerRemoved -= this.RemoveAnswer;
                    });

                    this.RaisePropertyChanged(() => HasOptions);
                });
            }
        }

        private static bool AreOptionsReferencingSameAnswer(SingleOptionLinkedQuestionOptionViewModel option1, SingleOptionLinkedQuestionOptionViewModel option2)
        {
            return option1.RosterVector.SequenceEqual(option2.RosterVector);
        }

        private SingleOptionLinkedQuestionOptionViewModel GenerateOptionViewModelOrNull(
            BaseInterviewAnswer referencedAnswer,
            LinkedSingleOptionAnswer linkedAnswerModel, 
            IStatefulInterview interview, 
            IQuestionnaire questionnaire)
        {
            if (referencedAnswer == null)
                return null;

            var title = this.GenerateOptionTitle(this.referencedQuestionId, referencedAnswer, interview, questionnaire);

            var isSelected =
                linkedAnswerModel != null &&
                linkedAnswerModel.IsAnswered &&
                linkedAnswerModel.Answer.SequenceEqual(referencedAnswer.RosterVector);

            if (!referencedAnswer.IsAnswered && !isSelected)
                return null;

            var optionViewModel = new SingleOptionLinkedQuestionOptionViewModel
            {
                Enablement = this.QuestionState.Enablement,

                RosterVector = referencedAnswer.RosterVector,
                Title = title,
                Selected = isSelected,
                QuestionState = this.QuestionState
            };

            optionViewModel.BeforeSelected += this.OptionSelected;
            optionViewModel.AnswerRemoved += this.RemoveAnswer;
            return optionViewModel;
        }

        private string GenerateOptionTitle(Guid referencedQuestionId, BaseInterviewAnswer referencedAnswer, IStatefulInterview interview, IQuestionnaire questionnaire)
        {
            string answerAsTitle = this.answerToStringService.AnswerToUIString(referencedQuestionId, referencedAnswer, interview, questionnaire);

            int currentRosterLevel = this.Identity.RosterVector.Length;

            IEnumerable<string> parentRosterTitlesWithoutLastOneAndFirstKnown =
                interview
                    .GetParentRosterTitlesWithoutLast(referencedAnswer.Id, referencedAnswer.RosterVector)
                    .Skip(currentRosterLevel);

            string rosterPrefixes = string.Join(": ", parentRosterTitlesWithoutLastOneAndFirstKnown);

            return string.IsNullOrEmpty(rosterPrefixes) ? answerAsTitle : string.Join(": ", rosterPrefixes, answerAsTitle);
        }
    }
}