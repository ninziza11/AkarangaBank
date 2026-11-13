using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

[Authorize]
public class OperationsController : Controller
{
    private readonly IComptabiliteService _comptabilite;
    private readonly ICurrentUserService _currentUser;
    private readonly AkarangaBankContext _context;

    public OperationsController(
        IComptabiliteService comptabilite,
        ICurrentUserService currentUser,
        AkarangaBankContext context)
    {
        _comptabilite = comptabilite;
        _currentUser = currentUser;
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Depot()
    {
        await ChargerComptesAsync();

        return View(new DepotViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Depot(DepotViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await ChargerComptesAsync(vm.CompteId);
            return View(vm);
        }

        try
        {
            var userId = await _currentUser.GetUserIdAsync();

            var operation = await _comptabilite.DeposerAsync(vm, userId);

            TempData["Success"] =
                $"Dépôt enregistré (réf. {operation.Reference}).";

            return RedirectToAction(nameof(Journal));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await ChargerComptesAsync(vm.CompteId);

            return View(vm);
        }
    }

    [HttpGet]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Retrait()
    {
        await ChargerComptesAsync();

        return View(new RetraitViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Retrait(RetraitViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await ChargerComptesAsync(vm.CompteId);
            return View(vm);
        }

        try
        {
            var userId = await _currentUser.GetUserIdAsync();

            var operation = await _comptabilite.RetirerAsync(vm, userId);

            TempData["Success"] =
                $"Retrait enregistré (réf. {operation.Reference}).";

            return RedirectToAction(nameof(Journal));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await ChargerComptesAsync(vm.CompteId);

            return View(vm);
        }
    }

    [HttpGet]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Virement()
    {
        await ChargerComptesAsync();

        return View(new VirementViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Virement(VirementViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await ChargerComptesAsync(
                vm.CompteSourceId,
                vm.CompteDestinationId);

            return View(vm);
        }

        try
        {
            var userId = await _currentUser.GetUserIdAsync();

            var operation = await _comptabilite.VirerAsync(vm, userId);

            TempData["Success"] =
                $"Virement enregistré (réf. {operation.Reference}).";

            return RedirectToAction(nameof(Journal));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            await ChargerComptesAsync(
                vm.CompteSourceId,
                vm.CompteDestinationId);

            return View(vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Journal(
        DateTime? du,
        DateTime? au,
        string? typeOperation,
        int page = 1)
    {
        const int taille = 20;

        var vm = await _comptabilite.GetJournalAsync(
            du,
            au,
            typeOperation,
            page,
            taille);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Releve(int id)
    {
        var vm = await _comptabilite.GetReleveAsync(id);

        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ControleEquilibre()
    {
        var equilibre = await _comptabilite.VerifierEquilibreGlobalAsync();

        ViewBag.Equilibre = equilibre;

        return View();
    }

    private async Task ChargerComptesAsync(
        int? sourceId = null,
        int? destinationId = null)
    {
        var comptes = await _context.Comptes
            .Include(c => c.Client)
            .Include(c => c.Agence)
            .Where(c => c.Statut == "Actif")
            .OrderBy(c => c.NumeroCompte)
            .Select(c => new
            {
                c.Id,
                Libelle = $"{c.NumeroCompte} — {c.Client.Nom} {c.Client.Prenom} ({c.Solde:N2} {c.Devise})"
            })
            .ToListAsync();

        ViewBag.Comptes = new SelectList(
            comptes,
            "Id",
            "Libelle",
            sourceId);

        ViewBag.ComptesDestination = new SelectList(
            comptes,
            "Id",
            "Libelle",
            destinationId);
    }
}