﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AccountController.cs" company="">
//   
// </copyright>
// <summary>
//   The account controller.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Main.Core.View;
using Ncqrs.Commanding.ServiceModel;
using WB.UI.Designer.Code;

namespace WB.UI.Designer.Controllers
{
    using System;
    using System.Web.Mvc;
    using System.Web.Security;
    using System.Web.UI;

    using Postal;

    using Recaptcha;

    using WB.UI.Designer.Extensions;
    using WB.UI.Designer.Models;

    using WebMatrix.WebData;

    /// <summary>
    ///     The account controller.
    /// </summary>
    [CustomAuthorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
    public class AccountController : BaseController
    {
        #region Public Methods and Operators

        public AccountController(IViewRepository repository, ICommandService commandService, IUserHelper userHelper) : base(repository, commandService, userHelper)
        {
        }

        /// <summary>
        ///     The confirmation failure.
        /// </summary>
        /// <returns>
        ///     The <see cref="ActionResult" />.
        /// </returns>
        [AllowAnonymous]
        public ActionResult ConfirmationFailure()
        {
            return this.View();
        }

        /// <summary>
        ///     The index.
        /// </summary>
        /// <returns>
        ///     The <see cref="ActionResult" />.
        /// </returns>
        public ActionResult Index()
        {
            return this.RedirectToAction("Index", "Questionnaire");
        }

        /// <summary>
        ///     The log off.
        /// </summary>
        /// <returns>
        ///     The <see cref="ActionResult" />.
        /// </returns>
        public ActionResult LogOff()
        {
            UserHelper.Logout();

            return this.RedirectToAction("login", "account");
        }

        /// <summary>
        /// The login.
        /// </summary>
        /// <param name="returnUrl">
        /// The return url.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
        public ActionResult Login(string returnUrl)
        {
            this.ViewBag.ReturnUrl = returnUrl;
            return this.View(new LoginModel());
        }

        /// <summary>
        /// The login.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="returnUrl">
        /// The return url.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (this.ModelState.IsValid
                && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
            {
                return this.RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            this.Error("The user name or password provided is incorrect.");
            return View(model);
        }

        /// <summary>
        /// The manage.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
        public ActionResult Manage(AccountManageMessageId? message)
        {
            if (message.HasValue)
            {
                this.Success(message.Value.ToUIMessage());
            }

            this.ViewBag.ReturnUrl = this.Url.Action("manage");
            return this.View(new LocalPasswordModel());
        }

        /// <summary>
        /// The manage.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
        public ActionResult Manage(LocalPasswordModel model)
        {
            this.ViewBag.ReturnUrl = this.Url.Action("manage");
            if (this.ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                bool changePasswordSucceeded;
                try
                {
                    changePasswordSucceeded = UserHelper.CurrentUser.ChangePassword(
                        model.OldPassword, model.Password);
                }
                catch (Exception)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    // Success();
                    return this.RedirectToAction(
                        "manage", new { message = AccountManageMessageId.ChangePasswordSuccess });
                }
                else
                {
                    this.Error("The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        /// <summary>
        ///     The password reset.
        /// </summary>
        /// <returns>
        ///     The <see cref="ActionResult" />.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// </exception>
        [AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
        public ActionResult PasswordReset()
        {
            if (!Membership.EnablePasswordReset)
            {
                throw new Exception("Password reset is not allowed");
            }

            return this.View(new ResetPasswordModel());
        }

        /// <summary>
        /// The password reset.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
        public ActionResult PasswordReset(ResetPasswordModel model)
        {
            if (!Membership.EnablePasswordReset)
            {
                throw new Exception("Password reset is not allowed");
            }

            if (this.ModelState.IsValid)
            {
                MembershipUser user = Membership.GetUser(model.UserName, false);
                if (user == null)
                {
                    this.Error(string.Format("User {0} does not exist. Please enter a valid user name", model.UserName));
                }
                else
                {
                    string confirmationToken = WebSecurity.GeneratePasswordResetToken(user.UserName);

                    this.SendEmail("ResetPasswordEmail",
                                recepientEmail: user.Email, userName: model.UserName, token: confirmationToken);

                    this.Attention("To complete the reset password process look for an email in your inbox that provides further instructions.");
                    return this.RedirectToAction("Login");
                }
            }

            return this.View(model);
        }

        /// <summary>
        ///     The register.
        /// </summary>
        /// <returns>
        ///     The <see cref="ActionResult" />.
        /// </returns>
        [AllowAnonymous]
        public ActionResult Register()
        {
            return this.View(new RegisterModel());
        }

        // POST: /Account/Register

        /// <summary>
        /// The register.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <param name="captchaValid">
        /// The captcha valid.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [RecaptchaControlMvc.CaptchaValidatorAttribute]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
        public ActionResult Register(RegisterModel model, bool captchaValid)
        {
            if (AppSettings.Instance.IsReCaptchaEnabled && !captchaValid)
            {
                this.Error("You did not type the verification word correctly. Please try again.");
            }
            else
            {
                if (this.ModelState.IsValid)
                {
                    // Attempt to register the user
                    try
                    {
                        string confirmationToken = WebSecurity.CreateUserAndAccount(
                            model.UserName, model.Password, new { model.Email }, true);

                        if (!string.IsNullOrEmpty(confirmationToken))
                        {
                            Roles.Provider.AddUsersToRoles(new[] { model.UserName }, new[] { UserHelper.USERROLENAME });

                            this.SendEmail("ConfirmationEmail",
                                 model.Email, userName: model.UserName, token: confirmationToken);

                            return this.RegisterStepTwo();
                        }
                    }
                    catch (MembershipCreateUserException e)
                    {
                        this.Error(e.StatusCode.ToErrorCode());
                    }
                    catch (Exception e)
                    {
                        this.Error(e.Message);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        /// <summary>
        /// The register confirmation.
        /// </summary>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [AllowAnonymous]
        public ActionResult RegisterConfirmation(string token)
        {
            if (WebSecurity.ConfirmAccount(token))
            {
                this.Success("You have completed the registration process. You can now logon to the system");
                return this.RedirectToAction("Login");
            }

            return this.RedirectToAction("ConfirmationFailure");
        }

        /// <summary>
        /// The resend confirmation.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [AllowAnonymous]
        public ActionResult ResendConfirmation(string id)
        {
            MembershipUser model = Membership.GetUser(id, false);
            if (model != null)
            {
                if (!model.IsApproved)
                {
                    string token =
                        ((DesignerMembershipProvider)Membership.Provider).GetConfirmationTokenByUserName(model.UserName);
                    if (!string.IsNullOrEmpty(token))
                    {
                        this.SendEmail( "ConfirmationEmail", model.Email, userName: model.UserName, token: token);
                        return this.RegisterStepTwo();
                    }
                    else
                    {
                        this.Error("Unexpected problem. Contact with administrator to solve this problem.");
                    }
                }
                else
                {
                    this.Error(string.Format("User {0} already confirmed in system.", model.UserName));
                }
            }
            else
            {
                this.Error(string.Format("User {0} does not exist. Please enter a valid user name.", id));
            }

            return this.RedirectToAction("Login");
        }

        /// <summary>
        /// The reset password confirmation.
        /// </summary>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation(string token)
        {
            return this.View(new ResetPasswordConfirmationModel { Token = token });
        }

        /// <summary>
        /// The reset password confirmation.
        /// </summary>
        /// <param name="model">
        /// The model.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None", Location = OutputCacheLocation.None)]
        public ActionResult ResetPasswordConfirmation(ResetPasswordConfirmationModel model)
        {
            if (this.ModelState.IsValid)
            {
                if (WebSecurity.ResetPassword(model.Token, model.Password))
                {
                    this.Success("Your password successfully changed. Now you can login with your new password");
                    return this.RedirectToAction("Login");
                }
                else
                {
                    return this.RedirectToAction("ResetPasswordConfirmationFailure");
                }
            }

            return this.View(model);
        }

        /// <summary>
        ///     The reset password confirmation failure.
        /// </summary>
        /// <returns>
        ///     The <see cref="ActionResult" />.
        /// </returns>
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmationFailure()
        {
            return this.View();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The redirect to local.
        /// </summary>
        /// <param name="returnUrl">
        /// The return url.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (this.Url.IsLocalUrl(returnUrl))
            {
                return this.Redirect(returnUrl);
            }
            else
            {
                return this.RedirectToAction("Index");
            }
        }

        /// <summary>
        ///     The register step two.
        /// </summary>
        /// <returns>
        ///     The <see cref="ActionResult" />.
        /// </returns>
        private ActionResult RegisterStepTwo()
        {
            this.Attention(
                "To complete the registration process look for an email in your inbox that provides further instructions.");
            return this.RedirectToAction("Login");
        }

        /// <summary>
        /// The send confirmation email.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="recepientEmail">
        /// The to.
        /// </param>
        /// <param name="userName">
        /// The user name.
        /// </param>
        /// <param name="token">
        /// The token.
        /// </param>
        private async void SendEmail(string title, string recepientEmail, string userName, string token)
        {
            dynamic email = new Email(title);

            if (AppSettings.Instance.ReformatEmail)
                recepientEmail = String.Format("<{0}>", recepientEmail);

            email.To = recepientEmail;
            email.UserName = userName;
            email.ConfirmationToken = token;

            await email.SendAsync();
        }

        #endregion
    }
}