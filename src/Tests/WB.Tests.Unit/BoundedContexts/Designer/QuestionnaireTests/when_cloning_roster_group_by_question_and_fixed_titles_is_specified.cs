﻿using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Exceptions;

namespace WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireTests
{
    internal class when_cloning_roster_group_by_question_and_fixed_titles_is_specified : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
            var chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            sourceGroupId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            targetGroupId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            rosterSizeQuestionId = Guid.Parse("11111111111111111111111111111111");
            rosterFixedTitles = new[] { new Tuple<decimal, string>(1,"fixed title") };

            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.Apply(new NewGroupAdded { PublicKey = chapterId });
            questionnaire.Apply(new NewQuestionAdded
            {
                PublicKey = rosterSizeQuestionId,
                QuestionType = QuestionType.MultyOption,
                GroupPublicKey = chapterId
            });
        };

        Because of = () =>
            exception =
                Catch.Exception(
                    () =>
                        questionnaire.CloneGroupWithoutChildren(targetGroupId, responsibleId, "title",null, rosterSizeQuestionId, null, null,
                            null, sourceGroupId, 0, isRoster: true, rosterSizeSource: RosterSizeSourceType.Question,
                            rosterFixedTitles: rosterFixedTitles, rosterTitleQuestionId: null));
        
        It should_throw_QuestionnaireException = () =>
            exception.ShouldBeOfExactType<QuestionnaireException>();

        It should_throw_exception_with_message_containting__fixed__ = () =>
            exception.Message.ToLower().ShouldContain("fixed");

        It should_throw_exception_with_message_containting__titles__ = () =>
            exception.Message.ToLower().ShouldContain("titles");

        It should_throw_exception_with_message_containting__should__ = () =>
            exception.Message.ToLower().ShouldContain("should");

        It should_throw_exception_with_message_containting__be__ = () =>
            exception.Message.ToLower().ShouldContain("be");

        It should_throw_exception_with_message_containting__empty__ = () =>
            exception.Message.ToLower().ShouldContain("empty");

        private static Exception exception;
        private static Questionnaire questionnaire;
        private static Guid responsibleId;
        private static Guid sourceGroupId;
        private static Guid rosterSizeQuestionId;
        private static Guid targetGroupId;
        private static Tuple<decimal, string>[] rosterFixedTitles;
    }
}