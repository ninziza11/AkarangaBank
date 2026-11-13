using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;

namespace AkarangaBank.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AkarangaBankContext _context;

        public HomeController(AkarangaBankContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalClients = 0;
            ViewBag.TotalComptes = 0;
            ViewBag.VolumeDepots = 0m;
            ViewBag.AlertesAmlEnAttente = 0;
            ViewBag.TotalChequiersOpposition = 0;

            try
            {
                ViewBag.TotalClients = await _context.Clients.CountAsync();
                ViewBag.TotalComptes = await _context.Comptes.CountAsync();
                ViewBag.VolumeDepots = await _context.Comptes.SumAsync(c => c.Solde);
            }
            catch (Exception)
            {
                // Protection si une table du référentiel est absente.
            }

            try
            {
                ViewBag.AlertesAmlEnAttente = await _context.AlertesAML
                    .CountAsync(a =>
                        a.Statut == "EnAttente" ||
                        a.Statut == "Commande");
            }
            catch (Exception)
            {
                // Protection si la table AlertesAML est absente.
            }

            ViewBag.TotalChequiersOpposition = 0;

            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}