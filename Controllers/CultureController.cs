using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace AkarangaBank.Web.Controllers;

public class CultureController : Controller
{
    private static readonly string[] CulturesAutorisees = { "fr", "en", "rn", "sw" };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string? returnUrl)
    {
        // On refuse toute langue non prévue
        if (!CulturesAutorisees.Contains(culture))
        {
            culture = "fr";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        // Sécurité : on ne redirige que vers une adresse locale
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {

            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}