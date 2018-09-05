﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WB.Core.BoundedContexts.Supervisor.Views;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Implementation;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.SharedKernels.Enumerator.Implementation.Services.Synchronization;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.Services.Synchronization;
using WB.Core.SharedKernels.Enumerator.Views;

namespace WB.Core.BoundedContexts.Supervisor.Services.Implementation
{
    public class SupervisorSynchronizationProcess : OnlineSynchronizationProcessBase
    {
        private readonly IPrincipal principal;
        private readonly IPlainStorage<SupervisorIdentity> supervisorsPlainStorage;
        private readonly IPasswordHasher passwordHasher;

        public SupervisorSynchronizationProcess(
            ISupervisorSynchronizationService synchronizationService,
            IPlainStorage<SupervisorIdentity> supervisorsPlainStorage,
            IPlainStorage<InterviewView> interviewViewRepository,
            IPrincipal principal,
            ILogger logger,
            IUserInteractionService userInteractionService,
            IPasswordHasher passwordHasher,
            IAssignmentsSynchronizer assignmentsSynchronizer,
            IHttpStatistician httpStatistician,
            IAssignmentDocumentsStorage assignmentsStorage,
            ISupervisorSettings supervisorSettings,
            IAuditLogSynchronizer auditLogSynchronizer,
            IAuditLogService auditLogService,
            IServiceLocator serviceLocator) : base(synchronizationService,
            interviewViewRepository, principal, logger, userInteractionService, assignmentsSynchronizer, httpStatistician,
            assignmentsStorage, auditLogSynchronizer, auditLogService, supervisorSettings, serviceLocator)
        {
            this.principal = principal;
            this.supervisorsPlainStorage = supervisorsPlainStorage;
            this.passwordHasher = passwordHasher;
        }

        protected override Task CheckAfterStartSynchronization(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override void UpdatePasswordOfResponsible(RestCredentials credentials)
        {
            var localSupervisor = this.supervisorsPlainStorage.FirstOrDefault();
            localSupervisor.PasswordHash = this.passwordHasher.Hash(credentials.Password);
            localSupervisor.Token = credentials.Token;

            this.supervisorsPlainStorage.Store(localSupervisor);
            this.principal.SignIn(localSupervisor.Name, credentials.Password, true);
        }

        protected virtual Task<List<Guid>> FindObsoleteInterviewsAsync(IEnumerable<InterviewView> localInterviews,
            IEnumerable<InterviewApiView> remoteInterviews, IProgress<SyncProgressInfo> progress, CancellationToken cancellationToken)
            => Task.FromResult(new List<Guid>());
    }
}
