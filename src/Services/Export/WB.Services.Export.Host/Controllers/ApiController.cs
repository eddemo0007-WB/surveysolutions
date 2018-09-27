﻿using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WB.Services.Export.Interview;
using WB.Services.Export.Questionnaire;
using WB.Services.Export.Services.Processing;
using WB.Services.Export.Services.Processing.Good;
using WB.Services.Export.Tenant;

namespace WB.Services.Export.Host.Controllers
{
    [Route("api/v1/job")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IDataExportProcessesService exportProcessesService;
        private readonly ILogger<JobController> logger;
        private readonly IDataExportStatusReader dataExportStatusReader;


        public JobController(IDataExportProcessesService exportProcessesService,
        //    IDataExportStatusReader dataExportStatusReader,
            ILogger<JobController> logger)
        {
            this.exportProcessesService = exportProcessesService;
            this.logger = logger;
            this.dataExportStatusReader = dataExportStatusReader;
        }

        [HttpPut]
        public ActionResult RequestUpdate(string questionnaireId,
            DataExportFormat format, InterviewStatus? status, DateTime? from, DateTime? to, 
            string archiveName, string archivePassword, string apiKey,
            [FromHeader(Name = "Origin")]string tenantBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(archiveName))
            {
                return BadRequest("ArchiveName is required");
            }

            var args = new DataExportProcessDetails(format, new QuestionnaireId(questionnaireId), null)
            {
                Tenant = new TenantInfo(tenantBaseUrl, apiKey),
                InterviewStatus = status,
                FromDate = from,
                ToDate = to,
                ArchivePassword = archivePassword,
                ArchiveName = archiveName
            };

            exportProcessesService.AddDataExport(args);

            return Ok();
        }
    }
}
