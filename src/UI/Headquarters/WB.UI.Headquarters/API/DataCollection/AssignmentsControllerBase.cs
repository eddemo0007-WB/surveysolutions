﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Commands.Assignment;
using WB.Core.SharedKernels.DataCollection.WebApi;

namespace WB.UI.Headquarters.API.DataCollection
{
    public abstract class AssignmentsControllerBase : ApiController
    {
        protected readonly IAuthorizedUser authorizedUser;
        private readonly IAssignmentsService assignmentsService;
        private readonly ICommandService commandService;

        protected AssignmentsControllerBase(IAuthorizedUser authorizedUser,
            IAssignmentsService assignmentsService,
            ICommandService commandService)
        {
            this.authorizedUser = authorizedUser;
            this.assignmentsService = assignmentsService;
            this.commandService = commandService;
        }

        public virtual Task<AssignmentApiDocument> GetAssignmentAsync(int id, CancellationToken cancellationToken)
        {
            var authorizedUserId = this.authorizedUser.Id;

            Assignment assignment = this.assignmentsService.GetAssignment(id);

            if (assignment.ResponsibleId != authorizedUserId && assignment.Responsible.ReadonlyProfile.SupervisorId != authorizedUserId)
            {
                throw new HttpResponseException(HttpStatusCode.Forbidden);
            }

            AssignmentApiDocument assignmentApiDocument = this.assignmentsService.MapAssignment(assignment);

            return Task.FromResult(assignmentApiDocument);
        }

        public virtual Task<List<AssignmentApiView>> GetAssignmentsAsync(CancellationToken cancellationToken)
        {
            var authorizedUserId = this.authorizedUser.Id;

            var assignments = GetAssignmentsForResponsible(authorizedUserId);

            var assignmentApiViews = new List<AssignmentApiView>();

            foreach (var assignment in assignments)
            {
                assignmentApiViews.Add(new AssignmentApiView
                {
                    Id = assignment.Id,
                    Quantity = assignment.InterviewsNeeded, // + assignment.InterviewsProvided,
                    QuestionnaireId = assignment.QuestionnaireId,
                    ResponsibleId = assignment.ResponsibleId,
                    ResponsibleName = assignment.Responsible.Name,
                    IsAudioRecordingEnabled = assignment.AudioRecording
                });
            }

            return Task.FromResult(assignmentApiViews);
        }

        public virtual HttpResponseMessage Received(int id)
        {
            var assignment = this.assignmentsService.GetAssignment(id);
            if (assignment == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Assignment not found");
            }

            var authorizedUserId = this.authorizedUser.Id;
            if (assignment.ResponsibleId != authorizedUserId &&
                assignment.Responsible.ReadonlyProfile.SupervisorId != authorizedUserId)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Assignment was reassigned");
            }

            commandService.Execute(new MarkAssignmentAsReceivedByTablet(assignment.PublicKey, authorizedUserId));

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        protected abstract IEnumerable<Assignment> GetAssignmentsForResponsible(Guid responsibleId);

    }
}