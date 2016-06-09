﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.Core.ViewModels;
using WB.Core.BoundedContexts.Interviewer.Properties;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.ViewModels;

namespace WB.Core.BoundedContexts.Interviewer.Views
{
    public class LoadingViewModel : BaseViewModel, IDisposable
    {
        protected Guid interviewId;
        private readonly IStatefulInterviewRepository interviewRepository;
        private readonly IViewModelNavigationService viewModelNavigationService;
        private readonly ICommandService commandService;
        private readonly IPrincipal principal;
        private CancellationTokenSource loadingCancellationTokenSource;

        public LoadingViewModel(IPrincipal principal,
            IViewModelNavigationService viewModelNavigationService,
            IStatefulInterviewRepository interviewRepository, ICommandService commandService) : base(principal, viewModelNavigationService)
        {
            this.interviewRepository = interviewRepository;
            this.commandService = commandService;
            this.principal = principal;
            this.viewModelNavigationService = viewModelNavigationService;
        }

        public IMvxCommand CancelLoadingCommand => new MvxCommand(this.CancelLoading);

        public void Dispose()
        {
        }

        public void Init(Guid interviewId)
        {
            if (interviewId == null) throw new ArgumentNullException(nameof(interviewId));
            this.interviewId = interviewId;
        }

        public async Task RestoreInterviewAndNavigateThere()
        {
            this.loadingCancellationTokenSource = new CancellationTokenSource();
            var interviewIdString = this.interviewId.FormatGuid();

            var progress = new Progress<int>();
            progress.ProgressChanged += Progress_ProgressChanged;
            this.IsInProgress = true;
            this.ProgressInPercents =InterviewerUIResources.Interview_Loading;
            try
            {
                this.loadingCancellationTokenSource.Token.ThrowIfCancellationRequested();

                IStatefulInterview interview =
                    await
                        this.interviewRepository.GetAsync(interviewIdString, progress, this.loadingCancellationTokenSource.Token);

                if (interview.Status==InterviewStatus.Completed)
                {
                    this.loadingCancellationTokenSource.Token.ThrowIfCancellationRequested();
                    var restartInterviewCommand = new RestartInterviewCommand(this.interviewId, this.principal.CurrentUserIdentity.UserId, "", DateTime.UtcNow);
                    await this.commandService.ExecuteAsync(restartInterviewCommand);
                }

                this.loadingCancellationTokenSource.Token.ThrowIfCancellationRequested();

                if (interview.CreatedOnClient)
                {
                    await this.viewModelNavigationService.NavigateToPrefilledQuestionsAsync(interviewIdString);
                }
                else
                {
                    await this.viewModelNavigationService.NavigateToInterviewAsync(interviewIdString);
                }
            }
            catch (OperationCanceledException)
            {

            }
            progress.ProgressChanged -= Progress_ProgressChanged;
            this.IsInProgress = false;
        }
        private string percentage;
        public string ProgressInPercents
        {
            get { return this.percentage; }
            set { this.RaiseAndSetIfChanged(ref this.percentage, value); }
        }

        private bool isInProgress;
        public bool IsInProgress
        {
            get { return this.isInProgress; }
            set { this.isInProgress = value; this.RaisePropertyChanged(); }
        }

        private void Progress_ProgressChanged(object sender, int e)
        {
            this.ProgressInPercents = string.Format(InterviewerUIResources.Interview_Loading_With_Percents, e);
        }

        public void CancelLoading()
        {
            if (this.loadingCancellationTokenSource != null && !this.loadingCancellationTokenSource.IsCancellationRequested)
                this.loadingCancellationTokenSource.Cancel();
        }

        public IMvxAsyncCommand NavigateToDashboardCommand => new MvxAsyncCommand(this.viewModelNavigationService.NavigateToDashboardAsync);

        public IMvxAsyncCommand SignOutCommand => new MvxAsyncCommand(this.SignOutAsync);

        private async Task SignOutAsync()
        {
            await this.principal.SignOutAsync();
            await this.viewModelNavigationService.NavigateToLoginAsync();
        }
    }
}