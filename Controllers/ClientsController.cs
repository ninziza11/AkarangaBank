using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace AkarangaBank.Web.Controllers;


[Authorize]
public class ClientsController : Controller
{
    
    private readonly IClientService _clientService;
    private readonly AkarangaBankContext _context;
    

    public ClientsController(IClientService clientService, AkarangaBankContext context)
    {
        _clientService = clientService;
        _context = context;
    }

    // GET: /Clients
    public async Task<IActionResult> Index(string? recherche, int? agenceId, string? statut, int page = 1, int taille = 10)
    {
        var vm = await _clientService.GetPagedAsync(recherche, agenceId, statut, page, taille);
        return View(vm);
    }

    // GET: /Clients/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _clientService.GetDetailsAsync(id);
        if (vm is null) return NotFound();

        return View(vm);
    }

    // GET: /Clients/Create
    public async Task<IActionResult> Create()
    {
        var agences = await _context.Agences.AsNoTracking().ToListAsync();
        
        var vm = new ClientCreateViewModel
        {
            AgencesDisponibles = agences.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.Nom}"
            }).ToList()
        };

        return View(vm);
    }

    // POST: /Clients/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientCreateViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.NumeroClient) && await _clientService.NumeroClientExisteAsync(vm.NumeroClient))
        {
            ModelState.AddModelError("NumeroClient", "Ce numéro de client est déjà attribué.");
        }

        if (!string.IsNullOrWhiteSpace(vm.NumeroCNI) && await _clientService.NumeroCNIExisteAsync(vm.NumeroCNI))
        {
            ModelState.AddModelError("NumeroCNI", "Ce numéro de CNI est déjà enregistré dans le système.");
        }

        if (!ModelState.IsValid)
        {
            var agences = await _context.Agences.AsNoTracking().ToListAsync();
            vm.AgencesDisponibles = agences.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.Nom}"
            }).ToList();

            return View(vm);
        }

        var client = await _clientService.CreateAsync(vm);
        TempData["Success"] = $"Le client {client.Nom} {client.Prenom} a été créé avec succès.";
        
        return RedirectToAction(nameof(Details), new { id = client.Id });
    }

    // GET: /Clients/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client is null) return NotFound();

        var agences = await _context.Agences.AsNoTracking().ToListAsync();

        var vm = new ClientEditViewModel
        {
            Id = client.Id,
            NumeroClient = client.NumeroClient,
            Nom = client.Nom,
            Prenom = client.Prenom,
            DateNaissance = client.DateNaissance,
            NumeroCNI = client.NumeroCNI,
            Telephone = client.Telephone,
            Email = client.Email,
            Adresse = client.Adresse,
            AgenceId = client.AgenceId,
            Statut = client.Statut,
            AgencesDisponibles = agences.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.Nom}"
            }).ToList()
        };

        return View(vm);
    }

    // POST: /Clients/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClientEditViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(vm.NumeroClient) && await _clientService.NumeroClientExisteAsync(vm.NumeroClient, vm.Id))
        {
            ModelState.AddModelError("NumeroClient", "Ce numéro de client est déjà utilisé par un autre dossier.");
        }

        if (!string.IsNullOrWhiteSpace(vm.NumeroCNI) && await _clientService.NumeroCNIExisteAsync(vm.NumeroCNI, vm.Id))
        {
            ModelState.AddModelError("NumeroCNI", "Ce numéro de CNI est déjà rattaché à un autre client.");
        }

        if (!ModelState.IsValid)
        {
            var agences = await _context.Agences.AsNoTracking().ToListAsync();
            vm.AgencesDisponibles = agences.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.Nom}"
            }).ToList();

            return View(vm);
        }

        var ok = await _clientService.UpdateAsync(vm);
        if (!ok) return NotFound();

        TempData["Success"] = "Les informations du client ont été mises à jour.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    // GET: /Clients/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client is null) return NotFound();

        return View(client);
    }

    // POST: /Clients/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var ok = await _clientService.DeleteAsync(id);
            if (!ok) return NotFound();

            TempData["Success"] = "Le dossier client a été définitivement supprimé.";
        }
        catch (System.InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = id });
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Clients/Suspendre/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspendre(int id)
    {
        var ok = await _clientService.SuspendreAsync(id);
        if (!ok) return NotFound();

        TempData["Success"] = "Le statut du client a été passé à Suspendu. Ses accès applicatifs sont restreints.";
        return RedirectToAction(nameof(Details), new { id = id });
    }

    // GET: /Clients/ExporterCSV
    public async Task<IActionResult> ExporterCSV()
    {
        var clients = await _context.Clients
            .AsNoTracking()
            .Include(c => c.Agence)
            .OrderBy(c => c.Nom)
            .ToListAsync();

        var csvContent = new System.Text.StringBuilder();
        csvContent.AppendLine("NumeroClient;Nom;Prenom;DateNaissance;CNI;Telephone;Email;Agence;Statut");

        foreach (var client in clients)
        {
            csvContent.AppendLine($"{client.NumeroClient};" +
                                  $"{client.Nom.ToUpper()};" +
                                  $"{client.Prenom};" +
                                  $"{client.DateNaissance:yyyy-MM-dd};" +
                                  $"{client.NumeroCNI};" +
                                  $"{client.Telephone ?? ""};" +
                                  $"{client.Email ?? ""};" +
                                  $"{client.Agence?.Code ?? ""};" +
                                  $"{client.Statut}");
        }

        var fileBytes = System.Text.Encoding.UTF8.GetBytes(csvContent.ToString());
        string fileName = $"AkarangaBank_Clients_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(fileBytes, "text/csv", fileName);
    }

    // =========================================================================
    // MODULÉ DE CERTIFICATION & MISE A JOUR DES PIECES KYC (AJOUTÉ ICI)
    // =========================================================================

    // GET: /Clients/Kyc/5
    [HttpGet]
    public async Task<IActionResult> Kyc(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client is null) return NotFound();

        var vm = new ClientKycUpdateViewModel
        {
            ClientId = client.Id,
            NumeroClient = client.NumeroClient ?? "N/A",
            NomComplet = $"{client.Nom.ToUpper()} {client.Prenom}",
            NumeroCNI = client.NumeroCNI ?? string.Empty,
            StatutKyc = client.Statut ?? "Actif"
        };

        return View(vm);
    }

    // POST: /Clients/Kyc/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Kyc(ClientKycUpdateViewModel vm)
    {
        if (vm.DateExpirationPiece <= DateTime.Today)
        {
            ModelState.AddModelError("DateExpirationPiece", "Opération refusée : La pièce d'identité fournie est légalement expirée.");
        }

        if (!ModelState.IsValid) 
        {
            return View(vm);
        }

        var client = await _clientService.GetByIdAsync(vm.ClientId);
        if (client is null) return NotFound();

        var clientEditVm = new ClientEditViewModel
        {
            Id = client.Id,
            NumeroClient = client.NumeroClient,
            Nom = client.Nom,
            Prenom = client.Prenom,
            DateNaissance = client.DateNaissance,
            NumeroCNI = vm.NumeroCNI,
            Telephone = client.Telephone,
            Email = client.Email,
            Adresse = client.Adresse,
            AgenceId = client.AgenceId,
            Statut = vm.StatutKyc
        };

        var ok = await _clientService.UpdateAsync(clientEditVm);
        if (!ok) 
        {
            ModelState.AddModelError(string.Empty, "Erreur lors de la mise à jour des données de conformité.");
            return View(vm);
        }

        TempData["Success"] = $"Le dossier de conformité KYC du client {client.Nom.ToUpper()} a été certifié avec succès.";
        return RedirectToAction("Details", new { id = vm.ClientId });
    }
}
