﻿using System;
using System.Collections.Generic;
using System.Text;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Moq;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;
using WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using WB.Core.SharedKernels.SurveyManagement.Views.SampleImport;
using WB.UI.Headquarters.Implementation.Services;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Applications.Headquarters.ServicesTests.InterviewImportServiceTests
{
    internal class when_import_interview_with_column_by_gps_question_with_double_underscore : InterviewImportServiceTestsContext
    {
        private Establish context = () =>
        {
            var mockOfSampleUploadVievFactory = new Mock<IViewFactory<SampleUploadViewInputModel, SampleUploadView>>();

            mockOfSampleUploadVievFactory.Setup(x => x.Load(Moq.It.IsAny<SampleUploadViewInputModel>()))
                .Returns(new SampleUploadView(questionnaireIdentity.QuestionnaireId, questionnaireIdentity.Version,
                    new List<FeaturedQuestionItem>()
                    {
                        new FeaturedQuestionItem(gpsQuestionId, "", "LongLat__Latitude"),
                        new FeaturedQuestionItem(gpsQuestionId, "", "LongLat__Longitude")
                    }));

            var questionnaireRepository = Create.CreateQuestionnaireReadSideKeyValueStorage(
                Create.QuestionnaireDocumentWithOneChapter(
                    Create.GpsCoordinateQuestion(questionId: gpsQuestionId, variableName: "LongLat", isPrefilled: true)));

            mockOfCommandService.Setup(x => x.Execute(Moq.It.IsAny<CreateInterviewByPrefilledQuestions>(), null))
                .Callback<ICommand, string>(
                    (command, ordinal) =>
                    {
                        executedCommand = command as CreateInterviewByPrefilledQuestions;
                    });


            interviewImportService =
                CreateInterviewImportService(questionnaireDocumentRepository: questionnaireRepository,
                    sampleUploadViewFactory: mockOfSampleUploadVievFactory.Object,
                    sampleImportSettings: new SampleImportSettings(1),
                    commandService: mockOfCommandService.Object);
        };

        Because of = () => exception = Catch.Exception(() =>
                interviewImportService.ImportInterviews(questionnaireIdentity, csvBytes,
                    Guid.Parse("33333333333333333333333333333333"), Guid.Parse("22222222222222222222222222222222")));

        It should_not_be_exception = () =>
            exception.ShouldBeNull();

        It should_call_execute_command_service_once = () =>
            mockOfCommandService.Verify(x => x.Execute(Moq.It.IsAny<CreateInterviewByPrefilledQuestions>(), null), Times.Once);

        It should_LongLat__Latitude_column_parse_to_specified_string_value = () =>
            (executedCommand.AnswersOnPrefilledQuestions[gpsQuestionId] as GeoPosition).Latitude.ShouldEqual(-6);

        It should_LongLat__Longitude_column_parse_to_specified_string_value = () =>
            (executedCommand.AnswersOnPrefilledQuestions[gpsQuestionId] as GeoPosition).Longitude.ShouldEqual(1);

        private static readonly byte[] csvBytes = Encoding.UTF8.GetBytes(
            "LongLat__Latitude	LongLat__Longitude\r\n" +
            @"-6	1");

        private static CreateInterviewByPrefilledQuestions executedCommand = null;
        private static Exception exception;
        private static readonly Mock<ICommandService> mockOfCommandService = new Mock<ICommandService>();
        private static readonly QuestionnaireIdentity questionnaireIdentity = new QuestionnaireIdentity(Guid.Parse("11111111111111111111111111111111"), 1);
        private static InterviewImportService interviewImportService;
        private static readonly Guid gpsQuestionId = Guid.Parse("10101010101010101010101010101010");
    }
}