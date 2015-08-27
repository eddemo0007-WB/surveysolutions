using System;
using System.Collections.Generic;
using Machine.Specifications;
using Moq;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Entities.Interview;
using WB.Core.SharedKernels.Enumerator.Models.Questionnaire;
using WB.Core.SharedKernels.Enumerator.Models.Questionnaire.Questions;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.Enumerator.ViewModels.IntegerQuestionViewModelTests
{
    internal class when_answering_numeric_question_with_negative_value : IntegerQuestionViewModelTestContext
    {
        Establish context = () =>
        {
            SetUp();

            var integerNumericAnswer = Mock.Of<IntegerNumericAnswer>(_ => _.IsAnswered == false);

            var interview = Mock.Of<IStatefulInterview>(_
                => _.QuestionnaireId == questionnaireId
                   && _.GetIntegerNumericAnswer(questionIdentity) == integerNumericAnswer);

            var interviewRepository = Mock.Of<IStatefulInterviewRepository>(x => x.Get(interviewGuid.FormatGuid()) == interview);

            var cascadingQuestionModel = Mock.Of<IntegerNumericQuestionModel>(_
                => _.Id == questionIdentity.Id && _.IsRosterSizeQuestion == false);

            var questionnaireModel = Mock.Of<QuestionnaireModel>(_ => _.Questions == new Dictionary<Guid, BaseQuestionModel> { { questionIdentity.Id, cascadingQuestionModel } });

            var questionnaireRepository = Mock.Of<IPlainKeyValueStorage<QuestionnaireModel>>(x => x.GetById(questionnaireId) == questionnaireModel);

            integerModel = CreateIntegerQuestionViewModel(
                interviewRepository: interviewRepository,
                questionnaireRepository: questionnaireRepository);

            integerModel.Init(interviewGuid.FormatGuid(), questionIdentity, navigationState);
        };

        Because of = () =>
        {
            integerModel.AnswerAsString = "-4";
            integerModel.ValueChangeCommand.Execute();
        };

        It should_not_mark_question_as_invalid_with_message = () =>
            ValidityModelMock.Verify(x => x.MarkAnswerAsNotSavedWithMessage(Moq.It.IsAny<string>()), Times.Never);

        It should_send_answer_command = () =>
            AnsweringViewModelMock.Verify(x => x.SendAnswerQuestionCommandAsync(Moq.It.IsAny<AnswerNumericIntegerQuestionCommand>()), Times.Once);


        private static IntegerQuestionViewModel integerModel;
    }
}