﻿using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Main.Core.Entities.SubEntities;
using Resources;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.UserProfile;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.SurveyManagement.Web.Controllers;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Headquarters.Code;
using WB.UI.Headquarters.Filters;

namespace WB.UI.Headquarters.Controllers
{
    [AuthorizeOr403(Roles = "Administrator, Observer")]
    [ValidateInput(false)]
    public class HeadquartersController : TeamController
    {
        public HeadquartersController(ICommandService commandService, 
                              ILogger logger,
                              IAuthorizedUser authorizedUser,
                              HqUserManager userManager,
                              IPlainKeyValueStorage<ProfileSettings> profileSettingsStorage)
            : base(commandService, logger, authorizedUser, userManager, profileSettingsStorage)
        {
        }

        public ActionResult Create()
        {
            this.ViewBag.ActivePage = MenuItem.Headquarters;

            return this.View(new UserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeOr403(Roles = "Administrator")]
        [ObserverNotAllowed]
        public async Task<ActionResult> Create(UserModel model)
        {
            if (ModelState.IsValid)
            {
                var creationResult = await this.CreateUserAsync(model, UserRoles.Headquarter);

                if (creationResult.Succeeded)
                {
                    this.Success(HQ.UserWasCreatedFormat.FormatString(model.UserName));
                    return this.RedirectToAction("Index");
                }
                AddErrors(creationResult);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AuthorizeOr403(Roles = "Administrator, Observer")]
        public ActionResult Index()
        {
            this.ViewBag.ActivePage = MenuItem.Headquarters;

            return this.View();
        }

        [AuthorizeOr403(Roles = "Administrator")]
        public async Task<ActionResult> Edit(Guid id)
        {
            this.ViewBag.ActivePage = MenuItem.Headquarters;

            var user = await this.userManager.FindByIdAsync(id);

            if(user == null) throw new HttpException(404, string.Empty);
            if (!user.IsInRole(UserRoles.Headquarter)) throw new HttpException(403, HQ.NoPermission);

            return this.View(new UserEditModel()
                {
                    Id = user.Id,
                    Email = user.Email,
                    IsLocked = user.IsLockedByHeadquaters,
                    UserName = user.UserName,
                    PersonName = user.FullName,
                    PhoneNumber = user.PhoneNumber
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeOr403(Roles = "Administrator")]
        [ObserverNotAllowed]
        public async Task<ActionResult> Edit(UserEditModel model)
        {
            this.ViewBag.ActivePage = MenuItem.Headquarters;

            if (ModelState.IsValid)
            {
                var updateResult = await this.UpdateAccountAsync(model);
                if (updateResult.Succeeded)
                {
                    this.Success(string.Format(HQ.UserWasUpdatedFormat, model.UserName));
                    return this.RedirectToAction("Index");
                }
                AddErrors(updateResult);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }
}
