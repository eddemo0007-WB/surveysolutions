using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;

namespace WB.Core.BoundedContexts.Capi.Views.InterviewDetails
{
    public class QuestionnaireScreenViewModel : Cirrious.MvvmCross.ViewModels.MvxViewModel, IQuestionnaireViewModel
    {
        protected QuestionnaireScreenViewModel(Guid questionnaireId, string screenName, string title, bool enabled,
                                            InterviewItemId screenId, IList<IQuestionnaireItemViewModel> items,
                                            IEnumerable<InterviewItemId> breadcrumbs)
        {

            this.QuestionnaireId = questionnaireId;
            this.Items = items;
            this.ScreenId = screenId;
            this.Breadcrumbs = (breadcrumbs ?? new List<InterviewItemId>()).Union(new InterviewItemId[1] { this.ScreenId }).ToList();
            this.Title = title;
            this.Enabled = enabled;
            this.ScreenName = screenName;

            foreach (var item in this.Items)
            {
                item.PropertyChanged += item_PropertyChanged;
            }
        }

        [JsonConstructor]
        public QuestionnaireScreenViewModel(Guid questionnaireId, string screenName, string title, bool enabled,
                                          InterviewItemId screenId, IList<IQuestionnaireItemViewModel> items,
                                          IEnumerable<InterviewItemId> siblings,
                                          IEnumerable<InterviewItemId> breadcrumbs)
            : this(questionnaireId, screenName, title, enabled, screenId, items, breadcrumbs)
        {
            this.Siblings = siblings;
        }

        public Guid QuestionnaireId { get; private set; }
        public InterviewItemId ScreenId { get; private set; }
        public string Title { get; private set; }
        public string ScreenName { get; protected set; }
        public bool Enabled { get; private set; }
        public IList<IQuestionnaireItemViewModel> Items { get; private set; }
        public virtual IEnumerable<InterviewItemId> Siblings { get; private set; }
        public IEnumerable<InterviewItemId> Breadcrumbs { get; protected set; }

        public int Answered
        {
            get
            {
                if (!this.Enabled)
                    return 0;
                if (!this.answered.HasValue)
                    this.UpdateCounters();
                return answered ?? 0;
            }
            private set { this.answered = value; }
        }

        public int Total
        {
            get
            {
                if (!this.Enabled)
                    return 0;
                if (!this.total.HasValue)
                    this.UpdateCounters();
                return total ?? 0;
            }
            private set { this.total = value; }
        }

        private int? answered;
        private int? total;

        protected void UpdateCounters()
        {
            var newTotal = 0;
            var newAnswered = 0;
            foreach (var item in this.Items)
            {
                var question = item as QuestionViewModel;
                if (question != null)
                {
                    if (question.IsEnabled())
                    {
                        newTotal++;
                        if (question.Status.HasFlag(QuestionStatus.Answered))
                            newAnswered++;
                    }
                    continue;
                }
                var group = item as QuestionnaireNavigationPanelItem;
                if (group != null)
                {
                    newTotal = newTotal + group.Total;
                    newAnswered = newAnswered + group.Answered;
                }

            }
            if (newTotal != this.total)
            {
                this.total = newTotal;
                this.RaisePropertyChanged("Total");
            }
            if (newAnswered != this.answered)
            {
                answered = newAnswered;
                this.RaisePropertyChanged("Answered");
            }
        }

        public void SetEnabled(bool enabled)
        {
            if (this.Enabled == enabled)
                return;
            this.Enabled = enabled;
            this.RaisePropertyChanged("Enabled");
        }

        protected void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var question = sender as QuestionViewModel;
            if (question != null)
            {
                if (e.PropertyName != "Status")
                    return;
                this.UpdateCounters();
            }
            var group = sender as QuestionnaireNavigationPanelItem;
            if (group != null)
            {
                if (e.PropertyName != "Answered" && e.PropertyName != "Total")
                    return;
                this.UpdateCounters();
            }
        }
    }
}