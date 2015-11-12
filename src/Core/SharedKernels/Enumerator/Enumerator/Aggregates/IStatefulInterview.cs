using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator.Entities.Interview;

namespace WB.Core.SharedKernels.Enumerator.Aggregates
{
    public interface IStatefulInterview
    {
        QuestionnaireIdentity QuestionnaireIdentity { get; }
        string QuestionnaireId { get; }
        Guid InterviewerId { get; }
        InterviewStatus Status { get; }

        Guid Id { get; }
        string InterviewerCompleteComment { get; }
        string SupervisorRejectComment { get; }

        IReadOnlyDictionary<string, BaseInterviewAnswer> Answers { get; }
        
        bool HasErrors { get; }
        bool IsCompleted { get; }
        bool CreatedOnClient { get; }

        InterviewRoster GetRoster(Identity identity);

        GpsCoordinatesAnswer GetGpsCoordinatesAnswer(Identity identity);

        DateTimeAnswer GetDateTimeAnswer(Identity identity);

        MultimediaAnswer GetMultimediaAnswer(Identity identity);

        TextAnswer GetQRBarcodeAnswer(Identity identity);

        TextListAnswer GetTextListAnswer(Identity identity);

        LinkedSingleOptionAnswer GetLinkedSingleOptionAnswer(Identity identity);

        MultiOptionAnswer GetMultiOptionAnswer(Identity identity);

        LinkedMultiOptionAnswer GetLinkedMultiOptionAnswer(Identity identity);

        IntegerNumericAnswer GetIntegerNumericAnswer(Identity identity);

        RealNumericAnswer GetRealNumericAnswer(Identity identity);

        TextAnswer GetTextAnswer(Identity identity);

        SingleOptionAnswer GetSingleOptionAnswer(Identity identity);

        YesNoQuestionAnswer GetYesNoAnswer(Identity identity);

        bool HasGroup(Identity group);

        bool IsValid(Identity identity);

        bool IsEnabled(Identity entityIdentity);

        bool WasAnswered(Identity entityIdentity);

        string GetInterviewerAnswerComment(Identity entityIdentity);

        string GetRosterTitle(Identity rosterIdentity);

        /// <summary>
        /// Gets an answer by roster vector that will be reduced until requested question is found.
        /// </summary>
        /// <returns>null if question is not answered yet.</returns>
        BaseInterviewAnswer FindBaseAnswerByOrDeeperRosterLevel(Guid questionId, RosterVector targetRosterVector);

        IEnumerable<BaseInterviewAnswer> FindAnswersOfReferencedQuestionForLinkedQuestion(Guid referencedQuestionId, Identity linkedQuestion);

        InterviewRoster FindRosterByOrDeeperRosterLevel(Guid rosterId, RosterVector targetRosterVector);

        IEnumerable<string> GetParentRosterTitlesWithoutLast(Guid questionId, RosterVector rosterVector);

        int CountInterviewerQuestionsInGroupRecursively(Identity groupIdentity);

        int CountActiveInterviewerQuestionsInGroupOnly(Identity group);

        int GetGroupsInGroupCount(Identity group);

        int CountAnsweredInterviewerQuestionsInGroupRecursively(Identity groupIdentity);

        int CountAnsweredInterviewerQuestionsInGroupOnly(Identity group);

        int CountInvalidInterviewerAnswersInGroupRecursively(Identity groupIdentity);

        int CountInvalidInterviewerQuestionsInGroupOnly(Identity group);

        bool HasInvalidInterviewerQuestionsInGroupOnly(Identity group);

        bool HasUnansweredInterviewerQuestionsInGroupOnly(Identity group);

        Identity GetParentGroup(Identity groupOrQuestion);

        IEnumerable<Identity> GetChildQuestions(Identity groupIdentity);

        IEnumerable<Identity> GetInterviewerEntities(Identity groupIdentity);

        IEnumerable<Identity> GetEnabledGroupInstances(Guid groupId, RosterVector parentRosterVector);

        IEnumerable<Identity> GetEnabledSubgroups(Identity group);

        int CountAnsweredQuestionsInInterview();

        int CountActiveQuestionsInInterview();

        int CountInvalidQuestionsInInterview();
    }
}