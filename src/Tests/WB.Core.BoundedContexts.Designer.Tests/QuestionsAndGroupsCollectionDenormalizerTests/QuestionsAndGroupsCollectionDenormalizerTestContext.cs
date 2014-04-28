﻿using System;
using System.Collections.Generic;
using Main.Core.Documents;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using Moq;
using Ncqrs.Eventing.ServiceModel.Bus;
using WB.Core.BoundedContexts.Designer.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Implementation.Factories;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionInfo;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;

namespace WB.Core.BoundedContexts.Designer.Tests.QuestionsAndGroupsCollectionDenormalizerTests
{
    internal class QuestionsAndGroupsCollectionDenormalizerTestContext
    {
        protected static QuestionsAndGroupsCollectionDenormalizer CreateQuestionnaireInfoDenormalizer(
            IReadSideRepositoryWriter<QuestionsAndGroupsCollectionView> readsideRepositoryWriter = null,
            IQuestionDetailsFactory questionDetailsFactory = null, 
            IQuestionFactory questionFactory = null)
        {
            return new QuestionsAndGroupsCollectionDenormalizer(readsideRepositoryWriter ?? Mock.Of<IReadSideRepositoryWriter<QuestionsAndGroupsCollectionView>>(),
                questionDetailsFactory ?? Mock.Of<IQuestionDetailsFactory>(),
                questionFactory ?? Mock.Of<IQuestionFactory>());
        }

        protected static IPublishedEvent<T> ToPublishedEvent<T>(T @event)
           where T : class
        {
            return Mock.Of<IPublishedEvent<T>>(publishedEvent
                => publishedEvent.Payload == @event);
        }

        protected static IPublishedEvent<GroupDeleted> CreateGroupDeletedEvent(Guid groupId)
        {
            return ToPublishedEvent(new GroupDeleted
            {
                GroupPublicKey = groupId,
            });
        }

        protected static IPublishedEvent<QuestionDeleted> CreateQuestionDeletedEvent(Guid questionId)
        {
            return ToPublishedEvent(new QuestionDeleted
            {
                QuestionId = questionId,
            });
        }

        protected static IPublishedEvent<NewGroupAdded> CreateNewGroupAddedEvent(Guid groupId, Guid parentGroupId, string enablementCondition, string description, string title)
        {
            return ToPublishedEvent(new NewGroupAdded
            {
                PublicKey = groupId,
                GroupText = title,
                ParentGroupPublicKey = parentGroupId,
                Description = description,
                ConditionExpression = enablementCondition
            });
        }

        protected static IPublishedEvent<GroupCloned> CreateGroupClonedEvent(Guid groupId, Guid parentGroupId, string enablementCondition, string description, string title)
        {
            return ToPublishedEvent(new GroupCloned
            {
                PublicKey = groupId,
                GroupText = title,
                ParentGroupPublicKey = parentGroupId,
                Description = description,
                ConditionExpression = enablementCondition
            });
        }

        protected static IPublishedEvent<GroupUpdated> CreateGroupUpdatedEvent(Guid groupId,
            string title = "Updated Group Title X")
        {
            return ToPublishedEvent(new GroupUpdated
            {
                GroupPublicKey = groupId,
                GroupText = title,
            });
        }

        protected static IPublishedEvent<NewQuestionAdded> CreateNewQuestionAddedEvent(Guid questionId, Guid? groupId = null, string title = "New Question X", List<Guid> triggers = null)
        {
            return ToPublishedEvent(new NewQuestionAdded
            {
                PublicKey = questionId,
                GroupPublicKey = groupId,
                QuestionText = title,
                QuestionType = QuestionType.Numeric,
                Triggers = triggers ?? new List<Guid>()
            });
        }

        protected static IPublishedEvent<QuestionChanged> CreateQuestionChangedEvent(Guid questionId, string title, QuestionType questionType = QuestionType.Numeric, List<Guid> triggers = null)
        {
            return ToPublishedEvent(new QuestionChanged
            {
                PublicKey = questionId,
                QuestionType = questionType,
                QuestionText = title,
                Triggers = triggers ?? new List<Guid>()
            });
        }

        protected static IPublishedEvent<QuestionnaireItemMoved> CreateQuestionnaireItemMovedEvent(Guid itemId, Guid? targetGroupId)
        {
            return ToPublishedEvent(new QuestionnaireItemMoved
            {
                PublicKey = itemId,
                GroupKey = targetGroupId,
            });
        }

        protected static IPublishedEvent<GroupBecameARoster> CreateGroupBecameARosterEvent(Guid groupId)
        {
            return ToPublishedEvent(new GroupBecameARoster(Guid.NewGuid(), groupId));
        }

        protected static IPublishedEvent<GroupStoppedBeingARoster> CreateGroupStoppedBeingARosterEvent(Guid groupId)
        {
            return ToPublishedEvent(new GroupStoppedBeingARoster(Guid.NewGuid(), groupId));
        }

        protected static IPublishedEvent<RosterChanged> CreateRosterChangedEvent(Guid groupId, Guid? rosterSizeQuestionId, RosterSizeSourceType rosterSizeSource, string[] rosterFixedTitles, Guid? rosterTitleQuestionId)
        {
            return
                ToPublishedEvent(new RosterChanged(Guid.NewGuid(), groupId, rosterSizeQuestionId, rosterSizeSource, rosterFixedTitles,
                    rosterTitleQuestionId));
        }

        protected static IPublishedEvent<NumericQuestionAdded> CreateNumericQuestionAddedEvent(
            Guid questionId, Guid? parentGroupId = null, int? maxValue = null, List<Guid> triggers = null)
        {
            return ToPublishedEvent(new NumericQuestionAdded
            {
                PublicKey = questionId,
                GroupPublicKey = parentGroupId ?? Guid.NewGuid(),
                MaxAllowedValue = maxValue,
                Triggers = triggers ?? new List<Guid>()
            });
        }

        protected static IPublishedEvent<QRBarcodeQuestionAdded> CreateQRBarcodeQuestionAddedEvent(
            Guid questionId, Guid parentGroupId)
        {
            return ToPublishedEvent(new QRBarcodeQuestionAdded
            {
                QuestionId = questionId,
                ParentGroupId = parentGroupId
            });
        }

        protected static IPublishedEvent<TextListQuestionAdded> CreateTextListQuestionAddedEvent(
            Guid questionId, Guid parentGroupId)
        {
            return ToPublishedEvent(new TextListQuestionAdded
            {
                PublicKey = questionId,
                GroupId = parentGroupId
            });
        }

        protected static IPublishedEvent<TextListQuestionCloned> CreateTextListQuestionClonedEvent(Guid questionId, Guid? groupId = null, Guid? sourceQuestionId = null)
        {
            return ToPublishedEvent(new TextListQuestionCloned
            {
                PublicKey = questionId,
                SourceQuestionId = sourceQuestionId ?? Guid.NewGuid(),
                GroupId = groupId ?? Guid.NewGuid(),
            });
        }

        protected static IPublishedEvent<TextListQuestionChanged> CreateTextListQuestionChangedEvent(Guid questionId)
        {
            return ToPublishedEvent(new TextListQuestionChanged
            {
                PublicKey = questionId
            });
        }

        protected static IPublishedEvent<QRBarcodeQuestionUpdated> CreateQRBarcodeQuestionUpdatedEvent(
            Guid questionId)
        {
            return ToPublishedEvent(new QRBarcodeQuestionUpdated
            {
                QuestionId = questionId,
            });
        }

        protected static IPublishedEvent<NumericQuestionChanged> CreateNumericQuestionChangedEvent(
            Guid questionId, int? maxValue = null, List<Guid> triggers = null)
        {
            return ToPublishedEvent(new NumericQuestionChanged
            {
                PublicKey = questionId,
                MaxAllowedValue = maxValue,
                Triggers = triggers ?? new List<Guid>()
            });
        }

        protected static IPublishedEvent<QRBarcodeQuestionCloned> CreateQRBarcodeQuestionClonedEvent(Guid questionId,Guid parentGroupId, Guid? sourceQuestionId = null)
        {
            return ToPublishedEvent(new QRBarcodeQuestionCloned
            {
                QuestionId = questionId,
                SourceQuestionId = sourceQuestionId ?? Guid.NewGuid(),
                ParentGroupId = parentGroupId
            });
        }
        protected static IPublishedEvent<NumericQuestionCloned> CreateNumericQuestionClonedEvent(
            Guid questionId, Guid? sourceQuestionId = null, Guid? parentGroupId = null, int? maxValue = null, List<Guid> triggers = null)
        {
            return ToPublishedEvent(new NumericQuestionCloned
            {
                PublicKey = questionId,
                SourceQuestionId = sourceQuestionId ?? Guid.NewGuid(),
                GroupPublicKey = parentGroupId ?? Guid.NewGuid(),
                MaxAllowedValue = maxValue,
                Triggers = triggers ?? new List<Guid>()
            });
        }

        protected static IPublishedEvent<QuestionCloned> CreateQuestionClonedEvent(
            Guid questionId, QuestionType questionType = QuestionType.Numeric, Guid? sourceQuestionId = null, Guid? parentGroupId = null, int? maxValue = null, List<Guid> triggers = null)
        {
            return ToPublishedEvent(new QuestionCloned
            {
                PublicKey = questionId,
                QuestionType = questionType,
                SourceQuestionId = sourceQuestionId ?? Guid.NewGuid(),
                GroupPublicKey = parentGroupId ?? Guid.NewGuid(),
                Triggers = triggers ?? new List<Guid>()
            });
        }

        protected static IPublishedEvent<QuestionnaireDeleted> CreateQuestionnaireDeletedEvent()
        {
            return ToPublishedEvent(new QuestionnaireDeleted
            {
            });
        }

        protected static IPublishedEvent<NewQuestionnaireCreated> CreateNewQuestionnaireCreatedEvent()
        {
            return ToPublishedEvent(new NewQuestionnaireCreated
            {
            });
        }


        protected static IPublishedEvent<TemplateImported> CreateTemplateImportedEvent(QuestionnaireDocument questionnaireDocument = null)
        {
            return ToPublishedEvent(new TemplateImported
            {
                Source = questionnaireDocument ?? new QuestionnaireDocument()
            });
        }

        protected static IPublishedEvent<QuestionnaireCloned> CreateQuestionnaireClonedEvent(QuestionnaireDocument questionnaireDocument = null)
        {
            return ToPublishedEvent(new QuestionnaireCloned
            {
                QuestionnaireDocument = questionnaireDocument ?? new QuestionnaireDocument()
            });
        }


        protected static IPublishedEvent<TextListQuestionAdded> CreateTextListQuestionAddedEvent(
            Guid questionId, Guid? parentGroupId = null, int? maxAnswerCount = null)
        {
            return ToPublishedEvent(new TextListQuestionAdded
            {
                PublicKey = questionId,
                GroupId = parentGroupId ?? Guid.NewGuid(),
                MaxAnswerCount = maxAnswerCount
            });
        }

        protected static IPublishedEvent<TextListQuestionCloned> TextListQuestionClonedEvent(
            Guid questionId, Guid? sourceQuestionId = null, Guid? parentGroupId = null, int? maxAnswerCount = null)
        {
            return ToPublishedEvent(new TextListQuestionCloned
            {
                PublicKey = questionId,
                SourceQuestionId = sourceQuestionId ?? Guid.NewGuid(),
                GroupId = parentGroupId ?? Guid.NewGuid(),
                MaxAnswerCount = maxAnswerCount
            });
        }

        protected static IPublishedEvent<TextListQuestionChanged> CreateTextListQuestionChangedEvent(
            Guid questionId, int? maxAnswerCount = null)
        {
            return ToPublishedEvent(new TextListQuestionChanged
            {
                PublicKey = questionId,
                MaxAnswerCount = maxAnswerCount

            });
        }
    }
}
