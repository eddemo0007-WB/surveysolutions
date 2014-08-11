﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WB.Core.SharedKernels.DataCollection
{
    public abstract class AbstractInterviewExpressionState : IInterviewExpressionState
    {
        public Dictionary<string, IExpressionExecutable> InterviewScopes = new Dictionary<string, IExpressionExecutable>();
        public Dictionary<string, List<string>> SiblingRosters = new Dictionary<string, List<string>>();

        public abstract void AddRoster(Guid rosterId, decimal[] outerRosterVector, decimal rosterInstanceId, int? sortIndex);
        public abstract void RemoveRoster(Guid rosterId, decimal[] rosterVector, decimal rosterInstanceId);

        public abstract void UpdateIntAnswer(Guid questionId, decimal[] rosterVector, long answer);
        public abstract void UpdateDecimalAnswer(Guid questionId, decimal[] rosterVector, decimal answer);
        public abstract void UpdateDateAnswer(Guid questionId, decimal[] rosterVector, DateTime answer);
        public abstract void UpdateTextAnswer(Guid questionId, decimal[] rosterVector, string answer);
        public abstract void UpdateQrBarcodeAnswer(Guid questionId, decimal[] rosterVector, string answer);
        public abstract void UpdateSingleOptionAnswer(Guid questionId, decimal[] rosterVector, decimal answer);
        public abstract void UpdateMultiOptionAnswer(Guid questionId, decimal[] rosterVector, decimal[] answer);

        public abstract void UpdateGeoLocationAnswer(Guid questionId, decimal[] propagationVector, double latitude, double longitude,
            double accuracy);

        public abstract void UpdateTextListAnswer(Guid questionId, decimal[] propagationVector, Tuple<decimal, string>[] answers);

        public abstract void UpdateLinkedSingleOptionAnswer(Guid questionId, decimal[] propagationVector,
            decimal[] selectedPropagationVector);

        public abstract void UpdateLinkedMultiOptionAnswer(Guid questionId, decimal[] propagationVector,
            decimal[][] selectedPropagationVectors);

        public abstract Dictionary<Guid, Guid[]> GetParentsMap();

        public abstract IInterviewExpressionState Clone();


        public void DeclareAnswersInvalid(IEnumerable<Identity> invalidQuestions)
        {
            foreach (var identity in invalidQuestions)
            {
                var targetLevel = this.GetRosterByIdAndVector(identity.Id, identity.RosterVector);
                if (targetLevel == null) return;

                targetLevel.DeclareAnswerInvalid(identity.Id);
            }
        }

        public void DeclareAnswersValid(IEnumerable<Identity> validQuestions)
        {
            foreach (var identity in validQuestions)
            {
                var targetLevel = this.GetRosterByIdAndVector(identity.Id, identity.RosterVector);
                if (targetLevel == null) return;

                targetLevel.DeclareAnswerValid(identity.Id);
            }
        }

        public void DisableGroups(IEnumerable<Identity> groupsToDisable)
        {
            foreach (var identity in groupsToDisable)
            {
                var targetLevel = this.GetRosterByIdAndVector(identity.Id, identity.RosterVector);
                if (targetLevel == null) return;

                targetLevel.DisableGroup(identity.Id);
            }
        }

        public void EnableGroups(IEnumerable<Identity> groupsToEnable)
        {
            foreach (var identity in groupsToEnable)
            {
                var targetLevel = this.GetRosterByIdAndVector(identity.Id, identity.RosterVector);
                if (targetLevel == null) return;

                targetLevel.EnableGroup(identity.Id);
            }
        }

        public void DisableQuestions(IEnumerable<Identity> questionsToDisable)
        {
            foreach (var identity in questionsToDisable)
            {
                var targetLevel = this.GetRosterByIdAndVector(identity.Id, identity.RosterVector);
                if (targetLevel == null) return;

                targetLevel.DisableQuestion(identity.Id);
            }
        }

        public void EnableQuestions(IEnumerable<Identity> questionsToEnable)
        {
            foreach (var identity in questionsToEnable)
            {
                var targetLevel = this.GetRosterByIdAndVector(identity.Id, identity.RosterVector);
                if (targetLevel == null) return;

                targetLevel.EnableQuestion(identity.Id);
            }
        }

        protected IExpressionExecutable GetRosterByIdAndVector(Guid questionId, decimal[] rosterVector)
        {
            var parentsMap = this.GetParentsMap();
            if (!parentsMap.ContainsKey(questionId))
                return null;

            var rosterKey = Util.GetRosterKey(parentsMap[questionId], rosterVector);
            var rosterStringKey = Util.GetRosterStringKey(rosterKey);
            return this.InterviewScopes.ContainsKey(rosterStringKey) ? this.InterviewScopes[rosterStringKey] : null;
        }

        public void ProcessValidationExpressions(out List<Identity> questionsToBeValid, out List<Identity> questionsToBeInvalid)
        {
            questionsToBeValid = new List<Identity>();
            questionsToBeInvalid = new List<Identity>();

            foreach (var interviewScopeKvp in this.InterviewScopes)
            {
                List<Identity> questionsToBeValidByScope;
                List<Identity> questionsToBeInvalidByScope;

                interviewScopeKvp.Value.CalculateValidationChanges(out questionsToBeValidByScope,out questionsToBeInvalidByScope);

                questionsToBeValid.AddRange(questionsToBeValidByScope);
                questionsToBeInvalid.AddRange(questionsToBeInvalidByScope);
            }
        }

        public void ProcessConditionExpressions(out List<Identity> questionsToBeEnabled, out List<Identity> questionsToBeDisabled,
            out List<Identity> groupsToBeEnabled, out List<Identity> groupsToBeDisabled)
        {
            questionsToBeEnabled = new List<Identity>();
            questionsToBeDisabled = new List<Identity>();
            groupsToBeEnabled = new List<Identity>();
            groupsToBeDisabled = new List<Identity>();

            foreach (var interviewScopeKvp in this.InterviewScopes)
            {
                List<Identity> questionsToBeEnabledArray;
                List<Identity> questionsToBeDisabledArray;
                List<Identity> groupsToBeEnabledArray;
                List<Identity> groupsToBeDisabledArray;

                interviewScopeKvp.Value.CalculateConditionChanges(out questionsToBeEnabledArray, out questionsToBeDisabledArray, out groupsToBeEnabledArray,
                    out groupsToBeDisabledArray);

                questionsToBeEnabled.AddRange(questionsToBeEnabledArray);
                questionsToBeDisabled.AddRange(questionsToBeDisabledArray);
                groupsToBeEnabled.AddRange(groupsToBeEnabledArray);
                groupsToBeDisabled.AddRange(groupsToBeDisabledArray);
            }
        }

        public IEnumerable<IExpressionExecutable> GetRosterInstances(Identity[] rosterKey, Guid scopeId)
        {
            var siblingsKey = Util.GetSiblingsKey(rosterKey, scopeId);

            var siblingRosters = this.SiblingRosters.ContainsKey(siblingsKey)
                ? this.SiblingRosters[siblingsKey].Select(x => this.InterviewScopes[x])
                : null;

            return siblingRosters;
        }

        protected void SetSiblings(Identity[] rosterKey, string rosterStringKey)
        {
            var siblingsKey = Util.GetSiblingsKey(rosterKey);

            if (!this.SiblingRosters.ContainsKey(siblingsKey))
            {
                this.SiblingRosters.Add(siblingsKey, new List<string>());
            }
            this.SiblingRosters[siblingsKey].Add(rosterStringKey);
        }
    }
}