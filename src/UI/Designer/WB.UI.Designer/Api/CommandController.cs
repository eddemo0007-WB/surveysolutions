﻿using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WB.Core.BoundedContexts.Designer.Exceptions;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.UI.Designer.Code;
using WB.UI.Designer.Code.Implementation;
using WB.UI.Designer.Models;
using WB.UI.Shared.Web.CommandDeserialization;

namespace WB.UI.Designer.Api
{
    [Authorize]
    public class CommandController : ApiController
    {
        public struct CommandExecutionModel
        {
            public string Type { get; set; }
            public string Command { get; set; }
        }

        private readonly ICommandService commandService;
        private readonly ICommandDeserializer commandDeserializer;
        private readonly ILogger logger;
        private readonly ICommandInflater commandInflater;
        private readonly ICommandPostprocessor commandPostprocessor;

        public CommandController(ICommandService commandService, ICommandDeserializer commandDeserializer, ILogger logger, ICommandInflater commandPreprocessor, ICommandPostprocessor commandPostprocessor)
        {
            this.logger = logger;
            this.commandInflater = commandPreprocessor;
            this.commandService = commandService;
            this.commandDeserializer = commandDeserializer;
            this.commandPostprocessor = commandPostprocessor;
        }

        public HttpResponseMessage Post(CommandExecutionModel model)
        {
            try
            {
                var concreteCommand = this.commandDeserializer.Deserialize(model.Type, model.Command);
                this.commandInflater.PrepareDeserializedCommandForExecution(concreteCommand);

                this.commandService.Execute(concreteCommand);

                this.commandPostprocessor.ProcessCommandAfterExecution(concreteCommand);
            }
            catch (CommandInflaitingException exc)
            {
                if (exc.ExceptionType == CommandInflatingExceptionType.Forbidden)
                {
                    return this.Request.CreateErrorResponse(HttpStatusCode.Forbidden, exc.Message);
                }

                if (exc.ExceptionType == CommandInflatingExceptionType.EntityNotFound)
                {
                    return this.Request.CreateErrorResponse(HttpStatusCode.NotFound, exc.Message);
                }

                return this.Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, exc.Message);
            }
            catch (Exception e)
            {
                var domainEx = e.GetSelfOrInnerAs<QuestionnaireException>();
                if (domainEx == null)
                {
                    this.logger.Error(string.Format("Error on command of type ({0}) handling ", model.Type), e);
                    throw;
                }

                if (domainEx.ErrorType == DomainExceptionType.DoesNotHavePermissionsForEdit)
                {
                    return this.Request.CreateErrorResponse(HttpStatusCode.Forbidden, domainEx.Message);
                }

                return this.Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, domainEx.Message);
            }

            return Request.CreateResponse(new JsonQuestionnaireResult());
        }
    }
}