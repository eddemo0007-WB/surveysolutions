using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WB.Core.BoundedContexts.Interviewer.Properties;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard.DashboardItems;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Groups;

namespace WB.Core.BoundedContexts.Interviewer.Views.Dashboard
{
    public class StartedInterviewsViewModel : ListViewModel<IDashboardItem>
    {
        public override GroupStatus InterviewStatus => GroupStatus.Started;

        private readonly IPlainStorage<InterviewView> interviewViewRepository;
        private readonly IInterviewViewModelFactory viewModelFactory;
        private readonly IPrincipal principal;

        public event EventHandler OnInterviewRemoved;

        public StartedInterviewsViewModel(
            IPlainStorage<InterviewView> interviewViewRepository,
            IInterviewViewModelFactory viewModelFactory,
            IPrincipal principal)
        {
            this.interviewViewRepository = interviewViewRepository;
            this.viewModelFactory = viewModelFactory;
            this.principal = principal;
        }

        public void Load()
        {
            this.Items = this.UiItems = new List<IDashboardItem>();

            Task.Run(() =>
            {
                var items = this.GetStartedInterviews().ToList();
                var subTitle = this.viewModelFactory.GetNew<DashboardSubTitleViewModel>();
                subTitle.Title = InterviewerUIResources.Dashboard_StartedTabText;
                var uiItems = subTitle.ToEnumerable().Concat(items).ToList();
                return Tuple.Create(items, uiItems);
            }).ContinueWith(task =>
            {
                this.Items = task.Result.Item1;
                this.UiItems = task.Result.Item2;

                this.Title = string.Format(InterviewerUIResources.Dashboard_StartedLinkText, this.Items.Count);
            }, TaskScheduler.FromCurrentSynchronizationContext());
            this.Title = string.Format(InterviewerUIResources.Dashboard_StartedLinkText, 0);
        }

        private IEnumerable<IDashboardItem> GetStartedInterviews()
        {
            var interviewerId = this.principal.CurrentUserIdentity.UserId;

            var interviewViews = this.interviewViewRepository.Where(interview =>
                interview.ResponsibleId == interviewerId &&
                (interview.Status == SharedKernels.DataCollection.ValueObjects.Interview.InterviewStatus.InterviewerAssigned ||
                interview.Status == SharedKernels.DataCollection.ValueObjects.Interview.InterviewStatus.Restarted));

            foreach (var interviewView in interviewViews)
            {
                var interviewDashboardItem = this.viewModelFactory.GetNew<InterviewDashboardItemViewModel>();
                interviewDashboardItem.Init(interviewView);
                interviewDashboardItem.OnItemRemoved += this.InterviewDashboardItem_OnItemRemoved;
                yield return interviewDashboardItem;
            }
        }

        private void InterviewDashboardItem_OnItemRemoved(object sender, System.EventArgs e)
        {
            this.Load();
            this.OnInterviewRemoved(sender, e);
        }
    }
}