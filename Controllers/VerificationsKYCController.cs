using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.Services;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

public class VerificationsKYCController : Controller 
{
    private readonly IVerificationKYCService _kycService;
    private readonly IClientService _clientService;

    public VerificationsKYCController(
        IVerificationKYCService kycService,
        IClientService clientService) 
    {
        _kycService = kycService;
        _clientService = clientService;
    }

    // GET: /VerificationsKYC/Create?clientId=5
    public async Task<IActionResult> Create(int clientId) 
    {
        var client = await _clientService.GetByIdAsync(clientId);
        if (client is null) return NotFound();
        
        return View(new VerificationKYCCreateViewModel {
            ClientId = client.Id,
            ClientNom = $"{client.Nom} {client.Prenom}",
            ClientNumero = client.NumeroClient
        });
    }

    // POST: /VerificationsKYC/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VerificationKYCCreateViewModel vm) 
    {
        if (!ModelState.IsValid) return View(vm);

        // CORRECTION : Le commentaire textuel au milieu du code provoquait une erreur de syntaxe
        // L'utilisateur vérificateur est fictif pour l'instant (id=2 : gestionnaire).
        // L'identité réelle sera branchée au chapitre 7.
        const int verifieParId = 2;
        
        await _kycService.CreateAsync(vm, verifieParId);
        
        TempData["Success"] = "Vérification KYC créée. En attente de validation.";
        return RedirectToAction("Details", "Clients", new { id = vm.ClientId });
    }

    // POST: /VerificationsKYC/Valider/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(int id) 
    {
        // CORRECTION : La constante n'était pas initialisée et la ligne s'arrêtait brusquement
        const int verifieParId = 2; 
        
        var ok = await _kycService.ValiderAsync(id, verifieParId);
        if (!ok) return NotFound();
        
        TempData["Success"] = "Vérification KYC validée.";
        return RedirectToAction("Index", "Clients");
    }

    // POST: /VerificationsKYC/Rejeter/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rejeter(int id, string commentaire)
    {
        const int verifieParId = 2;
        
        var ok = await _kycService.RejeterAsync(id, verifieParId, commentaire ?? "");
        if (!ok) return NotFound();
        
        TempData["Success"] = "Vérification KYC rejetée.";
        return RedirectToAction("Index", "Clients");
    }
}
