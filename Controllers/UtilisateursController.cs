using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

[Authorize(Roles = "admin")]
public class UtilisateursController : Controller
{
    private static readonly string[] Roles =
        { "admin", "gestionnaire", "guichetier", "auditeur", "client" };

    private readonly IUtilisateurService _service;
    private readonly AkarangaBankContext _context;

    public UtilisateursController(IUtilisateurService service, AkarangaBankContext context)
    {
        _service = service;
        _context = context;
    }

    // GET: /Utilisateurs
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _service.GetAllAsync());
    }

    // GET: /Utilisateurs/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await ChargerListesAsync();
        return View(new UtilisateurCreateViewModel());
    }

    // POST: /Utilisateurs/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UtilisateurCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await ChargerListesAsync();
            return View(vm);
        }

        var (succes, erreur) = await _service.CreateAsync(vm);
        if (!succes)
        {
            ModelState.AddModelError(string.Empty, erreur ?? "Erreur inconnue.");
            await ChargerListesAsync();
            return View(vm);
        }

        TempData["Success"] = $"L'utilisateur {vm.NomComplet} a été créé avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Utilisateurs/ToggleActif/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActif(int id)
    {
        // Un administrateur ne peut pas suspendre son propre compte
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
        {
            TempData["Error"] = "Vous ne pouvez pas suspendre votre propre compte.";
            return RedirectToAction(nameof(Index));
        }

        var (succes, erreur) = await _service.ToggleActifAsync(id);
        if (succes) TempData["Success"] = "Le statut de l'utilisateur a été modifié.";
        else TempData["Error"] = erreur;

        return RedirectToAction(nameof(Index));
    }

    private async Task ChargerListesAsync()
    {
        var agences = await _context.Agences.AsNoTracking().OrderBy(a => a.Nom).ToListAsync();
        ViewBag.Agences = agences
            .Select(a => new SelectListItem($"{a.Code} - {a.Nom}", a.Id.ToString()))
            .ToList();
        ViewBag.Roles = Roles.Select(r => new SelectListItem(r, r)).ToList();
    }
}