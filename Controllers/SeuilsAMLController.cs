using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AkarangaBank.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkarangaBank.Web.Controllers;

[Authorize(Roles = "Administrateur")]
public class SeuilsAMLController : Controller
{
    private readonly ISeuilAMLService _seuilService;

    public SeuilsAMLController(ISeuilAMLService seuilService)
    {
        _seuilService = seuilService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var seuils = await _seuilService.GetAllAsync();
        return View(seuils);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var seuils = await _seuilService.GetAllAsync();
        var seuil = seuils.FirstOrDefault(s => s.Id == id);

        if (seuil is null)
        {
            return NotFound("Seuil AML introuvable.");
        }

        return View(seuil);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, decimal? montant)
    {
        // Les informations du seuil sont relues depuis la base.
        // Seul le montant envoyé par le formulaire est modifiable.
        var seuils = await _seuilService.GetAllAsync();
        var seuil = seuils.FirstOrDefault(s => s.Id == id);

        if (seuil is null)
        {
            return NotFound("Seuil AML introuvable.");
        }

        if (!montant.HasValue)
        {
            ModelState.AddModelError(
                nameof(montant),
                "Saisis un montant valide.");
        }
        else if (montant.Value <= 0)
        {
            ModelState.AddModelError(
                nameof(montant),
                "Le montant doit être strictement positif.");
        }

        if (!ModelState.IsValid)
        {
            return View(seuil);
        }

        var identifiant = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(identifiant, out var utilisateurId)
            || utilisateurId <= 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Modification non enregistrée : l'identifiant entier " +
                "de l'utilisateur connecté est absent ou invalide. " +
                "Le mécanisme de connexion doit être vérifié.");

            return View(seuil);
        }

        if (seuil.Montant == montant!.Value)
        {
            ModelState.AddModelError(
                nameof(montant),
                "Ce montant est déjà enregistré. " +
                "Aucune modification n'a été effectuée.");

            return View(seuil);
        }

        var modificationEffectuee =
            await _seuilService.MettreAJourAsync(
                id,
                montant.Value,
                utilisateurId);

        if (!modificationEffectuee)
        {
            return NotFound("Le seuil AML n'existe plus.");
        }

        TempData["Succes"] =
            "Le seuil a été modifié et la trace d'audit enregistrée.";

        return RedirectToAction(nameof(Index));
    }
}