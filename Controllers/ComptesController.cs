using System;
using System.Linq;
using System.Threading.Tasks;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AkarangaBank.Web.Controllers;

[Authorize(Roles = "client,guichetier,gestionnaire,auditeur,admin")]
public class ComptesController : Controller
{
    private readonly AkarangaBankContext _context;
    private readonly ILogger<ComptesController> _logger;

    public ComptesController(
        AkarangaBankContext context,
        ILogger<ComptesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /Comptes
    // Accès provisoirement limité à l'administrateur :
    // le filtrage par client et par agence reste à implémenter.
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Index()
    {
        var comptes = await _context.Comptes
            .Include(c => c.Client)
            .Include(c => c.Agence)
            .AsNoTracking()
            .OrderBy(c => c.NumeroCompte)
            .ToListAsync();

        return View(comptes);
    }

    // GET: /Comptes/Details/5
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Details(int id)
    {
        if (id <= 0)
        {
            return BadRequest(
                "L'identifiant du compte est invalide.");
        }

        var compte = await _context.Comptes
            .Include(c => c.Client)
            .Include(c => c.Agence)
            .Include(c => c.TypeCompte)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compte is null)
        {
            return NotFound("Compte introuvable.");
        }

        return View(compte);
    }

    // GET: /Comptes/Solde/5
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Solde(int id)
    {
        if (id <= 0)
        {
            return BadRequest(
                "L'identifiant du compte est invalide.");
        }

        var compte = await _context.Comptes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (compte is null)
        {
            return NotFound("Compte introuvable.");
        }

        return View("Solde", compte);
    }

    // GET: /Comptes/Create?clientId=5
    // Limitation temporaire à admin tant que le périmètre
    // d'agence des autres agents n'est pas contrôlé.
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create(int clientId)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (client is null)
        {
            return NotFound("Client introuvable.");
        }

        if (client.Statut != "Actif")
        {
            TempData["Error"] =
                $"Ouverture refusée : le statut du client est " +
                $"{client.Statut}.";

            return RedirectToAction(
                "Details",
                "Clients",
                new { id = clientId });
        }

        var vm = new CompteCreateViewModel
        {
            ClientId = client.Id,
            ClientNomComplet =
                $"{client.Nom} {client.Prenom}".Trim(),
            Devise = "BIF"
        };

        await ChargerListesAsync(vm);

        return View(vm);
    }

    // POST: /Comptes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create(
        CompteCreateViewModel vm)
    {
        // Ces informations sont définies côté serveur.
        ModelState.Remove(
            nameof(CompteCreateViewModel.ClientNomComplet));

        ModelState.Remove(
            nameof(CompteCreateViewModel.Devise));

        ModelState.Remove(
            nameof(CompteCreateViewModel.AgencesDisponibles));

        ModelState.Remove(
            nameof(CompteCreateViewModel.TypesCompteDisponibles));

        vm.Devise = "BIF";

        var client = await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == vm.ClientId);

        if (client is null)
        {
            ModelState.AddModelError(
                nameof(vm.ClientId),
                "Client introuvable.");
        }
        else
        {
            vm.ClientNomComplet =
                $"{client.Nom} {client.Prenom}".Trim();

            if (client.Statut != "Actif")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Le client doit être actif pour ouvrir un compte.");
            }

            if (vm.AgenceId != client.AgenceId)
            {
                ModelState.AddModelError(
                    nameof(vm.AgenceId),
                    "Le compte doit appartenir à l'agence du client.");
            }
        }

        var agence = await _context.Agences
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == vm.AgenceId);

        if (agence is null)
        {
            ModelState.AddModelError(
                nameof(vm.AgenceId),
                "Sélectionne une agence valide.");
        }

        var typeCompteExiste = await _context.TypesCompte
            .AsNoTracking()
            .AnyAsync(t => t.Id == vm.TypeCompteId);

        if (!typeCompteExiste)
        {
            ModelState.AddModelError(
                nameof(vm.TypeCompteId),
                "Sélectionne un type de compte valide.");
        }

        if (!ModelState.IsValid)
        {
            await ChargerListesAsync(vm);
            return View(vm);
        }

        // Format conservé proche de ton contrôleur initial,
        // mais le préfixe utilise l'agence sélectionnée.
        var numeroCompte = await GenererNumeroCompteAsync(
            agence!.Code,
            vm.ClientId);

        var dateCreation = DateTime.UtcNow;

        var nouveauCompte = new Compte
        {
            ClientId = vm.ClientId,
            AgenceId = vm.AgenceId,
            TypeCompteId = vm.TypeCompteId,
            NumeroCompte = numeroCompte,

            // Aucun crédit fictif à l'ouverture.
            // Le dépôt initial doit passer par le service transactionnel.
            Solde = 0m,

            Devise = "BIF",
            Statut = "Actif",
            DateCreation = dateCreation,
            DateOuverture = dateCreation
        };

        try
        {
            _context.Comptes.Add(nouveauCompte);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Échec de création d'un compte pour le client {ClientId}.",
                vm.ClientId);

            _context.Entry(nouveauCompte).State =
                EntityState.Detached;

            ModelState.AddModelError(
                string.Empty,
                "Le compte n'a pas pu être enregistré. " +
                "Consulte les journaux de l'application " +
                "avant de réessayer.");

            await ChargerListesAsync(vm);

            return View(vm);
        }

        TempData["Success"] =
            $"Le compte {numeroCompte} a été ouvert avec succès. " +
            "Son solde initial est de 0 BIF. " +
            "Tout dépôt doit être enregistré au guichet.";

        return RedirectToAction(
            "Details",
            "Clients",
            new { id = vm.ClientId });
    }

    private async Task ChargerListesAsync(
        CompteCreateViewModel vm)
    {
        var agences = await _context.Agences
            .AsNoTracking()
            .OrderBy(a => a.Nom)
            .ToListAsync();

        var typesCompte = await _context.TypesCompte
            .AsNoTracking()
            .OrderBy(t => t.Libelle)
            .ToListAsync();

        vm.AgencesDisponibles = agences
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.Nom}",
                Selected = a.Id == vm.AgenceId
            })
            .ToList();

        vm.TypesCompteDisponibles = typesCompte
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"{t.Code} - {t.Libelle}",
                Selected = t.Id == vm.TypeCompteId
            })
            .ToList();
    }

    private async Task<string> GenererNumeroCompteAsync(
        string codeAgence,
        int clientId)
    {
        // Cette vérification réduit les collisions, mais ne remplace
        // pas une contrainte UNIQUE en base sur NumeroCompte.
        for (var tentative = 0; tentative < 20; tentative++)
        {
            var suffixe = Random.Shared.Next(1000, 10000);

            var numero =
                $"{codeAgence}-{clientId:D4}-{suffixe}";

            var existe = await _context.Comptes
                .AsNoTracking()
                .AnyAsync(c => c.NumeroCompte == numero);

            if (!existe)
            {
                return numero;
            }
        }

        throw new InvalidOperationException(
            "Impossible de générer un numéro de compte disponible.");
    }
}