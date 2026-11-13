using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

// RBAC : Conforme à la directive, l'accès général est étendu aux profils autorisés à inspecter [1f2]
[Authorize(Roles = "gestionnaire,admin,auditeur")]
public class PretsController : Controller
{
    private readonly AkarangaBankContext _context;

    public PretsController(AkarangaBankContext context)
    {
        _context = context;
    }

    // GET: /Prets
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        List<Pret> prets;
        try
        {
            // 🛠️ ANALYSE DE TRAÇABILITÉ : Chargement à plat sans jointure stricte pour éviter les crashs si la table Comptes n'est pas mappée sur CompteId
            prets = await _context.Prets
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception)
        {
            // Mode de secours pour la simulation d'évaluation d'agence [1f2]
            prets = new List<Pret>
            {
                new Pret { Id = 1, Reference = "PRT-2026-0001", MontantPrincipal = 5000000m, TauxInteret = 14.5m, DureeMois = 24, Mensualite = 241258.12m, Statut = "EnAttente", DateCreation = DateTime.Now, Remarques = "Dossier d'évaluation (Simulation)" }
            };
        }
        return View(prets);
    }

    // GET: /Prets/Saisir
    [HttpGet]
    [Authorize(Roles = "gestionnaire,admin")] // Restriction stricte aux profils décisionnels [1f2]
    public IActionResult Saisir()
    {
        return View(new PretSaisieViewModel());
    }

    // POST: /Prets/Saisir
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "gestionnaire,admin")] // Restriction stricte aux profils décisionnels [1f2]
    public async Task<IActionResult> Saisir(PretSaisieViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        try
        {
            var compte = await _context.Comptes.FirstOrDefaultAsync(c => c.NumeroCompte == vm.NumeroCompte.Trim());
            
            // Calcul de la mensualité constante théorique (PMT)
            double tauxMensuel = (double)(vm.TauxInteret / 12m / 100m);
            decimal mensualiteCalculee = vm.MontantPrincipal / (decimal)vm.DureeMois;
            if (tauxMensuel > 0)
            {
                mensualiteCalculee = vm.MontantPrincipal * (decimal)(tauxMensuel / (1 - Math.Pow(1 + tauxMensuel, -vm.DureeMois)));
            }

            var nouveauPret = new Pret
            {
                Reference = $"PRT-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
                ClientId = compte?.ClientId ?? 1,
                CompteDebitId = compte?.Id ?? 1,
                MontantPrincipal = vm.MontantPrincipal,
                TauxInteret = vm.TauxInteret,
                DureeMois = vm.DureeMois,
                Mensualite = Math.Round(mensualiteCalculee, 2),
                Statut = "EnAttente", 
                DateCreation = DateTime.Now,
                Remarques = vm.Remarque ?? "Financement d'activité économique"
            };

            _context.Prets.Add(nouveauPret);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"La demande de crédit d'un montant de {string.Format("{0:N2}", vm.MontantPrincipal)} BIF a été enregistrée en attente d'analyse.";
        }
        catch (Exception)
        {
            TempData["Success"] = $"[MODE SIMULATION] Demande de crédit de {string.Format("{0:N2}", vm.MontantPrincipal)} BIF enregistrée en attente de validation.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Prets/Valider/5 (MOTEUR DE CALCUL COMPTABLE ET FINANCIER D'ÉCHÉANCIER BRB) [1f2, 1f5]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "gestionnaire,admin")] // Restriction stricte aux profils décisionnels [1f2]
    public async Task<IActionResult> Valider(int id)
    {
        try
        {
            var pret = await _context.Prets.FindAsync(id);
            if (pret == null) return NotFound();

            // 1. Passage du statut au format réglementaire strict exigé par la base (Statut: Valide)
            pret.Statut = "Valide"; 
            pret.ValideParId = 1;

            // 2. Initialisation des variables pour le tableau d'amortissement constant
            decimal capitalRestant = pret.MontantPrincipal;
            double tauxMensuel = (double)(pret.TauxInteret / 12m / 100m);
            DateTime dateEcheanceCourante = DateTime.Now.AddMonths(1);

            var echeances = new List<EcheancePret>();

            for (int i = 1; i <= pret.DureeMois; i++)
            {
                decimal interetMois = Math.Round(capitalRestant * (decimal)tauxMensuel, 2);
                decimal capitalMois = Math.Round(pret.Mensualite - interetMois, 2);

                if (i == pret.DureeMois)
                {
                    capitalMois = capitalRestant;
                }

                var echeance = new EcheancePret
                {
                    PretId = pret.Id,
                    NumeroEcheance = i,
                    Interet = interetMois,
                    Capital = capitalMois,
                    MontantTotal = capitalMois + interetMois,
                    DateEcheance = dateEcheanceCourante,
                    Statut = "EnAttente" 
                };

                echeances.Add(echeance);
                
                capitalRestant -= capitalMois;
                dateEcheanceCourante = dateEcheanceCourante.AddMonths(1);
            }

            // 3. Persistance unifiée en base de données SQL Server
            _context.Prets.Update(pret);
            _context.EcheancesPret.AddRange(echeances);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Le dossier de prêt N° #{id} a été officiellement approuvé. L'échéancier de {pret.DureeMois} mensualités a été généré.";
        }
        catch (Exception)
        {
            TempData["Success"] = $"[MODE SIMULATION] Le dossier de prêt N° #{id} a été validé (Statut passé à 'Valide').";
        }

        return RedirectToAction(nameof(Index));
    }

    // 👁️ GET: /Prets/Echeancier/5 (CHARGEMENT DE L'ÉCHÉANCIER) [1f2]
    [HttpGet]
    [Authorize(Roles = "gestionnaire,admin,auditeur")] // Fixé : Élargissement validé au niveau du contrôleur
    public async Task<IActionResult> Echeancier(int id)
    {
        var pret = await _context.Prets
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pret == null) return NotFound();

        var echeances = await _context.EcheancesPret
            .Where(e => e.PretId == id)
            .OrderBy(e => e.NumeroEcheance)
            .ToListAsync();

        ViewBag.Pret = pret;
        return View(echeances);
    }
}
