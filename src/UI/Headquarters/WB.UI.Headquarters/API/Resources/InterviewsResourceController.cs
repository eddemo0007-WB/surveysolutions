﻿using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using WB.Core.Infrastructure.FunctionalDenormalization.Implementation.ReadSide;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.SurveyManagement.Factories;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using WB.UI.Headquarters.API.Attributes;
using WB.UI.Headquarters.API.Filters;
using WB.UI.Headquarters.API.Formatters;

namespace WB.UI.Headquarters.API.Resources
{
    [TokenValidationAuthorizationAttribute]
    [RoutePrefix("api/resources/interviews/v1")]
    public class InterviewsResourceController : ApiController
    {
        private readonly IReadSideRepositoryReader<ViewWithSequence<InterviewData>> interviewDataReader;
        private readonly IInterviewSynchronizationDtoFactory factory;

        public InterviewsResourceController(IReadSideRepositoryReader<ViewWithSequence<InterviewData>> interviewDataReader,
            IInterviewSynchronizationDtoFactory factory)
        {
            this.interviewDataReader = interviewDataReader;
            this.factory = factory;
        }

        [Route("{id}", Name = "api.interviewDetails")]
        public HttpResponseMessage Get(string id)
        {
            var interviewData = this.interviewDataReader.GetById(id);

            InterviewData document = interviewData.Document;
            InterviewSynchronizationDto interviewSynchronizationDto = 
                factory.BuildFrom(document);

            var result = Request.CreateResponse(HttpStatusCode.OK, interviewSynchronizationDto);

            return result;
        }
    }
}