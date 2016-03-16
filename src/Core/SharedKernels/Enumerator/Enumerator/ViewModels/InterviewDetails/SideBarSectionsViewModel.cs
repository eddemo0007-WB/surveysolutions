using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Utils;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Repositories;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails
{
    public class SideBarSectionsViewModel : MvxNotifyPropertyChanged,
        ILiteEventHandler<RosterInstancesAdded>,
        ILiteEventHandler<RosterInstancesRemoved>,
        ILiteEventHandler<GroupsEnabled>,
        ILiteEventHandler<GroupsDisabled>, IDisposable
    {
        private NavigationState navigationState;

        private readonly IPlainQuestionnaireRepository questionnaireRepository;
        readonly ILiteEventRegistry eventRegistry;
        private readonly ISideBarSectionViewModelsFactory modelsFactory;
        private readonly IStatefulInterviewRepository statefulInterviewRepository;
        private QuestionnaireIdentity questionnaireId;
        private string interviewId;

        protected SideBarSectionsViewModel()
        {
        }

        public ObservableCollection<SideBarSectionViewModel> Sections { get; set; }
        public ObservableCollection<SideBarSectionViewModel> AllVisibleSections { get; set; }

        public SideBarSectionsViewModel(IStatefulInterviewRepository statefulInterviewRepository,
            IPlainQuestionnaireRepository questionnaireRepository,
            ILiteEventRegistry eventRegistry,
            ISideBarSectionViewModelsFactory modelsFactory)
        {
            this.questionnaireRepository = questionnaireRepository;
            this.eventRegistry = eventRegistry;
            this.modelsFactory = modelsFactory;
            this.statefulInterviewRepository = statefulInterviewRepository;
        }

        public void Init(string questionnaireId,
            string interviewId,
            NavigationState navigationState)
        {
            if (navigationState == null) throw new ArgumentNullException("navigationState");
            if (this.navigationState != null) throw new Exception("ViewModel already initialized");
            if (interviewId == null) throw new ArgumentNullException("interviewId");
            if (questionnaireId == null) throw new ArgumentNullException("questionnaireId");

            this.interviewId = interviewId;

            this.eventRegistry.Subscribe(this, interviewId);
            this.navigationState = navigationState;
            this.navigationState.ScreenChanged += this.OnScreenChanged;
            IStatefulInterview interview = this.statefulInterviewRepository.Get(this.interviewId);
            this.questionnaireId = interview.QuestionnaireIdentity;
            this.interviewId = interviewId;

            this.BuildSectionsList();
        }

        private void BuildSectionsList()
        {
            var questionnaire = this.questionnaireRepository.GetQuestionnaire(this.questionnaireId);
            IStatefulInterview interview = this.statefulInterviewRepository.Get(this.interviewId);
            List<SideBarSectionViewModel> sections = new List<SideBarSectionViewModel>();

            foreach (Guid sectionId in questionnaire.GetAllSections())
            {
                var groupIdentity = new Identity(sectionId, new decimal[] { });
                if (interview.IsEnabled(groupIdentity))
                {
                    var sectionViewModel = this.BuildSectionItem(null, groupIdentity);
                    sections.Add(sectionViewModel);
                }
            }

            sections.Add(this.modelsFactory.BuildCompleteScreenSectionItem(navigationState, interviewId));

            this.Sections = new ObservableCollection<SideBarSectionViewModel>(sections);
            this.UpdateSideBarTree();
        }

        private void OnScreenChanged(ScreenChangedEventArgs eventArgs)
        {
            this.HighlightCurrentSection(eventArgs);
        }

        private void HighlightCurrentSection(ScreenChangedEventArgs eventArgs)
        {
            var allTreeElements = new ReadOnlyCollection<SideBarSectionViewModel>(this.Sections)
                .TreeToEnumerable(x => x.Children).ToList();
            SideBarSectionViewModel selectedGroup = allTreeElements
                .FirstOrDefault(x => x.ScreenType == ScreenType.Group && x.SectionIdentity.Equals(eventArgs.TargetGroup));

            if (eventArgs.TargetScreen == ScreenType.Complete)
            {
                this.Sections.Where(x => x.IsSelected).ForEach(x => x.IsSelected = false);
                this.Sections.Where(x => x.Expanded).ForEach(x => x.Expanded = false);
                this.UpdateSideBarTree();
                return;
            }

            var sideBarSectionToHighlight = selectedGroup;
            if (sideBarSectionToHighlight == null)
            {
                return;
            }      
      
            while (sideBarSectionToHighlight.Parent != null)
            {
                sideBarSectionToHighlight = sideBarSectionToHighlight.Parent;
            }

            this.Sections.Where(x => x != sideBarSectionToHighlight && x.IsSelected).ForEach(x => x.IsSelected = false);
            this.Sections.Where(x => x != sideBarSectionToHighlight && x.Expanded).ForEach(x => x.Expanded = false);

            sideBarSectionToHighlight.IsSelected = true;
            sideBarSectionToHighlight.Expanded = true;
            sideBarSectionToHighlight.TreeToEnumerable(s => s.Children)
                .Where(s => !s.IsSelected)
                .ForEach(s => s.IsSelected = true);
            selectedGroup.Expanded = true;

            this.UpdateSideBarTree();
        }

        public void Handle(RosterInstancesAdded @event)
        {
            IStatefulInterview interview = this.statefulInterviewRepository.Get(this.interviewId);

            foreach (var rosterInstance in @event.Instances)
            {
                var addedIdentity = rosterInstance.GetIdentity();
                this.RefreshListWithNewItemAdded(addedIdentity, interview);
            }

            this.RefreshHasChildrenFlags();
            this.UpdateSideBarTree();
        }

        public void Handle(GroupsEnabled @event)
        {
            IQuestionnaire questionnaire = this.questionnaireRepository.GetQuestionnaire(this.questionnaireId);
            IStatefulInterview interview = this.statefulInterviewRepository.Get(this.interviewId);

            foreach (var groupId in @event.Groups)
            {
                var addedIdentity = new Identity(groupId.Id, groupId.RosterVector);

                if (questionnaire.GetAllSections().Contains(addedIdentity.Id))
                    this.AddSection(addedIdentity.Id, questionnaire, interview);
                else
                    this.RefreshListWithNewItemAdded(addedIdentity, interview);
            }

            this.RefreshHasChildrenFlags();
            this.UpdateSideBarTree();
        }

        private void AddSection(Guid sectionId, IQuestionnaire questionnaire, IStatefulInterview interview)
        {
            var sectionIdentity = new Identity(sectionId, new decimal[0]);
            var sectionViewModel = this.BuildSectionItem(null, sectionIdentity);
            var index = questionnaire.GetAllSections()
                .Where(sId => interview.IsEnabled(new Identity(sId, new decimal[0])))
                .ToList()
                .IndexOf(sectionId);
            this.Sections.Insert(index, sectionViewModel);
        }

        private void RefreshListWithNewItemAdded(Identity addedIdentity, IStatefulInterview interview)
        {
            Identity parentId = interview.GetParentGroup(addedIdentity);
            var sectionToAddTo = this.AllVisibleSections.FirstOrDefault(x => x.ScreenType == ScreenType.Group && x.SectionIdentity.Equals(parentId));

            if (sectionToAddTo != null)
            {
                List<Identity> enabledSubgroups = interview.GetEnabledSubgroups(parentId).ToList();
                for (int i = 0; i < enabledSubgroups.Count; i++)
                {
                    var enabledSubgroupIdentity = enabledSubgroups[i];
                    if (i >= sectionToAddTo.Children.Count || !sectionToAddTo.Children[i].SectionIdentity.Equals(enabledSubgroupIdentity))
                    {
                        var sideBarItem = this.BuildSectionItem(sectionToAddTo, enabledSubgroupIdentity);
                        if (i < sectionToAddTo.Children.Count)
                        {
                            sectionToAddTo.Children.Insert(i, sideBarItem);
                        }
                        else
                        {
                            sectionToAddTo.Children.Add(sideBarItem);
                        }
                    }
                }
            }
        }

        public void Handle(GroupsDisabled @event)
        {
            var identities = @event.Groups.Select(i => new Identity(i.Id, i.RosterVector)).ToArray();
            this.RemoveFromSections(identities);
            this.RemoveFromChildrenSections(identities);
            this.RefreshHasChildrenFlags();
            this.UpdateSideBarTree();
        }

        public void Handle(RosterInstancesRemoved @event)
        {
            var identities = @event.Instances.Select(ri => ri.GetIdentity()).ToArray();
            this.RemoveFromChildrenSections(identities);
            this.RefreshHasChildrenFlags();
            this.UpdateSideBarTree();
        }

        private void RemoveFromChildrenSections(Identity[] identities)
        {
            foreach (var groupIdentity in identities)
            {
                var section = this.AllVisibleSections.FirstOrDefault(s => s.ScreenType == ScreenType.Group && s.SectionIdentity.Equals(groupIdentity));
                if (section != null)
                {
                    section.Parent?.Children.Remove(section);

                    section.RemoveMe();
                }
            }
        }

        private void RemoveFromSections(Identity[] identities)
        {
            foreach (var groupIdentity in identities)
            {
                var topLevelSectionToRemove = this.Sections.FirstOrDefault(s => s.ScreenType == ScreenType.Group && s.SectionIdentity.Equals(groupIdentity));
                if (topLevelSectionToRemove != null)
                {
                    this.Sections.Remove(topLevelSectionToRemove);
                    topLevelSectionToRemove.RemoveMe();
                }
            }
        }

        private void RefreshHasChildrenFlags()
        {
            foreach (var section in this.AllVisibleSections)
            {
                section.RefreshHasChildrenFlag();
            }
        }

        public void UpdateSideBarTree()
        {
            var tree = this.Sections.TreeToEnumerableDepthFirst(x => x.Expanded ? x.Children : Enumerable.Empty<SideBarSectionViewModel>()).ToList();
            this.AllVisibleSections = new ObservableCollection<SideBarSectionViewModel>(tree);
            this.RaisePropertyChanged(() => this.AllVisibleSections);
        }

        private SideBarSectionViewModel BuildSectionItem(SideBarSectionViewModel sectionToAddTo, Identity enabledSubgroupIdentity)
        {
            return this.modelsFactory.BuildSectionItem(this, sectionToAddTo, NavigationIdentity.CreateForGroup(enabledSubgroupIdentity), this.navigationState, this.interviewId);
        }

        public ICommand UpdateStatuses
        {
            get
            {
                return new MvxCommand(async () => await Task.Run(
                    () =>
                    {
                        this.AllVisibleSections.ForEach(x => x.SideBarGroupState.UpdateFromGroupModel());
                    }));
            }
        }

        public void Dispose()
        {
            this.eventRegistry.Unsubscribe(this, interviewId);
            this.navigationState.ScreenChanged -= this.OnScreenChanged;
        }
    }
}