﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Main.Core.Entities.SubEntities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WB.Core.BoundedContexts.Headquarters.Resources;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Users;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.SurveyManagement.Web.Models;
using WB.UI.Headquarters.Filters;
using WB.UI.Headquarters.Models;
using WB.UI.Headquarters.Models.CompanyLogo;
using WB.UI.Headquarters.Models.Users;
using WB.UI.Headquarters.Resources;
using WB.UI.Headquarters.Services.Impl;
using WB.UI.Shared.Web.Captcha;
using WB.UI.Shared.Web.Services;

namespace WB.UI.Headquarters.Controllers
{
    public class AccountController : Controller
    {
        private readonly IPlainKeyValueStorage<CompanyLogo> appSettingsStorage;
        private readonly ICaptchaService captchaService;
        private readonly ICaptchaProvider captchaProvider;
        private readonly SignInManager<HqUser> signInManager;
        private readonly HqUserStore userRepository;
        private readonly IAuthorizedUser authorizedUser;

        public AccountController(IPlainKeyValueStorage<CompanyLogo> appSettingsStorage, 
            ICaptchaService captchaService,
            ICaptchaProvider captchaProvider,
            SignInManager<HqUser> signInManager,
            HqUserStore userRepository,
            IAuthorizedUser authorizedUser)
        {
            this.appSettingsStorage = appSettingsStorage;
            this.captchaService = captchaService;
            this.captchaProvider = captchaProvider;
            this.signInManager = signInManager;
            this.userRepository = userRepository;
            this.authorizedUser = authorizedUser;
        }

        [HttpGet]
        public IActionResult LogOn(string returnUrl)
        {
            this.ViewBag.ActivePage = MenuItem.Logon;
            this.ViewBag.ReturnUrl = returnUrl;
            this.ViewBag.HasCompanyLogo = this.appSettingsStorage.GetById(CompanyLogo.CompanyLogoStorageKey) != null;

            return this.View(new LogOnModel
            {
                RequireCaptcha = this.captchaService.ShouldShowCaptcha(null)
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LogOn(LogOnModel model, string returnUrl)
        {
            this.ViewBag.ActivePage = MenuItem.Logon;
            this.ViewBag.HasCompanyLogo = this.appSettingsStorage.GetById(CompanyLogo.CompanyLogoStorageKey) != null;
            model.RequireCaptcha = this.captchaService.ShouldShowCaptcha(model.UserName);

            if (model.RequireCaptcha && !await this.captchaProvider.IsCaptchaValid(Request))
            {
                this.ModelState.AddModelError("InvalidCaptcha", ErrorMessages.PleaseFillCaptcha);
                return this.View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var signInResult = await this.signInManager.PasswordSignInAsync(model.UserName, model.Password, true, false);
            if (signInResult.Succeeded)
            {
                this.captchaService.ResetFailedLogin(model.UserName);
                return Redirect(returnUrl ?? Url.Content("~/"));
            }

            if (signInResult.IsLockedOut)
            {
                this.captchaService.ResetFailedLogin(model.UserName);
                this.ModelState.AddModelError("LockedOut", ErrorMessages.SiteAccessNotAllowed);
                return View(model);
            }

            this.captchaService.RegisterFailedLogin(model.UserName);
            model.RequireCaptcha = this.captchaService.ShouldShowCaptcha(model.UserName);
            this.ModelState.AddModelError("InvalidCredentials", ErrorMessages.IncorrectUserNameOrPassword);
            return View(model);
        }

        [HttpGet]
        [Authorize]
        [AntiForgeryFilter]
        public async Task<ActionResult> Manage(Guid? id)
        {
            var user = await this.userRepository.FindByIdAsync(id ?? this.authorizedUser.Id);
            if (user == null) return NotFound("User not found");

            return View(new
            {
                UserInfo = new ManageAccountDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    PersonName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    UserName = user.UserName,
                    Role = user.Roles.FirstOrDefault().Id.ToUserRole().ToUiString(),
                    IsOwnProfile = user.Id == this.authorizedUser.Id,
                    IsLockedByHeadquarters = user.IsLockedByHeadquaters,
                    IsLockedBySupervisor = user.IsLockedBySupervisor
                },
                Api = new
                {
                    UpdatePasswordUrl = Url.Action("UpdatePassword"),
                    UpdateUserUrl = Url.Action("UpdateUser")
                }
            });
        }

        [HttpGet]
        [Authorize]
        [AntiForgeryFilter]
        public ActionResult Create(string id)
        {
            if (!Enum.TryParse(id, true, out UserRoles role))
                return BadRequest("Unknown user type");

            return View(new
            {
                UserInfo = new {Role = id},
                Api = new {CreateUserUrl = Url.Action("CreateUser")}
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public async Task<ActionResult> CreateUser([FromBody] CreateUserModel model)
        {
            if (!this.ModelState.IsValid) return this.ModelState.ErrorsToJsonResult();

            if (model.SupervisorId.HasValue)
            {
                var supervisor = await this.userRepository.FindByIdAsync(model.SupervisorId.Value);
                if (supervisor == null || !supervisor.IsInRole(UserRoles.Supervisor) || supervisor.IsArchivedOrLocked)
                    this.ModelState.AddModelError(nameof(CreateUserModel.SupervisorId), HQ.SupervisorNotFound);
            }

            if (this.ModelState.IsValid)
            {
                var user = new HqUser
                {
                    Id = Guid.NewGuid(),
                    IsLockedBySupervisor = model.IsLockedBySupervisor,
                    IsLockedByHeadquaters = model.IsLockedByHeadquarters,
                    FullName = model.PersonName,
                    Email = model.Email,
                    UserName = model.UserName,
                    PhoneNumber = model.PhoneNumber,
                    Profile = model.SupervisorId.HasValue ? new HqUserProfile {SupervisorId = model.SupervisorId} : null
                };

                var identityResult = await this.userRepository.CreateAsync(user);
                if(!identityResult.Succeeded)
                    this.ModelState.AddModelError(nameof(CreateUserModel.UserName), string.Join(@", ", identityResult.Errors.Select(x => x.Description)));
                else
                {
                    identityResult = await this.userRepository.ChangePasswordAsync(user, model.Password);
                    if (!identityResult.Succeeded)
                        this.ModelState.AddModelError(nameof(CreateUserModel.Password), string.Join(@", ", identityResult.Errors.Select(x => x.Description)));
                    else
                        await this.userRepository.AddToRoleAsync(user, model.Role, CancellationToken.None);

                }
            }
            
            return this.ModelState.ErrorsToJsonResult();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public async Task<ActionResult> UpdatePassword([FromBody] ChangePasswordModel model)
        {
            if (!this.ModelState.IsValid) return this.ModelState.ErrorsToJsonResult();

            var currentUser = await this.userRepository.FindByIdAsync(model.UserId);
            if (currentUser == null) return NotFound("User not found");

            if (currentUser.IsArchived)
                return BadRequest(FieldsAndValidations.CannotUpdate_CurrentUserIsArchived);

            if (model.UserId == this.authorizedUser.Id)
            {
                bool isPasswordValid = !string.IsNullOrEmpty(model.OldPassword)
                                       && await this.userRepository.CheckPasswordAsync(currentUser,
                                           model.OldPassword);
                if (!isPasswordValid)
                    this.ModelState.AddModelError(nameof(ChangePasswordModel.OldPassword), FieldsAndValidations.OldPasswordErrorMessage);
            }

            if (this.ModelState.IsValid)
            {
                var updateResult = await this.userRepository.ChangePasswordAsync(currentUser, model.Password);

                if (!updateResult.Succeeded)
                    this.ModelState.AddModelError(nameof(ChangePasswordModel.Password), string.Join(@", ", updateResult.Errors.Select(x => x.Description)));
            }

            return this.ModelState.ErrorsToJsonResult();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ObserverNotAllowed]
        public async Task<ActionResult> UpdateUser([FromBody]EditUserModel editModel)
        {
            if (!this.ModelState.IsValid) return this.ModelState.ErrorsToJsonResult();

            var currentUser = await this.userRepository.FindByIdAsync(editModel.UserId);
            if(currentUser == null) return NotFound("User not found");

            currentUser.Email = editModel.Email;
            currentUser.FullName = editModel.PersonName;
            currentUser.PhoneNumber = editModel.PhoneNumber;

            if (this.authorizedUser.IsAdministrator || this.authorizedUser.IsHeadquarter)
                currentUser.IsLockedByHeadquaters = editModel.IsLockedByHeadquarters;
            if (this.authorizedUser.IsSupervisor)
                currentUser.IsLockedBySupervisor = editModel.IsLockedBySupervisor;

            var updateResult = await this.userRepository.UpdateAsync(currentUser);

            if (!updateResult.Succeeded)
                this.ModelState.AddModelError(nameof(EditUserModel.Email), string.Join(@", ", updateResult.Errors.Select(x => x.Description)));

            return this.ModelState.ErrorsToJsonResult();
        }

        public IActionResult LogOff()
        {
            this.signInManager.SignOutAsync();
            return this.Redirect("~/");
        }
    }
}