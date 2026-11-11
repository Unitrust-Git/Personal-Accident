using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GPADashboard.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var displayName = User.FindFirst("name")?.Value ?? "";
                var email = User.FindFirst("preferred_username")?.Value ?? "";

                HttpContext.Session.SetString("UserName", displayName);
                HttpContext.Session.SetString("Email", email);

                return RedirectToAction("Index", "IndividualPolicies");
            }

            return View();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            var redirectUrl = Url.Action("Index", "IndividualPolicies");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }

        [Authorize]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return SignOut(
                new AuthenticationProperties { RedirectUri = Url.Action("Index", "Account") },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }
    }
}
