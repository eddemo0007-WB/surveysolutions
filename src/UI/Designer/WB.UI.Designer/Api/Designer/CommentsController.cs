﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using WB.Core.BoundedContexts.Designer.Comments;
using WB.Core.BoundedContexts.Designer.Implementation.Services.Accounts.Membership;
using WB.UI.Designer.Filters;
using WB.UI.Designer.Models;
using WB.UI.Shared.Web.Filters;

namespace WB.UI.Designer.Api.Designer
{
    [Authorize]
    [QuestionnairePermissions]
    [ApiNoCache]
    [RoutePrefix("questionnaire/{id:Guid}")]
    public class CommentsController : ApiController
    {
        private readonly ICommentsService commentsService;
        private readonly IMembershipUserService userHelper;

        public CommentsController(
            ICommentsService commentsService, 
            IMembershipUserService userHelper)
        {
            this.commentsService = commentsService;
            this.userHelper = userHelper;
        }

        [HttpGet]
        [CamelCase]
        [Route("entity/{itemId:Guid}/comments")]
        public List<CommentView> Get(Guid id, Guid itemId)
        {
            return this.commentsService.LoadCommentsForEntity(id, itemId);
        }

        [HttpPost]
        [CamelCase]
        [Route("entity/{itemId:Guid}/addComment")]
        public HttpResponseMessage PostComment(Guid id, AddCommentModel comment)
        {
            IMembershipWebUser user = this.userHelper.WebUser;
            comment.UserName = user.UserName;
            comment.UserEmail = user.MembershipUser.Email;
            commentsService.PostComment(comment);
            return this.Request.CreateResponse(new JsonResponseResult());
        }

        [HttpPatch]
        [CamelCase]
        [Route("comment/resolve/{commentdId:Guid}")]
        public HttpResponseMessage ResolveComment(Guid id, Guid commentdId)
        {
            commentsService.ResolveComment(commentdId);
            return this.Request.CreateResponse(new JsonResponseResult());
        }
    }
}
