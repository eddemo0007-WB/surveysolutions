﻿using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.Invitations;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.Commands.Assignment;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models.Api;
using WB.UI.Headquarters.Resources;
using WB.UI.Shared.Web.Attributes;

namespace WB.UI.Headquarters.API
{
    [CamelCase]
    [RoutePrefix("api/Assignments")]
    public class AssignmentsApiController : ApiController
    {
        private readonly IAssignmentViewFactory assignmentViewFactory;
        private readonly IAuthorizedUser authorizedUser;
        private readonly IAssignmentsService assignmentsStorage;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly ISystemLog auditLog;
        private readonly IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaires;
        private readonly IInvitationService invitationService;
        private readonly IStatefulInterviewRepository interviews;
        private readonly IAssignmentPasswordGenerator passwordGenerator;
        private readonly ICommandService commandService;
        private readonly IAssignmentFactory assignmentFactory;

        public AssignmentsApiController(IAssignmentViewFactory assignmentViewFactory,
            IAuthorizedUser authorizedUser,
            IAssignmentsService assignmentsStorage,
            IQuestionnaireStorage questionnaireStorage,
            ISystemLog auditLog,
            IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaires, 
            IInvitationService invitationService,
            IStatefulInterviewRepository interviews, 
            IAssignmentPasswordGenerator passwordGenerator,
            ICommandService commandService,
            IAssignmentFactory assignmentFactory)
        {
            this.assignmentViewFactory = assignmentViewFactory;
            this.authorizedUser = authorizedUser;
            this.assignmentsStorage = assignmentsStorage;
            this.questionnaireStorage = questionnaireStorage;
            this.auditLog = auditLog;
            this.questionnaires = questionnaires;
            this.invitationService = invitationService;
            this.interviews = interviews;
            this.passwordGenerator = passwordGenerator;
            this.commandService = commandService;
            this.assignmentFactory = assignmentFactory;
        }
        
        [Route("")]
        [HttpGet]
        [Authorize(Roles = "Administrator, Headquarter, Supervisor, Interviewer")]
        public IHttpActionResult Get([FromUri]AssignmentsDataTableRequest request)
        {
            var isInterviewer = this.authorizedUser.IsInterviewer;
                       
            var input = new AssignmentsInputModel
            {
                Page = request.PageIndex,
                PageSize = request.PageSize,
                Order = request.GetSortOrder(),
                SearchBy = request.Search.Value,
                QuestionnaireId = request.QuestionnaireId,
                QuestionnaireVersion = request.QuestionnaireVersion,
                ResponsibleId = isInterviewer ? this.authorizedUser.Id :request.ResponsibleId,
                ShowArchive = !isInterviewer && request.ShowArchive,
                DateStart = request.DateStart?.ToUniversalTime(),
                DateEnd = request.DateEnd?.ToUniversalTime(),
                UserRole = request.UserRole,
                ReceivedByTablet = request.ReceivedByTablet,
                SupervisorId = request.TeamId,
                Id = request.Id
            };

            if (this.authorizedUser.IsSupervisor)
            {
                input.SupervisorId = this.authorizedUser.Id;
            }

            if (isInterviewer)
            {
                input.OnlyWithInterviewsNeeded = true;
                input.SearchByFields = AssignmentsInputModel.SearchTypes.Id 
                    | AssignmentsInputModel.SearchTypes.IdentifyingQuestions
                    | AssignmentsInputModel.SearchTypes.QuestionnaireTitle;
                input.ShowQuestionnaireTitle = true;
            }

            var result = this.assignmentViewFactory.Load(input);
            var response = new AssignmetsDataTableResponse
            {
                Draw = request.Draw + 1,
                RecordsTotal = result.TotalCount,
                RecordsFiltered = result.TotalCount,
                Data = result.Items
            };
            return this.Ok(response);
        }

        
        [Route("")]
        [HttpDelete]
        [Authorize(Roles = "Administrator, Headquarter")]
        [ObserverNotAllowedApi]
        public IHttpActionResult Delete([FromBody]int[] ids)
        {
            if (ids == null) return this.BadRequest();

            foreach (var id in ids)
            {
                Assignment assignment = this.assignmentsStorage.GetAssignment(id);
                commandService.Execute(new ArchiveAssignment(assignment.PublicKey, authorizedUser.Id));
            }

            return this.Ok();
        }

        [Route("Unarchive")]
        [HttpPost]
        [Authorize(Roles = "Administrator, Headquarter")]
        [ObserverNotAllowedApi]
        public IHttpActionResult Unarchive([FromBody]int[] ids)
        {
            if (ids == null) return this.BadRequest();
            
            foreach (var id in ids)
            {
                Assignment assignment = this.assignmentsStorage.GetAssignment(id);
                commandService.Execute(new UnarchiveAssignment(assignment.PublicKey, authorizedUser.Id));
            }

            return this.Ok();
        }

        [HttpPost]
        [Route("Assign")]
        [ObserverNotAllowedApi]
        public IHttpActionResult Assign([FromBody] AssignRequest request)
        {
            if (request?.Ids == null) return this.BadRequest();
            foreach (var idToAssign in request.Ids)
            {
                Assignment assignment = this.assignmentsStorage.GetAssignment(idToAssign);
                commandService.Execute(new ReassignAssignment(assignment.PublicKey, authorizedUser.Id, request.ResponsibleId, request.Comments));

                if (!string.IsNullOrEmpty(request.Comments))
                    assignment.SetComments(request.Comments);
            }

            return this.Ok();
        }

        [HttpPatch]
        [Route("{id:int}/SetQuantity")]
        [Authorize(Roles = "Administrator, Headquarter")]
        [ObserverNotAllowedApi]
        public IHttpActionResult SetQuantity(int id, [FromBody] UpdateAssignmentRequest request)
        {
            var assignment = this.assignmentsStorage.GetAssignment(id);

            if (request.Quantity < -1)
                return this.BadRequest(WB.UI.Headquarters.Resources.Assignments.InvalidSize);

            if(!string.IsNullOrEmpty(assignment.Email) || !string.IsNullOrEmpty(assignment.Password))
                return this.BadRequest(WB.UI.Headquarters.Resources.Assignments.WebMode);

            commandService.Execute(new UpdateAssignmentQuantity(assignment.PublicKey, authorizedUser.Id, request.Quantity));
            this.auditLog.AssignmentSizeChanged(id, request.Quantity);
            return this.Ok();
        }

        [HttpPost]
        [Route("Create")]
        [ObserverNotAllowedApi]
        public IHttpActionResult Create([FromBody] CreateAssignmentRequest request)
        {
            if (!this.authorizedUser.IsAdministrator && !this.authorizedUser.IsHeadquarter)
                return this.StatusCode(HttpStatusCode.Forbidden);

            if (request == null)
                return this.BadRequest();

            var interview = this.interviews.Get(request.InterviewId);
            if (interview == null)
                return this.NotFound();

            int? quantity;
            switch (request.Quantity)
            {
                case null:
                    quantity = 1;
                    break;
                case -1:
                    quantity = null;
                    break;
                default:
                    quantity = request.Quantity;
                    break;
            }

            var password = passwordGenerator.GetPassword(request.Password);

            //verify email
            if (!string.IsNullOrEmpty(request.Email) && AssignmentConstants.EmailRegex.Match(request.Email).Length <= 0)
                return this.BadRequest("Invalid Email");

            //verify pass
            if (!string.IsNullOrEmpty(password))
            {
                if ((password.Length < AssignmentConstants.PasswordLength ||
                     AssignmentConstants.PasswordStrength.Match(password).Length <= 0))
                    this.BadRequest("Invalid Password. At least 6 numbers and upper case letters or single symbol '?' to generate password");
            }

            //assignment with email must have quantity = 1
            if (!string.IsNullOrEmpty(request.Email) && request.Quantity != 1)
                this.BadRequest("For assignments with provided email allowed quantity is 1");

            if ((!string.IsNullOrEmpty(request.Email) || !string.IsNullOrEmpty(password)) && request.WebMode != true)
                this.BadRequest("For assignments having Email or Password Web Mode should be activated");

            if (quantity == 1 && (request.WebMode == null || request.WebMode == true) &&
                string.IsNullOrEmpty(request.Email) && !string.IsNullOrEmpty(password))
            {
                var hasPasswordInDb = this.assignmentsStorage.DoesExistPasswordInDb(interview.QuestionnaireIdentity, password);

                if (hasPasswordInDb)
                    return this.BadRequest(Assignments.DuplicatePasswordByWebModeWithQuantity1);
            }

            var questionnaire = this.questionnaireStorage.GetQuestionnaire(interview.QuestionnaireIdentity, null);
            var answers = Assignment.GetAnswersFromInterview(interview, questionnaire);
            bool isAudioRecordingEnabled = request.IsAudioRecordingEnabled ?? this.questionnaires.Query(_ => _
                                               .Where(q => q.Id == interview.QuestionnaireIdentity.ToString())
                                               .Select(q => q.IsAudioRecordingEnabled).FirstOrDefault());

            var assignment = assignmentFactory.CreateAssignment(authorizedUser.Id,
                interview.QuestionnaireIdentity,
                request.ResponsibleId,
                request.Quantity,
                request.Email,
                password,
                request.WebMode,
                isAudioRecordingEnabled,
                answers,
                null,
                request.Comments
            );

            this.invitationService.CreateInvitationForWebInterview(assignment);

            return this.Ok();
        }

        public class CreateAssignmentRequest
        {
            public string InterviewId { get; set; }
            public Guid ResponsibleId { get; set; }
            public int? Quantity { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public bool? WebMode { get; set; }
            public bool? IsAudioRecordingEnabled { get; set; }
            public string Comments { get; set; }
        }

        public class UpdateAssignmentRequest
        {
            public int? Quantity { get; set; }
        }

        public class AssignRequest
        {
            public Guid ResponsibleId { get; set; }
            public string Comments { get; set; }

            public int[] Ids { get; set; }
        }

        public class AssignmetsDataTableResponse : DataTableResponse<AssignmentRow>
        {
        }

        public class AssignmentsDataTableRequest : DataTableRequest
        {
            public Guid? QuestionnaireId { get; set; }
            public long? QuestionnaireVersion { get; set; }
            public Guid? ResponsibleId { get; set; }
            public Guid? TeamId { get; set; }

            public bool ShowArchive { get; set; }

            public DateTime? DateStart { get; set; }
            public DateTime? DateEnd { get; set; }
            public UserRoles? UserRole { get; set; }
            public AssignmentReceivedState ReceivedByTablet { get; set; }

            public int? Id { get; set; }
        }
    }
}