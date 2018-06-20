using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Moq;
using Ncqrs.Domain.Storage;
using Ncqrs.Eventing.ServiceModel.Bus;
using Ncqrs.Eventing.Storage;
using NHibernate;
using NSubstitute;
using System.Linq;
using NHibernate.Linq;
using Quartz;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.Implementation.Services.CodeGeneration;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Headquarters;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Upgrade;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport.Verifier;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.DataExport.Accessors;
using WB.Core.BoundedContexts.Headquarters.DataExport.Factories;
using WB.Core.BoundedContexts.Headquarters.DataExport.Services;
using WB.Core.BoundedContexts.Headquarters.DataExport.Services.Exporters;
using WB.Core.BoundedContexts.Headquarters.DataExport.Views;
using WB.Core.BoundedContexts.Headquarters.EventHandler;
using WB.Core.BoundedContexts.Headquarters.Implementation.Services;
using WB.Core.BoundedContexts.Headquarters.Implementation.Synchronization;
using WB.Core.BoundedContexts.Headquarters.IntreviewerProfiles;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Repositories;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.UserPreloading;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Dto;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Services;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Tasks;
using WB.Core.BoundedContexts.Headquarters.Views;
using WB.Core.BoundedContexts.Headquarters.Views.DataExport;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.BoundedContexts.Headquarters.Views.Interviews;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.BoundedContexts.Interviewer.Implementation.Services;
using WB.Core.BoundedContexts.Interviewer.Implementation.Storage;
using WB.Core.BoundedContexts.Interviewer.Services;
using WB.Core.BoundedContexts.Interviewer.Services.Infrastructure;
using WB.Core.BoundedContexts.Interviewer.Services.Synchronization;
using WB.Core.BoundedContexts.Interviewer.Views;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard;
using WB.Core.BoundedContexts.Tester.Implementation.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Implementation.Services;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.Aggregates;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.CommandBus.Implementation;
using WB.Core.Infrastructure.EventBus;
using WB.Core.Infrastructure.EventBus.Lite;
using WB.Core.Infrastructure.EventBus.Lite.Implementation;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.Implementation.Aggregates;
using WB.Core.Infrastructure.Implementation.EventDispatcher;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.Infrastructure.TopologicalSorter;
using WB.Core.Infrastructure.Transactions;
using WB.Core.Infrastructure.Versions;
using WB.Core.Infrastructure.WriteSide;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Implementation.Repositories;
using WB.Core.SharedKernels.DataCollection.Implementation.Services;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.Services;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.Enumerator;
using WB.Core.SharedKernels.Enumerator.Implementation.Services;
using WB.Core.SharedKernels.Enumerator.Repositories;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.SurveySolutions.Documents;
using WB.Infrastructure.Native.Files.Implementation.FileSystem;
using WB.Infrastructure.Native.Storage;
using WB.Infrastructure.Native.Storage.Postgre.Implementation;
using WB.Tests.Abc.Storage;
using WB.UI.Headquarters.API.WebInterview.Services;
using WB.UI.Shared.Web.Captcha;
using WB.UI.Shared.Web.Configuration;
using ILogger = WB.Core.GenericSubdomains.Portable.Services.ILogger;
using AttachmentContent = WB.Core.BoundedContexts.Headquarters.Views.Questionnaire.AttachmentContent;

namespace WB.Tests.Abc.TestFactories
{
    internal class ServiceFactory
    {
        public CommandService CommandService(
            IEventSourcedAggregateRootRepository repository = null,
            IPlainAggregateRootRepository plainRepository = null,
            IEventBus eventBus = null,
            IAggregateSnapshotter snapshooter = null,
            IServiceLocator serviceLocator = null,
            IAggregateLock aggregateLock = null,
            IAggregateRootCacheCleaner aggregateRootCacheCleaner = null)
        {
            return new CommandService(
                repository ?? Mock.Of<IEventSourcedAggregateRootRepository>(),
                eventBus ?? Mock.Of<IEventBus>(),
                snapshooter ?? Mock.Of<IAggregateSnapshotter>(),
                serviceLocator ?? Mock.Of<IServiceLocator>(),
                plainRepository ?? Mock.Of<IPlainAggregateRootRepository>(),
                aggregateLock ?? Stub.Lock(),
                aggregateRootCacheCleaner ?? Mock.Of<IAggregateRootCacheCleaner>());
        }

        public IAsyncRunner AsyncRunner() => new SyncAsyncRunner();

        public AttachmentContentService AttachmentContentService(
            IPlainStorageAccessor<AttachmentContent> attachmentContentPlainStorage)
            => new AttachmentContentService(
                attachmentContentPlainStorage ?? Mock.Of<IPlainStorageAccessor<AttachmentContent>>());

        public CumulativeChartDenormalizer CumulativeChartDenormalizer(
            INativeReadSideStorage<CumulativeReportStatusChange> cumulativeReportReader = null,
            IReadSideRepositoryWriter<CumulativeReportStatusChange> cumulativeReportStatusChangeStorage = null,
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewReferencesStorage = null)
            => new CumulativeChartDenormalizer(
                cumulativeReportStatusChangeStorage ??
                Mock.Of<IReadSideRepositoryWriter<CumulativeReportStatusChange>>(),
                interviewReferencesStorage ?? new TestInMemoryWriter<InterviewSummary>(),
                cumulativeReportReader ?? Mock.Of<INativeReadSideStorage<CumulativeReportStatusChange>>());

        public InterviewerDashboardEventHandler DashboardDenormalizer(
            IPlainStorage<InterviewView> interviewViewRepository = null,
            IQuestionnaireStorage questionnaireStorage = null,
            ILiteEventRegistry liteEventRegistry = null,
            IPlainStorage<PrefilledQuestionView> prefilledQuestions = null,
            IAnswerToStringConverter answerToStringConverter = null
        )
            => new InterviewerDashboardEventHandler(
                interviewViewRepository ?? Mock.Of<IPlainStorage<InterviewView>>(),
                prefilledQuestions ?? new InMemoryPlainStorage<PrefilledQuestionView>(),
                questionnaireStorage ?? Mock.Of<IQuestionnaireStorage>(),
                liteEventRegistry ?? Mock.Of<ILiteEventRegistry>(),
                answerToStringConverter ?? Mock.Of<IAnswerToStringConverter>());

        public DomainRepository DomainRepository(IAggregateSnapshotter aggregateSnapshotter = null,
            IServiceLocator serviceLocator = null)
            => new DomainRepository(
                aggregateSnapshotter: aggregateSnapshotter ?? Mock.Of<IAggregateSnapshotter>(),
                serviceLocator: serviceLocator ?? Mock.Of<IServiceLocator>());

        public EventSourcedAggregateRootRepository EventSourcedAggregateRootRepository(
            IEventStore eventStore = null, ISnapshotStore snapshotStore = null, IDomainRepository repository = null)
            => new EventSourcedAggregateRootRepository(eventStore, snapshotStore, repository);

        public EventSourcedAggregateRootRepositoryWithCache EventSourcedAggregateRootRepositoryWithCache(
            IEventStore eventStore = null, ISnapshotStore snapshotStore = null, IDomainRepository repository = null)
            => new EventSourcedAggregateRootRepositoryWithCache(
                eventStore ?? Mock.Of<IEventStore>(),
                snapshotStore ?? Mock.Of<ISnapshotStore>(),
                repository ?? Mock.Of<IDomainRepository>(),
                new AggregateLock());

        public EventSourcedAggregateRootRepositoryWithExtendedCache
            EventSourcedAggregateRootRepositoryWithExtendedCache(
                IEventStore eventStore = null, ISnapshotStore snapshotStore = null, IDomainRepository repository = null)
            => new EventSourcedAggregateRootRepositoryWithExtendedCache(
                eventStore ?? Mock.Of<IEventStore>(),
                snapshotStore ?? Mock.Of<ISnapshotStore>(),
                repository ?? Mock.Of<IDomainRepository>());

        public FileSystemIOAccessor FileSystemIOAccessor()
            => new FileSystemIOAccessor();

        public InterviewAnswersCommandValidator InterviewAnswersCommandValidator(
            IInterviewSummaryViewFactory interviewSummaryViewFactory = null)
            => new InterviewAnswersCommandValidator(
                interviewSummaryViewFactory ?? Mock.Of<IInterviewSummaryViewFactory>());
        
        public InterviewerInterviewAccessor InterviewerInterviewAccessor(
            IPlainStorage<InterviewView> interviewViewRepository = null,
            IInterviewerEventStorage eventStore = null,
            ICommandService commandService = null,
            IPlainStorage<QuestionnaireView> questionnaireRepository = null,
            IInterviewerPrincipal principal = null,
            IJsonAllTypesSerializer synchronizationSerializer = null,
            IEventSourcedAggregateRootRepositoryWithCache aggregateRootRepositoryWithCache = null,
            ISnapshotStoreWithCache snapshotStoreWithCache = null,
            IPlainStorage<InterviewMultimediaView> interviewMultimediaViewRepository = null,
            IPlainStorage<InterviewFileView> interviewFileViewRepository = null)
            => new InterviewerInterviewAccessor(
                questionnaireRepository ?? Mock.Of<IPlainStorage<QuestionnaireView>>(),
                Mock.Of<IPlainStorage<PrefilledQuestionView>>(),
                interviewViewRepository ?? Mock.Of<IPlainStorage<InterviewView>>(),
                interviewMultimediaViewRepository ?? Mock.Of<IPlainStorage<InterviewMultimediaView>>(),
                interviewFileViewRepository ?? Mock.Of<IPlainStorage<InterviewFileView>>(),
                commandService ?? Mock.Of<ICommandService>(),
                principal ?? Mock.Of<IInterviewerPrincipal>(),
                eventStore ?? Mock.Of<IInterviewerEventStorage>(),
                aggregateRootRepositoryWithCache ?? Mock.Of<IEventSourcedAggregateRootRepositoryWithCache>(),
                snapshotStoreWithCache ?? Mock.Of<ISnapshotStoreWithCache>(),
                synchronizationSerializer ?? Mock.Of<IJsonAllTypesSerializer>(),
                Mock.Of<IInterviewEventStreamOptimizer>(),
                Mock.Of<ILogger>());

        public InterviewEventStreamOptimizer InterviewEventStreamOptimizer()
            => new InterviewEventStreamOptimizer();

        public KeywordsProvider KeywordsProvider()
            => new KeywordsProvider(Create.Service.SubstitutionService());

        public LiteEventBus LiteEventBus(ILiteEventRegistry liteEventRegistry = null, IEventStore eventStore = null)
            => new LiteEventBus(
                liteEventRegistry ?? Stub<ILiteEventRegistry>.WithNotEmptyValues,
                eventStore ?? Mock.Of<IEventStore>());

        public LiteEventRegistry LiteEventRegistry()
            => new LiteEventRegistry();

        public NcqrCompatibleEventDispatcher NcqrCompatibleEventDispatcher(EventBusSettings eventBusSettings = null,
            ILogger logger = null, params IEventHandler[] handlers)
            => new NcqrCompatibleEventDispatcher(
                eventStore: Mock.Of<IEventStore>(),
                eventBusSettings: eventBusSettings ?? Create.Entity.EventBusSettings(),
                logger: logger ?? Mock.Of<ILogger>(),
                eventHandlers: handlers);
        
        public QuestionnaireKeyValueStorage QuestionnaireKeyValueStorage(
            IPlainStorage<QuestionnaireDocumentView> questionnaireDocumentViewRepository = null)
            => new QuestionnaireKeyValueStorage(
                questionnaireDocumentViewRepository ?? Mock.Of<IPlainStorage<QuestionnaireDocumentView>>());

        public QuestionnaireNameValidator QuestionnaireNameValidator(
            IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaireBrowseItemStorage = null)
            => new QuestionnaireNameValidator(
                questionnaireBrowseItemStorage ??
                Stub<IPlainStorageAccessor<QuestionnaireBrowseItem>>.WithNotEmptyValues);

        public IStatefulInterviewRepository StatefulInterviewRepository(
            IEventSourcedAggregateRootRepository aggregateRootRepository, ILiteEventBus liteEventBus = null)
            => new StatefulInterviewRepository(
                aggregateRootRepository: aggregateRootRepository ?? Mock.Of<IEventSourcedAggregateRootRepository>());

        public ISubstitutionService SubstitutionService()
            => new SubstitutionService();

        public TeamViewFactory TeamViewFactory(
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummaryReader = null)
            => new TeamViewFactory(interviewSummaryReader, Mock.Of<IUserRepository>());

        public ITopologicalSorter<T> TopologicalSorter<T>()
            => new TopologicalSorter<T>();

        public TransactionManagerProvider TransactionManagerProvider(
            Func<ICqrsPostgresTransactionManager> transactionManagerFactory = null,
            Func<ICqrsPostgresTransactionManager> noTransactionTransactionManagerFactory = null,
            ICqrsPostgresTransactionManager rebuildReadSideTransactionManager = null)
            => new TransactionManagerProvider(
                transactionManagerFactory ?? (() => Mock.Of<ICqrsPostgresTransactionManager>()),
                noTransactionTransactionManagerFactory ?? (() => Mock.Of<ICqrsPostgresTransactionManager>()));

        public VariableToUIStringService VariableToUIStringService()
            => new VariableToUIStringService();

        public IInterviewExpressionStatePrototypeProvider ExpressionStatePrototypeProvider(
            ILatestInterviewExpressionState expressionState = null)
        {
            var expressionStatePrototypeProvider = new Mock<IInterviewExpressionStatePrototypeProvider>();
            ILatestInterviewExpressionState latestInterviewExpressionState =
                expressionState ?? new InterviewExpressionStateStub();
            expressionStatePrototypeProvider.SetReturnsDefault(latestInterviewExpressionState);

            return expressionStatePrototypeProvider.Object;
        }

        public IDataExportStatusReader DataExportStatusReader(
            IDataExportProcessesService dataExportProcessesService = null,
            IFilebasedExportedDataAccessor filebasedExportedDataAccessor = null,
            IFileSystemAccessor fileSystemAccessor = null,
            IDataExportFileAccessor dataExportFileAccessor = null,
            IExternalFileStorage externalFileStorage = null,
            IQuestionnaireExportStructureStorage questionnaireExportStructureStorage = null)
        {
            return new DataExportStatusReader(
                dataExportProcessesService: dataExportProcessesService ?? Substitute.For<IDataExportProcessesService>(),
                filebasedExportedDataAccessor: filebasedExportedDataAccessor ??
                                               Substitute.For<IFilebasedExportedDataAccessor>(),
                fileSystemAccessor: fileSystemAccessor ?? Substitute.For<IFileSystemAccessor>(),
                externalFileStorage: externalFileStorage ?? Substitute.For<IExternalFileStorage>(),
                exportFileAccessor: dataExportFileAccessor ?? Substitute.For<IDataExportFileAccessor>(),
                questionnaireExportStructureStorage: questionnaireExportStructureStorage ??
                                                     Substitute.For<IQuestionnaireExportStructureStorage>());
        }

        public ISubstitutionTextFactory SubstitutionTextFactory()
        {
            return new SubstitutionTextFactory(Create.Service.SubstitutionService(),
                Create.Service.VariableToUIStringService());
        }

        public InterviewViewModelFactory InterviewViewModelFactory(IQuestionnaireStorage questionnaireRepository,
            IStatefulInterviewRepository interviewRepository,
            IEnumeratorSettings settings)
        {
            return new InterviewViewModelFactory(questionnaireRepository ?? Mock.Of<IQuestionnaireStorage>(),
                interviewRepository ?? Mock.Of<IStatefulInterviewRepository>(),
                settings ?? Mock.Of<IEnumeratorSettings>());
        }

        public AllInterviewsFactory AllInterviewsFactory(
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummarys = null)
        {
            return new AllInterviewsFactory(interviewSummarys ??
                                            Mock.Of<IQueryableReadSideRepositoryReader<InterviewSummary>>());
        }

        public ITeamInterviewsFactory TeamInterviewsFactory(
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummarys = null)
        {
            return new TeamInterviewsFactory(interviewSummarys ??
                                             Mock.Of<IQueryableReadSideRepositoryReader<InterviewSummary>>());
        }

        public PlainPostgresTransactionManager PlainPostgresTransactionManager(ISessionFactory sessionFactory = null)
            => new PlainPostgresTransactionManager(sessionFactory ?? Stub<ISessionFactory>.WithNotEmptyValues);

        public CqrsPostgresTransactionManager CqrsPostgresTransactionManager(ISessionFactory sessionFactory = null)
            => new CqrsPostgresTransactionManager(sessionFactory ?? Stub<ISessionFactory>.WithNotEmptyValues);

        public IConfigurationManager ConfigurationManager(NameValueCollection appSettings = null,
            NameValueCollection membershipSettings = null)
        {
            return new ConfigurationManager(appSettings ?? new NameValueCollection(),
                membershipSettings ?? new NameValueCollection());
        }

        public WebCacheBasedCaptchaService WebCacheBasedCaptchaService(int? failedLoginsCount = 5,
            int? timeSpanForLogins = 5, IConfigurationManager configurationManager = null)
        {
            return new WebCacheBasedCaptchaService(configurationManager ?? this.ConfigurationManager(
                                                       new NameValueCollection
                                                       {
                                                           {
                                                               "CountOfFailedLoginAttemptsBeforeCaptcha",
                                                               (failedLoginsCount ?? 5).ToString()
                                                           },
                                                           {
                                                               "TimespanInMinutesCaptchaWillBeShownAfterFailedLoginAttempt",
                                                               (timeSpanForLogins ?? 5).ToString()
                                                           },
                                                       }));
        }

        public IRandomValuesSource RandomValuesSource(params int[] sequence)
        {
            var result = Substitute.For<IRandomValuesSource>();
            if (sequence?.Length > 0) result.Next(0).ReturnsForAnyArgs(sequence.First(), sequence.Skip(1).ToArray());
            else result.Next(0).ReturnsForAnyArgs(1, 2, 3, 4, 5, 7, 8, 9, 10);
            return result;
        }

        public ReadSideToTabularFormatExportService ReadSideToTabularFormatExportService(
            IFileSystemAccessor fileSystemAccessor = null,
            ICsvWriterService csvWriterService = null,
            ICsvWriter csvWriter = null,
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewStatuses = null,
            QuestionnaireExportStructure questionnaireExportStructure = null,
            IQueryableReadSideRepositoryReader<InterviewCommentaries> interviewCommentaries = null)
            => new ReadSideToTabularFormatExportService(fileSystemAccessor ?? Mock.Of<IFileSystemAccessor>(),
                csvWriter ?? Mock.Of<ICsvWriter>(_
                    => _.OpenCsvWriter(It.IsAny<Stream>(), It.IsAny<string>()) ==
                       (csvWriterService ?? Mock.Of<ICsvWriterService>())),
                Mock.Of<ILogger>(),
                Mock.Of<ITransactionManagerProvider>(x => x.GetTransactionManager() == Mock.Of<ITransactionManager>()),
                new TestInMemoryWriter<InterviewSummary>(),
                new InterviewDataExportSettings(),
                Mock.Of<IQuestionnaireExportStructureStorage>(_
                    => _.GetQuestionnaireExportStructure(It.IsAny<QuestionnaireIdentity>()) ==
                       questionnaireExportStructure),
                Mock.Of<IProductVersion>());

        public InterviewerPrincipal InterviewerPrincipal(IPlainStorage<InterviewerIdentity> interviewersPlainStorage,
            IPasswordHasher passwordHasher)
        {
            return new InterviewerPrincipal(
                interviewersPlainStorage ?? Mock.Of<IPlainStorage<InterviewerIdentity>>(),
                passwordHasher ?? Mock.Of<IPasswordHasher>());
        }

        public SynchronizationProcess SynchronizationProcess(
            IPlainStorage<InterviewView> interviewViewRepository = null,
            IPlainStorage<InterviewerIdentity> interviewersPlainStorage = null,
            IPlainStorage<InterviewMultimediaView> interviewMultimediaViewStorage = null,
            IPlainStorage<InterviewFileView> interviewFileViewStorage = null,
            ISynchronizationService synchronizationService = null,
            ILogger logger = null,
            IUserInteractionService userInteractionService = null,
            IPasswordHasher passwordHasher = null,
            IInterviewerPrincipal principal = null,
            IInterviewerQuestionnaireAccessor questionnaireFactory = null,
            IInterviewerInterviewAccessor interviewFactory = null,
            IHttpStatistician httpStatistician = null)
        {
            var syncServiceMock = synchronizationService ?? Mock.Of<ISynchronizationService>();
            return new SynchronizationProcess(
                syncServiceMock,
                interviewersPlainStorage ?? Mock.Of<IPlainStorage<InterviewerIdentity>>(),
                interviewViewRepository ?? new InMemoryPlainStorage<InterviewView>(),
                principal ?? Mock.Of<IInterviewerPrincipal>(),
                logger ?? Mock.Of<ILogger>(),
                userInteractionService ?? Mock.Of<IUserInteractionService>(),
                questionnaireFactory ?? Mock.Of<IInterviewerQuestionnaireAccessor>(),
                interviewFactory ?? Mock.Of<IInterviewerInterviewAccessor>(),
                interviewMultimediaViewStorage ?? Mock.Of<IPlainStorage<InterviewMultimediaView>>(),
                interviewFileViewStorage ?? Mock.Of<IPlainStorage<InterviewFileView>>(),
                new CompanyLogoSynchronizer(new InMemoryPlainStorage<CompanyLogo>(), syncServiceMock),
                Mock.Of<AttachmentsCleanupService>(),
                passwordHasher ?? Mock.Of<IPasswordHasher>(),
                Mock.Of<IAssignmentsSynchronizer>(),
                Mock.Of<IQuestionnaireDownloader>(),
                httpStatistician ?? Mock.Of<IHttpStatistician>(),
                Mock.Of<IAssignmentDocumentsStorage>(),
                Mock.Of<IAudioFileStorage>(),
                Mock.Of<ITabletDiagnosticService>(),
                Mock.Of<IInterviewerSettings>(),
                Mock.Of<IAuditLogSynchronizer>(),
                Mock.Of<IAuditLogService>());
        }

        public SynchronizationService SynchronizationService(IPrincipal principal = null,
            IRestService restService = null,
            IInterviewerSettings interviewerSettings = null,
            ISyncProtocolVersionProvider syncProtocolVersionProvider = null,
            IFileSystemAccessor fileSystemAccessor = null,
            ILogger logger = null)
        {
            return new SynchronizationService(
                principal ?? Mock.Of<IPrincipal>(),
                restService ?? Mock.Of<IRestService>(),
                interviewerSettings ?? Mock.Of<IInterviewerSettings>(),
                syncProtocolVersionProvider ?? Mock.Of<ISyncProtocolVersionProvider>(),
                fileSystemAccessor ?? Mock.Of<IFileSystemAccessor>(),
                Mock.Of<ICheckVersionUriProvider>(),
                logger ?? Mock.Of<ILogger>()
            );
        }

        public TesterImageFileStorage TesterPlainInterviewFileStorage(IFileSystemAccessor fileSystemAccessor,
            string rootDirectory)
        {
            return new TesterImageFileStorage(fileSystemAccessor, rootDirectory);
        }

        public IQuestionnaireDownloader QuestionnaireDownloader(
            IAttachmentContentStorage attachmentContentStorage = null,
            IInterviewerQuestionnaireAccessor questionnairesAccessor = null,
            ISynchronizationService synchronizationService = null)
        {
            return new QuestionnaireDownloader(
                attachmentContentStorage ?? Mock.Of<IAttachmentContentStorage>(),
                questionnairesAccessor ?? Mock.Of<IInterviewerQuestionnaireAccessor>(),
                synchronizationService ?? Mock.Of<ISynchronizationService>());
        }

        public IAssignmentsSynchronizer AssignmentsSynchronizer(
            IAssignmentSynchronizationApi synchronizationService = null,
            IAssignmentDocumentsStorage assignmentsRepository = null,
            IQuestionnaireDownloader questionnaireDownloader = null,
            IQuestionnaireStorage questionnaireStorage = null,
            IPlainStorage<InterviewView> interviewViewRepository = null)
        {
            return new AssignmentsSynchronizer(
                synchronizationService ?? Mock.Of<IAssignmentSynchronizationApi>(),
                assignmentsRepository ?? Create.Storage.AssignmentDocumentsInmemoryStorage(),
                questionnaireDownloader ?? Mock.Of<IQuestionnaireDownloader>(),
                questionnaireStorage ?? Mock.Of<IQuestionnaireStorage>(),
                Mock.Of<IAnswerToStringConverter>(),
                Mock.Of<IInterviewAnswerSerializer>(),
                interviewViewRepository ?? Mock.Of<IPlainStorage<InterviewView>>());
        }

        public IAnswerToStringConverter AnswerToStringConverter()
        {
            return new AnswerToStringConverter();
        }

        public ExpressionsPlayOrderProvider ExpressionsPlayOrderProvider(
            IExpressionProcessor expressionProcessor = null,
            IMacrosSubstitutionService macrosSubstitutionService = null)
        {
            if (expressionProcessor == null && !ServiceLocator.IsLocationProviderSet)
            {
                var serviceLocator = Stub<IServiceLocator>.WithNotEmptyValues;

                ServiceLocator.SetLocatorProvider(() => serviceLocator);
                Setup.StubToMockedServiceLocator<IExpressionProcessor>();
            }

            return new ExpressionsPlayOrderProvider(
                new ExpressionsGraphProvider(
                    expressionProcessor ?? ServiceLocator.Current.GetInstance<IExpressionProcessor>(),
                    macrosSubstitutionService ?? Create.Service.DefaultMacrosSubstitutionService()));
        }

        public IMacrosSubstitutionService DefaultMacrosSubstitutionService()
        {
            var macrosSubstitutionServiceMock = new Mock<IMacrosSubstitutionService>();
            macrosSubstitutionServiceMock.Setup(x => x.InlineMacros(It.IsAny<string>(), It.IsAny<IEnumerable<Macro>>()))
                .Returns((string e, IEnumerable<Macro> macros) => e);
            return macrosSubstitutionServiceMock.Object;
        }

        public IAssignmentsService AssignmentService(params Assignment[] assignments)
        {
            IPlainStorageAccessor<Assignment> accessor = new TestPlainStorage<Assignment>();
            foreach (var assignment in assignments)
            {
                accessor.Store(assignment, assignment.Id);
            }

            var service = new AssignmentsService(accessor, Mock.Of<IInterviewAnswerSerializer>());

            return service;
        }

        public IInterviewTreeBuilder InterviewTreeBuilder()
        {
            return new InterviewTreeBuilder(Create.Service.SubstitutionTextFactory());
        }

        public InterviewActionsExporter InterviewActionsExporter(ICsvWriter csvWriter = null,
            IFileSystemAccessor fileSystemAccessor = null,
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewStatuses = null,
            QuestionnaireExportStructure questionnaireExportStructure = null)
        {
            return new InterviewActionsExporter(new InterviewDataExportSettings(),
                fileSystemAccessor ?? Mock.Of<IFileSystemAccessor>(),
                csvWriter ?? Mock.Of<ICsvWriter>(),
                Create.Service.TransactionManagerProvider(),
                interviewStatuses ?? new TestInMemoryWriter<InterviewSummary>(),
                Mock.Of<ILogger>(),
                Mock.Of<ISessionProvider>());
        }

        public InterviewStatusTimeSpanDenormalizer InterviewStatusTimeSpanDenormalizer()
        {
            return new InterviewStatusTimeSpanDenormalizer();
        }

        public ICsvWriter CsvWriter(List<CsvData> writeTo)
        {
            var csvWriterMock = new Mock<ICsvWriter>();
            csvWriterMock
                .Setup(x => x.WriteData(It.IsAny<string>(), It.IsAny<IEnumerable<string[]>>(), It.IsAny<string>()))
                .Callback((string s, IEnumerable<string[]> p, string t) =>
                {
                    writeTo.Add(new CsvData
                    {
                        File = s,
                        Data = p.ToList()
                    });
                });
            return csvWriterMock.Object;
        }

        public UserImportService UserImportService(UserPreloadingSettings userPreloadingSettings = null,
            ICsvReader csvReader = null,
            IPlainStorageAccessor<UsersImportProcess> importUsersProcessRepository = null,
            IPlainStorageAccessor<UserToImport> importUsersRepository = null,
            IUserRepository userStorage = null,
            IUserImportVerifier userImportVerifier = null,
            IAuthorizedUser authorizedUser = null,
            ISessionProvider sessionProvider = null,
            UsersImportTask usersImportTask = null)
        {
            usersImportTask = usersImportTask ?? new UsersImportTask(Mock.Of<IScheduler>(x =>
                                  x.GetCurrentlyExecutingJobs() == Array.Empty<IJobExecutionContext>()));

            userPreloadingSettings = userPreloadingSettings ?? Create.Entity.UserPreloadingSettings();
            return new UserImportService(
                userPreloadingSettings,
                csvReader ?? Stub<ICsvReader>.WithNotEmptyValues,
                importUsersProcessRepository ?? Stub<IPlainStorageAccessor<UsersImportProcess>>.WithNotEmptyValues,
                importUsersRepository ?? Stub<IPlainStorageAccessor<UserToImport>>.WithNotEmptyValues,
                userStorage ?? Stub<IUserRepository>.WithNotEmptyValues,
                userImportVerifier ?? new UserImportVerifier(userPreloadingSettings),
                authorizedUser ?? Stub<IAuthorizedUser>.WithNotEmptyValues,
                sessionProvider ?? Stub<ISessionProvider>.WithNotEmptyValues,
                usersImportTask ?? Stub<UsersImportTask>.WithNotEmptyValues);
        }

        public ICsvReader CsvReader<T>(string[] headers, params T[] rows)
        {
            return Mock.Of<ICsvReader>(
                x => x.ReadAll<T>(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<bool>()) == rows &&
                     x.ReadHeader(It.IsAny<Stream>(), It.IsAny<string>()) == headers);
        }

        public InterviewerProfileFactory InterviewerProfileFactory(TestHqUserManager userManager = null,
            IQueryableReadSideRepositoryReader<InterviewSummary> interviewRepository = null,
            IDeviceSyncInfoRepository deviceSyncInfoRepository = null,
            IInterviewerVersionReader interviewerVersionReader = null,
            IInterviewFactory interviewFactory = null)
        {
            return new InterviewerProfileFactory(
                userManager ?? Mock.Of<HqUserManager>(),
                interviewRepository ?? Mock.Of<IQueryableReadSideRepositoryReader<InterviewSummary>>(),
                deviceSyncInfoRepository ?? Mock.Of<IDeviceSyncInfoRepository>(),
                interviewerVersionReader ?? Mock.Of<IInterviewerVersionReader>(),
                interviewFactory ?? Mock.Of<IInterviewFactory>(),
                Mock.Of<IAuthorizedUser>(),
                Mock.Of<IQRCodeHelper>());
        }

        public StatefullInterviewSearcher StatefullInterviewSearcher()
        {
            return new StatefullInterviewSearcher(Mock.Of<IInterviewFactory>(x =>
                x.GetFlaggedQuestionIds(It.IsAny<Guid>()) == new Identity[] { }));
        }

        public InterviewPackagesService InterviewPackagesService(
            IPlainStorageAccessor<InterviewPackage> interviewPackageStorage = null,
            IPlainStorageAccessor<BrokenInterviewPackage> brokenInterviewPackageStorage = null,
            ILogger logger = null,
            IJsonAllTypesSerializer serializer = null,
            ICommandService commandService = null,
            IInterviewUniqueKeyGenerator uniqueKeyGenerator = null,
            SyncSettings syncSettings = null,
            IQueryableReadSideRepositoryReader<InterviewSummary> interviews = null,
            ITransactionManager transactionManager = null,
            IUserRepository userRepository = null)
        {
            InterviewKey generatedInterviewKey = new InterviewKey(5533);

            var userRepositoryMock = new Mock<IUserRepository>();

            var hqUserProfile = Mock.Of<HqUserProfile>(_ => _.SupervisorId == Id.gB);

            var hqUser = Mock.Of<HqUser>(_ => _.Id == Id.gA
                                           && _.Profile == hqUserProfile);
            userRepositoryMock
                .Setup(arg => arg.FindByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(hqUser);

            return new InterviewPackagesService(
                syncSettings: syncSettings ?? Mock.Of<SyncSettings>(),
                logger: logger ?? Mock.Of<ILogger>(),
                serializer: serializer ?? new JsonAllTypesSerializer(),
                interviewPackageStorage: interviewPackageStorage ?? Mock.Of<IPlainStorageAccessor<InterviewPackage>>(),
                brokenInterviewPackageStorage: brokenInterviewPackageStorage ??
                                               Mock.Of<IPlainStorageAccessor<BrokenInterviewPackage>>(),
                commandService: commandService ?? Mock.Of<ICommandService>(),
                uniqueKeyGenerator: uniqueKeyGenerator ?? Mock.Of<IInterviewUniqueKeyGenerator>(x => x.Get() == generatedInterviewKey),
                interviews: interviews ?? new TestInMemoryWriter<InterviewSummary>(),
                transactionManager: transactionManager ?? Mock.Of<ITransactionManager>(),
                userRepository: userRepository ?? userRepositoryMock.Object,
                packagesTracker: new TestPlainStorage<ReceivedPackageLogEntry>());
        }

        public ImportDataVerifier ImportDataVerifier(IFileSystemAccessor fileSystem = null,
            IInterviewTreeBuilder interviewTreeBuilder = null,
            IUserViewFactory userViewFactory = null)
            => new ImportDataVerifier(fileSystem ?? new FileSystemIOAccessor(),
                interviewTreeBuilder ?? Mock.Of<IInterviewTreeBuilder>(),
                userViewFactory ?? Mock.Of<IUserViewFactory>());

        public IAssignmentsUpgrader AssignmentsUpgrader(IPreloadedDataVerifier importService = null,
            IQuestionnaireStorage questionnaireStorage = null,
            IPlainStorageAccessor<Assignment> assignments = null,
            IAssignmentsUpgradeService upgradeService = null)
        {
            return new AssignmentsUpgrader(assignments ?? new TestPlainStorage<Assignment>(),
                importService ?? Mock.Of<IPreloadedDataVerifier>(s => s.VerifyWithInterviewTree(It.IsAny<List<InterviewAnswer>>(), It.IsAny<Guid?>(), It.IsAny<IQuestionnaire>()) == null),
                questionnaireStorage ?? Mock.Of<IQuestionnaireStorage>(),
                upgradeService ?? Mock.Of<IAssignmentsUpgradeService>(),
                Create.Service.PlainPostgresTransactionManager());
        }

        public AssignmentsImportFileConverter AssignmentsImportFileConverter(IFileSystemAccessor fs = null, IUserViewFactory userViewFactory = null) 
            => new AssignmentsImportFileConverter(fs ?? Create.Service.FileSystemIOAccessor(), userViewFactory ?? Mock.Of<IUserViewFactory>());

        public AssignmentsImportReader AssignmentsImportReader(ICsvReader csvReader = null,
            IArchiveUtils archiveUtils = null)
            => new AssignmentsImportReader(csvReader ?? Create.Service.CsvReader(),
                archiveUtils ?? Create.Service.ArchiveUtils());

        public CsvReader CsvReader() => new CsvReader();
        public ZipArchiveUtils ArchiveUtils() => new ZipArchiveUtils();

        public AssignmentsImportService AssignmentsImportService(IUserViewFactory userViewFactory = null,
            IPreloadedDataVerifier verifier = null,
            IAuthorizedUser authorizedUser = null,
            IPlainSessionProvider sessionProvider = null,
            IPlainStorageAccessor<AssignmentsImportProcess> importAssignmentsProcessRepository = null,
            IPlainStorageAccessor<AssignmentToImport> importAssignmentsRepository = null,
            IInterviewCreatorFromAssignment interviewCreatorFromAssignment = null,
            IPlainStorageAccessor<Assignment> assignmentsStorage = null,
            IAssignmentsImportFileConverter assignmentsImportFileConverter = null)
        {
            var session = Mock.Of<ISession>(x =>
                x.Query<AssignmentsImportProcess>() == GetNhQueryable<AssignmentsImportProcess>() &&
                x.Query<AssignmentToImport>() == GetNhQueryable<AssignmentToImport>());

            sessionProvider = sessionProvider ?? Mock.Of<IPlainSessionProvider>(x => x.GetSession() == session);
            userViewFactory = userViewFactory ?? Mock.Of<IUserViewFactory>();

            return new AssignmentsImportService(userViewFactory,
                verifier ?? ImportDataVerifier(),
                authorizedUser ?? Mock.Of<IAuthorizedUser>(),
                sessionProvider,
                importAssignmentsProcessRepository ?? Mock.Of<IPlainStorageAccessor<AssignmentsImportProcess>>(),
                importAssignmentsRepository ?? Mock.Of<IPlainStorageAccessor<AssignmentToImport>>(),
                interviewCreatorFromAssignment ?? Mock.Of<IInterviewCreatorFromAssignment>(),
                assignmentsStorage ?? Mock.Of<IPlainStorageAccessor<Assignment>>(),
                assignmentsImportFileConverter ?? AssignmentsImportFileConverter(userViewFactory: userViewFactory));
        }

        private static IQueryable<TEntity> GetNhQueryable<TEntity>() => Mock.Of<IQueryable<TEntity>>(x => x.Provider == Mock.Of<INhQueryProvider>());
    }
}
