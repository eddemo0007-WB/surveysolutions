﻿extern alias designer;

using System;
using System.Linq;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Main.Core.Events.Questionnaire;
using Main.Core.Events.User;
using Moq;
using Ncqrs.Eventing;
using Ncqrs.Eventing.ServiceModel.Bus;
using System.Collections.Generic;
using System.Globalization;
using MvvmCross.Platform.Core;
using MvvmCross.Plugins.Messenger;
using Ncqrs;
using Ncqrs.Eventing.Storage;
using Ncqrs.Spec;
using NSubstitute;
using WB.Core.BoundedContexts.Designer.Events.Questionnaire;
using WB.Core.BoundedContexts.Designer.Implementation.Factories;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.V5.Templates;
using WB.Core.BoundedContexts.Designer.Implementation.Services.LookupTableService;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.BoundedContexts.Designer.Views.Account;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.SharedPersons;
using WB.Core.BoundedContexts.Headquarters.DataExport.DataExportDetails;
using WB.Core.BoundedContexts.Headquarters.DataExport.Dtos;
using WB.Core.BoundedContexts.Headquarters.DataExport.Views.Labels;
using WB.Core.BoundedContexts.Headquarters.UserPreloading;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Dto;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.BoundedContexts.Tester.Implementation.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernel.Structures.Synchronization.Designer;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;
using WB.Core.SharedKernels.DataCollection.Events.Questionnaire;
using WB.Core.SharedKernels.DataCollection.Events.User;
using WB.Core.SharedKernels.DataCollection.Implementation.Accessors;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.Snapshots;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Implementation.Factories;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.V2;
using WB.Core.SharedKernels.DataCollection.ValueObjects;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.DataCollection.Views;
using WB.Core.SharedKernels.DataCollection.Views.BinaryData;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;
using WB.Core.SharedKernels.Enumerator.Aggregates;
using WB.Core.SharedKernels.Enumerator.Entities.Interview;
using WB.Core.SharedKernels.Enumerator.Implementation.Aggregates;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Questions.State;
using WB.Core.SharedKernels.SurveyManagement.Implementation.Factories;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using WB.Core.SharedKernels.SurveyManagement.Views.User;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using WB.Infrastructure.Native.Storage;
using WB.UI.Designer.Models;
using IEvent = WB.Core.Infrastructure.EventBus.IEvent;
using ILogger = WB.Core.GenericSubdomains.Portable.Services.ILogger;
using Questionnaire = WB.Core.BoundedContexts.Designer.Aggregates.Questionnaire;
using QuestionnaireDeleted = WB.Core.SharedKernels.DataCollection.Events.Questionnaire.QuestionnaireDeleted;
using QuestionnaireVersion = WB.Core.SharedKernel.Structures.Synchronization.Designer.QuestionnaireVersion;
using QuestionnaireView = WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionnaireView;
using WB.Core.GenericSubdomains.Portable.Implementation.Services;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Core.SharedKernels.SurveyManagement.EventHandler.WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using WB.Core.SharedKernels.SurveyManagement.Views.ChangeStatus;
using TemplateImported = designer::Main.Core.Events.Questionnaire.TemplateImported;
using designer::WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration.V6.Templates;
using Ncqrs.Domain;
using Ncqrs.Domain.Storage;
using Ncqrs.Eventing.Sourcing.Snapshotting;
using WB.Core.BoundedContexts.Headquarters.Interviews.Denormalizers;
using WB.Core.GenericSubdomains.Portable.CustomCollections;
using WB.Core.Infrastructure.Aggregates;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.EventBus.Hybrid.Implementation;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.Enumerator.Implementation.Repositories;
using WB.Core.SharedKernels.Enumerator.Implementation.Services;
using WB.Core.SharedKernels.Enumerator.Views;
using WB.Core.SharedKernels.NonConficltingNamespace;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Implementation.Services;
using WB.Core.SharedKernels.SurveyManagement.Services;
using WB.Core.SharedKernels.SurveyManagement.Synchronization.Interview;
using AttachmentContent = WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire.AttachmentContent;
using AttachmentsController = WB.Core.SharedKernels.SurveyManagement.Web.Controllers.AttachmentsController;

namespace WB.Tests.Unit.TestFactories
{
    internal class OtherFactory
    {
        public AccountDocument AccountDocument(string userName="")
        {
            return new AccountDocument() { UserName = userName };
        }

        public DataExportProcessDetails AllDataExportProcess(QuestionnaireIdentity questionnaireIdentity = null)
        {
            return new DataExportProcessDetails(
                DataExportFormat.Tabular,
                questionnaireIdentity ?? new QuestionnaireIdentity(Guid.NewGuid(), 1),
                "some questionnaire");
        }

        public Answer Answer(string answer, decimal value, decimal? parentValue = null)
        {
            return new Answer()
            {
                AnswerText = answer,
                AnswerValue = value.ToString(),
                ParentValue = parentValue.HasValue ? parentValue.ToString() : null
            };
        }

        public AnsweredQuestionSynchronizationDto AnsweredQuestionSynchronizationDto(
            Guid? questionId = null, decimal[] rosterVector = null, object answer = null)
        {
            return new AnsweredQuestionSynchronizationDto(
                questionId ?? Guid.NewGuid(),
                rosterVector ?? Core.SharedKernels.DataCollection.RosterVector.Empty,
                answer ?? "42",
                "no comment");
        }

        public AnsweredYesNoOption AnsweredYesNoOption(decimal value, bool answer)
        {
            return new AnsweredYesNoOption(value, answer);
        }

        public AnswerNotifier AnswerNotifier()
        {
            return new AnswerNotifier(Create.Service.LiteEventRegistry());
        }

        public IAnswerToStringService AnswerToStringService()
        {
            return new AnswerToStringService();
        }

        public AttachmentContentService AttachmentContentService(IPlainStorageAccessor<AttachmentContent> attachmentContentPlainStorage)
        {
            return new AttachmentContentService(attachmentContentPlainStorage ?? Mock.Of<IPlainStorageAccessor<AttachmentContent>>());
        }

        public AttachmentsController AttachmentsController(IAttachmentContentService attachmentContentService)
        {
            return new AttachmentsController(attachmentContentService);
        }

        public AttachmentContent AttachmentContent(string contentHash = null, string contentType = null, byte[] content = null)
        {
            return new AttachmentContent
            {
                ContentHash = contentHash ?? "content id",
                ContentType = contentType ,
                Content = content ?? new byte[] {1, 2, 3}
            };
        }

        public Attachment Attachment(string attachementHash) => new Attachment { ContentId = attachementHash };

        public CategoricalQuestionOption CategoricalQuestionOption(int value, string title, int? parentValue = null)
        {
            return new CategoricalQuestionOption
                   {
                       Value = value,
                       Title = title,
                       ParentValue = parentValue
                   };
        }

        public Group Chapter(string title = "Chapter X",Guid? chapterId=null, bool hideIfDisabled = false, IEnumerable<IComposite> children = null)
        {
            return Create.Other.Group(
                title: title,
                groupId: chapterId,
                hideIfDisabled: hideIfDisabled,
                children: children);
        }

        public Group Section(string title = "Section X", Guid? sectionId = null, IEnumerable<IComposite> children = null)
            => Create.Other.Group(
                title: title,
                groupId: sectionId,
                children: children);

        public CodeGenerationSettings CodeGenerationSettings()
        {
            return new CodeGenerationSettings(
                additionInterfaces: new[] { "IInterviewExpressionStateV5" },
                namespaces: new[]
                {
                    "WB.Core.SharedKernels.DataCollection.V2",
                    "WB.Core.SharedKernels.DataCollection.V2.CustomFunctions",
                    "WB.Core.SharedKernels.DataCollection.V3.CustomFunctions",
                    "WB.Core.SharedKernels.DataCollection.V4",
                    "WB.Core.SharedKernels.DataCollection.V4.CustomFunctions",
                    "WB.Core.SharedKernels.DataCollection.V5",
                    "WB.Core.SharedKernels.DataCollection.V5.CustomFunctions"
                },
                isLookupTablesFeatureSupported: true,
                expressionStateBodyGenerator: expressionStateModel => new InterviewExpressionStateTemplateV5(expressionStateModel).TransformText());
        }

        public CodeGenerationSettings CodeGenerationSettingsV6()
        {
            return new CodeGenerationSettings(
                additionInterfaces: new[] { "IInterviewExpressionStateV6" },
                namespaces: new[]
                {
                    "WB.Core.SharedKernels.DataCollection.V2",
                    "WB.Core.SharedKernels.DataCollection.V2.CustomFunctions",
                    "WB.Core.SharedKernels.DataCollection.V3.CustomFunctions",
                    "WB.Core.SharedKernels.DataCollection.V4",
                    "WB.Core.SharedKernels.DataCollection.V4.CustomFunctions",
                    "WB.Core.SharedKernels.DataCollection.V5",
                    "WB.Core.SharedKernels.DataCollection.V5.CustomFunctions",
                    "WB.Core.SharedKernels.DataCollection.V5"
                },
                isLookupTablesFeatureSupported: true,
                expressionStateBodyGenerator: expressionStateModel => new InterviewExpressionStateTemplateV6(expressionStateModel).TransformText());
        }

        public CommittedEvent CommittedEvent(string origin = null, Guid? eventSourceId = null, IEvent payload = null,
            Guid? eventIdentifier = null, int eventSequence = 1, Guid? commitId = null)
        {
            return new CommittedEvent(
                commitId ?? Guid.NewGuid(),
                origin,
                eventIdentifier ?? Guid.Parse("44440000444440000004444400004444"),
                eventSourceId ?? Guid.Parse("55550000555550000005555500005555"),
                eventSequence,
                new DateTime(2014, 10, 22),
                0,
                payload ?? Mock.Of<IEvent>());
        }

        private QuestionnaireDocument CreateQuestionnaireDocument(string questionnaireId,
            string questionnaireTitle,
            string chapter1Id,
            string chapter1Title,
            string chapter2Id,
            string chapter2Title,
            string chapter1GroupId,
            string chapter1GroupTitle,
            string chapter2QuestionId,
            string chapter2QuestionTitle,
            string chapter2QuestionVariable,
            string chapter2QuestionConditionExpression,
            string chapter1StaticTextId,
            string chapter1StaticText,
            bool isPublic)
        {
            return new QuestionnaireDocument()
            {
                PublicKey = Guid.Parse(questionnaireId),
                Title = questionnaireTitle,
                IsPublic = isPublic,
                Children = new List<IComposite>()
                {
                    new Group()
                    {
                        PublicKey = Guid.Parse(chapter1Id),
                        Title = chapter1Title,
                        Children = new List<IComposite>()
                        {
                            StaticText(publicKey: GetQuestionnaireItemId(chapter1StaticTextId), text: chapter1StaticText),
                            new Group()
                            {
                                PublicKey = GetQuestionnaireItemId(chapter1GroupId),
                                Title = chapter1GroupTitle,
                                Children = new List<IComposite>()
                                {
                                    new Group()
                                    {
                                        IsRoster = true
                                    }
                                }
                            }
                        }
                    },
                    new Group()
                    {
                        PublicKey = Guid.Parse(chapter2Id),
                        Title = chapter2Title,
                        Children = new List<IComposite>()
                        {
                            new TextQuestion()
                            {
                                PublicKey = GetQuestionnaireItemId(chapter2QuestionId),
                                QuestionText = chapter2QuestionTitle,
                                StataExportCaption = chapter2QuestionVariable,
                                QuestionType = QuestionType.Text,
                                ConditionExpression = chapter2QuestionConditionExpression
                            }
                        }
                    }
                }
            };
        }

        public Core.SharedKernels.SurveyManagement.Implementation.Aggregates.Questionnaire DataCollectionQuestionnaire(
            IPlainQuestionnaireRepository plainQuestionnaireRepository = null,
            IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaireBrowseItemStorage = null,
            IFileSystemAccessor fileSystemAccessor = null)
            => new Core.SharedKernels.SurveyManagement.Implementation.Aggregates.Questionnaire(
                plainQuestionnaireRepository ?? Mock.Of<IPlainQuestionnaireRepository>(),
                Mock.Of<IQuestionnaireAssemblyFileAccessor>(),
                new ReferenceInfoForLinkedQuestionsFactory(),
                new QuestionnaireRosterStructureFactory(),
                questionnaireBrowseItemStorage ?? Mock.Of<IPlainStorageAccessor<QuestionnaireBrowseItem>>(),
                Mock.Of<IPlainKeyValueStorage<ReferenceInfoForLinkedQuestions>>(),
                Mock.Of<IPlainKeyValueStorage<QuestionnaireRosterStructure>>(),
                Mock.Of<IPlainKeyValueStorage<QuestionnaireQuestionsInfo>>(),
                fileSystemAccessor ?? Mock.Of<IFileSystemAccessor>());

        public DateTimeQuestion DateTimeQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            string variable = null, string validationMessage = null, string text = null, QuestionScope scope = QuestionScope.Interviewer, 
            bool preFilled = false, bool hideIfDisabled = false)
        {
            return new DateTimeQuestion("Question DT")
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                QuestionText = text,
                QuestionType = QuestionType.DateTime,
                StataExportCaption = variable,
                QuestionScope = scope,
                Featured = preFilled
            };
        }

        public IMacrosSubstitutionService DefaultMacrosSubstitutionService()
        {
            var macrosSubstitutionServiceMock = new Mock<IMacrosSubstitutionService>();
            macrosSubstitutionServiceMock.Setup(
                x => x.InlineMacros(It.IsAny<string>(), It.IsAny<IEnumerable<Macro>>()))
                .Returns((string e, IEnumerable<Macro> macros) =>
                {
                    return e;
                });

            return macrosSubstitutionServiceMock.Object;
        }

        public DownloadQuestionnaireRequest DownloadQuestionnaireRequest(Guid? questionnaireId, QuestionnaireVersion questionnaireVersion = null)
        {
            return new DownloadQuestionnaireRequest()
            {
                QuestionnaireId = questionnaireId ?? Guid.NewGuid(),
                SupportedVersion = questionnaireVersion ?? new QuestionnaireVersion()
            };
        }

        public EnablementChanges EnablementChanges(
            List<Identity> groupsToBeDisabled = null, 
            List<Identity> groupsToBeEnabled = null,
            List<Identity> questionsToBeDisabled = null, 
            List<Identity> questionsToBeEnabled = null)
        {
            return new EnablementChanges(
                groupsToBeDisabled ?? new List<Identity>(),
                groupsToBeEnabled ?? new List<Identity>(),
                questionsToBeDisabled ?? new List<Identity>(),
                questionsToBeEnabled ?? new List<Identity>(),
                new List<Identity>(),
                new List<Identity>());
        }

        public EnumerationStageViewModel EnumerationStageViewModel(
            IInterviewViewModelFactory interviewViewModelFactory = null,
            IPlainQuestionnaireRepository questionnaireRepository = null,
            IStatefulInterviewRepository interviewRepository = null,
            ISubstitutionService substitutionService = null,
            ILiteEventRegistry eventRegistry = null,
            IMvxMessenger messenger = null,
            IUserInterfaceStateService userInterfaceStateService = null,
            IMvxMainThreadDispatcher mvxMainThreadDispatcher = null)
            => new EnumerationStageViewModel(
                interviewViewModelFactory ?? Mock.Of<IInterviewViewModelFactory>(),
                questionnaireRepository ?? Stub<IPlainQuestionnaireRepository>.WithNotEmptyValues,
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                substitutionService ?? Mock.Of<ISubstitutionService>(),
                eventRegistry ?? Mock.Of<ILiteEventRegistry>(),
                userInterfaceStateService ?? Mock.Of<IUserInterfaceStateService>(),
                mvxMainThreadDispatcher ?? Stub.MvxMainThreadDispatcher());

        public EventContext EventContext()
        {
            return new EventContext();
        }

        public ExportedHeaderItem ExportedHeaderItem(Guid? questionId=null, string variableName="var")
        {
            return new ExportedHeaderItem()
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                ColumnNames = new[] {variableName}
            };
        }

        public ExportedQuestion ExportedQuestion()
        {
            return new ExportedQuestion() {Answers = new string[0]};
        }

        public FailedValidationCondition FailedValidationCondition(int? failedConditionIndex = null)
            => new FailedValidationCondition(failedConditionIndex ?? 1117);

        public FixedRosterTitle FixedRosterTitle(decimal value, string title)
        {
            return new FixedRosterTitle(value, title);
        }

        public GenerationResult GenerationResult(bool success=false)
        {
            return new GenerationResult() {Success = success};
        }

        public GeoPosition GeoPosition()
        {
            return new GeoPosition(1, 2, 3, 4, new DateTimeOffset(new DateTime(1984,4,18)));
        }

        private static Guid GetGuidIdByStringId(string stringId)
        {
            return string.IsNullOrEmpty(stringId) ? Guid.NewGuid() : Guid.Parse(stringId);
        }

        private static Guid GetQuestionnaireItemId(string questionnaireItemId)
        {
            return string.IsNullOrEmpty(questionnaireItemId) ? Guid.NewGuid() : Guid.Parse(questionnaireItemId);
        }

        private static Guid? GetQuestionnaireItemParentId(string questionnaireItemParentId)
        {
            return string.IsNullOrEmpty(questionnaireItemParentId)
                ? (Guid?)null
                : Guid.Parse(questionnaireItemParentId);
        }

        public GpsCoordinateQuestion GpsCoordinateQuestion(Guid? questionId = null, string variable = "var1", bool isPrefilled=false, string title = null,
            string enablementCondition = null, string validationExpression = null, bool hideIfDisabled = false)
        {
            return new GpsCoordinateQuestion()
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                StataExportCaption = variable,
                QuestionType = QuestionType.GpsCoordinates,
                Featured = isPrefilled,
                QuestionText = title,
                ValidationExpression = validationExpression,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
            };
        }

        public Group Group(
            Guid? groupId = null,
            string title = "Group X",
            string variable = null,
            string enablementCondition = null,
            bool hideIfDisabled = false,
            IEnumerable<IComposite> children = null)
        {
            return new Group(title)
            {
                PublicKey = groupId ?? Guid.NewGuid(),
                VariableName = variable,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                Children = children != null ? children.ToList() : new List<IComposite>(),
            };
        }

        public Variable Variable(Guid? id = null, VariableType type = VariableType.LongInteger, string variableName = "v1", string expression = "2*2")
        {
            return new Variable(publicKey: id ?? Guid.NewGuid(),
                variableData: new VariableData(type: type, name: variableName, expression: expression));
        }

        public IPublishedEvent<GroupBecameARoster> GroupBecameARosterEvent(string groupId)
        {
            return ToPublishedEvent(new GroupBecameARoster(responsibleId: new Guid(), groupId: Guid.Parse(groupId)));
        }

        public IPublishedEvent<GroupCloned> GroupClonedEvent(string groupId, string groupTitle = null,
            string parentGroupId = null)
        {
            return ToPublishedEvent(new GroupCloned()
            {
                PublicKey = Guid.Parse(groupId),
                ParentGroupPublicKey = GetQuestionnaireItemParentId(parentGroupId),
                GroupText = groupTitle,
                TargetIndex = 0
            });
        }

        public IPublishedEvent<GroupDeleted> GroupDeletedEvent(string groupId)
        {
            return ToPublishedEvent(new GroupDeleted()
            {
                GroupPublicKey = Guid.Parse(groupId)
            });
        }

        private static IPublishedEvent<T> ToPublishedEvent<T>(T @event,
            Guid? eventSourceId = null,
            string origin = null,
            DateTime? eventTimeStamp = null,
            Guid? eventId = null)
            where T : class, IEvent
        {
            var mock = new Mock<IPublishedEvent<T>>();
            var eventIdentifier = eventId ?? Guid.NewGuid();
            mock.Setup(x => x.Payload).Returns(@event);
            mock.Setup(x => x.EventSourceId).Returns(eventSourceId ?? Guid.NewGuid());
            mock.Setup(x => x.Origin).Returns(origin);
            mock.Setup(x => x.EventIdentifier).Returns(eventIdentifier);
            mock.Setup(x => x.EventTimeStamp).Returns((eventTimeStamp ?? DateTime.Now));
            var publishableEventMock = mock.As<IUncommittedEvent>();
            publishableEventMock.Setup(x => x.Payload).Returns(@event);
            return mock.Object;
        }

        public IPublishedEvent<GroupStoppedBeingARoster> GroupStoppedBeingARosterEvent(string groupId)
        {
            return ToPublishedEvent(new GroupStoppedBeingARoster(responsibleId: new Guid(), groupId: Guid.Parse(groupId)));
        }

        public IPublishedEvent<GroupUpdated> GroupUpdatedEvent(string groupId, string groupTitle)
        {
            return ToPublishedEvent(new GroupUpdated()
            {
                GroupPublicKey = Guid.Parse(groupId),
                GroupText = groupTitle
            });
        }

        public HeaderStructureForLevel HeaderStructureForLevel()
        {
            return new HeaderStructureForLevel() {LevelScopeVector = new ValueVector<Guid>()};
        }

        public HybridEventBus HybridEventBus(ILiteEventBus liteEventBus = null, IEventBus cqrsEventBus = null)
        {
            return new HybridEventBus(
                liteEventBus ?? Mock.Of<ILiteEventBus>(),
                cqrsEventBus ?? Mock.Of<IEventBus>());
        }

        public Identity Identity(string id, RosterVector rosterVector)
            => Create.Other.Identity(Guid.Parse(id), rosterVector);

        public Identity Identity(Guid id, RosterVector rosterVector
            ) => new Identity(id, rosterVector);

        public Interview Interview(Guid? interviewId = null, IPlainQuestionnaireRepository questionnaireRepository = null,
            IInterviewExpressionStatePrototypeProvider expressionProcessorStatePrototypeProvider = null)
        {
            var interview = new Interview(
                Mock.Of<ILogger>(),
                questionnaireRepository ?? Mock.Of<IPlainQuestionnaireRepository>(),
                expressionProcessorStatePrototypeProvider ?? Stub.InterviewExpressionStateProvider());

            interview.SetId(interviewId ?? Guid.NewGuid());

            return interview;
        }

        public IPublishedEvent<InterviewApprovedByHQ> InterviewApprovedByHQEvent(Guid? interviewId = null, string userId = null, string comment = null)
        {
            return ToPublishedEvent(new InterviewApprovedByHQ(userId: GetGuidIdByStringId(userId), comment: comment), eventSourceId: interviewId);
        }

        public IPublishedEvent<InterviewApproved> InterviewApprovedEvent(Guid? interviewId = null, string userId = null, string comment = null)
        {
            return ToPublishedEvent(new InterviewApproved(userId: GetGuidIdByStringId(userId), comment: comment, approveTime: DateTime.Now), eventSourceId: interviewId);
        }

        public InterviewBinaryDataDescriptor InterviewBinaryDataDescriptor()
        {
            return new InterviewBinaryDataDescriptor(Guid.NewGuid(), "test.jpeg", () => new byte[0]);
        }

        public InterviewComment InterviewComment(string comment=null)
        {
            return new InterviewComment() {Comment = comment};
        }

        public InterviewCommentaries InterviewCommentaries(Guid? questionnaireId = null, long? questionnaireVersion = null, params InterviewComment[] comments)
        {
            return new InterviewCommentaries()
            {
                QuestionnaireId = (questionnaireId ?? Guid.NewGuid()).FormatGuid(),
                QuestionnaireVersion = questionnaireVersion ?? 1,
                Commentaries = new List<InterviewComment>(comments)
            };
        }

        public InterviewCommentedStatus InterviewCommentedStatus(
            Guid? statusId = null, 
            Guid? interviewerId = null, 
            Guid? supervisorId = null,
            DateTime? timestamp = null, 
            TimeSpan? timeSpanWithPreviousStatus = null, 
            InterviewExportedAction status = InterviewExportedAction.ApprovedBySupervisor)
        {
            return new InterviewCommentedStatus()
            {
                Id = statusId ?? Guid.NewGuid(),
                Status = status,
                Timestamp = timestamp ?? DateTime.Now,
                InterviewerId = interviewerId??Guid.NewGuid(),
                SupervisorId = supervisorId??Guid.NewGuid(),
                TimeSpanWithPreviousStatus = timeSpanWithPreviousStatus
            };
        }

        public IPublishedEvent<InterviewCompleted> InterviewCompletedEvent(Guid? interviewId = null, string userId = null, string comment = null, Guid? eventId = null)
        {
            return ToPublishedEvent(new InterviewCompleted(userId: GetGuidIdByStringId(userId), completeTime: DateTime.Now, comment: comment), eventSourceId: interviewId, eventId: eventId);
        }

        public IPublishedEvent<InterviewCreated> InterviewCreatedEvent(Guid? interviewId = null, string userId = null,
            string questionnaireId = null, long questionnaireVersion = 0)
        {
            return
                ToPublishedEvent(new InterviewCreated(userId: GetGuidIdByStringId(userId),
                    questionnaireId: GetGuidIdByStringId(questionnaireId), questionnaireVersion: questionnaireVersion), eventSourceId: interviewId);
        }

        public InterviewData InterviewData(bool createdOnClient = false,
            InterviewStatus status = InterviewStatus.Created,
            Guid? interviewId = null, 
            Guid? responsibleId = null)
        {
            var result = new InterviewData
                         {
                             CreatedOnClient = createdOnClient,
                             Status = status,
                             InterviewId = interviewId.GetValueOrDefault(),
                             ResponsibleId = responsibleId.GetValueOrDefault()
                         };
            return result;
        }

        public InterviewData InterviewData(params InterviewQuestion[] topLevelQuestions)
        {
            var interviewData = new InterviewData() { InterviewId = Guid.NewGuid() };
            interviewData.Levels.Add("#", new InterviewLevel(new ValueVector<Guid>(), null, new decimal[0]));
            foreach (var interviewQuestion in topLevelQuestions)
            {
                interviewData.Levels["#"].QuestionsSearchCache.Add(interviewQuestion.Id, interviewQuestion);
            }
            return interviewData;
        }

        public InterviewDataExportLevelView InterviewDataExportLevelView(Guid interviewId, params InterviewDataExportRecord[] records)
        {
            return new InterviewDataExportLevelView(new ValueVector<Guid>(), "test", records);
        }

        public InterviewDataExportRecord InterviewDataExportRecord(
            Guid interviewId,
            params ExportedQuestion[] questions)
        {
            return new InterviewDataExportRecord("test", new string[0], new string[0], new string [0])
            {
                Answers = questions.Select(x => String.Join("\n", x)).ToArray(), 
                LevelName = ""
            };
        }

        public InterviewDataExportView InterviewDataExportView(
            Guid? interviewId = null, 
            Guid? questionnaireId = null, 
            long questionnaireVersion = 1, 
            params InterviewDataExportLevelView[] levels)
        {
            return new InterviewDataExportView(interviewId ?? Guid.NewGuid(), levels);
        }

        public IPublishedEvent<InterviewDeleted> InterviewDeletedEvent(string userId = null, string origin = null, Guid? interviewId = null)
        {
            return ToPublishedEvent(new InterviewDeleted(userId: GetGuidIdByStringId(userId)), origin: origin, eventSourceId: interviewId);
        }

        public IPublishedEvent<InterviewerAssigned> InterviewerAssignedEvent(Guid? interviewId=null, string userId = null,
            string interviewerId = null)
        {
            return
                ToPublishedEvent(new InterviewerAssigned(userId: GetGuidIdByStringId(userId),
                    interviewerId: GetGuidIdByStringId(interviewerId), assignTime: DateTime.Now), eventSourceId: interviewId);
        }

        public InterviewExportedDataRecord InterviewExportedDataRecord()
        {
            return new InterviewExportedDataRecord();
        }

        public IPublishedEvent<InterviewFromPreloadedDataCreated> InterviewFromPreloadedDataCreatedEvent(Guid? interviewId = null, string userId = null,
            string questionnaireId = null, long questionnaireVersion = 0)
        {
            return
                ToPublishedEvent(new InterviewFromPreloadedDataCreated(userId: GetGuidIdByStringId(userId),
                    questionnaireId: GetGuidIdByStringId(questionnaireId), questionnaireVersion: questionnaireVersion), eventSourceId: interviewId);
        }

        public IPublishedEvent<InterviewHardDeleted> InterviewHardDeletedEvent(string userId = null, Guid? interviewId = null)
        {
            return ToPublishedEvent(new InterviewHardDeleted(userId: GetGuidIdByStringId(userId)), eventSourceId: interviewId);
        }

        public InterviewItemId InterviewItemId(Guid id, decimal[] rosterVector = null)
        {
            return new InterviewItemId(id, rosterVector);
        }

        public IPublishedEvent<InterviewOnClientCreated> InterviewOnClientCreatedEvent(Guid? interviewId = null, string userId = null,
            string questionnaireId = null, long questionnaireVersion = 0)
        {
            return
                ToPublishedEvent(new InterviewOnClientCreated(userId: GetGuidIdByStringId(userId),
                    questionnaireId: GetGuidIdByStringId(questionnaireId), questionnaireVersion: questionnaireVersion), eventSourceId: interviewId);
        }

        public InterviewQuestion InterviewQuestion(Guid? questionId = null, object answer = null)
        {
            var interviewQuestion = new InterviewQuestion(questionId ?? Guid.NewGuid());
            interviewQuestion.Answer = answer;
            if (answer != null)
            {
                interviewQuestion.QuestionState = interviewQuestion.QuestionState | QuestionState.Answered;
            }
            return interviewQuestion;
        }

        public InterviewReferences InterviewReferences(
            Guid? questionnaireId = null,
            long? questionnaireVersion = null) 
            => new InterviewReferences(
                Guid.NewGuid(),
                questionnaireId ?? Guid.NewGuid(),
                questionnaireVersion ?? 301);

        public InterviewReferencesDenormalizer InterviewReferencesDenormalizer()
        {
            return new InterviewReferencesDenormalizer(
                Mock.Of<IReadSideKeyValueStorage<InterviewReferences>>());
        }

        public IPublishedEvent<InterviewRejectedByHQ> InterviewRejectedByHQEvent(Guid? interviewId = null, string userId = null, string comment = null)
        {
            return ToPublishedEvent(new InterviewRejectedByHQ(userId: GetGuidIdByStringId(userId), comment: comment), eventSourceId: interviewId);
        }

        public IPublishedEvent<InterviewRejected> InterviewRejectedEvent(Guid? interviewId = null, string userId = null, string comment = null)
        {
            return ToPublishedEvent(new InterviewRejected(userId: GetGuidIdByStringId(userId), comment: comment, rejectTime: DateTime.Now), eventSourceId: interviewId);
        }

        public IPublishedEvent<InterviewRestarted> InterviewRestartedEvent(Guid? interviewId = null, string userId = null, string comment = null)
        {
            return ToPublishedEvent(new InterviewRestarted(userId: GetGuidIdByStringId(userId), restartTime: DateTime.Now, comment: comment), eventSourceId: interviewId);
        }

        public IPublishedEvent<InterviewRestored> InterviewRestoredEvent(Guid? interviewId = null, string userId = null,
            string origin = null)
        {
            return ToPublishedEvent(new InterviewRestored(userId: GetGuidIdByStringId(userId)), origin: origin, eventSourceId: interviewId);
        }

        public InterviewsFeedDenormalizer InterviewsFeedDenormalizer(IReadSideRepositoryWriter<InterviewFeedEntry> feedEntryWriter = null,
            IReadSideKeyValueStorage<InterviewData> interviewsRepository = null, IReadSideRepositoryWriter<InterviewSummary> interviewSummaryRepository = null)
        {
            return new InterviewsFeedDenormalizer(feedEntryWriter ?? Substitute.For<IReadSideRepositoryWriter<InterviewFeedEntry>>(),
                interviewsRepository ?? Substitute.For<IReadSideKeyValueStorage<InterviewData>>(), interviewSummaryRepository ?? Substitute.For<IReadSideRepositoryWriter<InterviewSummary>>());
        }

        public InterviewState InterviewState(InterviewStatus? status = null, List<AnswerComment> answerComments = null, Guid? interviewerId=null)
        {
            return new InterviewState(Guid.NewGuid(), 1, status ?? InterviewStatus.SupervisorAssigned, new Dictionary<string, object>(),
                new Dictionary<string, Tuple<Guid, decimal[], decimal[]>>(), new Dictionary<string, Tuple<Guid, decimal[], decimal[][]>>(),
                new Dictionary<string, Tuple<decimal, string>[]>(), new HashSet<string>(),
                answerComments ?? new List<AnswerComment>(),
                new HashSet<string>(),
                new HashSet<string>(), new Dictionary<string, ConcurrentHashSet<decimal>>(),
                new HashSet<string>(), new HashSet<string>(), true, Mock.Of<IInterviewExpressionStateV2>(), interviewerId?? Guid.NewGuid());
        }

        public IPublishedEvent<InterviewStatusChanged> InterviewStatusChangedEvent(InterviewStatus status,
            string comment = null,
            Guid? interviewId = null)
        {
            return ToPublishedEvent(new InterviewStatusChanged(status, comment), interviewId ?? Guid.NewGuid());
        }

        public InterviewStatuses InterviewStatuses(Guid? interviewid=null, Guid? questionnaireId=null, long? questionnaireVersion=null,params InterviewCommentedStatus[] statuses)
        {
            return new InterviewStatuses()
            {
                InterviewId = (interviewid??Guid.NewGuid()).FormatGuid(),
                InterviewCommentedStatuses = statuses.ToList(),
                QuestionnaireId = questionnaireId ?? Guid.NewGuid(),
                QuestionnaireVersion = questionnaireVersion ?? 1
            };
        }

        public InterviewStatusTimeSpans InterviewStatusTimeSpans(Guid? questionnaireId = null, long? questionnaireVersion = null, string interviewId = null, params TimeSpanBetweenStatuses[] timeSpans)
        {
            return new InterviewStatusTimeSpans()
            {
                QuestionnaireId = questionnaireId ?? Guid.NewGuid(),
                QuestionnaireVersion = questionnaireVersion ?? 1,
                TimeSpansBetweenStatuses = timeSpans.ToHashSet(),
                InterviewId = interviewId
            };
        }

        public InterviewSummary InterviewSummary() // needed since overload cannot be used in lambda expression
        {
            return new InterviewSummary();
        }

        public InterviewSummary InterviewSummary(
            Guid? interviewId=null,
            Guid? questionnaireId = null, 
            long? questionnaireVersion = null,
            InterviewStatus? status = null,
            Guid? responsibleId = null,
            Guid? teamLeadId = null,
            string responsibleName = null,
            string teamLeadName = null,
            UserRoles role = UserRoles.Operator)
        {
            return new InterviewSummary()
            {
                InterviewId = interviewId ?? Guid.NewGuid(),
                QuestionnaireId = questionnaireId ?? Guid.NewGuid(),
                QuestionnaireVersion = questionnaireVersion ?? 1,
                Status = status.GetValueOrDefault(),
                ResponsibleId = responsibleId.GetValueOrDefault(),
                ResponsibleName = string.IsNullOrWhiteSpace(responsibleName) ? responsibleId.FormatGuid() : responsibleName,
                TeamLeadId = teamLeadId.GetValueOrDefault(),
                TeamLeadName = string.IsNullOrWhiteSpace(teamLeadName) ? teamLeadId.FormatGuid() : teamLeadName,
                ResponsibleRole = role
            };
        }

        public InterviewSynchronizationDto InterviewSynchronizationDto(
            Guid? questionnaireId = null,
            long? questionnaireVersion = null,
            Guid? userId = null, 
            AnsweredQuestionSynchronizationDto[] answers = null,
            HashSet<InterviewItemId> disabledGroups = null,
            HashSet<InterviewItemId> disabledQuestions = null,
            HashSet<InterviewItemId> validQuestions = null,
            HashSet<InterviewItemId> invalidQuestions = null,
            Guid? interviewId = null,
            Dictionary<Identity, IList<FailedValidationCondition>> failedValidationConditions = null,
            InterviewStatus status = InterviewStatus.SupervisorAssigned,
            Dictionary<InterviewItemId, RosterSynchronizationDto[]> rosterGroupInstances = null,
            bool? wasCompleted = false,
            List<Identity> disabledStaticTexts = null,
            List<Identity> validStaticTexts = null,
            List<KeyValuePair<Identity, List<FailedValidationCondition>>> invalidStaticTexts = null,
            Dictionary<InterviewItemId, object> variables=null,
            HashSet<InterviewItemId> disabledVariables=null)
        {
            return new InterviewSynchronizationDto(
                interviewId ?? Guid.NewGuid(),
                status,
                "", 
                null,
                null,
                userId ?? Guid.NewGuid(),
                questionnaireId ?? Guid.NewGuid(), 
                questionnaireVersion ?? 1, 
                answers ?? new AnsweredQuestionSynchronizationDto[0],
                disabledGroups ?? new HashSet<InterviewItemId>(),
                disabledQuestions ?? new HashSet<InterviewItemId>(),
                disabledStaticTexts,
                validQuestions ?? new HashSet<InterviewItemId>(),
                invalidQuestions ?? new HashSet<InterviewItemId>(),
                validStaticTexts,
                invalidStaticTexts,
                rosterGroupInstances ?? new Dictionary<InterviewItemId, RosterSynchronizationDto[]>(),
                failedValidationConditions?.ToList() ?? new List<KeyValuePair<Identity, IList<FailedValidationCondition>>>(),
                new Dictionary<InterviewItemId, RosterVector[]>(),
                variables??new Dictionary<InterviewItemId, object>(),
                disabledVariables??new HashSet<InterviewItemId>(),  
                wasCompleted ?? false);
        }

        public InterviewView InterviewView(Guid? prefilledQuestionId = null)
        {
            return new InterviewView()
            {
                GpsLocation = new InterviewGpsLocationView
                {
                    PrefilledQuestionId = prefilledQuestionId
                }
            };
        }

        public LabeledVariable LabeledVariable(string variableName="var", string label="lbl", Guid? questionId=null, params VariableValueLabel[] variableValueLabels)
        {
            return new LabeledVariable(variableName, label, questionId, variableValueLabels);
        }

        public LastInterviewStatus LastInterviewStatus(InterviewStatus status = InterviewStatus.ApprovedBySupervisor)
            => new LastInterviewStatus("entry-id", status);

        public LookupTable LookupTable(string tableName, string fileName = null)
        {
            return new LookupTable
            {
                TableName = tableName,
                FileName = fileName ?? "lookup.tab"
            };
        }

        public LookupTableContent LookupTableContent(string[] variableNames, params LookupTableRow[] rows)
        {
            return new LookupTableContent
            {
                VariableNames = variableNames,
                Rows = rows
            };
        }

        public LookupTableRow LookupTableRow(long rowcode, decimal?[] values)
        {
            return new LookupTableRow
            {
                RowCode = rowcode,
                Variables = values
            };
        }

        public Macro Macro(string name, string content = null, string description = null)
        {
            return new Macro
            {
                Name = name,
                Content = content,
                Description = description
            };
        }

        public MultimediaQuestion MultimediaQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            string variable = null, string validationMessage = null, string text = null, QuestionScope scope = QuestionScope.Interviewer
            , bool hideIfDisabled = false)
        {
            return new MultimediaQuestion("Question T")
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                QuestionType = QuestionType.Multimedia,
                StataExportCaption = variable,
                QuestionScope = scope,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                QuestionText = text
            };
        }

        public IPublishedEvent<MultimediaQuestionUpdated> MultimediaQuestionUpdatedEvent(string questionId, string questionVariable = null, string questionTitle = null, string questionConditionExpression = null)
        {
            return ToPublishedEvent(new MultimediaQuestionUpdated()
            {
                QuestionId = Guid.Parse(questionId),
                VariableName = questionVariable,
                Title = questionTitle,
                EnablementCondition = questionConditionExpression
            });
        }

        public MultiOptionAnswer MultiOptionAnswer(Guid questionId, decimal[] rosterVector)
        {
            return new MultiOptionAnswer(questionId, rosterVector);
        }

        public IMultyOptionsQuestion MultipleOptionsQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            bool areAnswersOrdered = false, int? maxAllowedAnswers = null, Guid? linkedToQuestionId = null, bool isYesNo = false, bool hideIfDisabled = false,
            params decimal[] answers)
        {
            return new MultyOptionsQuestion("Question MO")
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                StataExportCaption = "mo_question",
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                AreAnswersOrdered = areAnswersOrdered,
                MaxAllowedAnswers = maxAllowedAnswers,
                QuestionType = QuestionType.MultyOption,
                LinkedToQuestionId = linkedToQuestionId,
                YesNoView = isYesNo,
                Answers = answers.Select(a => Create.Other.Answer(a.ToString(), a)).ToList()
            };
        }

        public MultyOptionsQuestion MultyOptionsQuestion(Guid? id = null, 
            IEnumerable<Answer> options = null, Guid? linkedToQuestionId = null, string variable = null, bool yesNoView=false,
            string enablementCondition = null, string validationExpression = null, Guid? linkedToRosterId =null, bool areAnswersOrdered=false)
        {
            return new MultyOptionsQuestion
            {
                QuestionType = QuestionType.MultyOption,
                PublicKey = id ?? Guid.NewGuid(),
                Answers = linkedToQuestionId.HasValue ? null : new List<Answer>(options ?? new Answer[] { }),
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                StataExportCaption = variable,
                YesNoView = yesNoView,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
                AreAnswersOrdered = areAnswersOrdered
            };
        }

        public NavigationState NavigationState(IStatefulInterviewRepository interviewRepository = null)
        {
            var result = new NavigationState(
                Mock.Of<ICommandService>(),
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                Mock.Of<IUserInteractionService>(),
                Mock.Of<IUserInterfaceStateService>());
            return result;
        }

        public IPublishedEvent<NewGroupAdded> NewGroupAddedEvent(string groupId, string parentGroupId = null,
            string groupTitle = null)
        {
            return ToPublishedEvent(new NewGroupAdded()
            {
                PublicKey = Guid.Parse(groupId),
                ParentGroupPublicKey = GetQuestionnaireItemParentId(parentGroupId),
                GroupText = groupTitle
            });
        }

        public IPublishedEvent<NewQuestionAdded> NewQuestionAddedEvent(string questionId = null,
            string parentGroupId = null, QuestionType questionType = QuestionType.Text, string questionVariable = null,
            string questionTitle = null, string questionConditionExpression = null)
        {
            return ToPublishedEvent(Create.Event.NewQuestionAdded(
                publicKey : GetQuestionnaireItemId(questionId),
                groupPublicKey : GetQuestionnaireItemId(parentGroupId),
                questionType : questionType,
                stataExportCaption : questionVariable,
                questionText : questionTitle,
                conditionExpression : questionConditionExpression
            ));
        }

        public IPublishedEvent<NewQuestionnaireCreated> NewQuestionnaireCreatedEvent(string questionnaireId,
            string questionnaireTitle = null,
            bool? isPublic = null)
        {
            return ToPublishedEvent(new NewQuestionnaireCreated()
            {
                PublicKey = new Guid(questionnaireId),
                Title = questionnaireTitle,
                IsPublic = isPublic ?? false
            }, new Guid(questionnaireId));
        }

        public NewUserCreated NewUserCreated(UserRoles role = UserRoles.Operator, Guid? supervisorId=null)
        {
            return new NewUserCreated() { Roles = new[] { role }, Supervisor = Create.Other.UserLight(supervisorId) };
        }

        public NumericQuestion NumericIntegerQuestion(Guid? id = null, 
            string variable = "numeric_question", 
            string enablementCondition = null, 
            string validationExpression = null, 
            QuestionScope scope = QuestionScope.Interviewer, 
            bool isPrefilled = false,
            bool hideIfDisabled = false,
            bool useFormatting = false, 
            IEnumerable<ValidationCondition> validationConditions = null, Guid? linkedToRosterId = null)
        {
            return new NumericQuestion
            {
                QuestionType = QuestionType.Numeric,
                PublicKey = id ?? Guid.NewGuid(),
                StataExportCaption = variable,
                IsInteger = true,
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                QuestionScope = scope,
                Featured = isPrefilled,
                UseFormatting = useFormatting,
                ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                LinkedToRosterId = linkedToRosterId,
            };
        }

        public INumericQuestion NumericQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null, 
            bool isInteger = false, int? countOfDecimalPlaces = null, string variableName = "var1", bool prefilled = false, string title=null)
        {
            return new NumericQuestion("Question N")
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
                IsInteger = isInteger,
                CountOfDecimalPlaces = countOfDecimalPlaces,
                QuestionType = QuestionType.Numeric,
                StataExportCaption = variableName,
                Featured = prefilled,
                QuestionText = title
            };
        }

        public IPublishedEvent<NumericQuestionChanged> NumericQuestionChangedEvent(string questionId,
            string questionVariable = null, string questionTitle = null, string questionConditionExpression = null)
        {
            return ToPublishedEvent(Create.Event.NumericQuestionChanged(
                publicKey : Guid.Parse(questionId),
                stataExportCaption : questionVariable,
                questionText : questionTitle,
                conditionExpression : questionConditionExpression
            ));
        }

        public IPublishedEvent<NumericQuestionCloned> NumericQuestionClonedEvent(string questionId = null,
            string parentGroupId = null, string questionVariable = null, string questionTitle = null,
            string questionConditionExpression = null, string sourceQuestionId = null)
        {
            return ToPublishedEvent(Create.Event.NumericQuestionCloned(
                publicKey : GetQuestionnaireItemId(questionId),
                groupPublicKey : GetQuestionnaireItemId(parentGroupId),
                stataExportCaption : questionVariable,
                questionText : questionTitle,
                conditionExpression : questionConditionExpression,
                sourceQuestionId : GetQuestionnaireItemId(sourceQuestionId),
                targetIndex : 0
            ));
        }

        public NumericQuestion NumericRealQuestion(Guid? id = null, 
            string variable = null, 
            string enablementCondition = null, 
            string validationExpression = null, 
            bool useFomatting = false,
            IEnumerable<ValidationCondition> validationConditions = null)
        {
            return new NumericQuestion
            {
                QuestionType = QuestionType.Numeric,
                PublicKey = id ?? Guid.NewGuid(),
                StataExportCaption = variable,
                IsInteger = false,
                UseFormatting = useFomatting,
                ConditionExpression = enablementCondition,
                ValidationConditions = validationConditions?.ToList() ?? new List<ValidationCondition>(),
                ValidationExpression = validationExpression
            };
        }

        public Group NumericRoster(Guid? rosterId, string variable, Guid? rosterSizeQuestionId, params IComposite[] children)
        {
            Group group = Create.Other.Group(
                groupId: rosterId,
                title: "Roster X",
                variable: variable,
                children: children);

            group.IsRoster = true;
            group.RosterSizeSource = RosterSizeSourceType.Question;
            group.RosterSizeQuestionId = rosterSizeQuestionId;
            return group;
        }

        public Answer Option(string value = null, string text = null, string parentValue = null, Guid? id = null)
        {
            return new Answer
            {
                PublicKey = id ?? Guid.NewGuid(),
                AnswerText = text ?? "text",
                AnswerValue = value ?? "1",
                ParentValue = parentValue
            };
        }

        public ParaDataExportProcessDetails ParaDataExportProcess()
        {
            return new ParaDataExportProcessDetails(DataExportFormat.Tabular);
        }

        public PlainQuestionnaire PlainQuestionnaire(QuestionnaireDocument document = null, long version = 19)
        {
            return new PlainQuestionnaire(
                document: document,
                version: version);
        }

        public IPublishableEvent PublishableEvent(Guid? eventSourceId = null, IEvent payload = null)
        {
            return Mock.Of<IPublishableEvent>(_ => _.Payload == (payload ?? Mock.Of<IEvent>()) && _.EventSourceId == (eventSourceId ?? Guid.NewGuid()));
        }

        public QRBarcodeQuestion QRBarcodeQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            string variable = null, string validationMessage = null, string text = null, QuestionScope scope = QuestionScope.Interviewer, bool preFilled = false, 
            bool hideIfDisabled = false)
        {
            return new QRBarcodeQuestion()
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                QuestionText = text,
                QuestionType = QuestionType.QRBarcode,
                StataExportCaption = variable,
                QuestionScope = scope,
                Featured = preFilled
            };
        }

        public IPublishedEvent<QRBarcodeQuestionCloned> QRBarcodeQuestionClonedEvent(string questionId = null,
            string parentGroupId = null, string questionVariable = null, string questionTitle = null,
            string questionConditionExpression = null, string sourceQuestionId = null,
            IList<ValidationCondition> validationConditions = null)
        {
            return ToPublishedEvent(new QRBarcodeQuestionCloned()
            {
                QuestionId = GetQuestionnaireItemId(questionId),
                ParentGroupId = GetQuestionnaireItemId(parentGroupId),
                VariableName = questionVariable,
                Title = questionTitle,
                EnablementCondition = questionConditionExpression,
                SourceQuestionId = GetQuestionnaireItemId(sourceQuestionId),
                TargetIndex = 0,
                ValidationConditions = validationConditions ?? new List<ValidationCondition>()
            });
        }

        public IPublishedEvent<QRBarcodeQuestionUpdated> QRBarcodeQuestionUpdatedEvent(string questionId,
            string questionVariable = null, string questionTitle = null, string questionConditionExpression = null)
        {
            return ToPublishedEvent(new QRBarcodeQuestionUpdated()
            {
                QuestionId = Guid.Parse(questionId),
                VariableName = questionVariable,
                Title = questionTitle,
                EnablementCondition = questionConditionExpression
            });
        }

        public IQuestion Question(
            Guid? questionId = null,
            string variable = "question",
            string enablementCondition = null,
            string validationExpression = null,
            string validationMessage = null,
            QuestionType questionType = QuestionType.Text,
            IList<ValidationCondition> validationConditions = null,
            params Answer[] answers)
        {
            return new TextQuestion("Question X")
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                QuestionType = questionType,
                StataExportCaption = variable,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                Answers = answers.ToList(),
                ValidationConditions = validationConditions ?? new List<ValidationCondition>()
            };
        }

        public IPublishedEvent<QuestionnaireDeleted> QuestionaireDeleted(Guid questionnaireId, long version)
        {
            return ToPublishedEvent(new QuestionnaireDeleted{QuestionnaireVersion = version}, eventSourceId: questionnaireId);
        }

        public IPublishedEvent<QuestionChanged> QuestionChangedEvent(string questionId, string parentGroupId=null,
            string questionVariable = null, string questionTitle = null, QuestionType? questionType = null, string questionConditionExpression = null)
        {
            return ToPublishedEvent(Create.Event.QuestionChanged(
                publicKey : Guid.Parse(questionId),
                groupPublicKey : Guid.Parse(parentGroupId?? Guid.NewGuid().ToString()),
                stataExportCaption : questionVariable,
                questionText : questionTitle,
                questionType : questionType ?? QuestionType.Text,
                conditionExpression : questionConditionExpression
            ));
        }

        public IPublishedEvent<QuestionCloned> QuestionClonedEvent(string questionId = null,
            string parentGroupId = null, string questionVariable = null, string questionTitle = null,
            QuestionType questionType = QuestionType.Text, string questionConditionExpression = null,
            string sourceQuestionId = null,
            IList<ValidationCondition> validationConditions = null, bool hideIfDisabled = false,
            QuestionProperties properties = null)
        {
            return ToPublishedEvent(new QuestionCloned(
                publicKey : GetQuestionnaireItemId(questionId),
                groupPublicKey : GetQuestionnaireItemId(parentGroupId),
                stataExportCaption : questionVariable,
                questionText : questionTitle,
                questionType : questionType,
                conditionExpression : questionConditionExpression,
                hideIfDisabled: hideIfDisabled,
                sourceQuestionId : GetQuestionnaireItemId(sourceQuestionId),
                targetIndex: 0,
                featured: false,
                instructions: null,
                properties: properties ?? new QuestionProperties(false, false),
                responsibleId: Guid.NewGuid(),
                capital: false,
                questionScope: QuestionScope.Interviewer,
                variableLabel: null,
                validationExpression: null,
                validationMessage:  null,
                answerOrder: null,
                answers: null,
                linkedToQuestionId: null,
                linkedToRosterId: null,
                isInteger: null,
                areAnswersOrdered: null,
                yesNoView: null,
                mask:null,
                maxAllowedAnswers: null,
                isFilteredCombobox: null,
                cascadeFromQuestionId: null,
                sourceQuestionnaireId: null,
                maxAnswerCount: null,
                countOfDecimalPlaces: null,
                validationConditions: validationConditions,
                linkedFilterExpression: null
            ));
        }

        public IPublishedEvent<QuestionDeleted> QuestionDeletedEvent(string questionId = null)
        {
            return ToPublishedEvent(new QuestionDeleted(GetQuestionnaireItemId(questionId)));
        }

        public Questionnaire Questionnaire(IExpressionProcessor expressionProcessor = null)
        {
            return new Questionnaire(
                new QuestionnaireEntityFactory(),
                Mock.Of<ILogger>(),
                Mock.Of<IClock>(),
                expressionProcessor ?? Mock.Of<IExpressionProcessor>(),
                Create.Service.SubstitutionService(),
                Create.Service.KeywordsProvider(),
                Mock.Of<ILookupTableService>(),
                Mock.Of<IAttachmentService>());
        }

        public IPublishedEvent<QuestionnaireAssemblyImported> QuestionnaireAssemblyImported(Guid questionnaireId, long version)
        {
            return ToPublishedEvent(new QuestionnaireAssemblyImported { Version = version }, eventSourceId: questionnaireId);
        }

        public QuestionnaireBrowseItem QuestionnaireBrowseItem(
            Guid? questionnaireId = null, long? version = null, QuestionnaireIdentity questionnaireIdentity = null,
            string title = "Questionnaire Browse Item X", bool disabled = false)
            => new QuestionnaireBrowseItem
            {
                QuestionnaireId = questionnaireIdentity?.QuestionnaireId ?? questionnaireId ?? Guid.NewGuid(),
                Version = questionnaireIdentity?.Version ?? version ?? 1,
                Title = title,
                Disabled = disabled,
            };

        public QuestionnaireBrowseItem QuestionnaireBrowseItem(QuestionnaireDocument questionnaire)
        {
            return new QuestionnaireBrowseItem(questionnaire, 1, false,1);
        }

        public QuestionnaireChangeRecord QuestionnaireChangeRecord(
            string questionnaireId = null,
            QuestionnaireActionType? action = null, 
            Guid? targetId = null, 
            QuestionnaireItemType? targetType = null,
            params QuestionnaireChangeReference[] reference)
        {
            return new QuestionnaireChangeRecord()
            {
                QuestionnaireId = questionnaireId,
                ActionType = action ?? QuestionnaireActionType.Add,
                TargetItemId = targetId ?? Guid.NewGuid(),
                TargetItemType = targetType ?? QuestionnaireItemType.Group,
                References = reference.ToHashSet()
            };
        }

        public QuestionnaireChangeReference QuestionnaireChangeReference(
            Guid? referenceId = null,
            QuestionnaireItemType? referenceType = null)
        {
            return new QuestionnaireChangeReference()
            {
                ReferenceId = referenceId ?? Guid.NewGuid(),
                ReferenceType = referenceType ?? QuestionnaireItemType.Group
            };
        }

        public IPublishedEvent<QuestionnaireCloned> QuestionnaireClonedEvent(string questionnaireId,
            string chapter1Id = null, string chapter1Title = "", string chapter2Id = null, string chapter2Title = "",
            string questionnaireTitle = null, string chapter1GroupId = null, string chapter1GroupTitle = null,
            string chapter2QuestionId = null, string chapter2QuestionTitle = null,
            string chapter2QuestionVariable = null,
            string chapter2QuestionConditionExpression = null,
            string chapter1StaticTextId = null, string chapter1StaticText = null,
            bool? isPublic = null,
            Guid? clonedFromQuestionnaireId=null)
        {
            var result = ToPublishedEvent(new QuestionnaireCloned()
            {
                QuestionnaireDocument =
                    CreateQuestionnaireDocument(questionnaireId: questionnaireId, questionnaireTitle: questionnaireTitle,
                        chapter1Id: chapter1Id ?? Guid.NewGuid().FormatGuid(), chapter1Title: chapter1Title, chapter2Id: chapter2Id ?? Guid.NewGuid().FormatGuid(),
                        chapter2Title: chapter2Title, chapter1GroupId: chapter1GroupId,
                        chapter1GroupTitle: chapter1GroupTitle, chapter2QuestionId: chapter2QuestionId,
                        chapter2QuestionTitle: chapter2QuestionTitle, chapter2QuestionVariable: chapter2QuestionVariable,
                        chapter2QuestionConditionExpression: chapter2QuestionConditionExpression,
                        chapter1StaticTextId: chapter1StaticTextId, chapter1StaticText: chapter1StaticText,
                        isPublic: isPublic ?? false),
                ClonedFromQuestionnaireId = clonedFromQuestionnaireId?? Guid.NewGuid()
            }, new Guid(questionnaireId));
            return result;
        }

        public IPublishedEvent<Main.Core.Events.Questionnaire.QuestionnaireDeleted> QuestionnaireDeleted(Guid questionnaireId)
        {
            return ToPublishedEvent(new Main.Core.Events.Questionnaire.QuestionnaireDeleted(),eventSourceId: questionnaireId);
        }

        public QuestionnaireDocument QuestionnaireDocument(Guid? id = null, params IComposite[] children)
        {
            return new QuestionnaireDocument
            {
                PublicKey = id ?? Guid.NewGuid(),
                Children = children?.ToList() ?? new List<IComposite>(),
            };
        }

        public QuestionnaireDocument QuestionnaireDocument(Guid? id = null, bool usesCSharp = false, IEnumerable<IComposite> children = null)
        {
            return new QuestionnaireDocument
            {
                PublicKey = id ?? Guid.NewGuid(),
                Children = children?.ToList() ?? new List<IComposite>(),
                UsesCSharp = usesCSharp,
            };
        }

        public QuestionnaireDocument QuestionnaireDocumentWithOneChapter(params IComposite[] children)
        {
            return QuestionnaireDocumentWithOneChapter(null, children);
        }

        public QuestionnaireDocument QuestionnaireDocumentWithOneChapter(Guid? chapterId = null, params IComposite[] children)
        {
            var result = new QuestionnaireDocument();
            var chapter = new Group("Chapter") { PublicKey = chapterId.GetValueOrDefault() };

            result.Children.Add(chapter);

            foreach (var child in children)
            {
                chapter.Children.Add(child);
            }

            return result;
        }

        public QuestionnaireDocument QuestionnaireDocumentWithAttachments(Guid? chapterId = null, params Attachment[] attachments)
        {
            var result = new QuestionnaireDocument();
            var chapter = new Group("Chapter") { PublicKey = chapterId.GetValueOrDefault() };

            result.Children.Add(chapter);

            result.Attachments = attachments.ToList();

            return result;
        }

        public QuestionnaireExportStructure QuestionnaireExportStructure(Guid? questionnaireId = null, long? version = null)
        {
            return new QuestionnaireExportStructure
            {
                QuestionnaireId = questionnaireId ?? Guid.Empty,
                Version = version ?? 0
            };
        }

        public QuestionnaireIdentity QuestionnaireIdentity(Guid? questionnaireId = null, long? questionnaireVersion = null)
        {
            return new QuestionnaireIdentity(questionnaireId ?? Guid.NewGuid(), questionnaireVersion ?? 7);
        }

        public IPublishedEvent<QuestionnaireItemMoved> QuestionnaireItemMovedEvent(string itemId,
            string targetGroupId = null, int? targetIndex = null, string questionnaireId=null)
        {
            return ToPublishedEvent(new QuestionnaireItemMoved()
            {
                PublicKey = Guid.Parse(itemId),
                GroupKey = GetQuestionnaireItemParentId(targetGroupId),
                TargetIndex = targetIndex ?? 0
            }, Guid.Parse(questionnaireId??Guid.NewGuid().ToString()));
        }

        public QuestionnaireLevelLabels QuestionnaireLevelLabels(string levelName="level", params LabeledVariable[] variableLabels)
        {
            return new QuestionnaireLevelLabels(levelName, variableLabels);
        }

        public IPlainQuestionnaireRepository QuestionnaireRepositoryStubWithOneQuestionnaire(
            Guid questionnaireId, IQuestionnaire questionnaire = null, long? questionnaireVersion = null)
        {
            questionnaire = questionnaire ?? Mock.Of<IQuestionnaire>();

            return Mock.Of<IPlainQuestionnaireRepository>(repository
                => repository.GetHistoricalQuestionnaire(questionnaireId, questionnaireVersion ?? questionnaire.Version) == questionnaire
                && repository.GetHistoricalQuestionnaire(questionnaireId, questionnaireVersion ?? 1) == questionnaire
                && repository.GetQuestionnaire(Moq.It.IsAny<QuestionnaireIdentity>()) == questionnaire);
        }

        public QuestionnaireSharedPersons QuestionnaireSharedPersons(Guid? questionnaireId = null)
        {
            return  new QuestionnaireSharedPersons(questionnaireId ?? Guid.NewGuid());
        }

        public QuestionnaireStateTracker QuestionnaireStateTacker()
        {
            return new QuestionnaireStateTracker();
        }

        public IPublishedEvent<QuestionnaireUpdated> QuestionnaireUpdatedEvent(string questionnaireId,
            string questionnaireTitle,
            bool isPublic = false)
        {
            return ToPublishedEvent(new QuestionnaireUpdated() { Title = questionnaireTitle, IsPublic = isPublic }, new Guid(questionnaireId));
        }

        public QuestionnaireView QuestionnaireView(Guid? createdBy)
            => Create.Other.QuestionnaireView(new QuestionnaireDocument { CreatedBy = createdBy ?? Guid.NewGuid( )});

        public QuestionnaireView QuestionnaireView(QuestionnaireDocument questionnaireDocument)
            => new QuestionnaireView(questionnaireDocument);

        public ReadSideCacheSettings ReadSideCacheSettings(int cacheSizeInEntities = 128, int storeOperationBulkSize = 8)
            => new ReadSideCacheSettings(true, "folder", cacheSizeInEntities, storeOperationBulkSize);

        public ReadSideSettings ReadSideSettings()
            => new ReadSideSettings(readSideVersion: 0);

        public Group FixedRoster(Guid? rosterId = null, IEnumerable<string> fixedTitles = null, IEnumerable<IComposite> children = null)
            => Create.Other.Roster(
                rosterId: rosterId,
                children: children,
                fixedTitles: fixedTitles?.ToArray() ?? new[] { "Fixed Roster 1", "Fixed Roster 2", "Fixed Roster 3" });

        public Group Roster(
            Guid? rosterId = null, 
            string title = "Roster X", 
            string variable = "roster_var", 
            string enablementCondition = null,
            string[] fixedTitles = null, 
            IEnumerable<IComposite> children = null,
            RosterSizeSourceType rosterSizeSourceType = RosterSizeSourceType.FixedTitles,
            Guid? rosterSizeQuestionId = null, 
            Guid? rosterTitleQuestionId = null,
            FixedRosterTitle[] fixedRosterTitles = null)
        {
            Group group = Create.Other.Group(
                groupId: rosterId,
                title: title,
                variable: variable,
                enablementCondition: enablementCondition,
                children: children);

            group.IsRoster = true;
            group.RosterSizeSource = rosterSizeSourceType;

            if (rosterSizeSourceType == RosterSizeSourceType.FixedTitles)
            {
                if (fixedRosterTitles == null)
                {
                    group.RosterFixedTitles = fixedTitles ?? new[] { "Roster X-1", "Roster X-2", "Roster X-3" };
                }
                else
                {
                    group.FixedRosterTitles = fixedRosterTitles;
                }
            }

            group.RosterSizeQuestionId = rosterSizeQuestionId;
            group.RosterTitleQuestionId = rosterTitleQuestionId;

            return group;
        }

        public IPublishedEvent<RosterChanged> RosterChanged(string groupId)
        {
            return ToPublishedEvent(new RosterChanged(responsibleId: new Guid(), groupId: Guid.Parse(groupId)));
        }

        public RosterInstancesAdded RosterInstancesAdded(Guid? rosterGroupId = null)
        {
            return new RosterInstancesAdded(new[]
                {
                    new AddedRosterInstance(rosterGroupId ?? Guid.NewGuid(), new decimal[0], 0.0m, null)
                });
        }

        public RosterInstancesRemoved RosterInstancesRemoved(Guid? rosterGroupId = null)
        {
            return new RosterInstancesRemoved(new[]
                {
                    new RosterInstance(rosterGroupId ?? Guid.NewGuid(), new decimal[0], 0.0m)
                });
        }

        public RosterInstancesTitleChanged RosterInstancesTitleChanged(Guid? rosterId = null, 
            string rosterTitle = null,
            decimal[] outerRosterVector = null,
            decimal? instanceId= null)
        {
            return new RosterInstancesTitleChanged(
                new[]
                {
                    new ChangedRosterInstanceTitleDto(new RosterInstance(rosterId ?? Guid.NewGuid(), outerRosterVector ?? new decimal[0], instanceId ?? 0.0m), rosterTitle ?? "title")
                });
        }

        public RosterVector RosterVector(params decimal[] coordinates)
        {
            return new RosterVector(coordinates ?? Enumerable.Empty<decimal>());
        }

        public IPublishedEvent<SharedPersonFromQuestionnaireRemoved> SharedPersonFromQuestionnaireRemoved(Guid questionnaireId, Guid personId)
        {
            return ToPublishedEvent(new SharedPersonFromQuestionnaireRemoved() { PersonId = personId }, questionnaireId);
        }

        public IPublishedEvent<SharedPersonToQuestionnaireAdded> SharedPersonToQuestionnaireAdded(Guid questionnaireId, Guid personId)
        {
            return ToPublishedEvent(new SharedPersonToQuestionnaireAdded() { PersonId = personId }, questionnaireId);
        }

        public SingleOptionLinkedQuestionViewModel SingleOptionLinkedQuestionViewModel(
            IQuestionnaire questionnaire = null,
            IStatefulInterview interview = null,
            ILiteEventRegistry eventRegistry = null,
            QuestionStateViewModel<SingleOptionLinkedQuestionAnswered> questionState = null,
            AnsweringViewModel answering = null)
        {
            var userIdentity = Mock.Of<IUserIdentity>(y => y.UserId == Guid.NewGuid());
            questionnaire = questionnaire ?? Mock.Of<IQuestionnaire>();
            interview = interview ?? Mock.Of<IStatefulInterview>();

            return new SingleOptionLinkedQuestionViewModel(
                Mock.Of<IPrincipal>(_ => _.CurrentUserIdentity == userIdentity),
                Mock.Of<IPlainQuestionnaireRepository>(_ => _.GetQuestionnaire(It.IsAny<QuestionnaireIdentity>()) == questionnaire),
                Mock.Of<IStatefulInterviewRepository>(_ => _.Get(It.IsAny<string>()) == interview),
                Create.Service.AnswerToStringService(),
                eventRegistry ?? Mock.Of<ILiteEventRegistry>(),
                Stub.MvxMainThreadDispatcher(),
                questionState ?? Stub<QuestionStateViewModel<SingleOptionLinkedQuestionAnswered>>.WithNotEmptyValues,
                answering ?? Mock.Of<AnsweringViewModel>());
        }

        public SingleQuestion SingleOptionQuestion(Guid? questionId = null, string variable = null, string enablementCondition = null, string validationExpression = null,
            Guid? linkedToQuestionId = null,
            Guid? cascadeFromQuestionId = null, 
            decimal[] answerCodes = null, 
            decimal[] parentCodes = null,
            string title = null, 
            bool hideIfDisabled = false, 
            string linkedFilterExpression = null,
            Guid? linkedToRosterId = null)
        {
            var answers = (answerCodes ?? new decimal[] { 1, 2, 3 }).Select(a => Create.Other.Answer(a.ToString(), a)).ToList();
            if (parentCodes != null)
            {
                for (int i = 0; i < parentCodes.Length; i++)
                {
                    answers[i].ParentValue = parentCodes[i].ToString(CultureInfo.InvariantCulture);
                }
            }
            return new SingleQuestion
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                StataExportCaption = variable ?? "single_option_question",
                QuestionText = title??"SO Question",
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                QuestionType = QuestionType.SingleOption,
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                CascadeFromQuestionId = cascadeFromQuestionId,
                Answers = answers,
                LinkedFilterExpression= linkedFilterExpression
            };
        }

        public SingleQuestion SingleQuestion(Guid? id = null, string variable = null, string enablementCondition = null, string validationExpression = null,
            Guid? cascadeFromQuestionId = null, List<Answer> options = null, Guid? linkedToQuestionId = null, QuestionScope scope = QuestionScope.Interviewer, 
            bool isFilteredCombobox = false, Guid? linkedToRosterId = null)
        {
            return new SingleQuestion
            {
                QuestionType = QuestionType.SingleOption,
                PublicKey = id ?? Guid.NewGuid(),
                StataExportCaption = variable,
                ConditionExpression = enablementCondition,
                ValidationExpression = validationExpression,
                Answers = options ?? new List<Answer>(),
                CascadeFromQuestionId = cascadeFromQuestionId,
                LinkedToQuestionId = linkedToQuestionId,
                LinkedToRosterId = linkedToRosterId,
                QuestionScope = scope,
                IsFilteredCombobox = isFilteredCombobox
            };
        }

        public StatefulInterview StatefulInterview(Guid? questionnaireId = null, 
            long? questionnaireVersion = null,
            Guid? userId = null, 
            IPlainQuestionnaireRepository questionnaireRepository = null)
        {
            questionnaireId = questionnaireId ?? Guid.NewGuid();
            var statefulInterview = new StatefulInterview(
                Mock.Of<ILogger>(),
                questionnaireRepository ?? Mock.Of<IPlainQuestionnaireRepository>(),
                Stub<IInterviewExpressionStatePrototypeProvider>.WithNotEmptyValues)
            {
                QuestionnaireIdentity = new QuestionnaireIdentity(questionnaireId.Value, questionnaireVersion ?? 1),
            };

            statefulInterview.Apply(new InterviewCreated(userId ?? Guid.NewGuid(), questionnaireId.Value, questionnaireVersion ?? 1));

            return statefulInterview;
        }

        public StatefulInterview StatefulInterview(Guid? questionnaireId = null, Guid? userId = null,
    IQuestionnaire questionnaire = null)
        {
            questionnaireId = questionnaireId ?? Guid.NewGuid();
            var statefulInterview = new StatefulInterview(
                Mock.Of<ILogger>(),
                Mock.Of<IPlainQuestionnaireRepository>(x => x.GetQuestionnaire(Moq.It.IsAny<QuestionnaireIdentity>()) == questionnaire),
                Stub<IInterviewExpressionStatePrototypeProvider>.WithNotEmptyValues)
            {
                QuestionnaireIdentity = new QuestionnaireIdentity(questionnaireId.Value, 1),
            };

            statefulInterview.Apply(new InterviewCreated(userId ?? Guid.NewGuid(), questionnaireId.Value, 1));

            return statefulInterview;
        }

        public StaticText StaticText(
            Guid? publicKey = null,
            string text = "Static Text X",
            string attachmentName = null,
            List<ValidationCondition> validationConditions = null)
        {
            return new StaticText(publicKey ?? Guid.NewGuid(), text, null, false, validationConditions ?? new List<ValidationCondition>(), attachmentName);
        }

        public IPublishedEvent<StaticTextAdded> StaticTextAddedEvent(string entityId = null, Guid? responsibleId = null, string parentId = null, string text = null)
        {
            return ToPublishedEvent(new StaticTextAdded(
                GetQuestionnaireItemId(entityId),
                responsibleId ?? Guid.NewGuid(),
                GetQuestionnaireItemId(parentId),
                text,
                null,
                false,
                null));
        }

        public IPublishedEvent<StaticTextCloned> StaticTextClonedEvent(string entityId = null,
            string parentId = null, string sourceEntityId = null, string text = null, int targetIndex = 0)
        {
            return ToPublishedEvent(Create.Event.StaticTextCloned(
                publicKey : GetQuestionnaireItemId(entityId),
                parentId : GetQuestionnaireItemId(parentId),
                sourceEntityId : GetQuestionnaireItemId(sourceEntityId),
                text : text,
                targetIndex : targetIndex));
        }

        public IPublishedEvent<StaticTextDeleted> StaticTextDeletedEvent(string entityId = null)
        {
            return ToPublishedEvent(new StaticTextDeleted()
            {
                EntityId = GetQuestionnaireItemId(entityId)
            });
        }

        public IPublishedEvent<StaticTextUpdated> StaticTextUpdatedEvent(string entityId = null, string text = null,
            string enablementCondition = null, bool? hideIfDisabled = null, IList<ValidationCondition> validationConditions = null)
        {
            return ToPublishedEvent(Create.Event.StaticTextUpdated(

                GetQuestionnaireItemId(entityId),
                text));
        }

        public IPublishedEvent<SupervisorAssigned> SupervisorAssignedEvent(Guid? interviewId = null, string userId = null,
            string supervisorId = null)
        {
            return
                ToPublishedEvent(new SupervisorAssigned(userId: GetGuidIdByStringId(userId),
                    supervisorId: GetGuidIdByStringId(supervisorId)), eventSourceId: interviewId);
        }

        public IAsyncExecutor SyncAsyncExecutor()
        {
            return new SyncAsyncExecutorStub();
        }

        public IPublishedEvent<SynchronizationMetadataApplied> SynchronizationMetadataAppliedEvent(string userId = null,
            InterviewStatus status = InterviewStatus.Created, string questionnaireId = null,
            AnsweredQuestionSynchronizationDto[] featuredQuestionsMeta = null, bool createdOnClient = false)
        {
            return
                ToPublishedEvent(new SynchronizationMetadataApplied(userId: GetGuidIdByStringId(userId), status: status,
                    questionnaireId: GetGuidIdByStringId(questionnaireId), questionnaireVersion: 1, featuredQuestionsMeta: featuredQuestionsMeta,
                    createdOnClient: createdOnClient, comments: null, rejectedDateTime: null, interviewerAssignedDateTime: null));
        }

        public IPublishedEvent<TemplateImported> TemplateImportedEvent(
            string questionnaireId,
            string chapter1Id = null,
            string chapter1Title = null,
            string chapter2Id = null,
            string chapter2Title = null,
            string questionnaireTitle = null,
            string chapter1GroupId = null, string chapter1GroupTitle = null,
            string chapter2QuestionId = null,
            string chapter2QuestionTitle = null,
            string chapter2QuestionVariable = null,
            string chapter2QuestionConditionExpression = null,
            string chapter1StaticTextId = null, string chapter1StaticText = null,
            bool? isPublic = null)
        {
            return ToPublishedEvent(new TemplateImported()
            {
                Source =
                    CreateQuestionnaireDocument(questionnaireId: questionnaireId, questionnaireTitle: questionnaireTitle,
                        chapter1Id: chapter1Id ?? Guid.NewGuid().FormatGuid(), chapter1Title: chapter1Title,
                        chapter2Id: chapter2Id ?? Guid.NewGuid().FormatGuid(),
                        chapter2Title: chapter2Title, chapter1GroupId: chapter1GroupId,
                        chapter1GroupTitle: chapter1GroupTitle, chapter2QuestionId: chapter2QuestionId,
                        chapter2QuestionTitle: chapter2QuestionTitle, chapter2QuestionVariable: chapter2QuestionVariable,
                        chapter2QuestionConditionExpression: chapter2QuestionConditionExpression,
                        chapter1StaticTextId: chapter1StaticTextId, chapter1StaticText: chapter1StaticText,
                        isPublic: isPublic ?? false)
            }, new Guid(questionnaireId));
        }

        public TextAnswer TextAnswer(string answer)
        {
            return Create.Other.TextAnswer(answer, null, null);
        }

        public TextAnswer TextAnswer(string answer, Guid? questionId, decimal[] rosterVector)
        {
            var masedMaskedTextAnswer = new TextAnswer(questionId ?? Guid.NewGuid(), rosterVector ?? Empty.RosterVector);

            if (answer != null)
            {
                masedMaskedTextAnswer.SetAnswer(answer);
            }

            return masedMaskedTextAnswer;
        }

        public ITextListQuestion TextListQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            int? maxAnswerCount = null, string variable=null, bool hideIfDisabled = false)
        {
            return new TextListQuestion("Question TL")
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                MaxAnswerCount = maxAnswerCount,
                QuestionType = QuestionType.TextList,
                StataExportCaption = variable
            };
        }

        public IPublishedEvent<TextListQuestionChanged> TextListQuestionChangedEvent(string questionId,
            string questionVariable = null, string questionTitle = null, string questionConditionExpression = null)
        {
            return ToPublishedEvent(new TextListQuestionChanged()
            {
                PublicKey = Guid.Parse(questionId),
                StataExportCaption = questionVariable,
                QuestionText = questionTitle,
                ConditionExpression = questionConditionExpression
            });
        }

        public IPublishedEvent<TextListQuestionCloned> TextListQuestionClonedEvent(string questionId = null,
            string parentGroupId = null, string questionVariable = null, string questionTitle = null,
            string questionConditionExpression = null, string sourceQuestionId = null)
        {
            return ToPublishedEvent(new TextListQuestionCloned()
            {
                PublicKey = GetQuestionnaireItemId(questionId),
                GroupId = GetQuestionnaireItemId(parentGroupId),
                StataExportCaption = questionVariable,
                QuestionText = questionTitle,
                ConditionExpression = questionConditionExpression,
                SourceQuestionId = GetQuestionnaireItemId(sourceQuestionId),
                TargetIndex = 0
            });
        }

        public TextQuestion TextQuestion(Guid? questionId = null, string enablementCondition = null, string validationExpression = null,
            string mask = null, 
            string variable = "text_question", 
            string validationMessage = null, 
            string text = "Question T", 
            QuestionScope scope = QuestionScope.Interviewer, 
            bool preFilled=false,
            string label=null,
            string instruction=null,
            IEnumerable<ValidationCondition> validationConditions = null, 
            bool hideIfDisabled = false)
            
        {
            return new TextQuestion(text)
            {
                PublicKey = questionId ?? Guid.NewGuid(),
                ConditionExpression = enablementCondition,
                HideIfDisabled = hideIfDisabled,
                ValidationExpression = validationExpression,
                ValidationMessage = validationMessage,
                Mask = mask,
                QuestionText = text,
                QuestionType = QuestionType.Text,
                StataExportCaption = variable,
                QuestionScope = scope,
                Featured = preFilled,
                VariableLabel = label,
                Instructions = instruction,
                ValidationConditions = validationConditions?.ToList().ConcatWithOldConditionIfNotEmpty(validationExpression, validationMessage)
            };
        }

        public IPublishedEvent<TextQuestionAnswered> TextQuestionAnsweredEvent(Guid? interviewId = null, string userId = null)
        {
            return
                ToPublishedEvent(new TextQuestionAnswered(GetGuidIdByStringId(userId), Guid.NewGuid(), new decimal[0],
                    DateTime.Now, "tttt"));
        }

        public TimeSpanBetweenStatuses TimeSpanBetweenStatuses(Guid? interviewerId = null, Guid? supervisorId = null, DateTime? timestamp = null, TimeSpan? timeSpanWithPreviousStatus = null, InterviewExportedAction endStatus= InterviewExportedAction.ApprovedByHeadquarter)
        {
            return new TimeSpanBetweenStatuses()
            {
                BeginStatus = InterviewExportedAction.InterviewerAssigned,
                EndStatus = endStatus,
                EndStatusTimestamp = timestamp ?? DateTime.Now,
                InterviewerId = interviewerId ?? Guid.NewGuid(),
                SupervisorId = supervisorId ?? Guid.NewGuid(),
                TimeSpan = timeSpanWithPreviousStatus?? new TimeSpan()
            };
        }

        public IPublishedEvent<UnapprovedByHeadquarters> UnapprovedByHeadquartersEvent(Guid? interviewId = null, string userId = null, string comment = null)
        {
            return ToPublishedEvent(new UnapprovedByHeadquarters(userId: GetGuidIdByStringId(userId), comment: comment), eventSourceId: interviewId);
        }

        public UncommittedEvent UncommittedEvent(Guid? eventSourceId = null, 
            IEvent payload = null,
            int sequence = 1,
            int initialVersion = 1)
        {
            return new UncommittedEvent(Guid.NewGuid(), eventSourceId ?? Guid.NewGuid(), sequence, initialVersion, DateTime.Now, payload);
        }

        public User User(Guid? userId=null)
        {
            var user = new User();
            user.SetId(userId??Guid.NewGuid());
            return user;
        }

        public UserArchived UserArchived()
        {
           return new UserArchived();
        }

        public UserDocument UserDocument(Guid? userId = null, Guid? supervisorId = null, bool? isArchived = null, string userName="name", bool isLockedByHQ = false)
        {
            var user = new UserDocument() { PublicKey = userId ?? Guid.NewGuid(), IsArchived = isArchived ?? false, UserName = userName, IsLockedByHQ = isLockedByHQ };
            if (supervisorId.HasValue)
            {
                user.Roles.Add(UserRoles.Operator);
                user.Supervisor = new UserLight(supervisorId.Value, "supervisor");
            }
            else
            {
                user.Roles.Add(UserRoles.Supervisor);
            }
            return user;
        }

        public UserLight UserLight(Guid? userId = null)
        {
            return new UserLight(userId ?? Guid.NewGuid(), "test");
        }

        public UserPreloadingDataRecord UserPreloadingDataRecord(string login = "test", string supervisor = "", string password = "test", string email="", string phoneNumber="", string role=null)
        {
            return new UserPreloadingDataRecord()
            {
                Login = login,
                Supervisor = supervisor,
                Role = role??(string.IsNullOrEmpty(supervisor) ? "supervisor" : "interviewer"),
                Password = password,
                Email = email,
                PhoneNumber = phoneNumber
            };
        }

        public UserPreloadingProcess UserPreloadingProcess(string userPreloadingProcessId = null,
            UserPrelodingState state = UserPrelodingState.Uploaded, int recordsCount=0, params UserPreloadingDataRecord[] dataRecords)
        {
            var result = new UserPreloadingProcess()
            {
                UserPreloadingProcessId = userPreloadingProcessId ?? Guid.NewGuid().FormatGuid(),
                State = state,
                RecordsCount = recordsCount,
                LastUpdateDate = DateTime.Now
            };
            foreach (var userPreloadingDataRecord in dataRecords)
            {
                result.UserPrelodingData.Add(userPreloadingDataRecord);
            }
            return result;
        }

        public UserPreloadingSettings UserPreloadingSettings()
        {
            return new UserPreloadingSettings(5, 5, 12, 1, 10000, 100, 100, "^[a-zA-Z0-9_]{3,15}$",
                @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$",
                "^(?=.*[a-z])(?=.*[0-9])(?=.*[A-Z]).*$",
                @"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$");
        }

        public UserPreloadingVerificationError UserPreloadingVerificationError()
        {
            return new UserPreloadingVerificationError();
        }

        public VariableValueLabel VariableValueLabel(string value="1", string label="l1")
        {
            return new VariableValueLabel(value, label);
        }

        public QuestionnaireVerificationMessage VerificationError(string code, string message, params QuestionnaireVerificationReference[] questionnaireVerificationReferences)
        {
            return QuestionnaireVerificationMessage.Error(code, message, questionnaireVerificationReferences);
        }

        public QuestionnaireVerificationMessage VerificationWarning(string code, string message, params QuestionnaireVerificationReference[] questionnaireVerificationReferences)
        {
            return QuestionnaireVerificationMessage.Warning(code, message, questionnaireVerificationReferences);
        }

        public VerificationMessage VerificationMessage(string code, string message, params VerificationReferenceEnriched[] references)
        {
            return new VerificationMessage
            {
                Code = code,
                Message = message,
                Errors = new List<VerificationMessageError>()
                { 
                    new VerificationMessageError()
                    {
                        References = references.ToList()
                    }
                }
            };
        }

        public QuestionnaireVerificationReference VerificationReference(Guid? id = null, QuestionnaireVerificationReferenceType type = QuestionnaireVerificationReferenceType.Question)
        {
            return new QuestionnaireVerificationReference(type, id ?? Guid.NewGuid());
        }

        public VerificationReferenceEnriched VerificationReferenceEnriched(QuestionnaireVerificationReferenceType type, Guid id, string title)
        {
            return new VerificationReferenceEnriched
            {
                Type = type,
                ItemId = id.FormatGuid(),
                Title = title
            };
        }

        public YesNoAnswer YesNoAnswer(Guid questionId, decimal[] rosterVector)
        {
            return new YesNoAnswer(questionId, rosterVector);
        }

        public YesNoAnswers YesNoAnswers(decimal[] allOptionCodes, YesNoAnswersOnly yesNoAnswersOnly = null)
        {
            return new YesNoAnswers(allOptionCodes: allOptionCodes, yesNoAnswersOnly: yesNoAnswersOnly);
        }

        public IMultyOptionsQuestion YesNoQuestion(Guid? questionId = null, decimal[] answers = null)
        {
            return Create.Other.MultipleOptionsQuestion(
                isYesNo: true,
                questionId: questionId,
                answers: answers ?? new decimal[] {});
        }

        private class SyncAsyncExecutorStub : IAsyncExecutor
        {
            public void ExecuteAsync(Action action)
            {
                action.Invoke();
            }
        }

        public CommentedStatusHistroyView CommentedStatusHistroyView(InterviewStatus status=InterviewStatus.InterviewerAssigned, string comment=null, DateTime? timestamp=null)
        {
            return new CommentedStatusHistroyView()
            {
                Status = status,
                Comment = comment,
                Date = timestamp ?? DateTime.Now
            };
        }

        public InterviewRoster InterviewRoster(Guid? rosterId=null, decimal[] rosterVector=null, string rosterTitle="titile")
        {
            return new InterviewRoster()
            {
                Id = rosterId ?? Guid.NewGuid(),
                IsDisabled = false,
                RosterVector = rosterVector ?? new decimal[0],
                Title = rosterTitle
            };
        }

        public ValidationCondition ValidationCondition(string expression = "self != null", string message = "should be answered")
        {
            return new ValidationCondition(expression, message);
        }

        public InterviewLinkedQuestionOptions InterviewLinkedQuestionOptions(params ChangedLinkedOptions[] options)
        {
            var result = new InterviewLinkedQuestionOptions();

            foreach (var changedLinkedQuestion in options)
            {
                result.LinkedQuestionOptions[changedLinkedQuestion.QuestionId.ToString()] = changedLinkedQuestion.Options;
            }

            return result;
        }

        public ChangedLinkedOptions ChangedLinkedOptions(Guid questionId,decimal[] questionRosterVector=null, RosterVector[] options=null)
        {
            return new ChangedLinkedOptions(new Identity(questionId, questionRosterVector ?? new decimal[0]),
                options ?? new RosterVector[0]);
        }
        
        public AttachmentContentMetadata AttachmentContentMetadata(string contentType)
        {
            return new AttachmentContentMetadata()
            {
                ContentType = contentType,
            };
        }
        
        public AttachmentContentData AttachmentContentData(byte[] content)
        {
            return new AttachmentContentData()
            {
                Content = content,
            };
        }

        public AttachmentViewModel AttachmentViewModel(
            IPlainQuestionnaireRepository questionnaireRepository,
            IStatefulInterviewRepository interviewRepository,
            IAttachmentContentStorage attachmentContentStorage)
        {
            return new AttachmentViewModel(
                questionnaireRepository: questionnaireRepository,
                interviewRepository: interviewRepository,
                attachmentContentStorage: attachmentContentStorage);
        }

        public WB.Core.SharedKernels.Enumerator.Views.AttachmentContent Enumerator_AttachmentContent(string id)
        {
            return new Core.SharedKernels.Enumerator.Views.AttachmentContent()
            {
                Id = id,

            };
        }

        public ISnapshotStore SnapshotStore(Guid aggregateRootId, Snapshot snapshot = null)
        {
            return Mock.Of<ISnapshotStore>(_ => _.GetSnapshot(aggregateRootId, Moq.It.IsAny<int>()) == snapshot);
        }

        public IEventStore EventStore(Guid eventSourceId, IEnumerable<CommittedEvent> committedEvents)
        {
            return Mock.Of<IEventStore>(_ =>
                _.Read(eventSourceId, Moq.It.IsAny<int>()) == new CommittedEventStream(eventSourceId, committedEvents));
        }

        public IAggregateSnapshotter AggregateSnapshotter(EventSourcedAggregateRoot aggregateRoot = null, bool isARLoadedFromSnapshotSuccessfully = false)
        {
            return Mock.Of<IAggregateSnapshotter>(_ =>
                _.TryLoadFromSnapshot(Moq.It.IsAny<Type>(), Moq.It.IsAny<Snapshot>(),
                    Moq.It.IsAny<CommittedEventStream>(), out aggregateRoot) ==
                isARLoadedFromSnapshotSuccessfully);
        }

        public IStatefulInterviewRepository StatefulInterviewRepositoryWith(IStatefulInterview interview)
        {
            var result = Substitute.For<IStatefulInterviewRepository>();
            result.Get(null).ReturnsForAnyArgs(interview);
            return result;
        }

        public IPlainQuestionnaireRepository PlainQuestionnaireRepositoryWith(IQuestionnaire questionnaire)
        {
            var result = Substitute.For<IPlainQuestionnaireRepository>();
            result.GetQuestionnaire(null).ReturnsForAnyArgs(questionnaire);
            return result;
        }

        public EventBusSettings EventBusSettings() => new EventBusSettings
        {
            EventHandlerTypesWithIgnoredExceptions = new Type[] {},
            DisabledEventHandlerTypes = new Type[] {},
        };
    }
}