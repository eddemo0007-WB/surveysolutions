﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Moq;
using WB.Core.GenericSubdomains.Utils;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.EventHandlers.Interview.InterviewExportedDataEventHandlerTests
{
    internal class when_InterviewApproved_recived_by_interview_with_1_answerd_questions_and_1_unananswered :
        InterviewExportedDataEventHandlerTestContext
    {
        Establish context = () =>
        {
            answeredQuestionId = Guid.Parse("10000000000000000000000000000000");
            unansweredQuestionId = Guid.Parse("11111111111111111111111111111111");
            dataExportServiceMock = CreateDataExportService(r => result = r);
            variableNameAndQuestionId = new Dictionary<string, Guid>
            {
                { "q1", answeredQuestionId },
                { "q2", unansweredQuestionId }
            };
            questionnaireDocument = CreateQuestionnaireDocument(variableNameAndQuestionId);
            interviewExportedDataDenormalizer = CreateInterviewExportedDataEventHandlerForQuestionnarieCreatedByMethod(
                templateCreationAction:() => questionnaireDocument,
                dataCreationAction: () => CreateInterviewWithAnswers(variableNameAndQuestionId.Values.Take(1)), dataExportService: dataExportServiceMock.Object);
        };

        Because of = () =>
            interviewExportedDataDenormalizer.Handle(CreatePublishableEvent(() => new InterviewApproved(Guid.NewGuid(), "")));

        It should_records_count_equals_1 = () =>
            result.Levels[0].Records.Length.ShouldEqual(1);

        It should__first_record_have_1_answers = () =>
            result.Levels[0].Records[0].Questions.Length.ShouldEqual(2);

        It should_first_record_id_equals_0 = () =>
            result.Levels[0].Records[0].RecordId.ShouldEqual(result.Levels[0].Records[0].InterviewId.FormatGuid());

        It should_first_parent_ids_be_empty = () =>
           result.Levels[0].Records[0].ParentRecordIds.ShouldBeEmpty();

        It should_answered_question_be_not_empty = () =>
           result.Levels[0].Records[0].Questions.ShouldQuestionHasOneNotEmptyAnswer(answeredQuestionId);

        It should_unanswered_question_be_empty = () =>
          result.Levels[0].Records[0].Questions.ShouldQuestionHasNoAnswers(unansweredQuestionId);

        private static InterviewExportedDataDenormalizer interviewExportedDataDenormalizer;
        private static InterviewDataExportView result;
        private static Dictionary<string, Guid> variableNameAndQuestionId;
        private static Guid answeredQuestionId;
        private static Guid unansweredQuestionId;
        private static QuestionnaireDocument questionnaireDocument;
        private static Mock<IDataExportService> dataExportServiceMock;
    }
}
