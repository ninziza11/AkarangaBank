using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.ViewModels;
using AkarangaBank.Web.Services;

namespace AkarangaBank.Web.Controllers
{
    // RBAC : Limite l'accès aux guichetiers et aux gestionnaires d'agences
    [Authorize(Roles = "guichetier,gestionnaire")]
    public class GuichetController : Controller
    {
        private readonly TransactionService _transactionService;

        public GuichetController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        // GET: /Guichet/Operation
        [HttpGet]
        public IActionResult Operation(string? numCompte)
        {
            var vm = new OperationGuichetViewModel();
            if (!string.IsNullOrEmpty(numCompte))
            {
                vm.NumeroCompte = numCompte;
            }
            return View(vm);
        }

        // POST: /Guichet/Operation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Operation(OperationGuichetViewModel vm)
        {
            if (!ModelState.IsValid) 
            {
                return View(vm);
            }

            // Exécution de l'opération via le service de transaction en partie double
            var resultat = await _transactionService.ExecuterOperationGuichetAsync(
                vm.NumeroCompte, 
                vm.TypeOperation, 
                vm.Montant, 
                vm.NomDonneur, 
                vm.Remarque
            );

            // Gestion de l'échec (Alerte provision ou blocage LAB/CFT)
            if (resultat.Succeeded)
            {
                ModelState.AddModelError(string.Empty, resultat.Message);
                return View(vm);
            }

            TempData["Success"] = resultat.Message;
            return RedirectToAction("Index", "Comptes");
        }
    }
}
