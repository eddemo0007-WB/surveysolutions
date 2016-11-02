using System;
using System.Collections.Generic;
using Main.Core.Entities.SubEntities;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.Invariants;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates
{
    public partial class Interview
    {
        public void AnswerGeoLocationQuestion(Guid userId, Guid questionId, RosterVector rosterVector, DateTime answerTime, double latitude, double longitude,
            double accuracy, double altitude, DateTimeOffset timestamp)
        {
            new InterviewPropertiesInvariants(this.properties).RequireAnswerCanBeChanged();

            IQuestionnaire questionnaire = this.GetQuestionnaireOrThrow(this.questionnaireId, this.questionnaireVersion, this.language);
            var answeredQuestion = new Identity(questionId, rosterVector);

            var sourceInterviewTree = this.BuildInterviewTree(questionnaire, this.interviewState);
            CheckGpsCoordinatesInvariants(questionId, rosterVector, questionnaire, answeredQuestion, this.interviewState, sourceInterviewTree);
          
            var changedInterviewTree = sourceInterviewTree.Clone();

            var changedQuestionIdentities = new List<Identity> { answeredQuestion };
            var answer = new GeoPosition(latitude, longitude, accuracy, altitude, timestamp);
            changedInterviewTree.GetQuestion(answeredQuestion).AsGps.SetAnswer(answer);
            this.ApplyTreeDiffChanges(userId, changedInterviewTree, questionnaire, changedQuestionIdentities, sourceInterviewTree);
        }
    }
}