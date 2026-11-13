using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Controllers
{
    [Authorize(Roles = "auditeur,admin")]
    public class AuditController : Controller
    {
        private readonly AkarangaBankContext _context;
        private readonly ILogger<AuditController> _logger;

        public AuditController(
            AkarangaBankContext context,
            ILogger<AuditController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Audit
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Journal));
        }

        // GET: /Audit/PisteAudit
        [HttpGet]
        public IActionResult PisteAudit()
        {
            return RedirectToAction(nameof(Journal));
        }

        // GET: /Audit/Journal
        [HttpGet]
        public async Task<IActionResult> Journal()
        {
            try
            {
                var journaux = await _context.AuditLogs
                    .AsNoTracking()
                    .OrderByDescending(a => a.DateEvenement)
                    .ToListAsync();

                return View(journaux);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Impossible de charger le journal d’audit.");

                return AfficherErreurLecture(
                    "Impossible de charger le journal d’audit depuis " +
                    "la base de données. Consultez les journaux " +
                    "de l’application.");
            }
        }

        // GET: /Audit/GrandLivre
        [HttpGet]
        public async Task<IActionResult> GrandLivre()
        {
            try
            {
                var ecritures = await _context.Ecritures
                    .Include(e => e.Compte)
                    .AsNoTracking()
                    .OrderByDescending(e => e.DateEcriture)
                    .ToListAsync();

                return View(ecritures);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Impossible de charger le grand livre.");

                return AfficherErreurLecture(
                    "Impossible de charger les écritures du grand livre. " +
                    "Consultez les journaux de l’application.");
            }
        }

        // GET: /Audit/VerificationPartieDouble
        [HttpGet]
        public async Task<IActionResult> VerificationPartieDouble()
        {
            try
            {
                var ecritures = await _context.Ecritures
                    .AsNoTracking()
                    .Select(e => new
                    {
                        e.OperationId,
                        e.Sens,
                        e.Montant
                    })
                    .ToListAsync();

                if (ecritures.Count == 0)
                {
                    ViewBag.StatutComptable =
                        "NON VÉRIFIABLE : aucune écriture enregistrée.";

                    return View(new List<object>());
                }

                var sensInconnus = ecritures
                    .Where(e =>
                        e.Sens != "Debit" &&
                        e.Sens != "Credit")
                    .ToList();

                if (sensInconnus.Count > 0)
                {
                    _logger.LogWarning(
                        "Vérification interrompue : {Nombre} écriture(s) " +
                        "possèdent un sens différent de Debit ou Credit.",
                        sensInconnus.Count);

                    ViewBag.StatutComptable =
                        "ANOMALIE : certaines écritures possèdent " +
                        "un sens différent de Debit ou Credit. " +
                        "Le contrôle d’équilibre n’est pas concluant.";

                    return View(new List<object>());
                }

                var desequilibres = ecritures
                    .GroupBy(e => e.OperationId)
                    .Select(g => new
                    {
                        OperationId = g.Key,

                        TotalDebit = g
                            .Where(e => e.Sens == "Debit")
                            .Sum(e => e.Montant),

                        TotalCredit = g
                            .Where(e => e.Sens == "Credit")
                            .Sum(e => e.Montant)
                    })
                    .Where(g => g.TotalDebit != g.TotalCredit)
                    .ToList();

                if (desequilibres.Count > 0)
                {
                    ViewBag.StatutComptable =
                        $"ANOMALIE : {desequilibres.Count} opération(s) " +
                        "présentent un écart entre débits et crédits.";

                    return View(desequilibres);
                }

                ViewBag.StatutComptable =
                    "CONTRÔLE RÉUSSI : les totaux débits et crédits " +
                    "sont égaux pour chaque opération enregistrée. " +
                    "Ce contrôle ne vérifie pas l’exhaustivité " +
                    "ni la validité des imputations.";

                return View(new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Échec de la vérification de la partie double.");

                return AfficherErreurLecture(
                    "La vérification de la partie double a échoué. " +
                    "Aucun résultat d’équilibre ne peut être confirmé. " +
                    "Consultez les journaux de l’application.");
            }
        }

        private ContentResult AfficherErreurLecture(string message)
        {
            return new ContentResult
            {
                StatusCode = 500,
                ContentType = "text/plain; charset=utf-8",
                Content = message
            };
        }
    }
}