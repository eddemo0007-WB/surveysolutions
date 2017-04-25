﻿using System.Web.Mvc;
using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.Tests.Abc;
using WB.UI.Headquarters.Controllers;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.PeriodicStatusReportTests
{
    internal class when_request_quantity_report_by_interviewers_for_user_in_hq_role
    {
        private Establish context = () =>
        {
            var authorizedUser = Mock.Of<IAuthorizedUser>(x => x.IsHeadquarter == true);
            reportController = Create.Controller.ReportsController(authorizedUser: authorizedUser);
        };

        Because of = () =>
            result = reportController.QuantityByInterviewers(null) as ViewResult;

        It should_active_page_be_NumberOfCompletedInterviews = () =>
            ((MenuItem)result.ViewBag.ActivePage).ShouldEqual(MenuItem.NumberOfCompletedInterviews);

        It should_responsible_name_be_not_a_link = () =>
            ((PeriodicStatusReportModel)result.Model).CanNavigateToQuantityByTeamMember.ShouldEqual(false);

        It should_go_back_to_supervisor_button_be_visible = () =>
            ((PeriodicStatusReportModel)result.Model).CanNavigateToQuantityBySupervisors.ShouldEqual(true);

        It should_WebApiActionName_be_ByInterviewers = () =>
          ((PeriodicStatusReportModel)result.Model).WebApiActionName.ShouldEqual(PeriodicStatusReportWebApiActionName.ByInterviewers);

        It should_ReportName_be_Quantity = () =>
           ((PeriodicStatusReportModel)result.Model).ReportName.ShouldEqual("Quantity");

        private static ReportsController reportController;
        private static ViewResult result;
    }
}
