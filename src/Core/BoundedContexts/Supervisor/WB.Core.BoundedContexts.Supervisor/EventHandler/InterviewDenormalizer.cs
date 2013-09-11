﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Ncqrs.Eventing.ServiceModel.Bus;
using Ncqrs.Eventing.ServiceModel.Bus.ViewConstructorEventBus;
using WB.Core.BoundedContexts.Supervisor.Views.Interview;
using WB.Core.BoundedContexts.Supervisor.Views.Questionnaire;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.ReadSide;

namespace WB.Core.BoundedContexts.Supervisor.EventHandler
{
    internal class InterviewDenormalizer : IEventHandler,
                                         IEventHandler<InterviewCreated>,
                                         IEventHandler<InterviewStatusChanged>,
                                         IEventHandler<SupervisorAssigned>,
                                         IEventHandler<InterviewerAssigned>,

        IEventHandler<GroupPropagated>,
        IEventHandler<AnswerCommented>,
        IEventHandler<MultipleOptionsQuestionAnswered>,
        IEventHandler<NumericQuestionAnswered>,
        IEventHandler<TextQuestionAnswered>,
        IEventHandler<SingleOptionQuestionAnswered>,
        IEventHandler<DateTimeQuestionAnswered>,
        IEventHandler<GeoLocationQuestionAnswered>,
        IEventHandler<GroupDisabled>,
        IEventHandler<GroupEnabled>,
        IEventHandler<QuestionDisabled>,
        IEventHandler<QuestionEnabled>,
        IEventHandler<AnswerDeclaredInvalid>,
        IEventHandler<AnswerDeclaredValid>
    {
        private readonly IReadSideRepositoryReader<UserDocument> users;
        private readonly IVersionedReadSideRepositoryReader<QuestionnairePropagationStructure> questionnriePropagationStructures;
        private readonly IReadSideRepositoryWriter<InterviewData> interviews;

        public InterviewDenormalizer(IReadSideRepositoryReader<UserDocument> users, 
                                     IVersionedReadSideRepositoryReader<QuestionnairePropagationStructure> questionnriePropagationStructures, 
                                     IReadSideRepositoryWriter<InterviewData> interviews)
        {
            this.users = users;
            this.questionnriePropagationStructures = questionnriePropagationStructures;
            this.interviews = interviews;
        }

        public void Handle(IPublishedEvent<InterviewCreated> evnt)
        {
            var responsible = this.users.GetById(evnt.Payload.UserId);

            var interview = new InterviewData()
            {
                InterviewId = evnt.EventSourceId,
                UpdateDate = evnt.EventTimeStamp,
                QuestionnaireId = evnt.Payload.QuestionnaireId,
                QuestionnaireVersion = evnt.Payload.QuestionnaireVersion,
                ResponsibleId = evnt.Payload.UserId, // Creator is responsible
                ResponsibleRole = responsible.Roles.FirstOrDefault()
            };
            var emptyVector = new int[0];
            interview.Levels.Add(CreateLevelIdFromPropagationVector(emptyVector), new InterviewLevel(evnt.EventSourceId, emptyVector));
            this.interviews.Store(interview, interview.InterviewId);
        }

        public void Handle(IPublishedEvent<InterviewStatusChanged> evnt)
        {
            var interview = this.interviews.GetById(evnt.EventSourceId);

            interview.Status = evnt.Payload.Status;

            this.interviews.Store(interview, interview.InterviewId);
        }

        public void Handle(IPublishedEvent<SupervisorAssigned> evnt)
        {
            var interview = this.interviews.GetById(evnt.EventSourceId);

            interview.ResponsibleId = evnt.Payload.SupervisorId;
            interview.ResponsibleRole = UserRoles.Supervisor;

            this.interviews.Store(interview, interview.InterviewId);
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        public Type[] UsesViews
        {
            get { return new[] { typeof(UserDocument), typeof(QuestionnairePropagationStructure) }; }
        }
        public Type[] BuildsViews
        {
            get { return new [] { typeof(InterviewData) }; }
        }

        public void Handle(IPublishedEvent<GroupPropagated> evnt)
        {
            var interview = this.interviews.GetById(evnt.EventSourceId);

            var scopeOfCurrentGroup = GetScopeOfPassedGroup(interview,
                                                            evnt.Payload.GroupId);
            /*if (scopeOfCurrentGroup == null)
                return;*/

            var keysOfLevelsByScope =
                GetLevelsByScopeFromInterview(interview: interview, scopeId: scopeOfCurrentGroup);

            var countOfLevelByScope = keysOfLevelsByScope.Count();

            if (evnt.Payload.Count == countOfLevelByScope)
                return;

            if (countOfLevelByScope < evnt.Payload.Count)
            {
                AddNewLevelsToInterview(interview, startIndex: countOfLevelByScope,
                             count: evnt.Payload.Count - countOfLevelByScope,
                             outerVecor: evnt.Payload.OuterScopePropagationVector, scopeId: scopeOfCurrentGroup);
            }
            else
            {
                var keysOfLevelToBeDeleted =
                    keysOfLevelsByScope.Skip(evnt.Payload.Count).Take(evnt.Payload.Count - countOfLevelByScope);
                RemoveLevelsFromInterview(interview, keysOfLevelToBeDeleted);
            }

            this.interviews.Store(interview, interview.InterviewId);
        }

        public void Handle(IPublishedEvent<AnswerCommented> evnt)
        {

            var commenter = this.users.GetById(evnt.Payload.UserId);
            if (commenter == null)
                return;
            SaveComment(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                        evnt.Payload.Comment, evnt.Payload.UserId, commenter.UserName, evnt.Payload.CommentTime);

        }

        public void Handle(IPublishedEvent<MultipleOptionsQuestionAnswered> evnt)
        {
            SaveAnswer(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                       evnt.Payload.SelectedValues);
        }

        public void Handle(IPublishedEvent<NumericQuestionAnswered> evnt)
        {
            SaveAnswer(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                       evnt.Payload.Answer);
        }

        public void Handle(IPublishedEvent<TextQuestionAnswered> evnt)
        {
            SaveAnswer(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                    evnt.Payload.Answer);
        }

        public void Handle(IPublishedEvent<SingleOptionQuestionAnswered> evnt)
        {
            SaveAnswer(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                        evnt.Payload.SelectedValue);
        }

        public void Handle(IPublishedEvent<DateTimeQuestionAnswered> evnt)
        {
            SaveAnswer(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                     evnt.Payload.Answer);
        }

        public void Handle(IPublishedEvent<GeoLocationQuestionAnswered> evnt)
        {
            SaveAnswer(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                       evnt.Payload.Answer);
        }

        public void Handle(IPublishedEvent<GroupDisabled> evnt)
        {
            PreformActionOnLevel(evnt.EventSourceId, evnt.Payload.PropagationVector, (level) =>
                {
                    if (!level.DisabledGroups.Contains(evnt.Payload.GroupId))
                    {
                        level.DisabledGroups.Add(evnt.Payload.GroupId);
                        return true;
                    }
                    return false;
                });
        }

        public void Handle(IPublishedEvent<GroupEnabled> evnt)
        {
            PreformActionOnLevel(evnt.EventSourceId, evnt.Payload.PropagationVector, (level) =>
                {
                    if (level.DisabledGroups.Contains(evnt.Payload.GroupId))
                    {
                        level.DisabledGroups.Remove(evnt.Payload.GroupId);
                        return true;
                    }
                    return false;
                });
        }

        public void Handle(IPublishedEvent<QuestionDisabled> evnt)
        {
            ChangeQuestionConditionState(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                 false);
        }

        public void Handle(IPublishedEvent<QuestionEnabled> evnt)
        {
            ChangeQuestionConditionState(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
                 true);
        }

        public void Handle(IPublishedEvent<AnswerDeclaredInvalid> evnt)
        {
            ChangeQuestionConditionValidity(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
               false);
        }

        public void Handle(IPublishedEvent<AnswerDeclaredValid> evnt)
        {
            ChangeQuestionConditionValidity(evnt.EventSourceId, evnt.Payload.PropagationVector, evnt.Payload.QuestionId,
               true);
        }

        private string CreateLevelIdFromPropagationVector(int[] vector)
        {
            if (vector.Length == 0)
                return "#";
            return string.Join(",", vector);
        }

        private void SaveAnswer(Guid interviewId, int[] vector, Guid questionId, object answer)
        {
            PreformActionOnQuestion(interviewId, vector, questionId, (question) =>
                {
                    question.Answer = answer;
                    return true;
                });
        }

        private void SaveComment(Guid interviewId, int[] vector, Guid questionId, string comment, Guid userId, string userName, DateTime commentTime)
        {
            var interviewQuestionComment = new InterviewQuestionComment()
                {
                    Id = Guid.NewGuid(), 
                    Text = comment, 
                    CommenterId = userId,
                    CommenterName = userName,
                    Date = commentTime
                };

            PreformActionOnQuestion(interviewId, vector, questionId, (question) =>
                {
                    if(question.Comments==null)
                        question.Comments=new List<InterviewQuestionComment>();
                    question.Comments.Add(interviewQuestionComment);
                    return true;
                });
        }

        private void ChangeQuestionConditionState(Guid interviewId, int[] vector, Guid questionId, bool newState)
        {
            PreformActionOnQuestion(interviewId, vector, questionId, (question) =>
                {
                    if (question.Enabled == newState)
                        return false;
                    question.Enabled = newState;
                    return true;
                });
        }

        private void ChangeQuestionConditionValidity(Guid interviewId, int[] vector, Guid questionId, bool valid)
        {
            PreformActionOnQuestion(interviewId, vector, questionId, (question) =>
                {
                    if (question.Valid == valid)
                        return false;
                    question.Valid = valid;
                    return true;
                });
        }

        private void PreformActionOnLevel(Guid interviewId, int[] vector, Func<InterviewLevel, bool> action)
        {
            var interview = this.interviews.GetById(interviewId);
            var levelId = CreateLevelIdFromPropagationVector(vector);
            if(action(interview.Levels[levelId]))
            {
                this.interviews.Store(interview, interview.InterviewId);
            }
        }


        private void PreformActionOnQuestion(Guid interviewId, int[] vector, Guid questionId,
                                             Func<InterviewQuestion, bool> action)
        {
            PreformActionOnLevel(interviewId, vector, (questionsAtTheLevel) =>
                {
                    var answeredQuestion = questionsAtTheLevel.Questions.FirstOrDefault(q => q.Id == questionId);
                    if (answeredQuestion == null)
                    {
                        answeredQuestion = new InterviewQuestion(questionId);
                        questionsAtTheLevel.Questions.Add(answeredQuestion);
                    }
                    answeredQuestion.IsAnswered = true;
                    return action(answeredQuestion);
                });
        }

        private void RemoveLevelFromInterview(InterviewData interview, string levelKey)
        {
            if (interview.Levels.ContainsKey(levelKey))
            {
                interview.Levels.Remove(levelKey);
            }
        }

        private void AddLevelToInterview(InterviewData interview, int[] vector, int index, Guid scopeId)
        {
            var newVecor = CreateNewVector(vector, index);
            var levelKey = CreateLevelIdFromPropagationVector(newVecor);
            if (!interview.Levels.ContainsKey(levelKey))
            {
                interview.Levels.Add(levelKey, new InterviewLevel(scopeId, newVecor));
            }
            else
            {
                var level = interview.Levels[levelKey];
                if (level.ScopeId == scopeId)
                    return;
                AddLevelToInterview(interview, vector, index + 1, scopeId);
            }

        }

        private int[] CreateNewVector(int[] outerScopePropagationVector, int indexInScope)
        {
            var scopeVecor = new int[outerScopePropagationVector.Length + 1];
            outerScopePropagationVector.CopyTo(scopeVecor, 0);
            scopeVecor[scopeVecor.Length - 1] = indexInScope;
            return scopeVecor;
        }

        private Guid GetScopeOfPassedGroup(InterviewData interview, Guid groupId)
        {
            var questionnarie = questionnriePropagationStructures.GetById(interview.QuestionnaireId,interview.QuestionnaireVersion);
            foreach (var scopeId in questionnarie.PropagationScopes.Keys)
            {
                foreach (var trigger in questionnarie.PropagationScopes[scopeId])
                {
                    if (trigger == groupId)
                        return scopeId;
                }
            }
            throw new ArgumentException(string.Format("group {0} is missing in any propagation scope of questionnaire",
                                                      groupId));
        }

        private void RemoveLevelsFromInterview(InterviewData interview, IEnumerable<string> levelKeysForDelete)
        {
            foreach (var levelKey in levelKeysForDelete)
            {
                RemoveLevelFromInterview(interview, levelKey);
            }
        }

        private void AddNewLevelsToInterview(InterviewData interview, int startIndex, int count, int[] outerVecor, Guid scopeId)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                AddLevelToInterview(interview, outerVecor, startIndex, scopeId);
            }
        }

        private List<string> GetLevelsByScopeFromInterview(InterviewData interview, Guid scopeId)
        {
            return interview.Levels.Where(level => level.Value.ScopeId == scopeId)
                            .Select(level => level.Key).ToList();
        }

        public void Handle(IPublishedEvent<InterviewerAssigned> evnt)
        {
            var interview = this.interviews.GetById(evnt.EventSourceId);

            interview.ResponsibleId = evnt.Payload.InterviewerId;
            interview.ResponsibleRole = UserRoles.Operator;

            this.interviews.Store(interview, interview.InterviewId);
        }
    }
}
