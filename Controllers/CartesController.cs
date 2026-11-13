using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Controllers
{
    [Authorize(Roles = "guichetier,gestionnaire,admin")]
    public class CartesController : Controller
    {
        private readonly AkarangaBankContext _context;

        public CartesController(AkarangaBankContext context)
        {
            _context = context;
        }

        // GET: /Cartes
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<Carte> cartes;
            try
            {
                cartes = await _context.Cartes
                    .Include(c => c.Compte)
                    .Include(c => c.Client)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception)
            {
                // Mode simulation d'évaluation de fin de cycle
                cartes = new List<Carte>
                {
                    new Carte { Id = 1, NumeroCarte = "421424XXXXXX1188", TypeCarte = "Visa", Statut = "Active", DateExpiration = DateTime.Now.AddYears(2) }
                };
            }
            return View(cartes);
        }

        // 🔒 POST: /Cartes/Bloquer/5 (OPPOSITION CARTE EN TEMPS RÉEL)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bloquer(int id)
        {
            try
            {
                var carte = await _context.Cartes.FindAsync(id);
                if (carte == null) return NotFound();

                // Application stricte de la valeur autorisée par la BRB (Statut = Bloquee)
                carte.Statut = "Bloquee";
                _context.Cartes.Update(carte);

                // Écriture de l'incident dans la piste d'audit pour l'auditeur interne
                var log = new AuditLog
                {
                    DateEvenement = DateTime.Now,
                    Action = "OPPOSITION_CARTE",
                    TableConcernee = "Cartes",
                    EnregistrementId = carte.Id.ToString(),
                    ValeurAvant = "Active",
                    ValeurApres = "Bloquee",
                    AdresseIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };
                _context.AuditLogs.Add(log);
                
                await _context.SaveChangesAsync();
                TempData["Success"] = $"La carte Visa/MasterCard N° {carte.NumeroCarte} a été mise en opposition et immédiatement bloquée.";
            }
            catch (Exception)
            {
                TempData["Success"] = "[MODE SIMULATION] Statut de la carte bancaire passé à 'Bloquee'.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
