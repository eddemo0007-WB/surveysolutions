using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cirrious.CrossCore.Core;
using Cirrious.MvvmCross.Plugins.Messenger;
using Cirrious.MvvmCross.ViewModels;
using WB.Core.BoundedContexts.Tester.Implementation.Aggregates;
using WB.Core.BoundedContexts.Tester.Implementation.Entities;
using WB.Core.BoundedContexts.Tester.Repositories;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Utils;

namespace WB.Core.BoundedContexts.Tester.ViewModels
{
    public class SideBarSectionsViewModel : MvxNotifyPropertyChanged,
        ILiteEventHandler<RosterInstancesAdded>,
        ILiteEventHandler<RosterInstancesRemoved>,
        ILiteEventHandler<GroupsEnabled>,
        ILiteEventHandler<GroupsDisabled>
    {
        private NavigationState navigationState;

        private readonly IPlainKeyValueStorage<QuestionnaireModel> questionnaireRepository;
        readonly ILiteEventRegistry eventRegistry;
        private readonly ISideBarSectionViewModelsFactory modelsFactory;
        private readonly IMvxMainThreadDispatcher mainThreadDispatcher;
        private readonly IStatefulInterviewRepository statefulInterviewRepository;
        private string questionnaireId;
        private string interviewId;

        public ObservableCollection<SideBarSectionViewModel> Sections { get; set; }
        public ObservableCollection<SideBarSectionViewModel> AllVisibleSections { get; set; }

        public SideBarSectionsViewModel(IStatefulInterviewRepository statefulInterviewRepository,
            IPlainKeyValueStorage<QuestionnaireModel> questionnaireRepository,
            ILiteEventRegistry eventRegistry,
            ISideBarSectionViewModelsFactory modelsFactory,
            IMvxMainThreadDispatcher mainThreadDispatcher
)
        {
            this.questionnaireRepository = questionnaireRepository;
            this.eventRegistry = eventRegistry;
            this.modelsFactory = modelsFactory;
            this.mainThreadDispatcher = mainThreadDispatcher;
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

            eventRegistry.Subscribe(this, interviewId);

            this.navigationState = navigationState;
            this.navigationState.GroupChanged += this.NavigationStateGroupChanged;
            this.questionnaireId = questionnaireId;
            this.interviewId = interviewId;

            BuildSectionsList();
        }

        private void BuildSectionsList()
        {
            var questionnaire = this.questionnaireRepository.GetById(questionnaireId);
            IStatefulInterview interview = this.statefulInterviewRepository.Get(this.interviewId);
            List<SideBarSectionViewModel> sections = new List<SideBarSectionViewModel>();

            foreach (GroupsHierarchyModel section in questionnaire.GroupsHierarchy)
            {
                var groupIdentity = new Identity(section.Id, new decimal[] { });
                if (interview.IsEnabled(groupIdentity))
                {
                    var sectionViewModel = this.BuildSectionItem(null, groupIdentity);
                    sections.Add(sectionViewModel);
                }
            }

            this.Sections = new ObservableCollection<SideBarSectionViewModel>(sections);
            this.UpdateSideBarTree();
        }

        void NavigationStateGroupChanged(GroupChangedEventArgs navigationParams)
        {
            HighlightCurrentSection(navigationParams);
        }

        private void HighlightCurrentSection(GroupChangedEventArgs navigationParams)
        {
            SideBarSectionViewModel selectedGroup = AllVisibleSections
                .FirstOrDefault(x => x.SectionIdentity.Equals(navigationParams.TargetGroup));

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
        }

        public void Handle(RosterInstancesAdded @event)
        {
            using (GlobalStopwatcher.Scope("sidebar SideBarSectionsViewModel"))
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
        }

        public void Handle(GroupsEnabled @event)
        {
            QuestionnaireModel questionnaire = this.questionnaireRepository.GetById(questionnaireId);
            IStatefulInterview interview = this.statefulInterviewRepository.Get(this.interviewId);

            foreach (var groupId in @event.Groups)
            {
                var addedIdentity = new Identity(groupId.Id, groupId.RosterVector);

                var section = questionnaire.GroupsHierarchy.FirstOrDefault(s => s.Id == addedIdentity.Id);
                if (section != null)
                    this.AddSection(section, questionnaire, interview);
                else
                    this.RefreshListWithNewItemAdded(addedIdentity, interview);
            }

            this.RefreshHasChildrenFlags();
            this.UpdateSideBarTree();
        }

        void AddSection(GroupsHierarchyModel section, QuestionnaireModel questionnaire, IStatefulInterview interview)
        {
            var sectionIdentity = new Identity(section.Id, new decimal[0]);
            var sectionViewModel = this.BuildSectionItem(null, sectionIdentity);
            var index = questionnaire.GroupsHierarchy
                .Where(s => interview.IsEnabled(sectionIdentity))
                .ToList()
                .IndexOf(section);
            Sections.Insert(index, sectionViewModel);
            //this.mainThreadDispatcher.RequestMainThreadAction(() => Sections.Insert(index, sectionViewModel));
        }

        private void RefreshListWithNewItemAdded(Identity addedIdentity, IStatefulInterview interview)
        {
            Identity parentId = interview.GetParentGroup(addedIdentity);
            var sectionToAddTo = AllVisibleSections.SingleOrDefault(x => x.SectionIdentity.Equals(parentId));

            if (sectionToAddTo != null)
            {
                List<Identity> enabledSubgroups = interview.GetEnabledSubgroups(parentId).ToList();
                //this.mainThreadDispatcher.RequestMainThreadAction(() =>
                //{
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
                //});
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
                var section = AllVisibleSections.FirstOrDefault(s => s.SectionIdentity.Equals(groupIdentity));
                if (section != null)
                {
                    //this.mainThreadDispatcher.RequestMainThreadAction(() => section.Parent.Children.Remove(section));
                    section.Parent.Children.Remove(section);
                    section.RemoveMe();
                }
            }
        }

        private void RemoveFromSections(Identity[] identities)
        {
            foreach (var groupIdentity in identities)
            {
                var topLevelSectionToRemove = this.Sections.FirstOrDefault(s => s.SectionIdentity.Equals(groupIdentity));
                if (topLevelSectionToRemove != null)
                {
                    //this.mainThreadDispatcher.RequestMainThreadAction(() => this.Sections.Remove(topLevelSectionToRemove));
                    this.Sections.Remove(topLevelSectionToRemove);
                    topLevelSectionToRemove.RemoveMe();
                }
            }
        }

        private void RefreshHasChildrenFlags()
        {
            foreach (var section in AllVisibleSections)
            {
                section.RefreshHasChildrenFlag();
            }
        }

        public void UpdateSideBarTree()
        {
            var tree = this.Sections.TreeToEnumerableDepthFirst(
                x => x.Expanded ? x.Children : Enumerable.Empty<SideBarSectionViewModel>()
                ).ToList();
            this.AllVisibleSections = new ObservableCollection<SideBarSectionViewModel>(tree);
            this.RaisePropertyChanged(() => this.AllVisibleSections);
        }

        private SideBarSectionViewModel BuildSectionItem(SideBarSectionViewModel sectionToAddTo, Identity enabledSubgroupIdentity)
        {
            return this.modelsFactory.BuildSectionItem(this, sectionToAddTo, enabledSubgroupIdentity, this.navigationState, this.interviewId);
        }
    }
}