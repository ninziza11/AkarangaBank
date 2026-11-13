using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers
{
    [Authorize(Roles = "gestionnaire,admin")]
    public class ChequiersController : Controller
    {
        private readonly AkarangaBankContext _context;

        public ChequiersController(AkarangaBankContext context)
        {
            _context = context;
        }

        // GET: /Chequiers
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var chequiers = await _context.Chequiers
                .Include(c => c.Compte)
                .ThenInclude(compte => compte!.Client)
                .AsNoTracking()
                .ToListAsync();

            var viewModel = new ChequierSuiviViewModel
            {
                Chequiers = chequiers,
                FiltreStatut = null
            };

            return View(viewModel);
        }

        // GET: /Chequiers/Commander?compteId=5
        [HttpGet]
        public async Task<IActionResult> Commander(int compteId)
        {
            var compte = await _context.Comptes
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == compteId);

            if (compte == null)
            {
                return NotFound();
            }

            ViewBag.CompteInfo =
                $"{compte.NumeroCompte} - " +
                $"{compte.Client?.Nom?.ToUpper()}";

            var chequier = new Chequier
            {
                CompteId = compteId,
                NombreFeuillets = 25,
                NumeroDebut = new Random()
                    .Next(100000, 499999)
                    .ToString()
            };

            return View(chequier);
        }

        // POST: /Chequiers/Commander
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Commander(Chequier chequier)
        {
            if (!ModelState.IsValid)
            {
                await ChargerCompteInfoAsync(chequier.CompteId);
                return View(chequier);
            }

            if (!int.TryParse(chequier.NumeroDebut, out var debut))
            {
                ModelState.AddModelError(
                    nameof(Chequier.NumeroDebut),
                    "Le numéro de début doit être un entier valide.");

                await ChargerCompteInfoAsync(chequier.CompteId);
                return View(chequier);
            }

            if (chequier.NombreFeuillets <= 0)
            {
                ModelState.AddModelError(
                    nameof(Chequier.NombreFeuillets),
                    "Le nombre de feuillets doit être supérieur à zéro.");

                await ChargerCompteInfoAsync(chequier.CompteId);
                return View(chequier);
            }

            var compteExiste = await _context.Comptes
                .AnyAsync(c => c.Id == chequier.CompteId);

            if (!compteExiste)
            {
                ModelState.AddModelError(
                    nameof(Chequier.CompteId),
                    "Le compte sélectionné est introuvable.");

                return View(chequier);
            }

            var numeroFin = (long)debut + chequier.NombreFeuillets - 1;

            if (numeroFin > int.MaxValue)
            {
                ModelState.AddModelError(
                    nameof(Chequier.NombreFeuillets),
                    "La plage de numéros demandée est trop grande.");

                await ChargerCompteInfoAsync(chequier.CompteId);
                return View(chequier);
            }

            chequier.NumeroFin = numeroFin.ToString();
            chequier.Statut = "EnAttente";
            chequier.DateDemande = DateTime.Now;

            _context.Chequiers.Add(chequier);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "La commande n’a pas pu être enregistrée. " +
                    "Consultez les journaux de l’application.");

                await ChargerCompteInfoAsync(chequier.CompteId);
                return View(chequier);
            }

            TempData["Success"] =
                "La commande de chéquier a été enregistrée avec succès.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Chequiers/MettreEnOpposition/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MettreEnOpposition(int id)
        {
            var chequier = await _context.Chequiers
                .Include(c => c.Compte)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chequier == null)
            {
                return NotFound();
            }

            if (chequier.Statut == "Opposition")
            {
                TempData["Error"] =
                    "Ce chéquier est déjà en opposition.";

                return RedirectToAction(nameof(Index));
            }

            var ancienStatut = chequier.Statut;

            chequier.Statut = "Opposition";

            var log = new AuditLog
            {
                DateEvenement = DateTime.Now,
                Action = "MISE_OPPOSITION",
                TableConcernee = "Chequiers",
                EnregistrementId = chequier.Id.ToString(),
                ValeurAvant = ancienStatut,
                ValeurApres = "Opposition",
                AdresseIP =
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "Inconnue",
                UserAgent =
                    Request.Headers["User-Agent"].ToString()
            };

            _context.AuditLogs.Add(log);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "La mise en opposition n’a pas pu être enregistrée. " +
                    "Consultez les journaux de l’application.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                $"Le chéquier (série n° {chequier.NumeroDebut} " +
                $"à {chequier.NumeroFin}) associé au compte " +
                $"{chequier.Compte?.NumeroCompte} " +
                "a été mis en opposition.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ChargerCompteInfoAsync(int compteId)
        {
            var compte = await _context.Comptes
                .Include(c => c.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == compteId);

            if (compte != null)
            {
                ViewBag.CompteInfo =
                    $"{compte.NumeroCompte} - " +
                    $"{compte.Client?.Nom?.ToUpper()}";
            }
        }
    }
}