using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.Services;

namespace AkarangaBank.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUtilisateurService _utilisateurService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUtilisateurService utilisateurService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _utilisateurService = utilisateurService;
        _logger = logger;
    }

    // Affichage de la page de connexion.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        return View();
    }

    // Traitement du formulaire de connexion.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string identifiant,
        string motDePasse,
        string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (string.IsNullOrWhiteSpace(identifiant) ||
            string.IsNullOrWhiteSpace(motDePasse))
        {
            _logger.LogInformation(
                "Diagnostic Login : identifiant ou mot de passe absent.");

            ModelState.AddModelError(
                string.Empty,
                "L'identifiant et le mot de passe sont obligatoires.");

            return View();
        }

        var utilisateur = await TrouverUtilisateurAsync(identifiant);

        if (utilisateur is null)
        {
            _logger.LogInformation(
                "Diagnostic Login : aucun utilisateur trouvé.");

            ModelState.AddModelError(
                string.Empty,
                "Identifiant ou mot de passe incorrect.");

            return View();
        }

        if (!utilisateur.EstActif)
        {
            _logger.LogInformation(
                "Diagnostic Login : compte désactivé.");

            ModelState.AddModelError(
                string.Empty,
                "Votre compte est désactivé. Contactez l'administrateur.");

            return View();
        }

        var resultat = await _signInManager.PasswordSignInAsync(
            utilisateur,
            motDePasse,
            isPersistent: false,
            lockoutOnFailure: true);

        // Diagnostic temporaire : ne pas journaliser le mot de passe.
        _logger.LogInformation(
            "Diagnostic Login : Succeeded={Succeeded}, " +
            "IsLockedOut={IsLockedOut}, " +
            "IsNotAllowed={IsNotAllowed}, " +
            "RequiresTwoFactor={RequiresTwoFactor}",
            resultat.Succeeded,
            resultat.IsLockedOut,
            resultat.IsNotAllowed,
            resultat.RequiresTwoFactor);

        if (resultat.Succeeded)
        {
            utilisateur.DerniereConnexion = DateTime.UtcNow;

            var miseAJour = await _userManager.UpdateAsync(utilisateur);

            if (!miseAJour.Succeeded)
            {
                foreach (var erreur in miseAJour.Errors)
                {
                    _logger.LogWarning(
                        "Échec de mise à jour de DerniereConnexion : " +
                        "{Code} - {Description}",
                        erreur.Code,
                        erreur.Description);
                }
            }

            _logger.LogInformation(
                "Connexion réussie pour {Utilisateur}.",
                utilisateur.UserName);

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (resultat.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Votre compte est temporairement bloqué après " +
                "plusieurs tentatives échouées.");

            return View();
        }

        if (resultat.RequiresTwoFactor)
        {
            return RedirectToAction(
                nameof(LoginWithTwoFactor),
                new
                {
                    returnUrl,
                    souvenirMois = false
                });
        }

        if (resultat.IsNotAllowed)
        {
            ModelState.AddModelError(
                string.Empty,
                "La connexion n'est pas autorisée pour ce compte. " +
                "Contactez l'administrateur.");

            return View();
        }

        ModelState.AddModelError(
            string.Empty,
            "Identifiant ou mot de passe incorrect.");

        return View();
    }

    // Affichage de la page de vérification en deux étapes.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithTwoFactor(
        bool souvenirMois,
        string? returnUrl = null)
    {
        var utilisateur = await _signInManager
            .GetTwoFactorAuthenticationUserAsync();

        if (utilisateur is null)
        {
            return RedirectToAction(
                nameof(Login),
                new { returnUrl });
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["SouvenirMois"] = souvenirMois;

        return View();
    }

    // Traitement du code de vérification en deux étapes.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithTwoFactor(
        string codeVerification,
        bool souvenirMois,
        string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["SouvenirMois"] = souvenirMois;

        var utilisateur = await _signInManager
            .GetTwoFactorAuthenticationUserAsync();

        if (utilisateur is null)
        {
            return RedirectToAction(
                nameof(Login),
                new { returnUrl });
        }

        if (string.IsNullOrWhiteSpace(codeVerification))
        {
            ModelState.AddModelError(
                string.Empty,
                "Le code de vérification est obligatoire.");

            return View();
        }

        var codeNormalise = codeVerification
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);

        var resultat = await _signInManager.TwoFactorSignInAsync(
            TokenOptions.DefaultAuthenticatorProvider,
            codeNormalise,
            isPersistent: souvenirMois,
            rememberClient: false);

        _logger.LogInformation(
            "Diagnostic 2FA : Succeeded={Succeeded}, " +
            "IsLockedOut={IsLockedOut}, " +
            "IsNotAllowed={IsNotAllowed}",
            resultat.Succeeded,
            resultat.IsLockedOut,
            resultat.IsNotAllowed);

        if (resultat.Succeeded)
        {
            utilisateur.DerniereConnexion = DateTime.UtcNow;

            var miseAJour = await _userManager.UpdateAsync(utilisateur);

            if (!miseAJour.Succeeded)
            {
                foreach (var erreur in miseAJour.Errors)
                {
                    _logger.LogWarning(
                        "Échec de mise à jour de DerniereConnexion : " +
                        "{Code} - {Description}",
                        erreur.Code,
                        erreur.Description);
                }
            }

            _logger.LogInformation(
                "Connexion avec vérification en deux étapes réussie " +
                "pour {Utilisateur}.",
                utilisateur.UserName);

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (resultat.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Votre compte est temporairement bloqué.");

            return View();
        }

        if (resultat.IsNotAllowed)
        {
            ModelState.AddModelError(
                string.Empty,
                "La connexion n'est pas autorisée pour ce compte. " +
                "Contactez l'administrateur.");

            return View();
        }

        ModelState.AddModelError(
            string.Empty,
            "Code de vérification incorrect.");

        return View();
    }

    // Déconnexion.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }

    // Affichage du profil de l'utilisateur connecté.
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profil()
    {
        var utilisateur = await _userManager.GetUserAsync(User);

        if (utilisateur is null)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(utilisateur);
    }

    // Page d'accès refusé.
    [HttpGet]
    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Recherche par nom d'utilisateur, puis par e-mail.
    private async Task<ApplicationUser?> TrouverUtilisateurAsync(
        string identifiant)
    {
        var valeur = identifiant.Trim();

        var utilisateur = await _userManager.FindByNameAsync(valeur);

        if (utilisateur is not null)
        {
            return utilisateur;
        }

        return await _userManager.FindByEmailAsync(valeur);
    }
}