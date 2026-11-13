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
    [Authorize(Roles = "auditeur,admin,gestionnaire")]
    public class NotificationsController : Controller
    {
        private readonly AkarangaBankContext _context;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            AkarangaBankContext context,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Notifications
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var alertes = new List<AlerteAML>();

            try
            {
                alertes = await _context.AlertesAML
                    .AsNoTracking()
                    .OrderByDescending(a => a.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur lors du chargement des alertes AML.");

                ViewData["ErreurChargement"] =
                    "Impossible de charger les alertes depuis la base " +
                    "de données. Consultez les journaux de l’application.";
            }

            return View(alertes);
        }
    }
}