﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WB.Core.BoundedContexts.Designer.MembershipProvider;
using WB.UI.Designer.Resources;
using WB.UI.Shared.Web.Extensions;

namespace WB.UI.Designer.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<DesignerIdentityUser> _userManager;

        public ConfirmEmailModel(UserManager<DesignerIdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
            {
                var error = result.Errors.First();
                TempData[Alerts.ERROR] = error.Description;
                return Page();
            }

            TempData[Alerts.SUCCESS] = ErrorMessages.Your_email_is_verified;

            return RedirectToPage("Login");
        }
    }
}
