﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Domain;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Spec;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Exceptions;

namespace WB.Core.BoundedContexts.Designer.Tests.QuestionnaireTests
{
    [TestFixture]
    public class NewUpdateQuestionTests : QuestionnaireTestsContext
    {
        [SetUp]
        public void SetUp()
        {
            var serviceLocatorMock = new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock };
            ServiceLocator.SetLocatorProvider(() => serviceLocatorMock.Object);
        }

        [Test]
        public void NewUpdateQuestion_When_Title_is_not_empty_Then_QuestionChanged_event_contains_the_same_title_caption()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid questionKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestion(questionId: questionKey, responsibleId: responsibleId);

                string notEmptyTitle = "not empty :)";

                // act
                questionnaire.NewUpdateQuestion(questionKey, notEmptyTitle, QuestionType.Text, "test", false, false,
                                                QuestionScope.Interviewer, string.Empty, string.Empty,
                                                string.Empty,
                                                string.Empty, new Option[0], Order.AZ,  responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered:false, maxAllowedAnswers:null);

                // assert
                var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                Assert.AreEqual(notEmptyTitle, risedEvent.QuestionText);
            }
        }

        [Test]
        public void NewUpdateQuestion_When_Title_is_empty_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid questionKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestion(questionId: questionKey, responsibleId: responsibleId);

            // act
            TestDelegate act = () =>
                               questionnaire.NewUpdateQuestion(questionKey, "", QuestionType.Text, "test", false, false,
                                                               QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                                               string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionTitleRequired));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_AnswerTitle_is_absent_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            Guid questionKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            // arrange
            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(questionKey, questionType,
                new[] {new Option(Guid.NewGuid(), "123", "title"), new Option(Guid.NewGuid(), "2", "title1")},
                responsibleId: responsibleId);
            Option[] options = new Option[2] { new Option(Guid.NewGuid(), "1", string.Empty), new Option(Guid.NewGuid(), "2", string.Empty) };
            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(questionKey, "test", questionType, "test", false, false,
                                                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                                string.Empty, options, Order.AsIs, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);
            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorTextRequired));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_AnswerTitle_is_not_empty_Then_event_contains_the_same_answer_title(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                Guid questionKey = Guid.NewGuid();
                var notEmptyAnswerOptionTitle1 = "title";
                var notEmptyAnswerOptionTitle2 = "title1";
                Option[] options = new Option[2] { new Option(Guid.NewGuid(), "1", notEmptyAnswerOptionTitle1), new Option(Guid.NewGuid(), "2", notEmptyAnswerOptionTitle2) };
                Guid responsibleId = Guid.NewGuid();
                // arrange
                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(
                    questionKey, questionType, new[]
                        {
                            new Option(Guid.NewGuid(), "1", "option text"),
                            new Option(Guid.NewGuid(), "2", "option text1"),
                        },
                        responsibleId: responsibleId);


                // act
                questionnaire.NewUpdateQuestion(questionKey, "test", questionType, "test", false, false,
                                                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                                string.Empty, options, Order.AsIs, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);
                // assert
                var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                Assert.AreEqual(notEmptyAnswerOptionTitle1, risedEvent.Answers[0].AnswerText);
                Assert.AreEqual(notEmptyAnswerOptionTitle2, risedEvent.Answers[1].AnswerText);
            }
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_AnswerTitle_is_not_unique_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            Guid questionKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            // arrange
            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(questionKey, questionType, options: new[] { new Option(Guid.NewGuid(), "12", "title"), new Option(Guid.NewGuid(), "125", "title1") }, responsibleId: responsibleId);
            Option[] options = new Option[] { new Option(Guid.NewGuid(), "1", "title"), new Option(Guid.NewGuid(), "2", "title") };
            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(questionKey, "test", questionType, "test", false, false,
                                                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                                string.Empty, options, Order.AsIs, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);
            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorTextNotUnique));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_AnswerTitle_is_unique_Then_event_contains_the_same_answer_titles(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                var firstAnswerOptionTitle = "title1";
                var secondAnswerOptionTitleThatNotEqualsFirstOne = firstAnswerOptionTitle + "1";

                Guid questionKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(questionKey, questionType, options: new[] { new Option(Guid.NewGuid(), "121", "title"), new Option(Guid.NewGuid(), "12", "title1") }, responsibleId: responsibleId);
                Option[] options = new Option[] { new Option(Guid.NewGuid(), "1", firstAnswerOptionTitle), new Option(Guid.NewGuid(), "2", secondAnswerOptionTitleThatNotEqualsFirstOne) };
                
                // act
                questionnaire.NewUpdateQuestion(questionKey, "test", questionType, "test", false, false,
                                                QuestionScope.Interviewer, string.Empty, string.Empty, string.Empty,
                                                string.Empty, options, Order.AsIs, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);
                // assert
                var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);

                Assert.That(risedEvent.Answers[0].AnswerText, Is.EqualTo(firstAnswerOptionTitle));
                Assert.That(risedEvent.Answers[1].AnswerText, Is.EqualTo(secondAnswerOptionTitleThatNotEqualsFirstOne));
                
            }
        }

        [Test]
        public void NewUpdateQuestion_When_question_inside_non_propagated_group_is_featured_Then_raised_QuestionChanged_event_contains_the_same_featured_field()
        {
            using (var eventContext = new EventContext())
            {
                // Arrange
                Guid updatedQuestion = Guid.NewGuid();
                bool isFeatured = true;
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneGroupAndQuestionInIt(questionId: updatedQuestion, responsibleId: responsibleId);

                // Act
                questionnaire.NewUpdateQuestion(updatedQuestion, "What is your last name?", QuestionType.Text, "name", false,
                                                isFeatured, QuestionScope.Interviewer, "", "", "", "", new Option[0], Order.AsIs, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

                // Assert
                Assert.That(GetSingleEvent<QuestionChanged>(eventContext).Featured, Is.EqualTo(isFeatured));
            }
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_QuestionType_is_option_type_and_answer_options_list_is_empty_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // Arrange
            var emptyAnswersList = new Option[] { };

            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);

            // Act
            TestDelegate act = () =>
                               questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", questionType, "name",
                                                               false, false, QuestionScope.Interviewer, "", "", "",
                                                               "", emptyAnswersList, Order.AZ, responsibleId: responsibleId, 
                                                               linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorEmpty));
        }

        [TestCase("ma_name38")]
        [TestCase("__")]
        [TestCase("_123456789012345678901234567890_")]
        public void NewUpdateQuestion_When_variable_name_is_valid_Then_rised_QuestionChanged_event_contains_the_same_stata_caption(string validVariableName)
        {
            using (var eventContext = new EventContext())
            {
                // Arrange
                Guid targetQuestionPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);

                // Act
                questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
                                                validVariableName, false, false, QuestionScope.Interviewer, "", "", "", "",
                                                new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, 
                                                areAnswersOrdered: false, maxAllowedAnswers: null);

                // Assert
                Assert.That(GetSingleEvent<QuestionChanged>(eventContext).StataExportCaption, Is.EqualTo(validVariableName));
            }
        }

        [Test]
        public void NewUpdateQuestion_When_we_updating_absent_question_Then_DomainException_should_be_thrown()
        {
            // Arrange
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaire(responsibleId: responsibleId);

            // Act
            TestDelegate act = () =>
                               questionnaire.NewUpdateQuestion(Guid.NewGuid(), "Title", QuestionType.Text, "valid",
                                                               false, false, QuestionScope.Interviewer, "", "", "",
                                                               "", new Option[] { }, Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionNotFound));
        }

        [Test]
        public void NewUpdateQuestion_When_variable_name_has_33_chars_Then_DomainException_should_be_thrown()
        {
            // Arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);
            string longVariableName = "".PadRight(33, 'A');

            // Act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
                                                                     longVariableName, false, false, QuestionScope.Interviewer, "", "", "", "",
                                                                     new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VariableNameMaxLength));
        }

        [Test]
        public void NewUpdateQuestion_When_variable_name_starts_with_digit_Then_DomainException_should_be_thrown()
        {
            // Arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);

            string stataExportCaptionWithFirstDigit = "1aaaa";

            // Act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
                                                                     stataExportCaptionWithFirstDigit,
                                                                     false, false, QuestionScope.Interviewer, "", "", "", "",
                                                                     new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VariableNameStartWithDigit));
        }

        [Test]
        public void NewUpdateQuestion_When_variable_name_has_trailing_spaces_and_is_valid_Then_rised_QuestionChanged_evend_should_contains_trimed_stata_caption()
        {
            using (var eventContext = new EventContext())
            {
                // Arrange
                Guid targetQuestionPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);
                string variableNameWithTrailingSpaces = " my_name38  ";

                // Act
                questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
                                                variableNameWithTrailingSpaces,
                                                false, false, QuestionScope.Interviewer, "", "", "", "",
                                                new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);


                // Assert
                var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                Assert.AreEqual(variableNameWithTrailingSpaces.Trim(), risedEvent.StataExportCaption);
            }
        }

        [Test]
        public void NewUpdateQuestion_When_variable_name_is_empty_Then_DomainException_should_be_thrown()
        {
            // Arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);

            string emptyVariableName = string.Empty;

            // Act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
                                                                     emptyVariableName,
                                                                     false, false, QuestionScope.Interviewer, "", "", "", "",
                                                                     new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);


            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VariableNameRequired));
        }

        [Test]
        public void NewUpdateQuestion_When_variable_name_contains_any_non_underscore_letter_or_digit_character_Then_DomainException_should_be_thrown()
        {
            // Arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);
            
            string nonValidVariableNameWithBannedSymbols = "aaa:_&b";

            // Act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
                                                                     nonValidVariableNameWithBannedSymbols,
                                                                     false, false, QuestionScope.Interviewer, "", "", "", "",
                                                                     new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VariableNameSpecialCharacters));
        }

        [Test]
        public void NewUpdateQuestion_When_questionnaire_has_another_question_with_same_variable_name_Then_DomainException_should_be_thrown()
        {
            // Arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            string duplicateVariableName = "text";
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithTwoQuestions(secondQuestionId: targetQuestionPublicKey,
                responsibleId: responsibleId);

            // Act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.Text,
                                                                     duplicateVariableName,
                                                                     false, false, QuestionScope.Interviewer, "", "", "", "",
                                                                     new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.VarialbeNameNotUnique));
        }

        [Ignore("Validation about options count is temporary turned off. Should be turned on with new clone questionnaire feature implementation")]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_there_is_only_one_option_in_categorical_question_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(questionId: targetQuestionPublicKey, questionType: questionType, options: new Option[2]
                    {
                        new Option(id: Guid.NewGuid(), title: "text1", value: "1") ,
                        new Option(id: Guid.NewGuid(), title: "text2", value: "2") 
                    },
                    responsibleId: responsibleId);

            Option[] oneOption = new Option[1] { new Option(Guid.NewGuid(), "1", "title") };
            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(questionId: targetQuestionPublicKey,
                                   title: "What is your last name?",
                                   alias: "name",
                                   type: questionType,
                                   scope: QuestionScope.Interviewer,
                                   condition: string.Empty,
                                   validationExpression: string.Empty,
                                   validationMessage: string.Empty,
                                   isFeatured: false,
                                   isMandatory: false,
                                   optionsOrder: Order.AZ,
                                   instructions: string.Empty,
                                   options: oneOption,
                                   responsibleId: responsibleId,
                                   linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.TooFewOptionsInCategoryQuestion));
        }

        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_there_are_two_options_in_categorical_question_Then_raised_NewQuestionAdded_event_contains_the_same_options_count(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid targetQuestionPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                var questionnaire = CreateQuestionnaireWithOneQuestionInTypeAndOptions(questionId: targetQuestionPublicKey, questionType: questionType, options: new Option[2]
                    {
                        new Option(id: Guid.NewGuid(), title: "text1", value: "1") ,
                        new Option(id: Guid.NewGuid(), title: "text2", value: "2") 
                    },
                    responsibleId: responsibleId);

                const int answerOptionsCount = 2;

                Option[] options = new Option[answerOptionsCount] { new Option(Guid.NewGuid(), "1", "title"), new Option(Guid.NewGuid(), "2", "title1") };
                // act
                questionnaire.NewUpdateQuestion(questionId: targetQuestionPublicKey,
                                   title: "What is your last name?",
                                   alias: "name",
                                   type: questionType,
                                   scope: QuestionScope.Interviewer,
                                   condition: string.Empty,
                                   validationExpression: string.Empty,
                                   validationMessage: string.Empty,
                                   isFeatured: false,
                                   isMandatory: false,
                                   optionsOrder: Order.AZ,
                                   instructions: string.Empty,
                                   options: options,
                                   responsibleId: responsibleId,
                                   linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

                // assert
                var raisedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                Assert.That(raisedEvent.Answers.Length, Is.EqualTo(answerOptionsCount));
            }
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
#warning Roma: when part is incorrect should be something like when answer option value contains not number
        public void NewUpdateQuestion_When_answer_option_value_allows_only_numbers_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey,
                responsibleId: responsibleId);

            // Act
            TestDelegate act = () =>
                               questionnaire.NewUpdateQuestion(
                                   questionId: targetQuestionPublicKey,
                                   title: "What is your last name?",
                                   alias: "name",
                                   type: QuestionType.MultyOption,
                                   scope: QuestionScope.Interviewer,
                                   condition: string.Empty,
                                   validationExpression: string.Empty,
                                   validationMessage: string.Empty,
                                   isFeatured: false,
                                   isMandatory: false,
                                   optionsOrder: Order.AZ,
                                   instructions: string.Empty,
                                   options: new Option[1] { new Option(id: Guid.NewGuid(), title: "text", value: "some text") },
                                   responsibleId: responsibleId,
                                   linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorValueSpecialCharacters));
        }

        [Test]
        [TestCase(QuestionType.Numeric)]
        [TestCase(QuestionType.AutoPropagate)]
        public void NewUpdateQuestion_When_question_type_is_handled_by_type_specific_command_Then_DomainException_should_be_thrown(
            QuestionType questionType)
        {
            Guid responsibleId = Guid.NewGuid();
            Guid targetQuestionPublicKey = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey,
              responsibleId: responsibleId);

            TestDelegate act = () =>
                               questionnaire.NewUpdateQuestion(
                                   questionId: Guid.NewGuid(),
                                   title: "What is your last name?",
                                   type: questionType,
                                   alias: "name",
                                   isMandatory: false,
                                   isFeatured: false,
                                   scope: QuestionScope.Interviewer,
                                   condition: string.Empty,
                                   validationExpression: string.Empty,
                                   validationMessage: string.Empty,
                                   instructions: string.Empty,
                                   optionsOrder: Order.AsIs,
                                   options: new Option[0],
                                   responsibleId: responsibleId,
                                   linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionTypeIsReroutedOnQuestionTypeSpecificCommand));
        }

        [Test]
        [TestCase(20)]
        [TestCase(0)]
        [TestCase(-1)]
        public void NewUpdateQuestion_When_countOfDecimalPlaces_is_incorrect_Then_DomainException_should_be_thrown(int countOfDecimalPlaces)
        {
            Guid responsibleId = Guid.NewGuid();
            Guid targetQuestionPublicKey = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey,
             responsibleId: responsibleId);

            TestDelegate act = () =>
                questionnaire.UpdateNumericQuestion(
                    questionId: targetQuestionPublicKey,
                    title: "What is your last name?",
                    isAutopropagating:false, 
                    alias: "name",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    responsibleId: responsibleId, maxValue: null, triggeredGroupIds: new Guid[0], isInteger: false, countOfDecimalPlaces: countOfDecimalPlaces);

            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.CountOfDecimalPlacesValueIsIncorrect));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_answer_option_value_contains_only_numbers_Then_raised_QuestionChanged_event_contains_question_answer_with_the_same_answe_values(
            QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid targetQuestionPublicKey = Guid.NewGuid();
                string answerValue1 = "10";
                string answerValue2 = "100";
                Guid responsibleId = Guid.NewGuid();
                var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);

                // act
                questionnaire.NewUpdateQuestion(
                    questionId: targetQuestionPublicKey,
                    title: "What is your last name?",
                    alias: "name",
                    type: questionType,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    isFeatured: false,
                    isMandatory: false,
                    optionsOrder: Order.AZ,
                    instructions: string.Empty,
                    options: new Option[2] { 
                        new Option(id: Guid.NewGuid(), title: "text1", value: answerValue1),
                        new Option(id: Guid.NewGuid(), title: "text2", value: answerValue2)},
                    responsibleId: responsibleId,
                    linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);


                // assert
                Assert.That(GetSingleEvent<QuestionChanged>(eventContext).Answers[0].AnswerValue, Is.EqualTo(answerValue1));
                Assert.That(GetSingleEvent<QuestionChanged>(eventContext).Answers[1].AnswerValue, Is.EqualTo(answerValue2));
            }
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        [TestCase(QuestionType.Text)]
        [TestCase(QuestionType.DateTime)]
        [TestCase(QuestionType.GpsCoordinates)]
        public void NewUpdateQuestion_When_question_type_is_allowed_Then_raised_QuestionChanged_event_with_same_question_type(
            QuestionType allowedQuestionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid questionId = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestion(questionId: questionId,
                    responsibleId: responsibleId);

                // act
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "What is your last name?",
                    alias: "name",
                    type: allowedQuestionType,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    isFeatured: false,
                    isMandatory: false,
                    optionsOrder: Order.AZ,
                    instructions: string.Empty,
                    options: AreOptionsRequiredByQuestionType(allowedQuestionType) ? CreateTwoOptions() : null,
                    responsibleId: responsibleId,
                    linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

                // assert
                Assert.That(GetSingleEvent<QuestionChanged>(eventContext).QuestionType, Is.EqualTo(allowedQuestionType));
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void UpdateNumericQuestion_When_question_type_is_allowed_Then_raised_QuestionChanged_event_with_same_question_type(
            bool isAutopropagating)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid questionId = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneQuestion(questionId: questionId,
                    responsibleId: responsibleId);

                // act
                questionnaire.UpdateNumericQuestion(
                    questionId: questionId,
                    title: "What is your last name?",
                    alias: "name",
                    isAutopropagating: isAutopropagating,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    isFeatured: false,
                    isMandatory: false,
                    instructions: string.Empty,
                    responsibleId: responsibleId, maxValue: null, triggeredGroupIds: new Guid[0], isInteger: true, countOfDecimalPlaces: null);

                // assert
                Assert.That(GetSingleEvent<NumericQuestionChanged>(eventContext).IsAutopropagating, Is.EqualTo(isAutopropagating));
            }
        }

        [Test]
        [TestCase(QuestionType.DropDownList)]
        [TestCase(QuestionType.YesNo)]
        public void NewUpdateQuestion_When_question_type_is_not_allowed_Then_DomainException_with_type_NotAllowedQuestionType_should_be_thrown(
            QuestionType notAllowedQuestionType)
        {
            // arrange
            Guid questionId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneQuestion(questionId: questionId, responsibleId: responsibleId);

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(
                questionId: questionId,
                title: "What is your last name?",
                alias: "name",
                type: notAllowedQuestionType,
                scope: QuestionScope.Interviewer,
                condition: string.Empty,
                validationExpression: string.Empty,
                validationMessage: string.Empty,
                isFeatured: false,
                isMandatory: false,
                optionsOrder: Order.AZ,
                instructions: string.Empty,
                options: null,
                responsibleId: responsibleId,
                linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.NotAllowedQuestionType));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_answer_option_value_is_required_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey,
                responsibleId: responsibleId);

            // Act
            TestDelegate act = () =>
                               questionnaire.NewUpdateQuestion(
                                   questionId: targetQuestionPublicKey,
                                   title: "What is your last name?",
                                   alias: "name",
                                   type: questionType,
                                   scope: QuestionScope.Interviewer,
                                   condition: string.Empty,
                                   validationExpression: string.Empty,
                                   validationMessage: string.Empty,
                                   isFeatured: false,
                                   isMandatory: false,
                                   optionsOrder: Order.AZ,
                                   instructions: string.Empty,
                                   options: new Option[1] { new Option(id: Guid.NewGuid(), title: "text", value: null) },
                                   responsibleId: responsibleId,
                                   linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorValueRequired));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_answer_option_value_is_not_null_or_empty_Then_raised_QuestionChanged_event_contains_not_null_and_not_empty_question_answer(
            QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid targetQuestionPublicKey = Guid.NewGuid();
                string notEmptyAnswerValue1 = "10";
                string notEmptyAnswerValue2 = "100";
                Guid responsibleId = Guid.NewGuid();
                var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey,
                    responsibleId: responsibleId);

                // act
                questionnaire.NewUpdateQuestion(
                    questionId: targetQuestionPublicKey,
                    title: "What is your last name?",
                    alias: "name",
                    type: questionType,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    isFeatured: false,
                    isMandatory: false,
                    optionsOrder: Order.AZ,
                    instructions: string.Empty,
                    options: new Option[2]
                        {
                            new Option(id: Guid.NewGuid(), title: "text", value: notEmptyAnswerValue1),
                            new Option(id: Guid.NewGuid(), title: "text1", value: notEmptyAnswerValue2)
                        },
                    responsibleId: responsibleId,
                    linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);


                // assert
                Assert.That(GetSingleEvent<QuestionChanged>(eventContext).Answers[0].AnswerValue, !Is.Empty);
                Assert.That(GetSingleEvent<QuestionChanged>(eventContext).Answers[1].AnswerValue, !Is.Empty);
            }
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_answer_option_values_not_unique_in_options_scope_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey,
                responsibleId: responsibleId);

            // Act
            TestDelegate act = () =>
                               questionnaire.NewUpdateQuestion(
                                   questionId: targetQuestionPublicKey,
                                   title: "What is your last name?",
                                   alias: "name",
                                   type: questionType,
                                   scope: QuestionScope.Interviewer,
                                   condition: string.Empty,
                                   validationExpression: string.Empty,
                                   validationMessage: string.Empty,
                                   isFeatured: false,
                                   isMandatory: false,
                                   optionsOrder: Order.AZ,
                                   instructions: string.Empty,
                                   options:
                                       new Option[2]
                                           {
                                               new Option(id: Guid.NewGuid(), value: "1", title: "text 1"),
                                               new Option(id: Guid.NewGuid(), value: "1", title: "text 2")
                                           },
                                   responsibleId: responsibleId,
                                   linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);

            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.SelectorValueNotUnique));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_answer_option_values_unique_in_options_scope_Then_raised_QuestionChanged_event_contains_only_unique_values_in_answer_values_scope(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid targetQuestionPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey,
                    responsibleId: responsibleId);

                // act
                questionnaire.NewUpdateQuestion(
                    questionId: targetQuestionPublicKey,
                    title: "What is your last name?",
                    alias: "name",
                    type: questionType,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    isFeatured: false,
                    isMandatory: false,
                    optionsOrder: Order.AZ,
                    instructions: string.Empty,
                    options:
                        new Option[2]
                            {
                                new Option(id: Guid.NewGuid(), title: "text 1", value: "1"),
                                new Option(id: Guid.NewGuid(), title: "text 2", value: "2")
                            },
                    responsibleId: responsibleId,
                    linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: null);


                // assert
                Assert.That(GetSingleEvent<QuestionChanged>(eventContext).Answers.Select(x => x.AnswerValue).Distinct().Count(),
                            Is.EqualTo(2));
            }
        }

        [Test]
        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_is_null_Then_rised_QuestionChanged_event_should_contains_null_in_triggers_field()
        {
            using (var eventContext = new EventContext())
            {
                // Arrange
                var groupId = Guid.NewGuid();
                var autoPropagateQuestionId = Guid.NewGuid();
                var autoPropagate = true;
                Guid[] emptyTriggedGroupIds = null;
                var responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneAutoGroupAndQuestionInIt(questionId: autoPropagateQuestionId, responsibleId: responsibleId);

                // Act
                questionnaire.UpdateNumericQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate, "name", false, false,
                                                QuestionScope.Interviewer, "", "", "", "", 0,
                                                emptyTriggedGroupIds, responsibleId: responsibleId, isInteger: true, countOfDecimalPlaces:null);


                // Assert
                Assert.That(GetSingleEvent<NumericQuestionChanged>(eventContext).Triggers, Is.Null);
            }
        }

        [Test]
        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_is_empty_Then_rised_QuestionChanged_event_should_contains_empty_list_in_triggers_field()
        {
            using (var eventContext = new EventContext())
            {
                // Arrange
                var groupId = Guid.NewGuid();
                var autoPropagateQuestionId = Guid.NewGuid();
                var autoPropagate = true;
                var emptyTriggedGroupIds = new Guid[0];
                var responsibleId = Guid.NewGuid();
                Questionnaire questionnaire = CreateQuestionnaireWithOneAutoGroupAndQuestionInIt(questionId: autoPropagateQuestionId, responsibleId: responsibleId);

                // Act
                questionnaire.UpdateNumericQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate, "name", false, false,
                                                QuestionScope.Interviewer, "", "", "", "",  0,
                                                emptyTriggedGroupIds, responsibleId: responsibleId, isInteger: true, countOfDecimalPlaces: null);


                // Assert
                Assert.That(GetSingleEvent<NumericQuestionChanged>(eventContext).Triggers, Is.Empty);
            }
        }

        [Test]
        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_contains_absent_group_id_Then_DomainException_should_be_thrown()
        {
            // Arrange
            var autoPropagate = true;
            var autoPropagateQuestionId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var absentGroupId = Guid.NewGuid();
            var triggedGroupIdsWithAbsentGroupId = new[] { absentGroupId };
            var responsibleId = Guid.NewGuid();

            Questionnaire questionnaire =
                CreateQuestionnaireWithOneGroupAndQuestionInIt(questionId: autoPropagateQuestionId, groupId: groupId,
                    questionType: QuestionType.AutoPropagate, responsibleId: responsibleId);

            // Act
            TestDelegate act = () => questionnaire.UpdateNumericQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate, "name", false, false,
                                                                     QuestionScope.Interviewer, "", "", "", "", 0,
                                                                     triggedGroupIdsWithAbsentGroupId, responsibleId: responsibleId, isInteger: true, countOfDecimalPlaces: null);

            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.TriggerLinksToNotExistingGroup));
        }

        [Test]
        public void NewUpdateQuestion_When_question_is_AutoPropagate_and_list_of_triggers_contains_non_propagate_group_id_Then_DomainException_should_be_thrown()
        {
            // Arrange
            var autoPropagate = true;
            var autoPropagateQuestionId = Guid.NewGuid();
            var nonPropagateGroupId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var triggedGroupIdsWithNonPropagateGroupId = new[] { nonPropagateGroupId };
            var responsibleId = Guid.NewGuid();

            Questionnaire questionnaire = CreateQuestionnaireWithTwoRegularGroupsAndQuestionInLast(nonPropagateGroupId, autoPropagateQuestionId, responsibleId:responsibleId);

            // Act
            TestDelegate act =
                () =>
                    questionnaire.UpdateNumericQuestion(autoPropagateQuestionId, "What is your last name?", autoPropagate,
                        "name", false, false,
                        QuestionScope.Interviewer, "", "", "", "",  0,
                        triggedGroupIdsWithNonPropagateGroupId, responsibleId: responsibleId, isInteger: true, countOfDecimalPlaces: null);


            // Assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.TriggerLinksToNotPropagatedGroup));
        }

        [Test]
        public void NewUpdateQuestion_When_User_Doesnot_Have_Permissions_For_Edit_Questionnaire_Then_DomainException_should_be_thrown()
        {
            // Arrange
            var rosterSizeQuestionId = Guid.NewGuid();
            var rosterGroupId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var triggedGroupIdsWithAutoPropagateGroupId = new[] { rosterGroupId };

            Questionnaire questionnaire = CreateQuestionnaireWithRosterGroupAndQuestion(rosterGroupId, rosterSizeQuestionId, responsibleId: Guid.NewGuid());

            // act
            TestDelegate act = () => questionnaire.UpdateNumericQuestion(rosterSizeQuestionId, "What is your last name?", false, "name", false, false,
                                            QuestionScope.Interviewer, "", "", "", "", 0,
                                            triggedGroupIdsWithAutoPropagateGroupId, Guid.NewGuid(), isInteger: true, countOfDecimalPlaces: null);
            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.DoesNotHavePermissionsForEdit));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_categorical_question_with_linked_question_that_does_not_exist_in_questionnaire_questions_scope_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    rosterId: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType);

            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: questionType,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: Guid.NewGuid(), areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.LinkedQuestionDoesNotExist));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_categorical_question_with_linked_question_that_exist_in_autopropagated_group_questions_scope_Then_question_changed_event_should_be_raised(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
                Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();

                Questionnaire questionnaire =
                    CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                        rosterId: autoGroupId,
                        secondGroup: groupId,
                        autoQuestionId: autoQuestionId,
                        questionId: questionId,
                        responsibleId: responsibleId,
                        questionType: questionType);


                // act
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: questionType,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId, 
                    areAnswersOrdered: false, 
                    maxAllowedAnswers: null);
                // assert
                var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                Assert.AreEqual(autoQuestionId, risedEvent.LinkedToQuestionId);
            }
        }

        [Test]
        [TestCase(QuestionType.DateTime)]
        [TestCase(QuestionType.GpsCoordinates)]
        [TestCase(QuestionType.Text)]
        public void NewUpdateQuestion_When_non_categorical_question_with_linked_question_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    rosterId: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: QuestionType.MultyOption);

            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: questionType,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.NotCategoricalQuestionLinkedToAnoterQuestion));
        }

        [Test]
        [TestCase(QuestionType.DateTime)]
        [TestCase(QuestionType.Numeric)]
        [TestCase(QuestionType.Text)]
        public void NewUpdateQuestion_When_categorical_question_with_linked_question_with_number_or_text_or_datetime_type(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
                Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();

                Questionnaire questionnaire = CreateQuestionnaireWithRosterGroupAndQuestionAndAndRegularGroupAndQuestionsInThem(
                        rosterId: autoGroupId,
                        nonRosterGroupId: groupId,
                        autoQuestionId: autoQuestionId,
                        questionId: questionId,
                        responsibleId: responsibleId,
                        firstQuestionType: QuestionType.MultyOption,
                        secondQuestionType: questionType);


                // act
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: QuestionType.MultyOption,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId, areAnswersOrdered: false, maxAllowedAnswers: null);
                // assert
                var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                Assert.AreEqual(autoQuestionId, risedEvent.LinkedToQuestionId);
            }
        }

        [Test]
        [TestCase(QuestionType.SingleOption, QuestionType.TextList)]
        [TestCase(QuestionType.SingleOption, QuestionType.GpsCoordinates)]
        [TestCase(QuestionType.MultyOption, QuestionType.TextList)]
        [TestCase(QuestionType.MultyOption, QuestionType.GpsCoordinates)]
        public void NewUpdateQuestion_When_categorical_question_with_linked_question_that_not_of_type_text_or_number_or_datetime_Then_DomainException_should_be_thrown(QuestionType questionType, QuestionType depricatedLinkedSourceType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    rosterId: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType,
                    autoQuestionType: depricatedLinkedSourceType);

            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: questionType, 
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.NotSupportedQuestionForLinkedQuestion));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_categorical_question_have_answers_and_linked_question_in_the_same_time_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    rosterId: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType);

            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: QuestionType.MultyOption,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: new Option[] {new Option(Guid.NewGuid(), "1", "auto")},
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ConflictBetweenLinkedQuestionAndOptions));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_categorical_question_with_linked_question_that_does_not_exist_in_questions_scope_from_autopropagate_groups_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid questionThatLinkedButNotFromPropagateGroupId = Guid.Parse("00000000-1111-0000-2222-222000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoAndRegularGroupsAnd1QuestionInAutoGroupAnd2QuestionsInRegular(
                    autoGroupPublicKey: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType,
                    questionThatLinkedButNotFromPropagateGroup: questionThatLinkedButNotFromPropagateGroupId);

            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: questionType, 
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: questionThatLinkedButNotFromPropagateGroupId, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.LinkedQuestionIsNotInPropagateGroup));
        }

        [Test]
        [TestCase(QuestionType.SingleOption)]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_categorical_question_featured_status_Then_DomainException_should_be_thrown(QuestionType questionType)
        {
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    rosterId: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: questionType);

            // act
            TestDelegate act =
                () =>
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: questionType,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: true,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId, areAnswersOrdered: false, maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.QuestionWithLinkedQuestionCanNotBeFeatured));
        }

        [Test]
        [TestCase(QuestionType.MultyOption)]
        public void NewUpdateQuestion_When_Categorical_Not_Linked_Multi_Question_That_Ordered_and_MaxAnswer_Are_Set_Then_event_contains_values(QuestionType questionType)
        {
            using (var eventContext = new EventContext())
            {
                var areAnswersOrdered = true;
                var maxAllowedAnswers = 1;

                // arrange
                Guid targetQuestionPublicKey = Guid.NewGuid();
                Guid responsibleId = Guid.NewGuid();
                var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);

                // act
                questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.MultyOption, "Question",
                    false, false, QuestionScope.Interviewer, "", "", "", "",
                    new Option[2] { new Option(Guid.NewGuid(), "1", "title"), new Option(Guid.NewGuid(), "2", "title1") }, Order.AZ,
                    responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: areAnswersOrdered, maxAllowedAnswers: maxAllowedAnswers);

                // assert
                var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                Assert.AreEqual(areAnswersOrdered, risedEvent.AreAnswersOrdered);
                Assert.AreEqual(maxAllowedAnswers, risedEvent.MaxAllowedAnswers);
            }
        }

        [Test]
        public void NewUpdateQuestion_When_MaxAllowedAnswers_For_MultiQuestion_Is_Negative_Then_DomainException_of_type_MaxAllowedAnswersIsNotPositive_should_be_thrown()
        {
            // arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.MultyOption, "Question",
                false, false, QuestionScope.Interviewer, "", "", "", "",
                new Option[2] { new Option(Guid.NewGuid(), "1", "title"), new Option(Guid.NewGuid(), "2", "title1") }, Order.AZ,
                responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: -1);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.MaxAllowedAnswersIsNotPositive));
        }

        [Test]
        public void NewUpdateQuestion_When_MaxAllowedAnswers_For_MultiQuestion_More_Than_Options_Then_DomainException_of_type_MaxAllowedAnswersMoreThanOptions_should_be_thrown()
        {
            // arrange
            Guid targetQuestionPublicKey = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            var questionnaire = CreateQuestionnaireWithOneQuestion(questionId: targetQuestionPublicKey, responsibleId: responsibleId);

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(targetQuestionPublicKey, "Title", QuestionType.MultyOption, "Question",
                false, false, QuestionScope.Interviewer, "", "", "", "",
                new Option[2] { new Option(Guid.NewGuid(), "1", "title"), new Option(Guid.NewGuid(), "2", "title1") }, Order.AZ,
                responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false, maxAllowedAnswers: 3);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.MaxAllowedAnswersMoreThanOptions));
        }

        [Test]
        public void NewUpdateQuestion_When_categorical_multi_question_with_linked_question_that_has_max_allowed_answers_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            const int maxAllowedAnswers = 5;
            // arrange
            Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
            Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
            Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
            Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire =
                CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                    rosterId: autoGroupId,
                    secondGroup: groupId,
                    autoQuestionId: autoQuestionId,
                    questionId: questionId,
                    responsibleId: responsibleId,
                    questionType: QuestionType.MultyOption);

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(
                questionId: questionId,
                title: "Question",
                type: QuestionType.MultyOption,
                alias: "test",
                isMandatory: false,
                isFeatured: false,
                scope: QuestionScope.Interviewer,
                condition: string.Empty,
                validationExpression: string.Empty,
                validationMessage: string.Empty,
                instructions: string.Empty,
                options: null,
                optionsOrder: Order.AZ,
                responsibleId: responsibleId,
                linkedToQuestionId: autoQuestionId,
                areAnswersOrdered: false,
                maxAllowedAnswers: maxAllowedAnswers);

            // assert
            Assert.DoesNotThrow(act);
        }        

        [Test]
        public void NewUpdateQuestion_When_categorical_multi_question_with_linked_question_that_has_max_allowed_answers_Then_QuestionChanged_event_with_max_allowed_answers_value_should_be_raised()
        {
                using (var eventContext = new EventContext())
                {
                    // arrange
                    const int maxAllowedAnswers = 5;
                    // arrange
                    Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
                    Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                    Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
                    Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                    Guid responsibleId = Guid.NewGuid();
                    Questionnaire questionnaire =
                        CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                            rosterId: autoGroupId,
                            secondGroup: groupId,
                            autoQuestionId: autoQuestionId,
                            questionId: questionId,
                            responsibleId: responsibleId,
                            questionType: QuestionType.MultyOption);

                    // act
                    questionnaire.NewUpdateQuestion(
                        questionId: questionId,
                        title: "Question",
                        type: QuestionType.MultyOption,
                        alias: "test",
                        isMandatory: false,
                        isFeatured: false,
                        scope: QuestionScope.Interviewer,
                        condition: string.Empty,
                        validationExpression: string.Empty,
                        validationMessage: string.Empty,
                        instructions: string.Empty,
                        options: null,
                        optionsOrder: Order.AZ,
                        responsibleId: responsibleId,
                        linkedToQuestionId: autoQuestionId,
                        areAnswersOrdered: false,
                        maxAllowedAnswers: maxAllowedAnswers);

                    // assert
                    var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                    Assert.AreEqual(maxAllowedAnswers, risedEvent.MaxAllowedAnswers);
                }
        }

        [Test]
        public void NewUpdateQuestion_When_categorical_multi_question_with_linked_question_ordered_Then_QuestionChanged_event_with_ordered_value_should_be_raised()
        {
            using (var eventContext = new EventContext())
            {
                // arrange
                const bool areAnswersOrdered = true;
                // arrange
                Guid autoQuestionId = Guid.Parse("00000000-1111-0000-2222-111000000000");
                Guid questionId = Guid.Parse("00000000-1111-0000-2222-000000000000");
                Guid autoGroupId = Guid.Parse("00000000-1111-0000-3333-111000000000");
                Guid groupId = Guid.Parse("00000000-1111-0000-3333-000000000000");
                Guid responsibleId = Guid.NewGuid();
                Questionnaire questionnaire =
                    CreateQuestionnaireWithAutoGroupAndRegularGroupAndQuestionsInThem(
                        rosterId: autoGroupId,
                        secondGroup: groupId,
                        autoQuestionId: autoQuestionId,
                        questionId: questionId,
                        responsibleId: responsibleId,
                        questionType: QuestionType.MultyOption);

                // act
                questionnaire.NewUpdateQuestion(
                    questionId: questionId,
                    title: "Question",
                    type: QuestionType.MultyOption,
                    alias: "test",
                    isMandatory: false,
                    isFeatured: false,
                    scope: QuestionScope.Interviewer,
                    condition: string.Empty,
                    validationExpression: string.Empty,
                    validationMessage: string.Empty,
                    instructions: string.Empty,
                    options: null,
                    optionsOrder: Order.AZ,
                    responsibleId: responsibleId,
                    linkedToQuestionId: autoQuestionId,
                    areAnswersOrdered: areAnswersOrdered,
                    maxAllowedAnswers: null);

                // assert
                var risedEvent = GetSingleEvent<QuestionChanged>(eventContext);
                Assert.AreEqual(areAnswersOrdered, risedEvent.AreAnswersOrdered);
            }
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Condition_With_Reference_To_Existing_Question_Variable_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasQuestion2 = "q2";
            string expression = string.Format("[{0}] > 0", aliasQuestion2);

            RegisterExpressionProcessorMock(expression, new[] { aliasQuestion2 });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, aliasQuestion2);

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, expression, string.Empty,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Condition_With_Reference_To_Existing_Question_Id_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid question2Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string expression = string.Format("[{0}] > 0", question2Id);

            RegisterExpressionProcessorMock(expression, new[] { question2Id.ToString() });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, question2Id, groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, expression, string.Empty,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Validation_With_Reference_To_Existing_Question_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasQuestion2 = "q2";
            string expression = string.Format("[{0}] > 0", aliasQuestion2);

            RegisterExpressionProcessorMock(expression, new[] { aliasQuestion2 });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, aliasQuestion2);
            
            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, string.Empty, expression,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Validation_With_Reference_To_Existing_Question_Variable_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasQuestion2 = "q2";
            string expression = string.Format("[{0}] > 0", aliasQuestion2);

            RegisterExpressionProcessorMock(expression, new[] { aliasQuestion2 });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, aliasQuestion2);

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, string.Empty, expression,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Validation_With_Reference_To_Existing_Question_Id_Then_DomainException_should_NOT_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid question2Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string expression = string.Format("[{0}] > 0", question2Id);

            RegisterExpressionProcessorMock(expression, new[] { question2Id.ToString() });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, question2Id, groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, string.Empty, expression,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            Assert.DoesNotThrow(act);
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Condition_With_Reference_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForNotExistingQuestion = "q3";
            string expression = string.Format("[{0}] > 0", aliasForNotExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForNotExistingQuestion });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, expression, string.Empty,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Condition_With_2_References_And_Second_Of_Them_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForSecondQuestion = "q2";
            string aliasForNotExistingQuestion = "q3";
            string expression = string.Format("[{0}] > 0 AND [{1}] > 1", aliasForSecondQuestion, aliasForNotExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForSecondQuestion, aliasForNotExistingQuestion });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, aliasForSecondQuestion);

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, expression, string.Empty,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Validation_With_Reference_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForNotExistingQuestion = "q3";
            string expression = string.Format("[{0}] > 0", aliasForNotExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForNotExistingQuestion });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, "q2");

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, string.Empty, expression,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
        }

        [Test]
        public void NewUpdateQuestion_When_Question_Have_Validation_With_2_References_And_Second_Of_Them_To_Not_Existing_Question_Then_DomainException_should_be_thrown()
        {
            // arrange
            Guid question1Id = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            Guid responsibleId = Guid.NewGuid();
            Questionnaire questionnaire = CreateQuestionnaireWithOneGroup(responsibleId: responsibleId,
                groupId: groupId);
            string aliasForSecondQuestion = "q2";
            string aliasForNotExistingQuestion = "q3";
            string expression = string.Format("[{0}] > 0 AND [{1}] > 1", aliasForSecondQuestion, aliasForNotExistingQuestion);

            RegisterExpressionProcessorMock(expression, new[] { aliasForSecondQuestion, aliasForNotExistingQuestion });

            AddQuestion(questionnaire, question1Id, groupId, responsibleId, QuestionType.Text, "q1");
            AddQuestion(questionnaire, Guid.NewGuid(), groupId, responsibleId, QuestionType.Text, aliasForSecondQuestion);

            // act
            TestDelegate act = () => questionnaire.NewUpdateQuestion(question1Id, "Title", QuestionType.Text, "test", false, false,
                QuestionScope.Interviewer, string.Empty, expression,
                string.Empty,
                string.Empty, new Option[0], Order.AZ, responsibleId: responsibleId, linkedToQuestionId: null, areAnswersOrdered: false,
                maxAllowedAnswers: null);

            // assert
            var domainException = Assert.Throws<QuestionnaireException>(act);
            Assert.That(domainException.ErrorType, Is.EqualTo(DomainExceptionType.ExpressionContainsNotExistingQuestionReference));
        }

    }
}