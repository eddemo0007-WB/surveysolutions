﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Quartz;
using WB.Core.BoundedContexts.Headquarters.DataExport;
using WB.Core.BoundedContexts.Headquarters.Factories;
using WB.Core.BoundedContexts.Headquarters.Repositories;
using WB.Core.BoundedContexts.Headquarters.Workspaces;
using WB.Core.BoundedContexts.Headquarters.Workspaces.Jobs;
using WB.Core.Infrastructure.Domain;
using WB.Infrastructure.Native.Workspaces;

namespace WB.UI.Headquarters.Code.Workspaces
{
    public class DeleteWorkspaceRequestHandler : IRequestHandler<DeleteWorkspaceRequest, DeleteWorkspaceResponse>
    {
        private readonly IInScopeExecutor<IQuestionnaireBrowseViewFactory> questionnaireViewFactory;
        private readonly IInScopeExecutor<IMapStorageService, IWorkspacesService> deleteService;
        private readonly IInScopeExecutor<IExportServiceApi> exportService;
        private readonly IScheduler scheduler;
        private readonly IWorkspacesCache workspacesCache;

        public DeleteWorkspaceRequestHandler(
            IWorkspacesCache workspacesCache,
            IInScopeExecutor<IQuestionnaireBrowseViewFactory> questionnaireViewFactory,
            IInScopeExecutor<IMapStorageService, IWorkspacesService> deleteService, 
            IInScopeExecutor<IExportServiceApi> exportService,
            IScheduler scheduler)
        {
            this.questionnaireViewFactory = questionnaireViewFactory;
            this.workspacesCache = workspacesCache;
            this.deleteService = deleteService;
            this.exportService = exportService;
            this.scheduler = scheduler;
        }

        public async Task<DeleteWorkspaceResponse> Handle(
            DeleteWorkspaceRequest request, 
            CancellationToken cancellationToken = default)
        {
            var workspace = workspacesCache.AllEnabledWorkspaces()
                                .FirstOrDefault(w => w.Name == request.WorkspaceName)
                            ?? throw new MissingWorkspaceException(
                                "Cannot find workspace with name: " + request.WorkspaceName);

            if (workspace.Name == WorkspaceConstants.DefaultWorkspaceName)
            {
                return new DeleteWorkspaceResponse
                {
                    Success = false,
                    ErrorMessage = "Cannot delete primary workspace"
                };
            }
            
            bool canDelete = false;

            questionnaireViewFactory.Execute(accessor =>
            {
                var questionnaires = accessor.GetAllQuestionnaireIdentities();
                canDelete = !questionnaires.Any();
            }, workspace.Name);

            if (!canDelete)
                return new DeleteWorkspaceResponse
                {
                    Success = false,
                    ErrorMessage = "Workspace cannot be deleted. There is questionnaire exists"
                };
            
            await exportService.ExecuteAsync(async export =>
            {
                await export.DeleteTenant();
            }, workspace.Name);

            await deleteService.ExecuteAsync(async (map, workspacesService) =>
            {
                await map.DeleteAllMaps();
                await workspacesService.DeleteAsync(workspace, cancellationToken);
            }, workspace.Name);

            workspacesCache.InvalidateCache();

            await DeleteWorkspaceSchemaJob.Schedule(scheduler, workspace);
            
            return new DeleteWorkspaceResponse
            {
                Success = true
            };
        }
    }
}