using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AkarangaBank.Web.Services;

namespace AkarangaBank.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class CNRFController : Controller
{
    private readonly ICNRFService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CNRFController> _logger;

    public CNRFController(
        ICNRFService service,
        ICurrentUserService currentUser,
        ILogger<CNRFController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    // GET: /CNRF
    [HttpGet]
    public async Task<IActionResult> Index(string? statut)
    {
        var declarations = await _service.GetDeclarationsAsync(statut);

        ViewBag.Statut = statut;

        return View(declarations);
    }

    // POST: /CNRF/Generer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generer(int alerteId)
    {
        if (alerteId <= 0)
        {
            TempData["Error"] =
                "Veuillez sélectionner une alerte valide.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            var userId = await _currentUser.GetUserIdAsync();

            var declaration = await _service.GenererDOSAsync(
                alerteId,
                userId);

            TempData["Success"] =
                $"DOS générée : {declaration.Reference}. " +
                "Statut initial : Brouillon.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Génération impossible pour l’alerte {AlerteId}.",
                alerteId);

            TempData["Error"] = ex.Message;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Échec d’enregistrement de la DOS " +
                "pour l’alerte {AlerteId}.",
                alerteId);

            TempData["Error"] =
                "La DOS n’a pas pu être enregistrée. " +
                "Consultez les journaux de l’application.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /CNRF/Transmettre
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transmettre(int id)
    {
        if (id <= 0)
        {
            TempData["Error"] =
                "L’identifiant de la déclaration est invalide.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            var resultat = await _service.TransmettreAsync(id);

            if (!resultat)
            {
                TempData["Error"] =
                    "La transmission de test n’a pas été effectuée : " +
                    "la déclaration est introuvable ou son statut " +
                    "n’autorise pas cette action.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Transmission de test enregistrée : " +
                "le statut est passé à Transmise. " +
                "Aucun envoi externe à la CNRF n’a été effectué.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Échec de mise à jour de la déclaration {DeclarationId}.",
                id);

            TempData["Error"] =
                "Le changement de statut n’a pas pu être enregistré. " +
                "Consultez les journaux de l’application.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /CNRF/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var declarations = await _service.GetDeclarationsAsync(null);

        var declaration = declarations.FirstOrDefault(d => d.Id == id);

        if (declaration is null)
        {
            return NotFound();
        }

        return View(declaration);
    }
}