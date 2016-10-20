﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Ncqrs.Domain;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.CustomCollections;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.EventBus;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.Invariants;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.Utils;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates
{
    public partial class Interview : AggregateRootMappedByConvention
    {
        public Interview() { }

        protected readonly InterviewEntities.InterviewProperties properties = new InterviewEntities.InterviewProperties();

        protected Guid questionnaireId;

        protected long questionnaireVersion;
        protected string language;

        public override Guid EventSourceId
        {
            get { return base.EventSourceId; }

            protected set
            {
                base.EventSourceId = value;
                this.properties.Id = value.FormatGuid();
            }
        }

        private ILatestInterviewExpressionState expressionProcessorStatePrototype = null;
        protected ILatestInterviewExpressionState ExpressionProcessorStatePrototype
        {
            get
            {
                if (this.expressionProcessorStatePrototype == null)
                {
                    this.expressionProcessorStatePrototype = this.expressionProcessorStatePrototypeProvider.GetExpressionState(this.questionnaireId, this.questionnaireVersion);
                    this.expressionProcessorStatePrototype.SetInterviewProperties(new InterviewProperties(EventSourceId));
                }

                return this.expressionProcessorStatePrototype;
            }

            set
            {
                expressionProcessorStatePrototype = value;
            }
        }

        protected InterviewStateDependentOnAnswers interviewState = new InterviewStateDependentOnAnswers();

        private void RemoveAnswerFromExpressionProcessorState(ILatestInterviewExpressionState state, Guid questionId, RosterVector rosterVector)
        {
            state.RemoveAnswer(new Identity(questionId, rosterVector));
        }

        #region Dependencies

        private readonly ILogger logger;

        /// <remarks>
        /// Repository operations are time-consuming.
        /// So this repository may be used only in command handlers.
        /// And should never be used in event handlers!!
        /// </remarks>
        private readonly IQuestionnaireStorage questionnaireRepository;

        private readonly IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider;

        #endregion

        public Interview(ILogger logger, IQuestionnaireStorage questionnaireRepository, IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider)
        {
            this.logger = logger;
            this.questionnaireRepository = questionnaireRepository;
            this.expressionProcessorStatePrototypeProvider = expressionProcessorStatePrototypeProvider;
        }

        private void SetQuestionnaireProperties(Guid questionnaireId, long questionnaireVersion)
        {
            this.questionnaireId = questionnaireId;
            this.questionnaireVersion = questionnaireVersion;
        }

        public void CreateInterviewWithPreloadedData(CreateInterviewWithPreloadedData command)
        {
            this.SetQuestionnaireProperties(command.QuestionnaireId, command.Version);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(command.QuestionnaireId, command.Version, language: null);

            var state = new InterviewStateDependentOnAnswers();

            var sourceInterviewTree = this.BuildInterviewTree(questionnaire, state);
            var changedInterviewTree = sourceInterviewTree.Clone();
            
            var orderedData = command.PreloadedData.Data.OrderBy(x => x.RosterVector.Length).ToArray();
            var changedQuestionIdentities = orderedData.SelectMany(x => x.Answers.Select(y => new Identity(y.Key, x.RosterVector))).ToList();

            foreach (var preloadedLevel in orderedData)
            {
                var answersToFeaturedQuestions = preloadedLevel.Answers;
                
                this.ValidatePrefilledQuestions(questionnaire, answersToFeaturedQuestions, preloadedLevel.RosterVector, state, false);

                var prefilledQuestionsWithAnswers = answersToFeaturedQuestions.ToDictionary(
                    answersToFeaturedQuestion => new Identity(answersToFeaturedQuestion.Key, preloadedLevel.RosterVector),
                    answersToFeaturedQuestion => answersToFeaturedQuestion.Value);

                this.UpdateTreeWithAnswersOnPrefilledQuestions(prefilledQuestionsWithAnswers, changedInterviewTree, questionnaire);
                this.UpdateTree(changedInterviewTree, questionnaire);
            }

            //apply events
            this.ApplyEvent(new InterviewFromPreloadedDataCreated(command.UserId, questionnaireId, questionnaire.Version));

            this.ApplyQuestionAnswer(userId: command.UserId, questionnaire: questionnaire,
                sourceInterviewTree: sourceInterviewTree, changedInterviewTree: changedInterviewTree,
                changedQuestionIdentities: changedQuestionIdentities);

            this.ApplyEvent(new SupervisorAssigned(command.UserId, command.SupervisorId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.SupervisorAssigned, comment: null));

            if (command.InterviewerId.HasValue)
            {
                this.ApplyEvent(new InterviewerAssigned(command.UserId, command.InterviewerId.Value, command.AnswersTime));
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.InterviewerAssigned, comment: null));
            }
        }

        public void CreateInterview(Guid questionnaireId, long questionnaireVersion, Guid supervisorId,
            Dictionary<Guid, object> answersToFeaturedQuestions, DateTime answersTime, Guid userId)
        {
            this.SetQuestionnaireProperties(questionnaireId, questionnaireVersion);

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(questionnaireId, questionnaireVersion, language: null);

            var state = new InterviewStateDependentOnAnswers();

            this.ValidatePrefilledQuestions(questionnaire, answersToFeaturedQuestions, RosterVector.Empty, state);

            var sourceInterviewTree = this.BuildInterviewTree(questionnaire, state);
            var changedInterviewTree = sourceInterviewTree.Clone();

            var prefilledQuestionsWithAnswers = answersToFeaturedQuestions.ToDictionary(x => new Identity(x.Key, RosterVector.Empty), x => x.Value);
            this.UpdateTreeWithAnswersOnPrefilledQuestions(prefilledQuestionsWithAnswers, changedInterviewTree, questionnaire);

            //apply events
            this.ApplyEvent(new InterviewCreated(userId, questionnaireId, questionnaire.Version));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Created, comment: null));
            
            this.ApplyQuestionAnswer(userId: userId, questionnaire: questionnaire,
                sourceInterviewTree: sourceInterviewTree, changedInterviewTree: changedInterviewTree,
                changedQuestionIdentities: prefilledQuestionsWithAnswers.Keys.ToList());

            this.ApplyEvent(new SupervisorAssigned(userId, supervisorId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.SupervisorAssigned, comment: null));
        }

        public void CreateInterviewOnClient(QuestionnaireIdentity questionnaireIdentity, Guid supervisorId, DateTime answersTime, Guid userId)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(questionnaireIdentity.QuestionnaireId, questionnaireIdentity.Version, language: null);
            this.SetQuestionnaireProperties(questionnaireIdentity.QuestionnaireId, questionnaire.Version);

            var state = new InterviewStateDependentOnAnswers();

            var sourceInterviewTree = this.BuildInterviewTree(questionnaire, state);
            var changedInterviewTree = sourceInterviewTree.Clone();

            //apply events
            this.ApplyEvent(new InterviewOnClientCreated(userId, questionnaireIdentity.QuestionnaireId, questionnaire.Version));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Created, comment: null));

            this.ApplyQuestionAnswer(userId: userId, questionnaire: questionnaire,
                sourceInterviewTree: sourceInterviewTree, changedInterviewTree: changedInterviewTree,
                changedQuestionIdentities: new List<Identity>());
            
            this.ApplyEvent(new SupervisorAssigned(userId, supervisorId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.SupervisorAssigned, comment: null));

            this.ApplyEvent(new InterviewerAssigned(userId, userId, answersTime));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.InterviewerAssigned, comment: null));
        }

        public void CreateInterviewCreatedOnClient(Guid questionnaireId, long questionnaireVersion,
            InterviewStatus interviewStatus,
            AnsweredQuestionSynchronizationDto[] featuredQuestionsMeta, bool isValid, Guid userId)
        {
            this.SetQuestionnaireProperties(questionnaireId, questionnaireVersion);

            this.GetQuestionnaireOrThrow(questionnaireId, questionnaireVersion, language: null);
            this.ApplyEvent(new InterviewOnClientCreated(userId, questionnaireId, questionnaireVersion));
            this.ApplyEvent(new SynchronizationMetadataApplied(userId, questionnaireId, questionnaireVersion,
                interviewStatus, featuredQuestionsMeta, true, null, null, null));
            this.ApplyEvent(new InterviewStatusChanged(interviewStatus, string.Empty));
            this.ApplyValidationEvent(isValid);
        }

        #region StaticMethods

        private static ConcurrentDictionary<string, ConcurrentDistinctList<decimal>> BuildRosterInstanceIdsFromSynchronizationDto(InterviewSynchronizationDto synchronizationDto)
        {
            return synchronizationDto.RosterGroupInstances.ToConcurrentDictionary(
                pair => ConversionHelper.ConvertIdAndRosterVectorToString(pair.Key.Id, pair.Key.InterviewItemRosterVector),
                pair => new ConcurrentDistinctList<decimal>(pair.Value.Select(rosterInstance => rosterInstance.RosterInstanceId).ToList()));
        }

        /// <remarks>
        /// If roster vector should be extended, result will be a set of vectors depending on roster count of corresponding groups.
        /// </remarks>
        protected static IEnumerable<RosterVector> ExtendRosterVector(IReadOnlyInterviewStateDependentOnAnswers state, RosterVector rosterVector, int length, Guid[] rosterGroupsStartingFromTop)
        {
            if (length < rosterVector.Length)
                throw new ArgumentException(string.Format(
                    "Cannot extend vector with length {0} to smaller length {1}.", rosterVector.Length, length));

            if (length == rosterVector.Length)
            {
                yield return rosterVector;
                yield break;
            }

            var outerVectorsForExtend = GetOuterVectorForParentRoster(state, rosterGroupsStartingFromTop, rosterVector);

            foreach (var outerVectorForExtend in outerVectorsForExtend)
            {
                IEnumerable<decimal> rosterInstanceIds = state.GetRosterInstanceIds(rosterGroupsStartingFromTop.Last(), outerVectorForExtend);
                foreach (decimal rosterInstanceId in rosterInstanceIds)
                {
                    yield return ((RosterVector)outerVectorForExtend).ExtendWithOneCoordinate(rosterInstanceId);
                }
            }
        }

        private static IEnumerable<decimal[]> GetOuterVectorForParentRoster(IReadOnlyInterviewStateDependentOnAnswers state,
            Guid[] rosterGroupsStartingFromTop, RosterVector rosterVector)
        {
            if (rosterGroupsStartingFromTop.Length <= 1 || rosterGroupsStartingFromTop.Length - 1 == rosterVector.Length)
            {
                yield return rosterVector;
                yield break;
            }

            var indexOfPreviousRoster = rosterGroupsStartingFromTop.Length - 2;

            var previousRoster = rosterGroupsStartingFromTop[rosterVector.Length];
            var previousRosterInstances = state.GetRosterInstanceIds(previousRoster, rosterVector);
            foreach (var previousRosterInstance in previousRosterInstances)
            {
                var extendedRoster = rosterVector.ExtendWithOneCoordinate(previousRosterInstance);
                if (indexOfPreviousRoster == rosterVector.Length)
                {
                    yield return extendedRoster;
                    continue;
                }
                foreach (var nextVector in GetOuterVectorForParentRoster(state, rosterGroupsStartingFromTop, extendedRoster))
                {
                    yield return nextVector;
                }
            }
        }

        private static string JoinDecimalsWithComma(IEnumerable<decimal> values)
        {
            return string.Join(", ", values.Select(value => value.ToString(CultureInfo.InvariantCulture)));
        }

        private static string FormatQuestionForException(Guid questionId, IQuestionnaire questionnaire)
        {
            return string.Format("'{0} [{1}]'",
                GetQuestionTitleForException(questionId, questionnaire),
                GetQuestionVariableNameForException(questionId, questionnaire));
        }

        private static string FormatGroupForException(Guid groupId, IQuestionnaire questionnaire)
        {
            return string.Format("'{0} ({1:N})'",
                GetGroupTitleForException(groupId, questionnaire),
                groupId);
        }

        private static string GetQuestionTitleForException(Guid questionId, IQuestionnaire questionnaire)
        {
            return questionnaire.HasQuestion(questionId)
                ? questionnaire.GetQuestionTitle(questionId) ?? "<<NO QUESTION TITLE>>"
                : "<<MISSING QUESTION>>";
        }

        private static string GetQuestionVariableNameForException(Guid questionId, IQuestionnaire questionnaire)
        {
            return questionnaire.HasQuestion(questionId)
                ? questionnaire.GetQuestionVariableName(questionId) ?? "<<NO VARIABLE NAME>>"
                : "<<MISSING QUESTION>>";
        }

        private static string GetGroupTitleForException(Guid groupId, IQuestionnaire questionnaire)
        {
            return questionnaire.HasGroup(groupId)
                ? questionnaire.GetGroupTitle(groupId) ?? "<<NO GROUP TITLE>>"
                : "<<MISSING GROUP>>";
        }

        private static ConcurrentHashSet<string> ToHashSetOfIdAndRosterVectorStrings(IEnumerable<InterviewItemId> synchronizationIdentities)
        {
            return new ConcurrentHashSet<string>(
                synchronizationIdentities.Select(
                    question => ConversionHelper.ConvertIdAndRosterVectorToString(question.Id, question.InterviewItemRosterVector)));
        }

        private static Identity ToIdentity(InterviewItemId synchronizationIdentity)
        {
            return new Identity(synchronizationIdentity.Id, synchronizationIdentity.InterviewItemRosterVector);
        }

        private static Identity GetInstanceOfQuestionWithSameAndUpperRosterLevelOrThrow(Guid questionId,
            RosterVector rosterVector, IQuestionnaire questionnare)
        {
            int vectorRosterLevel = rosterVector.Length;
            int questionRosterLevel = questionnare.GetRosterLevelForQuestion(questionId);

            if (questionRosterLevel > vectorRosterLevel)
                throw new InterviewException(string.Format(
                    "Question {0} expected to have roster level not deeper than {1} but it is {2}.",
                    FormatQuestionForException(questionId, questionnare), vectorRosterLevel, questionRosterLevel));

            decimal[] questionRosterVector = rosterVector.Shrink(questionRosterLevel);

            return new Identity(questionId, questionRosterVector);
        }

        protected IEnumerable<Identity> GetInstancesOfEntitiesWithSameAndDeeperRosterLevelOrThrow(
            IReadOnlyInterviewStateDependentOnAnswers state,
            IEnumerable<Guid> entityIds, RosterVector rosterVector, IQuestionnaire questionnare)
        {
            return entityIds.SelectMany(entityId =>
                GetInstancesOfEntitiesWithSameAndDeeperRosterLevelOrThrow(state, entityId, rosterVector, questionnare));
        }

        protected IEnumerable<Identity> GetInstancesOfEntitiesWithSameAndDeeperRosterLevelOrThrow(
            IReadOnlyInterviewStateDependentOnAnswers state,
            Guid entityId,
            RosterVector rosterVector,
            IQuestionnaire questionnare)
        {
            int vectorRosterLevel = rosterVector.Length;
            int entityRosterLevel = questionnare.GetRosterLevelForEntity(entityId);

            if (entityRosterLevel < vectorRosterLevel)
                throw new InterviewException(string.Format(
                    "Entity {0} expected to have roster level not upper than {1} but it is {2}. InterviewId: {3}",
                    FormatQuestionForException(entityId, questionnare), vectorRosterLevel, entityRosterLevel, EventSourceId));

            Guid[] parentRosterGroupsStartingFromTop =
                questionnare.GetRostersFromTopToSpecifiedEntity(entityId).ToArray();

            IEnumerable<RosterVector> entityRosterVectors = ExtendRosterVector(state,
                rosterVector, entityRosterLevel, parentRosterGroupsStartingFromTop);

            return entityRosterVectors.Select(entityRosterVector => new Identity(entityId, entityRosterVector));
        }

        protected IEnumerable<Identity> GetInstancesOfGroupsWithSameAndDeeperRosterLevelOrThrow(IReadOnlyInterviewStateDependentOnAnswers state,
            IEnumerable<Guid> groupIds, RosterVector rosterVector, IQuestionnaire questionnaire)
        {
            return groupIds.SelectMany(groupId =>
                GetInstancesOfGroupsByGroupIdWithSameAndDeeperRosterLevelOrThrow(state, groupId, rosterVector, questionnaire));
        }

        protected IEnumerable<Identity> GetInstancesOfGroupsByGroupIdWithSameAndDeeperRosterLevelOrThrow(IReadOnlyInterviewStateDependentOnAnswers state,
            Guid groupId, RosterVector rosterVector, IQuestionnaire questionnaire)
        {
            int vectorRosterLevel = rosterVector.Length;
            int groupRosterLevel = questionnaire.GetRosterLevelForGroup(groupId);

            if (groupRosterLevel < vectorRosterLevel)
                throw new InterviewException(string.Format(
                    "Question {0} expected to have roster level not upper than {1} but it is {2}. InterviewId: {3}",
                    FormatQuestionForException(groupId, questionnaire), vectorRosterLevel, groupRosterLevel, EventSourceId));

            Guid[] parentRosterGroupsStartingFromTop = questionnaire.GetRostersFromTopToSpecifiedGroup(groupId).ToArray();

            IEnumerable<RosterVector> groupRosterVectors = ExtendRosterVector(state,
                rosterVector, groupRosterLevel, parentRosterGroupsStartingFromTop);

            return groupRosterVectors.Select(groupRosterVector => new Identity(groupId, groupRosterVector));
        }

        protected Identity GetInstanceOfGroupWithSameAndUpperRosterLevelOrThrow(Guid groupId, RosterVector rosterVector, IQuestionnaire questionnaire)
        {
            int vectorRosterLevel = rosterVector.Length;

            int groupRosterLevel = questionnaire.GetRosterLevelForGroup(groupId);

            if (groupRosterLevel > vectorRosterLevel)
                throw new InterviewException(string.Format(
                    "Group {0} expected to have roster level not deeper than {1} but it is {2}. InterviewId: {3}",
                    FormatGroupForException(groupId, questionnaire), vectorRosterLevel, groupRosterLevel, this.EventSourceId));

            decimal[] groupRosterVector = rosterVector.Shrink(groupRosterLevel);

            return new Identity(groupId, groupRosterVector);
        }

        #endregion

        #region Handlers

        #region AnsweringQuestions
      

        private void ApplyQuestionAnswer(Guid userId, InterviewTree changedInterviewTree, IQuestionnaire questionnaire,
            List<Identity> changedQuestionIdentities, InterviewTree sourceInterviewTree)
        {
            var expressionProcessorState = this.GetClonedExpressionState();

            this.UpdateTree(changedInterviewTree, questionnaire);

            this.UpdateExpressionState(sourceInterviewTree, changedInterviewTree, expressionProcessorState);

            EnablementChanges enablementChanges = expressionProcessorState.ProcessEnablementConditions();

            this.UpdateTreeWithEnablementChanges(changedInterviewTree, enablementChanges);

            var structuralChanges = expressionProcessorState.GetStructuralChanges();
            this.UpdateTreeWithStructuralChanges(changedInterviewTree, structuralChanges);

            changedQuestionIdentities.AddRange(structuralChanges.ChangedMultiQuestions.Keys);
            changedQuestionIdentities.AddRange(structuralChanges.ChangedSingleQuestions.Keys);
            changedQuestionIdentities.AddRange(structuralChanges.ChangedYesNoQuestions.Keys);

            this.UpdateRosterTitles(changedInterviewTree, questionnaire);

            this.UpdateLinkedQuestions(changedInterviewTree, expressionProcessorState);

            VariableValueChanges variableValueChanges = expressionProcessorState.ProcessVariables();
            this.UpdateTreeWithVariableChanges(changedInterviewTree, variableValueChanges);

            ValidityChanges validationChanges = expressionProcessorState.ProcessValidationExpressions();
            this.UpdateTreeWithValidationChanges(changedInterviewTree, validationChanges);

            this.ApplySubstitutionEvents(changedInterviewTree, questionnaire, changedQuestionIdentities);

            this.ApplyEvents(sourceInterviewTree, changedInterviewTree, userId);
        }
        private static void RemoveAnswersForDependendCascadingQuestions(Identity questionIdentity, InterviewTree changedInterviewTree, IQuestionnaire questionnaire, List<Identity> changedQuestionIdentities)
        {
            IEnumerable<Guid> dependentQuestionIds = questionnaire.GetCascadingQuestionsThatDependUponQuestion(questionIdentity.Id);
            foreach (var dependentQuestionId in dependentQuestionIds)
            {
                var cascadingAnsweredQuestionsToRemoveAnswer = changedInterviewTree.FindQuestions(dependentQuestionId)
                    .Where(x => x.IsCascading && x.IsAnswered())
                    .Where(x => x.IsOnTheSameOrDeeperLevel(questionIdentity));

                foreach (var cascadingQuestion in cascadingAnsweredQuestionsToRemoveAnswer)
                {
                    cascadingQuestion.RemoveAnswer();
                    changedQuestionIdentities.Add(cascadingQuestion.Identity);
                }
            }
        }

        private void ApplySubstitutionEvents(InterviewTree tree, IQuestionnaire questionnaire, List<Identity> changedQuestionIdentities)
        {
            var changedQuestionTitles = new List<Identity>();
            var changedStaticTextTitles = new List<Identity>();
            var changedGroupTitles = new List<Identity>();
            foreach (var questionIdentity in changedQuestionIdentities)
            {
                var rosterLevel = questionIdentity.RosterVector.Length;

                var substitutedQuestionIds = questionnaire.GetSubstitutedQuestions(questionIdentity.Id);
                foreach (var substitutedQuestionId in substitutedQuestionIds)
                {
                    changedQuestionTitles.AddRange(tree.FindEntity(substitutedQuestionId)
                        .Select(x => x.Identity)
                        .Where(x => x.RosterVector.Take(rosterLevel).SequenceEqual(questionIdentity.RosterVector)));
                }

                var substitutedStaticTextIds = questionnaire.GetSubstitutedStaticTexts(questionIdentity.Id);
                foreach (var substitutedStaticTextId in substitutedStaticTextIds)
                {
                    changedStaticTextTitles.AddRange(tree.FindEntity(substitutedStaticTextId)
                        .Select(x => x.Identity)
                        .Where(x => x.RosterVector.Take(rosterLevel).SequenceEqual(questionIdentity.RosterVector)));
                }
             
                var substitutedGroupIds = questionnaire.GetSubstitutedGroups(questionIdentity.Id);
                foreach (var substitutedGroupId in substitutedGroupIds)
                {
                    changedGroupTitles.AddRange(tree.FindEntity(substitutedGroupId)
                        .Select(x => x.Identity)
                        .Where(x => x.RosterVector.Take(rosterLevel).SequenceEqual(questionIdentity.RosterVector)));
                }
            }

            if (changedQuestionTitles.Any() || changedStaticTextTitles.Any() || changedGroupTitles.Any())
            {
                this.ApplyEvent(new SubstitutionTitlesChanged(
                    changedQuestionTitles.ToArray(),
                    changedStaticTextTitles.ToArray(),
                    changedGroupTitles.ToArray()));
            }
        }

        #endregion

        
        //todo should respect changes calculated in ExpressionState
        public void ReevaluateSynchronizedInterview()
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            var expressionProcessorState = this.ExpressionProcessorStatePrototype.Clone();

            expressionProcessorState.SaveAllCurrentStatesAsPrevious();
            EnablementChanges enablementChanges = expressionProcessorState.ProcessEnablementConditions();
            ValidityChanges validationChanges = expressionProcessorState.ProcessValidationExpressions();

            this.ApplyEnablementChangesEvents(enablementChanges);

            this.ApplyValidityChangesEvents(validationChanges);

            if (!this.HasInvalidAnswers())
            {
                this.ApplyEvent(new InterviewDeclaredValid());
            }
        }

        public void RepeatLastInterviewStatus(RepeatLastInterviewStatus command)
        {
            this.ApplyEvent(new InterviewStatusChanged(this.properties.Status, command.Comment));
        }

        public void SwitchTranslation(SwitchTranslation command)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);
            IReadOnlyCollection<string> availableLanguages = questionnaire.GetTranslationLanguages();

            if (command.Language != null)
            {
                if (availableLanguages.All(language => language != command.Language))
                    throw new InterviewException(
                        $"Questionnaire does not have translation. Language: {command.Language}. Interview ID: {this.EventSourceId.FormatGuid()}. Questionnaire ID: {new QuestionnaireIdentity(this.questionnaireId, this.questionnaireVersion)}.");
            }

            var targetQuestionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, command.Language);

            this.ApplyEvent(new TranslationSwitched(command.Language, command.UserId));
            this.ApplyRosterTitleChanges(targetQuestionnaire);
        }

        protected void ApplyRosterTitleChanges(IQuestionnaire targetQuestionnaire)
        {
            var rosterInstances =
                this.GetInstancesOfGroupsWithSameAndDeeperRosterLevelOrThrow(this.interviewState,
                    targetQuestionnaire.GetRostersWithTitlesToChange(), RosterVector.Empty,
                    targetQuestionnaire).ToArray();

            var changedTitles = rosterInstances.Select(
                rosterInstance =>
                    new ChangedRosterInstanceTitleDto(
                        RosterInstance.CreateFromIdentity(rosterInstance),
                        this.GetRosterTitle(targetQuestionnaire, rosterInstance)))
                .ToArray();

            if (changedTitles.Any())
            {
                this.ApplyEvent(new RosterInstancesTitleChanged(changedTitles));
            }
        }

        private string GetRosterTitle(IQuestionnaire targetQuestionnaire, Identity rosterInstance)
        {
            if (targetQuestionnaire.IsFixedRoster(rosterInstance.Id))
            {
                return targetQuestionnaire.GetFixedRosterTitle(rosterInstance.Id,
                    rosterInstance.RosterVector.Coordinates.Last());
            }
            else if (targetQuestionnaire.IsNumericRoster(rosterInstance.Id))
            {
                var questionId = targetQuestionnaire.GetRosterTitleQuestionId(rosterInstance.Id);
                Identity rosterTitleQuestionIdentity = new Identity(questionId.Value, rosterInstance.RosterVector);
                var questionType = targetQuestionnaire.GetQuestionType(questionId.Value);
                var questionValue = this.interviewState.GetAnswerSupportedInExpressions(rosterTitleQuestionIdentity);

                switch (questionType)
                {
                    case QuestionType.SingleOption:
                    case QuestionType.MultyOption:
                        return AnswerUtils.AnswerToString(questionValue, x => targetQuestionnaire.GetAnswerOptionTitle(questionId.Value, x));
                    default:
                        return AnswerUtils.AnswerToString(questionValue);
                }
            }
            else
            {
                return targetQuestionnaire.GetAnswerOptionTitle(
                    targetQuestionnaire.GetRosterSizeQuestion(rosterInstance.Id),
                    rosterInstance.RosterVector.Last());
            }
        }

        public void CommentAnswer(Guid userId, Guid questionId, RosterVector rosterVector, DateTime commentTime, string comment)
        {
            new InterviewPropertiesInvariants(this.properties).RequireAnswerCanBeChanged();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);

            var tree = this.BuildInterviewTree(questionnaire);
            var treeInvariants = new InterviewTreeInvariants(tree);

            this.ThrowIfQuestionDoesNotExist(questionId, questionnaire);
            treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);

            this.ApplyEvent(new AnswerCommented(userId, questionId, rosterVector, commentTime, comment));
        }

        public void SetFlagToAnswer(Guid userId, Guid questionId, RosterVector rosterVector)
        {
            new InterviewPropertiesInvariants(this.properties).RequireAnswerCanBeChanged();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);

            var tree = this.BuildInterviewTree(questionnaire);
            var treeInvariants = new InterviewTreeInvariants(tree);

            this.ThrowIfQuestionDoesNotExist(questionId, questionnaire);
            treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);

            this.ApplyEvent(new FlagSetToAnswer(userId, questionId, rosterVector));
        }

        public void RemoveFlagFromAnswer(Guid userId, Guid questionId, RosterVector rosterVector)
        {
            new InterviewPropertiesInvariants(this.properties).RequireAnswerCanBeChanged();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);

            var tree = this.BuildInterviewTree(questionnaire);
            var treeInvariants = new InterviewTreeInvariants(tree);

            this.ThrowIfQuestionDoesNotExist(questionId, questionnaire);
            treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);

            this.ApplyEvent(new FlagRemovedFromAnswer(userId, questionId, rosterVector));
        }

        public void AssignSupervisor(Guid userId, Guid supervisorId)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Created, InterviewStatus.SupervisorAssigned);

            this.ApplyEvent(new SupervisorAssigned(userId, supervisorId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.SupervisorAssigned, comment: null));
        }

        public void AssignInterviewer(Guid userId, Guid interviewerId, DateTime assignTime)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.SupervisorAssigned, InterviewStatus.InterviewerAssigned, InterviewStatus.RejectedBySupervisor);
            propertiesInvariants.ThrowIfTryAssignToSameInterviewer(interviewerId);

            this.ApplyEvent(new InterviewerAssigned(userId, interviewerId, assignTime));
            if (!this.properties.WasCompleted && this.properties.Status != InterviewStatus.InterviewerAssigned)
            {
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.InterviewerAssigned, comment: null));
            }
        }

        public void Delete(Guid userId)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewWasCompleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(
                InterviewStatus.Created, InterviewStatus.SupervisorAssigned, InterviewStatus.InterviewerAssigned, InterviewStatus.Restored);

            this.ApplyEvent(new InterviewDeleted(userId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Deleted, comment: null));
        }

        public void HardDelete(Guid userId)
        {
            if (this.properties.IsHardDeleted)
                return;

            this.ApplyEvent(new InterviewHardDeleted(userId));
        }

        public void CancelByHQSynchronization(Guid userId)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            if (this.properties.Status != InterviewStatus.Completed)
            {
                this.ApplyEvent(new InterviewDeleted(userId));
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Deleted, comment: null));
            }
        }

        public void MarkInterviewAsReceivedByInterviwer(Guid userId)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            this.ApplyEvent(new InterviewReceivedByInterviewer());
        }

        public void MarkInterviewAsSentToHeadquarters(Guid userId)
        {
            if (!this.properties.IsHardDeleted)
            {
                this.ApplyEvent(new InterviewDeleted(userId));
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Deleted, comment: null));
            }
            this.ApplyEvent(new InterviewSentToHeadquarters());
        }

        public void Restore(Guid userId)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Deleted);

            this.ApplyEvent(new InterviewRestored(userId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Restored, comment: null));
        }

        public void Restart(Guid userId, string comment, DateTime restartTime)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed);

            this.ApplyEvent(new InterviewRestarted(userId, restartTime, comment));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.Restarted, comment));
        }

        public void Approve(Guid userId, string comment, DateTime approveTime)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed,
                InterviewStatus.RejectedBySupervisor,
                InterviewStatus.RejectedByHeadquarters);

            this.ApplyEvent(new InterviewApproved(userId, comment, approveTime));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.ApprovedBySupervisor, comment));
        }

        public void Reject(Guid userId, string comment, DateTime rejectTime)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.Completed,
                InterviewStatus.ApprovedBySupervisor,
                InterviewStatus.RejectedByHeadquarters);

            this.ApplyEvent(new InterviewRejected(userId, comment, rejectTime));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.RejectedBySupervisor, comment));
        }

        public void HqApprove(Guid userId, string comment)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.ApprovedBySupervisor, InterviewStatus.RejectedByHeadquarters);

            this.ApplyEvent(new InterviewApprovedByHQ(userId, comment));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.ApprovedByHeadquarters, comment));
        }

        public void UnapproveByHeadquarters(Guid userId, string comment)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.ApprovedByHeadquarters);

            string unapproveCommentMessage = "[Approved by Headquarters was revoked]";
            string unapproveComment = string.IsNullOrEmpty(comment)
                ? unapproveCommentMessage
                : string.Format("{0} \r\n {1}", unapproveCommentMessage, comment);
            this.ApplyEvent(new UnapprovedByHeadquarters(userId, unapproveComment));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.ApprovedBySupervisor, comment));
        }

        public void RejectInterviewFromHeadquarters(Guid userId,
            Guid supervisorId,
            Guid? interviewerId,
            InterviewSynchronizationDto interviewDto,
            DateTime synchronizationTime)
        {
            var commentedAnswers = (
                from answerDto in interviewDto.Answers
                from answerComment in answerDto.AllComments
                where !this.interviewState.AnswerComments.Contains(new AnswerComment(answerComment.UserId, answerComment.Date, answerComment.Text, answerDto.Id, answerDto.QuestionRosterVector))
                select new
                {
                    UserId = answerComment.UserId,
                    Date = answerComment.Date,
                    Text = answerComment.Text,
                    QuestionId = answerDto.Id,
                    RosterVector = answerDto.QuestionRosterVector
                });

            if (this.properties.Status == InterviewStatus.Deleted)
            {
                this.ApplyEvent(new InterviewRestored(userId));
            }


            this.ApplyEvent(new InterviewRejectedByHQ(userId, interviewDto.Comments));
            this.ApplyEvent(new InterviewStatusChanged(interviewDto.Status, comment: interviewDto.Comments));

            if (interviewerId.HasValue)
            {
                this.ApplyEvent(new InterviewerAssigned(userId, interviewerId.Value, synchronizationTime));
            }

            foreach (var commentedAnswer in commentedAnswers)
            {
                this.ApplyEvent(new AnswerCommented(commentedAnswer.UserId, commentedAnswer.QuestionId, commentedAnswer.RosterVector, commentedAnswer.Date, commentedAnswer.Text));
            }
        }

        public void HqReject(Guid userId, string comment)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();
            propertiesInvariants.ThrowIfInterviewStatusIsNotOneOfExpected(InterviewStatus.ApprovedBySupervisor, InterviewStatus.Deleted);

            this.ApplyEvent(new InterviewRejectedByHQ(userId, comment));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.RejectedByHeadquarters, comment));
        }

        public void SynchronizeInterview(Guid userId, InterviewSynchronizationDto synchronizedInterview)
        {
            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            this.ApplyEvent(new InterviewSynchronized(synchronizedInterview));
        }

        public void SynchronizeInterviewFromHeadquarters(Guid id, Guid userId, Guid supervisorId, InterviewSynchronizationDto interviewDto, DateTime synchronizationTime)
        {
            if (this.Version > 0)
            {
                throw new InterviewException(string.Format("Interview with id {0} already created", EventSourceId));
            }

            this.SetQuestionnaireProperties(interviewDto.QuestionnaireId, interviewDto.QuestionnaireVersion);

            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfInterviewHardDeleted();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(interviewDto.QuestionnaireId,
                interviewDto.QuestionnaireVersion, language: null);

            var rosters = CalculateRostersFromInterviewSynchronizationDto(interviewDto);

            var enablementChanges = new EnablementChanges(
                groupsToBeDisabled: interviewDto.DisabledGroups.Select(ToIdentity).ToList(),
                questionsToBeDisabled: interviewDto.DisabledQuestions.Select(ToIdentity).ToList(),
                groupsToBeEnabled: new List<Identity>(),
                questionsToBeEnabled: new List<Identity>());

            var validityChanges = new ValidityChanges(
                answersDeclaredInvalid: interviewDto.InvalidAnsweredQuestions.Select(ToIdentity).ToList(),
                answersDeclaredValid: new List<Identity>());

            if (interviewDto.CreatedOnClient)
                this.ApplyEvent(new InterviewOnClientCreated(userId, interviewDto.QuestionnaireId, interviewDto.QuestionnaireVersion));
            else
                this.ApplyEvent(new InterviewCreated(userId, interviewDto.QuestionnaireId, interviewDto.QuestionnaireVersion));

            this.ApplyEvent(new SupervisorAssigned(supervisorId, supervisorId));
            this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.SupervisorAssigned, comment: interviewDto.Comments));

            if (interviewDto.Status == InterviewStatus.InterviewerAssigned)
            {
                this.ApplyEvent(new InterviewerAssigned(supervisorId, userId, synchronizationTime));
                this.ApplyEvent(new InterviewStatusChanged(InterviewStatus.InterviewerAssigned, comment: null));
            }

            this.ApplyRostersEvents(rosters.ToArray());
            foreach (var answerDto in interviewDto.Answers.Where(x => x.Answer != null))
            {
                Guid questionId = answerDto.Id;
                QuestionType questionType = questionnaire.GetQuestionType(questionId);
                RosterVector rosterVector = answerDto.QuestionRosterVector;
                object answer = answerDto.Answer;

                switch (questionType)
                {
                    case QuestionType.Text:
                        this.ApplyEvent(new TextQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (string)answer));
                        break;

                    case QuestionType.DateTime:
                        this.ApplyEvent(new DateTimeQuestionAnswered(userId, questionId, rosterVector, synchronizationTime,
                            (DateTime)answer));
                        break;

                    case QuestionType.TextList:
                        this.ApplyEvent(new TextListQuestionAnswered(userId, questionId, rosterVector, synchronizationTime,
                            (Tuple<decimal, string>[])answer));
                        break;

                    case QuestionType.GpsCoordinates:
                        var geoPosition = (GeoPosition)answer;
                        this.ApplyEvent(new GeoLocationQuestionAnswered(userId, questionId, rosterVector, synchronizationTime,
                            geoPosition.Latitude, geoPosition.Longitude, geoPosition.Accuracy, geoPosition.Altitude,
                            geoPosition.Timestamp));
                        break;

                    case QuestionType.QRBarcode:
                        this.ApplyEvent(new QRBarcodeQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (string)answer));
                        break;

                    case QuestionType.Numeric:
                        this.ApplyEvent(questionnaire.IsQuestionInteger(questionId)
                            ? new NumericIntegerQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, Convert.ToInt32(answer)) as IEvent
                            : new NumericRealQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (decimal)answer) as IEvent);
                        break;

                    case QuestionType.SingleOption:
                        this.ApplyEvent(questionnaire.IsQuestionLinked(questionId)
                            ? new SingleOptionLinkedQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (decimal[])answer) as IEvent
                            : new SingleOptionQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (decimal)answer) as IEvent);
                        break;

                    case QuestionType.MultyOption:
                        this.ApplyEvent(questionnaire.IsQuestionLinked(questionId)
                            ? new MultipleOptionsLinkedQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (decimal[][])answer) as IEvent
                            : questionnaire.IsQuestionYesNo(questionId)
                                ? new YesNoQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (AnsweredYesNoOption[])answer) as IEvent
                                : new MultipleOptionsQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (decimal[])answer) as IEvent);
                        break;
                    case QuestionType.Multimedia:
                        this.ApplyEvent(new PictureQuestionAnswered(userId, questionId, rosterVector, synchronizationTime, (string)answer));
                        break;
                    default:
                        throw new InterviewException(string.Format("Question {0} has unknown type {1}. InterviewId: {2}",
                            FormatQuestionForException(questionId, questionnaire), questionType, EventSourceId));
                }
            }

            this.ApplyEnablementChangesEvents(enablementChanges);

            this.ApplyValidityChangesEvents(validityChanges);
        }

        public void SynchronizeInterviewEvents(Guid userId, Guid questionnaireId, long questionnaireVersion,
            InterviewStatus interviewStatus, IEvent[] synchronizedEvents, bool createdOnClient)
        {
            this.SetQuestionnaireProperties(questionnaireId, questionnaireVersion);

            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            propertiesInvariants.ThrowIfOtherInterviewerIsResponsible(userId);

            this.GetQuestionnaireOrThrow(questionnaireId, questionnaireVersion, language: null);

            var isInterviewNeedToBeCreated = createdOnClient && this.Version == 0;

            if (isInterviewNeedToBeCreated)
            {
                this.ApplyEvent(new InterviewOnClientCreated(userId, questionnaireId, questionnaireVersion));
            }
            else
            {
                if (this.properties.Status == InterviewStatus.Deleted)
                    this.Restore(userId);
                else
                    propertiesInvariants.ThrowIfStatusNotAllowedToBeChangedWithMetadata(interviewStatus);
            }

            foreach (var synchronizedEvent in synchronizedEvents)
            {
                this.ApplyEvent(synchronizedEvent);
            }

            this.ApplyEvent(new InterviewReceivedBySupervisor());
        }

        public void CreateInterviewFromSynchronizationMetadata(Guid id, Guid userId, Guid questionnaireId, long questionnaireVersion,
            InterviewStatus interviewStatus,
            AnsweredQuestionSynchronizationDto[] featuredQuestionsMeta,
            string comments,
            DateTime? rejectedDateTime,
            DateTime? interviewerAssignedDateTime,
            bool valid,
            bool createdOnClient)
        {
            this.SetQuestionnaireProperties(questionnaireId, questionnaireVersion);

            var propertiesInvariants = new InterviewPropertiesInvariants(this.properties);

            if (this.properties.Status == InterviewStatus.Deleted)
                this.Restore(userId);
            else
                propertiesInvariants.ThrowIfStatusNotAllowedToBeChangedWithMetadata(interviewStatus);

            this.ApplyEvent(new SynchronizationMetadataApplied(userId,
                questionnaireId,
                questionnaireVersion,
                interviewStatus,
                featuredQuestionsMeta,
                createdOnClient,
                comments,
                rejectedDateTime,
                interviewerAssignedDateTime));

            this.ApplyEvent(new InterviewStatusChanged(interviewStatus, comments));

            ApplyValidationEvent(valid);
        }

        private void ApplyValidationEvent(bool isValid)
        {
            if (isValid)
                this.ApplyEvent(new InterviewDeclaredValid());
            else
                this.ApplyEvent(new InterviewDeclaredInvalid());
        }

        #endregion

        #region EventApplying

        private void ApplyEnablementChangesEvents(EnablementChanges enablementChanges)
        {
            //should be removed. bad tests setup
            if (enablementChanges == null)
                return;

            if (enablementChanges.GroupsToBeDisabled.Any())
            {
                this.ApplyEvent(new GroupsDisabled(enablementChanges.GroupsToBeDisabled.ToArray()));
            }

            if (enablementChanges.GroupsToBeEnabled.Any())
            {
                this.ApplyEvent(new GroupsEnabled(enablementChanges.GroupsToBeEnabled.ToArray()));
            }

            if (enablementChanges.QuestionsToBeDisabled.Any())
            {
                this.ApplyEvent(new QuestionsDisabled(enablementChanges.QuestionsToBeDisabled.ToArray()));
            }

            if (enablementChanges.QuestionsToBeEnabled.Any())
            {
                this.ApplyEvent(new QuestionsEnabled(enablementChanges.QuestionsToBeEnabled.ToArray()));
            }

            if (enablementChanges.StaticTextsToBeDisabled.Any())
            {
                this.ApplyEvent(new StaticTextsDisabled(enablementChanges.StaticTextsToBeDisabled.ToArray()));
            }

            if (enablementChanges.StaticTextsToBeEnabled.Any())
            {
                this.ApplyEvent(new StaticTextsEnabled(enablementChanges.StaticTextsToBeEnabled.ToArray()));
            }
            if (enablementChanges.VariablesToBeDisabled.Any())
            {
                this.ApplyEvent(new VariablesDisabled(enablementChanges.VariablesToBeDisabled.ToArray()));
            }
            if (enablementChanges.VariablesToBeEnabled.Any())
            {
                this.ApplyEvent(new VariablesEnabled(enablementChanges.VariablesToBeEnabled.ToArray()));
            }
        }

        private void ApplyValidityChangesEvents(ValidityChanges validityChanges)
        {
            if (validityChanges != null)
            {
                if (validityChanges.AnswersDeclaredValid.Any())
                {
                    this.ApplyEvent(new AnswersDeclaredValid(validityChanges.AnswersDeclaredValid.ToArray()));
                }

                if (validityChanges.FailedValidationConditionsForQuestions.Any())
                {
                    this.ApplyEvent(new AnswersDeclaredInvalid(validityChanges.FailedValidationConditionsForQuestions));
                }

                if (validityChanges.StaticTextsDeclaredValid.Any())
                {
                    this.ApplyEvent(new StaticTextsDeclaredValid(validityChanges.StaticTextsDeclaredValid.ToArray()));
                }

                if (validityChanges.FailedValidationConditionsForStaticTexts.Any())
                {
                    this.ApplyEvent(new StaticTextsDeclaredInvalid(validityChanges.FailedValidationConditionsForStaticTexts.ToList()));
                }
            }
        }

        private void ApplyAnswersRemovanceEvents(List<Identity> answersToRemove)
        {
            if (answersToRemove.Any())
            {
                this.ApplyEvent(new AnswersRemoved(answersToRemove.ToArray()));
            }
        }

        private void ApplyRostersEvents(params RosterCalculationData[] rosterDatas)
        {
            var rosterInstancesToAdd = this.GetOrderedUnionOfUniqueRosterDataPropertiesByRosterAndNestedRosters(
                d => d.RosterInstancesToAdd, new RosterIdentityComparer(), rosterDatas);

            if (rosterInstancesToAdd.Any())
            {
                AddedRosterInstance[] instances = rosterInstancesToAdd
                    .Select(roster => new AddedRosterInstance(roster.GroupId, roster.OuterRosterVector, roster.RosterInstanceId, roster.SortIndex))
                    .ToArray();

                this.ApplyEvent(new RosterInstancesAdded(instances));
            }

            var rosterInstancesToRemove = this.GetOrderedUnionOfUniqueRosterDataPropertiesByRosterAndNestedRosters(
                d => d.RosterInstancesToRemove, new RosterIdentityComparer(), rosterDatas);

            if (rosterInstancesToRemove.Any())
            {
                RosterInstance[] instances = rosterInstancesToRemove
                    .Select(roster => new RosterInstance(roster.GroupId, roster.OuterRosterVector, roster.RosterInstanceId))
                    .ToArray();

                this.ApplyEvent(new RosterInstancesRemoved(instances));
            }

            var changedRosterRowTitleDtoFromRosterData = CreateChangedRosterRowTitleDtoFromRosterData(rosterDatas);
            if (changedRosterRowTitleDtoFromRosterData.Any())
                this.ApplyEvent(new RosterInstancesTitleChanged(CreateChangedRosterRowTitleDtoFromRosterData(rosterDatas)));

            this.ApplyAnswersRemovanceEvents(this.GetOrderedUnionOfUniqueRosterDataPropertiesByRosterAndNestedRosters(
                d => d.AnswersToRemoveByDecreasedRosterSize, new IdentityComparer(), rosterDatas));
        }

        private ChangedRosterInstanceTitleDto[] CreateChangedRosterRowTitleDtoFromRosterData(params RosterCalculationData[] datas)
        {
            var result = new List<ChangedRosterInstanceTitleDto>();
            foreach (var data in datas)
            {
                if (data.AreTitlesForRosterInstancesSpecified())
                {
                    var rosterRowTitlesChanged = new HashSet<RosterIdentity>(data.RosterInstancesToAdd, new RosterIdentityComparer());
                    if (data.RosterInstancesToChange != null)
                    {
                        foreach (var rosterIdentity in data.RosterInstancesToChange)
                        {
                            rosterRowTitlesChanged.Add(rosterIdentity);
                        }
                    }

                    foreach (var rosterIdentity in rosterRowTitlesChanged)
                    {
                        result.Add(
                            new ChangedRosterInstanceTitleDto(new RosterInstance(rosterIdentity.GroupId, rosterIdentity.OuterRosterVector,
                                rosterIdentity.RosterInstanceId), data.GetRosterInstanceTitle(rosterIdentity.GroupId, rosterIdentity.RosterInstanceId)));
                    }
                }

                foreach (var nestedRosterData in data.RosterInstantiatesFromNestedLevels)
                {
                    result.AddRange(CreateChangedRosterRowTitleDtoFromRosterData(nestedRosterData));
                }
            }
            return result.ToArray();
        }

        private List<T> GetOrderedUnionOfUniqueRosterDataPropertiesByRosterAndNestedRosters<T>(Func<RosterCalculationData, IEnumerable<T>> getProperty, IEqualityComparer<T> equalityComparer, params RosterCalculationData[] datas)
        {
            var result = new OrderedAdditiveSet<T>(equalityComparer);
            foreach (var data in datas)
            {
                var propertyValue = getProperty(data);

                propertyValue?.ForEach(x => result.Add(x));

                foreach (var rosterInstantiatesFromNestedLevel in data.RosterInstantiatesFromNestedLevels)
                {
                    this.GetOrderedUnionOfUniqueRosterDataPropertiesByRosterAndNestedRosters(getProperty, equalityComparer, rosterInstantiatesFromNestedLevel).ForEach(x => result.Add(x));
                }
            }

            return result.ToList();
        }

        #endregion

        #region Calculations

        // triggers roster

        private Dictionary<Identity, RosterVector[]> GetLinkedQuestionOptionsChanges(
            ILatestInterviewExpressionState interviewExpressionState,
            InterviewStateDependentOnAnswers updatedState,
            IQuestionnaire questionnaire)
        {
            if (!interviewExpressionState.AreLinkedQuestionsSupported())
                return this.CalculateLinkedQuestionOptionsChangesWithLogicBeforeV7(updatedState, questionnaire);

            var processLinkedQuestionFilters = interviewExpressionState.ProcessLinkedQuestionFilters();

            if (processLinkedQuestionFilters == null)
                return new Dictionary<Identity, RosterVector[]>();

            if (processLinkedQuestionFilters.LinkedQuestionOptions.Count == 0)
                return processLinkedQuestionFilters.LinkedQuestionOptionsSet;

            //old v7 options handling 
            var linkedOptions = new Dictionary<Identity, RosterVector[]>();

            foreach (var linkedQuestionOption in processLinkedQuestionFilters.LinkedQuestionOptions)
            {
                IEnumerable<Identity> linkedQuestionInstances =
                    this.GetInstancesOfEntitiesWithSameAndDeeperRosterLevelOrThrow(updatedState, linkedQuestionOption.Key, new decimal[0], questionnaire);
                linkedQuestionInstances.ForEach(x => linkedOptions.Add(x, linkedQuestionOption.Value));
            }

            return linkedOptions;
        }

        private Dictionary<Identity, RosterVector[]> CalculateLinkedQuestionOptionsChangesWithLogicBeforeV7(
            InterviewStateDependentOnAnswers updatedState,
            IQuestionnaire questionnaire)
        {
            var questionsLinkedOnRoster = questionnaire.GetQuestionsLinkedToRoster();
            var questionsLinkedOnQuestion = questionnaire.GetQuestionsLinkedToQuestion();
            if (!questionsLinkedOnRoster.Any() && !questionsLinkedOnQuestion.Any())
                return new Dictionary<Identity, RosterVector[]>();

            var result = new Dictionary<Identity, RosterVector[]>();
            foreach (var questionLinkedOnRoster in questionsLinkedOnRoster)
            {
                var rosterId = questionnaire.GetRosterReferencedByLinkedQuestion(questionLinkedOnRoster);
                IEnumerable<Identity> targetRosters =
                    this.GetInstancesOfGroupsWithSameAndDeeperRosterLevelOrThrow(updatedState,
                        new[] { rosterId }, new decimal[0], questionnaire).ToArray();

                var optionRosterVectors =
                    targetRosters.Where(
                        r =>
                            !updatedState.IsGroupDisabled(r) && !string.IsNullOrEmpty(updatedState.GetRosterTitle(r.Id, r.RosterVector)))
                        .Select(r => r.RosterVector)
                        .ToArray();

                IEnumerable<Identity> linkedQuestionInstances =
                    this.GetInstancesOfEntitiesWithSameAndDeeperRosterLevelOrThrow(updatedState, questionLinkedOnRoster, new decimal[0], questionnaire);

                foreach (var linkedQuestionInstance in linkedQuestionInstances)
                {
                    result.Add(linkedQuestionInstance, optionRosterVectors);
                }
            }

            foreach (var questionLinkedOnQuestion in questionsLinkedOnQuestion)
            {
                var referencedQuestionId = questionnaire.GetQuestionReferencedByLinkedQuestion(questionLinkedOnQuestion);
                IEnumerable<Identity> targetQuestions =
                    this.GetInstancesOfEntitiesWithSameAndDeeperRosterLevelOrThrow(updatedState,
                        referencedQuestionId, new decimal[0], questionnaire);

                var optionRosterVectors =
                    targetQuestions.Where(q => !updatedState.IsQuestionDisabled(q) && updatedState.GetAnswerSupportedInExpressions(q) != null)
                        .Select(q => q.RosterVector)
                        .ToArray();

                IEnumerable<Identity> linkedQuestionInstances =
                   this.GetInstancesOfEntitiesWithSameAndDeeperRosterLevelOrThrow(updatedState, questionLinkedOnQuestion, new decimal[0], questionnaire);

                foreach (var linkedQuestionInstance in linkedQuestionInstances)
                {
                    result.Add(linkedQuestionInstance, optionRosterVectors);
                }
            }
            return result;
        }

        protected IEnumerable<ChangedLinkedOptions> CreateChangedLinkedOptions(
            ILatestInterviewExpressionState interviewExpressionState,
            InterviewStateDependentOnAnswers currentState,
            IQuestionnaire questionnaire,
            List<AnswerChange> interviewByAnswerChanges,
            EnablementChanges enablementChanges,
            RosterCalculationData rosterCalculationData,
            Dictionary<Identity, string> rosterInstancesWithAffectedTitles)
        {
            var currentLinkedOptions = currentState.LinkedQuestionOptions;

            var updatedState = currentState.Clone();

            if (enablementChanges != null)
                updatedState.ApplyEnablementChanges(enablementChanges);

            if (rosterCalculationData != null)
                updatedState.ApplyRosterData(rosterCalculationData);

            if (rosterInstancesWithAffectedTitles != null)
            {
                updatedState.ChangeRosterTitles(
                    rosterInstancesWithAffectedTitles.Select(
                        r =>
                            new ChangedRosterInstanceTitleDto(
                                new RosterInstance(r.Key.Id, r.Key.RosterVector.WithoutLast().ToArray(), r.Key.RosterVector.Last()), r.Value)).ToArray());
            }
            if (interviewByAnswerChanges != null)
            {
                foreach (var interviewByAnswerChange in interviewByAnswerChanges)
                {
                    string questionKey =
                        ConversionHelper.ConvertIdAndRosterVectorToString(interviewByAnswerChange.QuestionId,
                            interviewByAnswerChange.RosterVector);
                    updatedState.AnswersSupportedInExpressions[questionKey] = interviewByAnswerChange.Answer;
                    updatedState.AnsweredQuestions.Add(questionKey);
                }
            }
            var newCurrentLinkedOptions = GetLinkedQuestionOptionsChanges(interviewExpressionState, updatedState, questionnaire);

            foreach (var linkedQuestionConditionalExecutionResult in newCurrentLinkedOptions)
            {
                Identity instanceOfTheLinkedQuestionsQuestions = linkedQuestionConditionalExecutionResult.Key;
                RosterVector[] optionsForLinkedQuestion = linkedQuestionConditionalExecutionResult.Value;

                var linkedQuestionId = instanceOfTheLinkedQuestionsQuestions.Id;
                var referencedEntityId = questionnaire.IsQuestionLinkedToRoster(linkedQuestionId)
                    ? questionnaire.GetRosterReferencedByLinkedQuestion(linkedQuestionId)
                    : questionnaire.GetQuestionReferencedByLinkedQuestion(linkedQuestionId);

                var rosterVectorToStartFrom = this.CalculateStartRosterVectorForAnswersOfLinkedToQuestion(referencedEntityId, instanceOfTheLinkedQuestionsQuestions, questionnaire);

                var changedOptionAvaliableForInstanceOfTheQuestion = optionsForLinkedQuestion.Where(o => rosterVectorToStartFrom.SequenceEqual(o.Take(rosterVectorToStartFrom.Length))).ToArray();

                var questionIdentity = new Identity(instanceOfTheLinkedQuestionsQuestions.Id, instanceOfTheLinkedQuestionsQuestions.RosterVector);
                if (!currentLinkedOptions.ContainsKey(questionIdentity))
                {
                    yield return new ChangedLinkedOptions(instanceOfTheLinkedQuestionsQuestions, changedOptionAvaliableForInstanceOfTheQuestion);
                    continue;
                }

                var presentLinkedOptions = currentLinkedOptions[questionIdentity];

                bool hasNumberOfOptionsChanged = presentLinkedOptions.Length !=
                                                changedOptionAvaliableForInstanceOfTheQuestion.Length;

                bool doesNewOptionsListContainOptionsWhichWasNotPresentBefore =
                    changedOptionAvaliableForInstanceOfTheQuestion.Any(o => !presentLinkedOptions.Contains(o));

                if (hasNumberOfOptionsChanged || doesNewOptionsListContainOptionsWhichWasNotPresentBefore)
                    yield return new ChangedLinkedOptions(instanceOfTheLinkedQuestionsQuestions, changedOptionAvaliableForInstanceOfTheQuestion);
            }
        }


        protected decimal[] CalculateStartRosterVectorForAnswersOfLinkedToQuestion(
            Guid linkedToEntityId, Identity linkedQuestion, IQuestionnaire questionnaire)
        {
            Guid[] linkSourceRosterVector = questionnaire.GetRosterSizeSourcesForEntity(linkedToEntityId);
            Guid[] linkedQuestionRosterSources = questionnaire.GetRosterSizeSourcesForEntity(linkedQuestion.Id);

            int commonRosterSourcesPartLength = Enumerable
                .Zip(linkSourceRosterVector, linkedQuestionRosterSources, (a, b) => a == b)
                .TakeWhile(areEqual => areEqual)
                .Count();

            int linkedQuestionRosterLevel = linkedQuestion.RosterVector.Length;

            int targetRosterLevel = Math.Min(commonRosterSourcesPartLength, Math.Min(linkSourceRosterVector.Length - 1, linkedQuestionRosterLevel));

            return linkedQuestion.RosterVector.Shrink(targetRosterLevel);
        }

        private static List<RosterCalculationData> CalculateRostersFromInterviewSynchronizationDto(InterviewSynchronizationDto interviewDto)
        {
            return interviewDto
                .RosterGroupInstances
                .Select(rosterPairDto => CalculateRosterDataFromSingleRosterInstancesSynchronizationDto(rosterPairDto.Value))
                .ToList();
        }

        private static RosterCalculationData CalculateRosterDataFromSingleRosterInstancesSynchronizationDto(
            RosterSynchronizationDto[] rosterInstancesDto)
        {
            List<RosterIdentity> rosterInstancesToAdd = rosterInstancesDto
                .Select(
                    instanceDto =>
                        new RosterIdentity(instanceDto.RosterId, instanceDto.OuterScopeRosterVector, instanceDto.RosterInstanceId,
                            instanceDto.SortIndex))
                .ToList();

            Dictionary<decimal, string> titlesForRosterInstancesToAdd = rosterInstancesDto.ToDictionary(
                dtoInstance => dtoInstance.RosterInstanceId,
                dtoInstance => dtoInstance.RosterTitle);

            return new RosterCalculationData(rosterInstancesToAdd, titlesForRosterInstancesToAdd);
        }

        #endregion

        protected IQuestionnaire GetQuestionnaireOrThrow(Guid id, long version, string language)
        {
            IQuestionnaire questionnaire = this.questionnaireRepository.GetQuestionnaire(new QuestionnaireIdentity(id, version), language);

            if (questionnaire == null)
                throw new InterviewException($"Questionnaire '{new QuestionnaireIdentity(id, version)}' was not found. InterviewId {EventSourceId}", InterviewDomainExceptionType.QuestionnaireIsMissing);

            return questionnaire;
        }

        private void ValidatePrefilledQuestions(IQuestionnaire questionnaire, Dictionary<Guid, object> answersToFeaturedQuestions,
            RosterVector rosterVector = null, InterviewStateDependentOnAnswers currentInterviewState = null, bool applyStrongChecks = true)
        {
            var currentRosterVector = rosterVector ?? (decimal[])RosterVector.Empty;
            foreach (KeyValuePair<Guid, object> answerToFeaturedQuestion in answersToFeaturedQuestions)
            {
                Guid questionId = answerToFeaturedQuestion.Key;
                object answer = answerToFeaturedQuestion.Value;

                var answeredQuestion = new Identity(questionId, currentRosterVector);

                QuestionType questionType = questionnaire.GetQuestionType(questionId);

                switch (questionType)
                {
                    case QuestionType.Text:
                        this.CheckTextQuestionInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, this.BuildInterviewTree(questionnaire, currentInterviewState), applyStrongChecks);
                        break;

                    case QuestionType.AutoPropagate:
                        this.CheckNumericIntegerQuestionInvariants(questionId, currentRosterVector, (int)answer, questionnaire,
                            answeredQuestion, this.BuildInterviewTree(questionnaire, currentInterviewState), applyStrongChecks);
                        break;
                    case QuestionType.Numeric:
                        if (questionnaire.IsQuestionInteger(questionId))
                            this.CheckNumericIntegerQuestionInvariants(questionId, currentRosterVector, (int)answer, questionnaire,
                                answeredQuestion, this.BuildInterviewTree(questionnaire, currentInterviewState), applyStrongChecks);
                        else
                            this.CheckNumericRealQuestionInvariants(questionId, currentRosterVector, (decimal)answer, questionnaire,
                                answeredQuestion, currentInterviewState, applyStrongChecks);
                        break;

                    case QuestionType.DateTime:
                        this.CheckDateTimeQuestionInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion,
                            currentInterviewState, applyStrongChecks);
                        break;

                    case QuestionType.SingleOption:
                        this.CheckSingleOptionQuestionInvariants(questionId, currentRosterVector, (decimal)answer, questionnaire,
                            answeredQuestion, this.BuildInterviewTree(questionnaire, currentInterviewState), applyStrongChecks);
                        break;

                    case QuestionType.MultyOption:
                        if (questionnaire.IsQuestionYesNo(questionId))
                        {
                            this.CheckYesNoQuestionInvariants(new Identity(questionId, currentRosterVector), (AnsweredYesNoOption[])answer, questionnaire, this.BuildInterviewTree(questionnaire, currentInterviewState), applyStrongChecks);
                        }
                        else
                        {
                            this.CheckMultipleOptionQuestionInvariants(questionId, currentRosterVector, (decimal[])answer, questionnaire, answeredQuestion, this.BuildInterviewTree(questionnaire, currentInterviewState), applyStrongChecks);
                        }
                        break;
                    case QuestionType.QRBarcode:
                        this.CheckQRBarcodeInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, currentInterviewState, applyStrongChecks);
                        break;
                    case QuestionType.GpsCoordinates:
                        this.CheckGpsCoordinatesInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, currentInterviewState, applyStrongChecks);
                        break;
                    case QuestionType.TextList:
                        this.CheckTextListInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, currentInterviewState,
                            (Tuple<decimal, string>[])answer, applyStrongChecks);
                        break;

                    default:
                        throw new InterviewException(string.Format(
                            "Question {0} has type {1} which is not supported as initial pre-filled question. InterviewId: {2}",
                            questionId, questionType, this.EventSourceId));
                }
            }
        }

        public virtual IEnumerable<CategoricalOption> GetFilteredOptionsForQuestion(Identity question, int? parentQuestionValue, string filter)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);
            var filteredOptions = questionnaire.GetOptionsForQuestion(question.Id, parentQuestionValue, filter);

            if (questionnaire.IsSupportFilteringForOptions(question.Id))
                return this.ExpressionProcessorStatePrototype.FilterOptionsForQuestion(question, filteredOptions);
            else
                return filteredOptions;
        }

        public CategoricalOption GetOptionForQuestionWithoutFilter(Identity question, int value, int? parentQuestionValue = null)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);

            return questionnaire.GetOptionsForQuestion(question.Id, parentQuestionValue, string.Empty).FirstOrDefault(x => x.Value == value);
        }

        public CategoricalOption GetOptionForQuestionWithFilter(Identity question, string optionText, int? parentQuestionValue = null)
        {
            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);
            var filteredOption = questionnaire.GetOptionForQuestionByOptionText(question.Id, optionText);

            if (filteredOption == null)
                return null;

            if (questionnaire.IsSupportFilteringForOptions(question.Id))
                return this.ExpressionProcessorStatePrototype.FilterOptionsForQuestion(question, Enumerable.Repeat(filteredOption, 1)).SingleOrDefault();
            else
                return filteredOption;
        }

        protected bool HasInvalidAnswers() => this.interviewState.InvalidAnsweredQuestions.Any(x => !this.interviewState.DisabledQuestions.Contains(ConversionHelper.ConvertIdentityToString(x.Key)));
        protected bool HasInvalidStaticTexts => this.interviewState.InvalidStaticTexts.Any(x => !this.interviewState.DisabledStaticTexts.Contains(x.Key));

        protected InterviewTree BuildInterviewTree(IQuestionnaire questionnaire, InterviewStateDependentOnAnswers interviewState = null)
        {
            var sections = this.BuildInterviewTreeSections(questionnaire, interviewState).ToList();

            return new InterviewTree(this.EventSourceId, sections);
        }

        private IEnumerable<InterviewTreeSection> BuildInterviewTreeSections(IQuestionnaire questionnaire, InterviewStateDependentOnAnswers interviewState)
        {
            var sectionIds = questionnaire.GetAllSections();

            foreach (var sectionId in sectionIds)
            {
                var sectionIdentity = new Identity(sectionId, RosterVector.Empty);
                var section = this.BuildInterviewTreeSection(sectionIdentity, questionnaire, interviewState);

                yield return section;
            }
        }

        private InterviewTreeSection BuildInterviewTreeSection(Identity sectionIdentity, IQuestionnaire questionnaire, InterviewStateDependentOnAnswers interviewState)
        {
            interviewState = interviewState ?? this.interviewState;

            var children = BuildInterviewTreeGroupChildren(sectionIdentity, questionnaire, interviewState).ToList();
            bool isDisabled = interviewState.IsGroupDisabled(sectionIdentity);

            return new InterviewTreeSection(sectionIdentity, children, isDisabled: isDisabled);
        }

        private InterviewTreeSubSection BuildInterviewTreeSubSection(Identity groupIdentity, IQuestionnaire questionnaire, InterviewStateDependentOnAnswers interviewState)
        {
            var children = BuildInterviewTreeGroupChildren(groupIdentity, questionnaire, interviewState).ToList();
            bool isDisabled = interviewState.IsGroupDisabled(groupIdentity);

            return new InterviewTreeSubSection(groupIdentity, children, isDisabled: isDisabled);
        }

        private InterviewTreeRoster BuildInterviewTreeRoster(Identity rosterIdentity, IQuestionnaire questionnaire, InterviewStateDependentOnAnswers interviewState)
        {
            var children = BuildInterviewTreeGroupChildren(rosterIdentity, questionnaire, interviewState).ToList();
            bool isDisabled = interviewState.IsGroupDisabled(rosterIdentity);
            string rosterGroupKey = ConversionHelper.ConvertIdAndRosterVectorToString(rosterIdentity.Id, rosterIdentity.RosterVector);
            string rosterTitle = this.interviewState.RosterTitles.ContainsKey(rosterGroupKey) 
                ? this.interviewState.RosterTitles[rosterGroupKey]
                : null;

            var rosterTitleQuestionId = questionnaire.GetRosterTitleQuestionId(rosterIdentity.Id);
            Identity rosterTitleQuestionIdentity = null;
            if (rosterTitleQuestionId.HasValue)
                rosterTitleQuestionIdentity = new Identity(rosterTitleQuestionId.Value, rosterIdentity.RosterVector);
            RosterType rosterType = RosterType.Fixed;
            Guid? sourceQuestionId = null;
            if (questionnaire.IsFixedRoster(rosterIdentity.Id))
                rosterType = RosterType.Fixed;
            else
            {
                sourceQuestionId = questionnaire.GetRosterSizeQuestion(rosterIdentity.Id);
                var questionaType = questionnaire.GetQuestionType(sourceQuestionId.Value);
                switch (questionaType)
                {
                    case QuestionType.MultyOption:
                        rosterType = questionnaire.IsQuestionYesNo(sourceQuestionId.Value) ? RosterType.YesNo : RosterType.Multi;
                        break;
                    case QuestionType.Numeric:
                        rosterType = RosterType.Numeric;
                        break;
                    case QuestionType.TextList:
                        rosterType = RosterType.List;
                        break;
                }
            }

            return new InterviewTreeRoster(rosterIdentity, children,
                rosterType: rosterType, 
                isDisabled: isDisabled, 
                rosterTitle: rosterTitle,
                rosterTitleQuestionIdentity: rosterTitleQuestionIdentity,
                rosterSizeQuestion: sourceQuestionId);
        }

        private static InterviewTreeQuestion BuildInterviewTreeQuestion(Identity questionIdentity, object answer, bool isQuestionDisabled, IReadOnlyCollection<RosterVector> linkedOptions, IQuestionnaire questionnaire)
        {
            QuestionType questionType = questionnaire.GetQuestionType(questionIdentity.Id);
            bool isDisabled = isQuestionDisabled;
            string title = questionnaire.GetQuestionTitle(questionIdentity.Id);
            string variableName = questionnaire.GetQuestionVariableName(questionIdentity.Id);
            bool isYesNoQuestion = questionnaire.IsQuestionYesNo(questionIdentity.Id);
            bool isDecimalQuestion = !questionnaire.IsQuestionInteger(questionIdentity.Id);
            bool isLinkedQuestion = questionnaire.IsQuestionLinked(questionIdentity.Id) || questionnaire.IsQuestionLinkedToRoster(questionIdentity.Id);
            var linkedSourceEntityId = isLinkedQuestion ?
                (questionnaire.IsQuestionLinked(questionIdentity.Id)
                  ? questionnaire.GetQuestionReferencedByLinkedQuestion(questionIdentity.Id)
                  : questionnaire.GetRosterReferencedByLinkedQuestion(questionIdentity.Id))
                  : (Guid?)null;

            Guid? commonParentRosterIdForLinkedQuestion = isLinkedQuestion ? questionnaire.GetCommontParentForLinkedQuestionAndItSource(questionIdentity.Id) : null;
            Identity commonParentIdentity = null;
            if (isLinkedQuestion && commonParentRosterIdForLinkedQuestion.HasValue)
            {
                var level = questionnaire.GetRosterLevelForEntity(commonParentRosterIdForLinkedQuestion.Value);
                var commonParentRosterVector = questionIdentity.RosterVector.Take(level).ToArray();
                commonParentIdentity = new Identity(commonParentRosterIdForLinkedQuestion.Value, commonParentRosterVector);
            }

            Guid ? cascadingParentQuestionId = questionnaire.GetCascadingQuestionParentId(questionIdentity.Id);
            var cascadingParentQuestionIdentity = cascadingParentQuestionId.HasValue
                ? GetInstanceOfQuestionWithSameAndUpperRosterLevelOrThrow(cascadingParentQuestionId.Value, questionIdentity.RosterVector, questionnaire)
                : null;

            return new InterviewTreeQuestion(
                questionIdentity,
                questionType: questionType,
                isDisabled: isDisabled,
                title: title,
                variableName: variableName,
                answer: answer,
                linkedOptions: linkedOptions,
                cascadingParentQuestionIdentity: cascadingParentQuestionIdentity,
                isYesNo: isYesNoQuestion,
                isDecimal: isDecimalQuestion,
                linkedSourceId: linkedSourceEntityId,
                commonParentRosterIdForLinkedQuestion: commonParentIdentity);
        }

        private static InterviewTreeStaticText BuildInterviewTreeStaticText(Identity staticTextIdentity, IQuestionnaire questionnaire, InterviewStateDependentOnAnswers interviewState)
        {
            bool isDisabled = interviewState.IsStaticTextDisabled(staticTextIdentity);

            var interviewStaticText =  new InterviewTreeStaticText(staticTextIdentity, isDisabled);
            if (interviewState.InvalidStaticTexts.ContainsKey(staticTextIdentity))
                interviewStaticText.MarkAsInvalid(interviewState.InvalidStaticTexts[staticTextIdentity]);
            return interviewStaticText;
        }

        private IEnumerable<IInterviewTreeNode> BuildInterviewTreeGroupChildren(Identity groupIdentity, IQuestionnaire questionnaire, InterviewStateDependentOnAnswers interviewState)
        {
            var childIds = questionnaire.GetChildEntityIds(groupIdentity.Id);

            foreach (var childId in childIds)
            {
                if (questionnaire.IsRosterGroup(childId))
                {
                    Guid[] rostersStartingFromTop = questionnaire.GetRostersFromTopToSpecifiedGroup(childId).ToArray();

                    IEnumerable<RosterVector> childRosterVectors = ExtendRosterVector(
                        interviewState, groupIdentity.RosterVector, rostersStartingFromTop.Length, rostersStartingFromTop);

                    foreach (var childRosterVector in childRosterVectors)
                    {
                        var childRosterIdentity = new Identity(childId, childRosterVector);
                        yield return BuildInterviewTreeRoster(childRosterIdentity, questionnaire, interviewState);
                    }
                }
                else if (questionnaire.HasGroup(childId))
                {
                    var childGroupIdentity = new Identity(childId, groupIdentity.RosterVector);
                    yield return BuildInterviewTreeSubSection(childGroupIdentity, questionnaire, interviewState);
                }
                else if (questionnaire.HasQuestion(childId))
                {
                    var childQuestionIdentity = new Identity(childId, groupIdentity.RosterVector);
                    var answer = interviewState.GetAnswer(childQuestionIdentity);

                    var interviewTreeQuestion = BuildInterviewTreeQuestion(childQuestionIdentity, answer,
                        interviewState.IsQuestionDisabled(childQuestionIdentity),
                        interviewState.GetOptionsForLinkedQuestion(childQuestionIdentity), questionnaire);

                    if (interviewState.InvalidAnsweredQuestions.ContainsKey(childQuestionIdentity))
                        interviewTreeQuestion.MarkAsInvalid(interviewState.InvalidAnsweredQuestions[childQuestionIdentity]);

                    yield return interviewTreeQuestion;
                }
                else if (questionnaire.IsStaticText(childId))
                {
                    var childStaticTextIdentity = new Identity(childId, groupIdentity.RosterVector);
                    yield return BuildInterviewTreeStaticText(childStaticTextIdentity, questionnaire, interviewState);
                }
                else if (questionnaire.IsVariable(childId))
                {
                    var childVariableIdentity = new Identity(childId, groupIdentity.RosterVector);
                    bool isDisabled = interviewState.IsVariableDisabled(childVariableIdentity);
                    yield return new InterviewTreeVariable(childVariableIdentity, isDisabled, null);
                }
            }
        }

        private void UpdateTreeWithVariableChanges(InterviewTree tree, VariableValueChanges variableValueChanges)
            => variableValueChanges?.ChangedVariableValues.ForEach(x => tree.GetVariable(x.Key).SetValue(x.Value));

        private void UpdateTreeWithValidationChanges(InterviewTree tree, ValidityChanges validationChanges)
        {
            if (validationChanges == null) return; // can be in tests only.

            validationChanges.AnswersDeclaredValid.ForEach(x => tree.GetQuestion(x).MarkAsValid());
            validationChanges.AnswersDeclaredInvalid.ForEach(x => tree.GetQuestion(x).MarkAsInvalid(new FailedValidationCondition(0).ToEnumerable()));
            validationChanges.FailedValidationConditionsForQuestions.ForEach(x => tree.GetQuestion(x.Key).MarkAsInvalid(x.Value));

            validationChanges.StaticTextsDeclaredValid.ForEach(x => tree.GetStaticText(x).MarkAsValid());
            validationChanges.FailedValidationConditionsForStaticTexts.ForEach(x => tree.GetStaticText(x.Key).MarkAsInvalid(x.Value));
        }

        private void UpdateTreeWithEnablementChanges(InterviewTree tree, EnablementChanges enablementChanges)
        {
            if (enablementChanges == null) return; // can be in tests only.

            enablementChanges.QuestionsToBeDisabled.ForEach(x => tree.GetQuestion(x).Disable());
            enablementChanges.QuestionsToBeEnabled.ForEach(x => tree.GetQuestion(x).Enable());

            enablementChanges.GroupsToBeDisabled.ForEach(x => tree.GetGroup(x).Disable());
            enablementChanges.GroupsToBeEnabled.ForEach(x => tree.GetGroup(x).Enable());

            enablementChanges.StaticTextsToBeDisabled.ForEach(x => tree.GetStaticText(x).Disable());
            enablementChanges.StaticTextsToBeEnabled.ForEach(x => tree.GetStaticText(x).Enable());

            enablementChanges.VariablesToBeDisabled.ForEach(x => tree.GetVariable(x).Disable());
            enablementChanges.VariablesToBeEnabled.ForEach(x => tree.GetVariable(x).Enable());
        }

        private void UpdateRosterTitles(InterviewTree tree, IQuestionnaire questionnaire)
        {
            foreach (var roster in tree.FindRosters().Where(x => x.IsList))
            {
                roster.UpdateRosterTitle();
            }

            foreach (var roster in tree.FindRosters().Where(x => x.IsNumeric))
            {
                var titleQuestion = tree.GetQuestion(roster.AsNumeric.RosterTitleQuestionIdentity);
                if (titleQuestion == null) continue;
                var rosterTitle = titleQuestion.IsAnswered()
                    ? titleQuestion.GetAnswerAsString(answerOptionValue => questionnaire.GetOptionsForQuestion(titleQuestion.Identity.Id, null, string.Empty).FirstOrDefault(x => x.Value == Convert.ToInt32(answerOptionValue)).Title)
                    : null;
                roster.SetRosterTitle(rosterTitle);
                //roster.UpdateRosterTitle(answerOptionValue => questionnaire.GetOptionsForQuestion(titleQuestion.Identity.Id, null, string.Empty).FirstOrDefault(x => x.Value == Convert.ToInt32(answerOptionValue)).Title);
            }
        }

        private void UpdateLinkedQuestions(InterviewTree tree, ILatestInterviewExpressionState interviewExpressionState)
        {
            if (!interviewExpressionState.AreLinkedQuestionsSupported())
            {
                var linkedQuestions = tree.FindQuestions().Where(x => x.IsLinked);
                foreach (InterviewTreeQuestion linkedQuestion in linkedQuestions)
                {
                    linkedQuestion.CalculateLinkedOptions();
                }
            }
            else
            {
                var processLinkedQuestionFilters = interviewExpressionState.ProcessLinkedQuestionFilters();
                foreach (var linkedQuestionWithOptions in processLinkedQuestionFilters.LinkedQuestionOptions)
                {
                    tree.FindQuestions(linkedQuestionWithOptions.Key).ForEach(x => x.AsLinked.SetOptions(linkedQuestionWithOptions.Value));
                }
                foreach (var linkedQuestionWithOptions in processLinkedQuestionFilters.LinkedQuestionOptionsSet)
                {
                    var linkedQuestion = tree.GetQuestion(linkedQuestionWithOptions.Key);
                    linkedQuestion.UpdateLinkedOptionsAndResetAnswerIfNeeded(linkedQuestionWithOptions.Value);
                }
            }
        }

        private void UpdateTreeWithStructuralChanges(InterviewTree tree, StructuralChanges structuralChanges)
        {
            foreach (var changedMultiQuestion in structuralChanges.ChangedMultiQuestions)
            {
                tree.GetQuestion(changedMultiQuestion.Key).AsMultiOption.SetAnswer(changedMultiQuestion.Value);
            }

            foreach (var changedSingleQuestion in structuralChanges.ChangedSingleQuestions)
            {
                var question = tree.GetQuestion(changedSingleQuestion.Key).AsSingleOption;
                if (changedSingleQuestion.Value.HasValue)
                    question.SetAnswer(changedSingleQuestion.Value.Value);
                else
                    question.RemoveAnswer();
            }

            foreach (var changedYesNoQuestion in structuralChanges.ChangedYesNoQuestions)
            {
                tree.GetQuestion(changedYesNoQuestion.Key).AsYesNo.SetAnswer(changedYesNoQuestion.Value);
            }

            foreach (var removedRosterIdentity in structuralChanges.RemovedRosters)
            {
                tree.RemoveNode(removedRosterIdentity);
            }
        }

        private void UpdateTree(InterviewTree tree, IQuestionnaire questionnaire)
        {
            var itemsQueue = new Queue<IInterviewTreeNode>(tree.Sections);

            while (itemsQueue.Count > 0)
            {
                var currentItem = itemsQueue.Dequeue();

                if (currentItem is InterviewTreeGroup)
                {
                    var currentGroup = currentItem as InterviewTreeGroup;
                    var parentRosterVector = currentGroup.Identity.RosterVector;
                    var childEntityIds = questionnaire.GetChildEntityIds(currentItem.Identity.Id);

                    foreach (var childEntityId in childEntityIds)
                    {
                        if (questionnaire.IsRosterGroup(childEntityId))
                        {
                            Guid rosterId = childEntityId;
                            List<RosterNodeDescriptor> expectedRosterIdentitiesWithTitles = new List<RosterNodeDescriptor>();
                            if (questionnaire.IsFixedRoster(childEntityId))
                            {
                                var rosterTitles = questionnaire.GetFixedRosterTitles(childEntityId);

                                expectedRosterIdentitiesWithTitles =
                                    rosterTitles
                                    .Select((x, i) => new RosterNodeDescriptor
                                    {
                                        Identity = new RosterIdentity(childEntityId, parentRosterVector, x.Value).ToIdentity(),
                                        Title = x.Title,
                                        Type = RosterType.Fixed
                                    }).ToList();
                            }
                            else
                            {
                                Guid sourceQuestionId = questionnaire.GetRosterSizeQuestion(rosterId);
                                var questionaType = questionnaire.GetQuestionType(sourceQuestionId);
                                var rosterSizeQuestion = currentGroup.GetQuestionFromThisOrUpperLevel(sourceQuestionId);
                                switch (questionaType)
                                {
                                    case QuestionType.MultyOption:
                                        if (questionnaire.IsQuestionYesNo(sourceQuestionId))
                                        {
                                            var newYesNoAnswer = rosterSizeQuestion.AsYesNo.IsAnswered ? rosterSizeQuestion.AsYesNo.GetAnswer() : new AnsweredYesNoOption[0];
                                            expectedRosterIdentitiesWithTitles = CreateRosterIdentitiesForYesNoQuestion(newYesNoAnswer, rosterId, parentRosterVector, rosterSizeQuestion, questionnaire);
                                        }
                                        else
                                        {
                                            var newMultiAnswer = rosterSizeQuestion.AsMultiOption.IsAnswered ? rosterSizeQuestion.AsMultiOption.GetAnswer() : new decimal[0];
                                            expectedRosterIdentitiesWithTitles = CreateRosterIdentitiesForMultiQuestion(newMultiAnswer, rosterId, parentRosterVector, rosterSizeQuestion, questionnaire);
                                        }
                                        break;
                                    case QuestionType.Numeric:
                                        var rosterTitleQuestionId = questionnaire.GetRosterTitleQuestionId(rosterId);
                                        var integerAnswer = (rosterSizeQuestion != null && rosterSizeQuestion.AsInteger.IsAnswered) ? rosterSizeQuestion.AsInteger.GetAnswer() : 0;
                                        expectedRosterIdentitiesWithTitles = CreateRosterIdentitiesForNumericQuestion
                                            (
                                                integerAnswer,
                                                rosterId,
                                                parentRosterVector,
                                                rosterSizeQuestion,
                                                rosterTitleQuestionId);

                                        break;
                                    case QuestionType.TextList:
                                        var listAnswer = rosterSizeQuestion.AsTextList.IsAnswered ? rosterSizeQuestion.AsTextList.GetAnswer() : new Tuple<decimal, string>[0];
                                        expectedRosterIdentitiesWithTitles = CreateRosterIdentitiesForListQuestion(listAnswer, rosterId, currentItem.Identity.RosterVector, rosterSizeQuestion);
                                        break;
                                }
                            }

                            var actualRosterIdentities = currentGroup.Children.Where(x => x.Identity.Id == rosterId).Select(x => x.Identity).ToList();

                            foreach (var actualRosterIdentity in actualRosterIdentities)
                            {
                                if (expectedRosterIdentitiesWithTitles.Any(x => x.Identity.Equals(actualRosterIdentity)))
                                    continue;
                                currentGroup.RemoveChildren(actualRosterIdentity);
                            }

                            for (int index = 0; index < expectedRosterIdentitiesWithTitles.Count; index++)
                            {
                                var rosterIdentityWithTitle = expectedRosterIdentitiesWithTitles[index];
                                if (currentGroup.HasChild(rosterIdentityWithTitle.Identity))
                                    continue;
                                var rosterNode = new InterviewTreeRoster(
                                    rosterIdentityWithTitle.Identity,
                                    Enumerable.Empty<IInterviewTreeNode>(),
                                    isDisabled: false,
                                    sortIndex: index,
                                    rosterTitle: rosterIdentityWithTitle.Title,
                                    rosterType: rosterIdentityWithTitle.Type,
                                    rosterSizeQuestion: rosterIdentityWithTitle.SizeQuestion?.Identity.Id,
                                    rosterTitleQuestionIdentity: rosterIdentityWithTitle.RosterTitleQuestionIdentity);

                                currentGroup.AddChildren(rosterNode);
                                itemsQueue.Enqueue(rosterNode);
                            }
                        }
                        else if (questionnaire.IsSubSection(childEntityId))
                        {
                            var subSectionIdentity = new Identity(childEntityId, parentRosterVector);
                            if (!currentGroup.HasChild(subSectionIdentity))
                            {
                                currentGroup.AddChildren(new InterviewTreeSubSection(subSectionIdentity, Enumerable.Empty<IInterviewTreeNode>(), false));
                            }
                        }
                        else if (questionnaire.IsStaticText(childEntityId))
                        {
                            var staticTextIdentity = new Identity(childEntityId, parentRosterVector);
                            if (!currentGroup.HasChild(staticTextIdentity))
                            {
                                currentGroup.AddChildren(new InterviewTreeStaticText(staticTextIdentity, false));
                            }
                        }
                        else if (questionnaire.IsQuestion(childEntityId))
                        {
                            var questionIdentity = new Identity(childEntityId, parentRosterVector);
                            if (!currentGroup.HasChild(questionIdentity))
                            {
                                currentGroup.AddChildren(BuildInterviewTreeQuestion(questionIdentity, null, false, new RosterVector[0], questionnaire));
                            }
                        }
                        else if (questionnaire.IsVariable(childEntityId))
                        {
                            var variableIdentity = new Identity(childEntityId, parentRosterVector);
                            if (!currentGroup.HasChild(variableIdentity))
                            {
                                currentGroup.AddChildren(new InterviewTreeVariable(variableIdentity, false, null));
                            }
                        }
                    }

                    var childItems = currentItem.Children;

                    if (childItems != null)
                    {
                        foreach (var childItem in childItems)
                        {
                            itemsQueue.Enqueue(childItem);
                        }
                    }
                }
            }
        }

        private void UpdateTreeWithAnswersOnPrefilledQuestions(Dictionary<Identity, object> answersToPrefilledQuestions,
            InterviewTree changedInterviewTree, IQuestionnaire questionnaire)
        {
            foreach (var newAnswer in answersToPrefilledQuestions)
            {
                var questionId = newAnswer.Key.Id;
                var question = changedInterviewTree.GetQuestion(newAnswer.Key);
                var answer = newAnswer.Value;

                QuestionType questionType = questionnaire.GetQuestionType(questionId);
                switch (questionType)
                {
                    case QuestionType.Text:
                        question.AsText.SetAnswer(answer as string);
                        break;
                    case QuestionType.Numeric:
                        if (questionnaire.IsQuestionInteger(questionId))
                            question.AsInteger.SetAnswer((int)answer);
                        else
                            question.AsDouble.SetAnswer(Convert.ToDouble(answer));
                        break;
                    case QuestionType.DateTime:
                        question.AsDateTime.SetAnswer((DateTime)answer);
                        break;
                    case QuestionType.SingleOption:
                        question.AsSingleOption.SetAnswer((int)answer);
                        break;

                    case QuestionType.MultyOption:
                        question.AsMultiOption.SetAnswer((decimal[])answer);
                        break;
                    case QuestionType.QRBarcode:
                        question.AsQRBarcode.SetAnswer(answer as string);
                        break;
                    case QuestionType.GpsCoordinates:
                        question.AsGps.SetAnswer((GeoPosition)answer);
                        break;
                    case QuestionType.TextList:
                        question.AsTextList.SetAnswer((Tuple<decimal, string>[])answer);
                        break;

                    default:
                        throw new InterviewException(string.Format(
                            "Question {0} has type {1} which is not supported as initial pre-filled question. InterviewId: {2}",
                            questionId, questionType, this.EventSourceId));
                }
            }
        }

        private class RosterNodeDescriptor
        {
            public Identity Identity { get; set; }
            public string Title { get; set; }

            public RosterType Type { get; set; }

            public InterviewTreeQuestion SizeQuestion { get; set; }
            public Identity RosterTitleQuestionIdentity { get; set; }
        }

        private static List<RosterNodeDescriptor> CreateRosterIdentitiesForMultiQuestion(decimal[] newMultiAnswer, Guid rosterId, RosterVector parentRosterVector, InterviewTreeQuestion rosterSizeQuestion, IQuestionnaire questionnaire)
        {
            return newMultiAnswer.Select((optionValue, index) => new RosterNodeDescriptor
            {
                Identity = new RosterIdentity(rosterId, parentRosterVector, optionValue, index).ToIdentity(),
                Title = questionnaire.GetAnswerOptionTitle(rosterSizeQuestion.Identity.Id, optionValue),
                Type = RosterType.Multi,
                SizeQuestion = rosterSizeQuestion
            }).ToList();
        }

        private static List<RosterNodeDescriptor> CreateRosterIdentitiesForYesNoQuestion(AnsweredYesNoOption[] newYesNoAnswer, Guid rosterId, RosterVector parentRosterVector, InterviewTreeQuestion rosterSizeQuestion, IQuestionnaire questionnaire)
        {
            return newYesNoAnswer
                .Where(x => x.Yes)
                .Select((selectedYesOption, index) => new RosterNodeDescriptor
                {
                    Identity = new RosterIdentity(rosterId, parentRosterVector, selectedYesOption.OptionValue, index).ToIdentity(),
                    Title = questionnaire.GetAnswerOptionTitle(rosterSizeQuestion.Identity.Id, selectedYesOption.OptionValue),
                    Type = RosterType.YesNo,
                    SizeQuestion = rosterSizeQuestion
                }).ToList();
        }

        private static List<RosterNodeDescriptor> CreateRosterIdentitiesForNumericQuestion(int answer, Guid rosterId, RosterVector parentRosterVector, InterviewTreeQuestion rosterSizeQuestion, Guid? rosterTitleQuestionId)
        {
            return Enumerable.Range(0, answer)
                .Select(index => new RosterIdentity(rosterId, parentRosterVector, index, index))
                .Select(x => new RosterNodeDescriptor
                {
                    Identity = x.ToIdentity(),
                    Title = (x.RosterInstanceId + 1).ToString(CultureInfo.InvariantCulture),
                    Type = RosterType.Numeric,
                    SizeQuestion = rosterSizeQuestion,
                    RosterTitleQuestionIdentity = rosterTitleQuestionId.HasValue ? new Identity(rosterTitleQuestionId.Value, x.ToIdentity().RosterVector) : null
                }).ToList();
        }

        private static List<RosterNodeDescriptor> CreateRosterIdentitiesForListQuestion(Tuple<decimal, string>[] answers, Guid rosterId, RosterVector parentRosterVector, InterviewTreeQuestion rosterSizeQuestion)
        {
            return answers
                .Select(answer => new RosterNodeDescriptor
                {
                    Identity = new RosterIdentity(rosterId, parentRosterVector, answer.Item1, 0).ToIdentity(),
                    Title = answer.Item2,
                    Type = RosterType.List,
                    SizeQuestion = rosterSizeQuestion
                }).ToList();
        }
    }
}