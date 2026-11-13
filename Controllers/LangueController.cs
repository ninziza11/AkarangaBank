using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AkarangaBank.Web.Controllers
{
    public class LangueController : Controller
    {
        // POST: /Langue/ChangerLangue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangerLangue(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                "Akaranga_Culture",
                culture,
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true }
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // POST: /Langue/BasculerContraste
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BasculerContraste(bool actif, string returnUrl)
        {
            Response.Cookies.Append(
                "Akaranga_ContrasteEleve",
                actif ? "true" : "false",
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true }
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
