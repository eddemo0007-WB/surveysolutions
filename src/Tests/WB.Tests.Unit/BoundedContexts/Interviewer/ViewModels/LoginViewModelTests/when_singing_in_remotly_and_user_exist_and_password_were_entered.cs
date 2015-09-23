using System;
using System.Linq;

using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;

using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.Interviewer.ViewModels.LoginViewModelTests
{
    public class when_singing_in_remotly_and_user_exist_and_new_password_were_entered : LoginViewModelTestContext
    {
        Establish context = () =>
        {
            var passwordHasher = Mock.Of<IPasswordHasher>(x => x.Hash(newUserPassword) == userPasswordHash);

            var interviewer = CreateInterviewerIdentity(userName, userPasswordHash);

            var principal = new Mock<IPrincipal>();
            principal.Setup(x => x.SignIn(userName, userPasswordHash, true)).Returns(true);

            InterviewersPlainStorage
              .Setup(x => x.Query(Moq.It.IsAny<Func<IQueryable<InterviewerIdentity>, InterviewerIdentity>>()))
              .Returns(interviewer);

            viewModel = CreateLoginViewModel(
                viewModelNavigationService: ViewModelNavigationServiceMock.Object,
                interviewersPlainStorage: InterviewersPlainStorage.Object,
                passwordHasher: passwordHasher,
                principal: principal.Object);

            viewModel.Init();
            viewModel.Password = newUserPassword;
        };

        Because of = () => viewModel.OnlineSignInCommand.Execute();

        It should_navigate_to_dashboard = () =>
            ViewModelNavigationServiceMock.Verify(x => x.NavigateToDashboard(), Times.Once);

        It should_store_entered_password = () =>
           InterviewersPlainStorage.Verify(x => x.StoreAsync(Moq.It.Is<InterviewerIdentity>(i => i.Password == userPasswordHash)), Times.Once);

        static LoginViewModel viewModel;
        private static readonly string userName = "Vasya";
        private static readonly string newUserPassword = "newPassword";
        private static readonly string userPasswordHash = "passwordHash";
        static Mock<IViewModelNavigationService> ViewModelNavigationServiceMock = new Mock<IViewModelNavigationService>();
        static Mock<IAsyncPlainStorage<InterviewerIdentity>> InterviewersPlainStorage;
    }
}