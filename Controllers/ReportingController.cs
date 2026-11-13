using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.Services;

namespace AkarangaBank.Web.Controllers;

[Authorize(Roles = "auditeur,admin")]
public class ReportingController : Controller
{
    private readonly ReportingService _reportingService;
    private readonly NotificationService _notificationService;

    public ReportingController(ReportingService reportingService, NotificationService notificationService)
    {
        _reportingService = reportingService;
        _notificationService = notificationService;
    }

    // GET: /Reporting
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Reporting/ExporterAml
    [HttpGet]
    public async Task<IActionResult> ExporterAml()
    {
        string csvContenu = await _reportingService.GenererRapportAmlCsvAsync();
        byte[] bytes = Encoding.UTF8.GetBytes(csvContenu);
        string nomFichier = $"AKARANGA_AML_REPORT_{DateTime.Now:yyyyMMdd}.csv";

        // Déclenchement d'une notification de traçabilité pour l'administrateur
        _notificationService.EnvoyerAlerteSecurite("admin@akarangabank.bi", $"Rapport réglementaire LAB/CFT extrait par {User.Identity?.Name}.", "Email");

        return File(bytes, "text/csv", nomFichier);
    }
}
