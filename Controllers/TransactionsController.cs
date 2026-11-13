using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;
using AkarangaBank.Web.Services;

namespace AkarangaBank.Web.Controllers
{
    // RBAC : L'accès au module des opérations de comptoir est réservé aux agents de guichet, gestionnaires et administrateurs [1f2]
    [Authorize(Roles = "guichetier,gestionnaire,admin")]
    public class TransactionsController : Controller
    {
        private readonly AkarangaBankContext _context;
        private readonly TransactionService _transactionService;

        public TransactionsController(AkarangaBankContext context, TransactionService transactionService)
        {
            _context = context;
            _transactionService = transactionService;
        }

        // GET: /Transactions/Guichet?numeroCompte=AKR-NGO-0006-2227
        [HttpGet]
        public async Task<IActionResult> Guichet(string numeroCompte)
        {
            var compte = await _context.Comptes
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.NumeroCompte == numeroCompte);
                
            if (compte == null) return NotFound("Compte d'agence introuvable.");

            var vm = new TransactionDepotViewModel
            {
                NumeroCompte = compte.NumeroCompte,
                ClientNomComplet = $"{compte.Client?.Nom.ToUpper()} {compte.Client?.Prenom}",
                SoldeActuel = compte.Solde
            };

            return View(vm);
        }

        // POST: /Transactions/Depot
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Depot(TransactionDepotViewModel vm)
        {
            if (!ModelState.IsValid) return View("Guichet", vm);

            var compte = await _context.Comptes.FirstOrDefaultAsync(c => c.NumeroCompte == vm.NumeroCompte.Trim());
            if (compte == null)
            {
                ModelState.AddModelError(string.Empty, "Compte d'agence introuvable.");
                return View("Guichet", vm);
            }

            // ⚡ APPEL DU MOTEUR TRANSACTIONNEL ACID : Délégation au service unifié en partie double [1f2]
            var resultat = await _transactionService.ExecuterOperationGuichetAsync(
                vm.NumeroCompte, 
                vm.Montant, 
                "Depot", 
                vm.Remarque ?? "Dépôt d'espèces au guichet d'agence"
            );

            if (!resultat.Succeeded)
            {
                ModelState.AddModelError(string.Empty, resultat.Message);
                return View("Guichet", vm);
            }

            // Récupération de l'opération qui vient d'être créée pour l'aiguillage du reçu de caisse
            var operation = await _context.Operations
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync(o => o.CompteId == compte.Id);

            // 🚨 INTERCEPTEUR AUTOMATIQUE COMPLIANCE : Application stricte du seuil AML à 5 000 000 BIF [1f2]
            if (vm.Montant >= 5000000m && operation != null)
            {
                var alerte = new AlerteAML
                {
                    ClientId = compte.ClientId,
                    OperationId = operation.Id,
                    TypeAlerte = "Dépôt d'espèces important",
                    Niveau = "Eleve", // Niveau ∈ {Faible, Moyen, Eleve, Critique}
                    Description = $"Versement de liquidités d'un montant de {string.Format("{0:N2}", vm.Montant)} BIF effectué sur le compte {compte.NumeroCompte}.",
                    Statut = "Ouverte"
                };
                
                _context.AlertesAML.Add(alerte);
                await _context.SaveChangesAsync();

                TempData["Warning"] = $"Versement opéré. Attention : En raison du montant élevé (>= 5 000 000 BIF), un dossier d'alerte AML automatique a été transmis au pôle de conformité.";
            }
            else
            {
                TempData["Success"] = $"Le dépôt de {string.Format("{0:N2}", vm.Montant)} BIF a été crédité avec succès.";
            }

            // 🚀 REDIRECTION AUTOMATIQUE VERS LE REÇU DE CAISSE (Chapitre 15, page 297) [1f2]
            if (operation != null)
            {
                return RedirectToAction("Recu", new { id = operation.Id });
            }

            return RedirectToAction("Details", "Comptes", new { id = compte.Id });
        }

        // 👁️ GET: /Transactions/Recu/5 (AFFICHAGE RÉGLEMENTAIRE DU BORDEREAU COMPTOIR) [1f2]
        [HttpGet]
        public async Task<IActionResult> Recu(int id)
        {
            var operation = await _context.Operations
                .Include(o => o.Compte)
                .ThenInclude(c => c!.Client)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (operation == null) return NotFound("Bordereau introuvable.");

            return View(operation);
        }
    }
}
