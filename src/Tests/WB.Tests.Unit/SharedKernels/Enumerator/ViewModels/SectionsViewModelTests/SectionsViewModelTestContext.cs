﻿using System;
using Machine.Specifications;
using Microsoft.Practices.ServiceLocation;
using Moq;
using MvvmCross.Plugins.Messenger;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Groups;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Repositories;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.SectionsViewModelTests
{
    [Subject(typeof(SideBarSectionsViewModel))]
    internal class SectionsViewModelTestContext
    {
        protected static SideBarSectionsViewModel CreateSectionsViewModel(
            IMvxMessenger messenger = null,
            IStatefulInterviewRepository interviewRepository = null,
            IQuestionnaireStorage questionnaireRepository = null,
            ISubstitutionService substitutionService = null,
            ISideBarSectionViewModelsFactory sideBarSectionViewModelsFactory = null)
        {

            return new SideBarSectionsViewModel(interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>(),
                Create.Service.LiteEventRegistry(),
                sideBarSectionViewModelsFactory ?? Stub.SideBarSectionViewModelsFactory(),
                mainThreadDispatcher: Create.Fake.MvxMainThreadDispatcher());
        }


        protected static SideBarSectionsViewModel CreateSectionsViewModel(IQuestionnaire questionnaire, IStatefulInterview interview)
        {
            var questionnaireRepository = new Mock<IQuestionnaireStorage>();
            questionnaireRepository.SetReturnsDefault(questionnaire);

            var interviewsRepository = new Mock<IStatefulInterviewRepository>();
            interviewsRepository.SetReturnsDefault(interview);

            var serviceLocatorMock = new Mock<IServiceLocator>();

            var sideBarSectionViewModelsFactory = new SideBarSectionViewModelFactory(serviceLocatorMock.Object);

            Func<SideBarSectionViewModel> sideBarSectionViewModel = () =>
            {
                var barSectionViewModel = new SideBarSectionViewModel(
                    interviewsRepository.Object,
                    sideBarSectionViewModelsFactory,
                    Mock.Of<IMvxMessenger>(),
                    Create.ViewModel.DynamicTextViewModel(
                        interviewRepository: interviewsRepository.Object));
                barSectionViewModel.NavigationState = Create.Other.NavigationState();
                return barSectionViewModel;
            };
            
            serviceLocatorMock.Setup(x => x.GetInstance<SideBarSectionViewModel>())
                .Returns(sideBarSectionViewModel);

            serviceLocatorMock.Setup(x => x.GetInstance<GroupStateViewModel>())
                .Returns(Mock.Of<GroupStateViewModel>());
            
            serviceLocatorMock.Setup(x => x.GetInstance<InterviewStateViewModel>())
                .Returns(Mock.Of<InterviewStateViewModel>());

            serviceLocatorMock.Setup(x => x.GetInstance<CoverStateViewModel>())
               .Returns(Mock.Of<CoverStateViewModel>());
           
            return CreateSectionsViewModel(questionnaireRepository: questionnaireRepository.Object,
                interviewRepository: interviewsRepository.Object,
                sideBarSectionViewModelsFactory: sideBarSectionViewModelsFactory);
        }
    }
}