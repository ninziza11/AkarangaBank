using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.Services;
namespace AkarangaBank.Web.Controllers;
[Authorize(Policy = "AdminOnly")]
public class ConformiteController : Controller {
 private readonly IDeclarationService _declarationService;
 private readonly IConformiteService _conformiteService;
 private readonly ICurrentUserService _currentUser;
 public ConformiteController(
 IDeclarationService declarationService,
 IConformiteService conformiteService,
 ICurrentUserService currentUser) {
 _declarationService = declarationService;
 _conformiteService = conformiteService;
 _currentUser = currentUser;
 }
 public async Task<IActionResult> Alertes(string? statut, string? niveau) {
 var alertes = await _declarationService.GetAlertesAsync(statut, niveau);
 ViewBag.Statut = statut;
 ViewBag.Niveau = niveau;
 return View(alertes);
 }
 public async Task<IActionResult> Details(int id) {
 var alertes = await _declarationService.GetAlertesAsync(null, null);
 var alerte = alertes.FirstOrDefault(a => a.Id == id);
 if (alerte is null) return NotFound();
 return View(alerte);
 }
 [HttpPost]
 [ValidateAntiForgeryToken]
 public async Task<IActionResult> Traiter(int id, string statut, string? commentaire) {
 var userId = await _currentUser.GetUserIdAsync();
 var ok = await _declarationService.TraiterAlerteAsync(id, userId, statut,commentaire);
 if (!ok) return NotFound();
 TempData["Success"] = "Alerte traitée.";
 return RedirectToAction(nameof(Alertes));
 }
 public async Task<IActionResult> RisqueClient(int id)
 {
 var risque = await _conformiteService.EvaluerRisqueClientAsync(id);
 ViewBag.ClientId = id;
 ViewBag.Risque = risque;
 return View();
 }
}
