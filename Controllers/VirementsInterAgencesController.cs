using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

// Tous les utilisateurs doivent être connectés.
// Les permissions précises sont définies sur chaque action.
[Authorize]
public class VirementsInterAgencesController : Controller
{
    private readonly IVirementInterAgenceService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly AkarangaBankContext _context;

    public VirementsInterAgencesController(
        IVirementInterAgenceService service,
        ICurrentUserService currentUser,
        AkarangaBankContext context)
    {
        _service = service;
        _currentUser = currentUser;
        _context = context;
    }

    // Liste accessible aux rôles prévus par RequireGuichetier :
    // guichetier, gestionnaire ou admin.
    [HttpGet]
    [Authorize(Policy = "RequireGuichetier")]
    public async Task<IActionResult> Index()
    {
        var agenceId = await _currentUser.GetAgenceIdAsync();

        var virements = await _service.GetEnAttenteAsync(agenceId);

        return View(virements);
    }

    // Création réservée au guichetier.
    [HttpGet]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Initier()
    {
        await ChargerComptesAsync();

        return View(new VirementInterAgenceViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "GuichetierOnly")]
    public async Task<IActionResult> Initier(
        VirementInterAgenceViewModel vm)
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

            var virement = await _service.InitierAsync(vm, userId);

            TempData["Success"] =
                $"Virement inter-agence initié " +
                $"(réf. {virement.Reference}).";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await ChargerComptesAsync(
                vm.CompteSourceId,
                vm.CompteDestinationId);

            return View(vm);
        }
    }

    // Validation réservée à l'administrateur.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Valider(int id)
    {
        try
        {
            var userId = await _currentUser.GetUserIdAsync();

            await _service.ValiderAsync(id, userId);

            TempData["Success"] = "Virement validé.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // Rejet réservé à l'administrateur.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Rejeter(int id, string motif)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            TempData["Error"] =
                "Le motif du rejet est obligatoire.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            var userId = await _currentUser.GetUserIdAsync();

            await _service.RejeterAsync(
                id,
                userId,
                motif.Trim());

            TempData["Success"] = "Virement rejeté.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // Historique accessible aux mêmes rôles que la liste.
    [HttpGet]
    [Authorize(Policy = "RequireGuichetier")]
    public async Task<IActionResult> Historique(
        DateTime? du,
        DateTime? au)
    {
        var agenceId = await _currentUser.GetAgenceIdAsync();

        var virements = await _service.GetHistoriqueAsync(
            agenceId,
            du,
            au);

        ViewBag.Du = du;
        ViewBag.Au = au;

        return View(virements);
    }

    private async Task ChargerComptesAsync(
    int? sourceId = null,
    int? destinationId = null)
{
    var nombreTotal = await _context.Comptes.CountAsync();

    var nombreActifs = await _context.Comptes
        .Where(c => c.Statut == "Actif")
        .CountAsync();

    var nombreEligibles = await _context.Comptes
        .Where(c => c.Statut == "Actif")
        .Where(c => !c.NumeroCompte.StartsWith("9000"))
        .CountAsync();

    var comptes = await _context.Comptes
        .Include(c => c.Client)
        .Include(c => c.Agence)
        .Where(c => c.Statut == "Actif")
        .Where(c => !c.NumeroCompte.StartsWith("9000"))
        .OrderBy(c => c.NumeroCompte)
        .Select(c => new
        {
            c.Id,
            Libelle =
                $"{c.NumeroCompte} — " +
                $"{c.Client.Nom} {c.Client.Prenom} " +
                $"({c.Agence.Code})"
        })
        .ToListAsync();

    Console.WriteLine(
        $"Diagnostic comptes : " +
        $"Total={nombreTotal}, " +
        $"Actifs={nombreActifs}, " +
        $"Eligibles={nombreEligibles}, " +
        $"Options={comptes.Count}");

    ViewBag.ComptesSource = new SelectList(
        comptes,
        "Id",
        "Libelle",
        sourceId);

    ViewBag.ComptesDestination = new SelectList(
        comptes,
        "Id",
        "Libelle",
        destinationId);
}}