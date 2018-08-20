﻿using System;
using System.Linq;
using Moq;
using MvvmCross.Plugin.Messenger;
using NSubstitute;
using NUnit.Framework;
using WB.Core.BoundedContexts.Interviewer.Services;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard.DashboardItems;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Tasks;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Core.SharedKernels.Enumerator.ViewModels.Dashboard;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.Tests.Abc;

namespace WB.Tests.Unit.BoundedContexts.Interviewer.ViewModels
{
    public class CreateNewModelTests
    {
        [Test]
        public void When_decreasing_count_of_created_interviews()
        {
            //arrange
            var localAssignmentsRepo = Create.Storage.AssignmentDocumentsInmemoryStorage();
            localAssignmentsRepo.Store(new[]
            {
                Create.Entity
                    .AssignmentDocument(4, 10, 0, Create.Entity.QuestionnaireIdentity(Id.gB).ToString(), Id.g1)
                    .WithAnswer(Create.Entity.Identity(Id.gA), "1")
                    .WithAnswer(Create.Entity.Identity(Id.gB), "2")
                    .Build(),
                Create.Entity
                    .AssignmentDocument(5, 10, 1, Create.Entity.QuestionnaireIdentity(Id.gB).ToString(), Id.g1, Id.g2)
                    .WithAnswer(Create.Entity.Identity(Id.gA), "1")
                    .WithAnswer(Create.Entity.Identity(Id.gB), "2")
                    .Build()
            });

            var mockOfSynchronizationViewModel = new Mock<LocalSynchronizationViewModel>(
                Mock.Of<IMvxMessenger>(), new SynchronizationCompleteSource());

            var viewFactory = CreateViewFactory();

            var model = CreateViewModel(assignmentsRepository: localAssignmentsRepo, viewModelFactory: viewFactory);
            model.Load(mockOfSynchronizationViewModel.Object);
            model.UpdateUiItems().WaitAndUnwrapException();

            //act
            model.UpdateAssignment(5);

            //assert
            var assignment = model.UiItems.OfType<InterviewerAssignmentDashboardItemViewModel>()
                .FirstOrDefault(x => x.AssignmentId == 5);

            Assert.That(assignment.InterviewsLeftByAssignmentCount, Is.EqualTo(10));
            Assert.That(localAssignmentsRepo.GetById(5).CreatedInterviewsCount, Is.EqualTo(0));
        }

        private IInterviewViewModelFactory CreateViewFactory()
        {
            var viewModelsFactory = new Mock<IInterviewViewModelFactory>();

            viewModelsFactory
                .Setup(x => x.GetNew<InterviewerAssignmentDashboardItemViewModel>())
                .Returns(Create.Entity.InterviewerAssignmentDashboardItemViewModel(ServiceLocator.Current));

            viewModelsFactory
                .Setup(x => x.GetNew<DashboardSubTitleViewModel>())
                .Returns(Create.Entity.DashboardSubTitleViewModel());

            return viewModelsFactory.Object;
        }

        private CreateNewViewModel CreateViewModel(
            IPlainStorage<QuestionnaireView> questionnaireViewRepository = null,
            IInterviewViewModelFactory viewModelFactory = null,
            IAssignmentDocumentsStorage assignmentsRepository = null,
            IViewModelNavigationService viewModelNavigationService = null,
            IInterviewerSettings interviewerSettings = null) =>
            new CreateNewViewModel(
                questionnaireViewRepository ??
                new Mock<IPlainStorage<QuestionnaireView>> {DefaultValue = DefaultValue.Mock}.Object,
                viewModelFactory ?? new Mock<IInterviewViewModelFactory> {DefaultValue = DefaultValue.Mock}.Object,
                assignmentsRepository ?? Mock.Of<IAssignmentDocumentsStorage>(),
                viewModelNavigationService ?? Mock.Of<IViewModelNavigationService>(),
                interviewerSettings ?? Mock.Of<IInterviewerSettings>());
    }
}
