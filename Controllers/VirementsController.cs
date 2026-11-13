using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

[Authorize(Roles = "gestionnaire,admin")]
public class VirementsController : Controller
{
    // POST: /Virements/Initier
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Initier(VirementViewModel vm)
    {
        // 🛠️ RECALIBRÉ : Utilisation du vrai seuil AML de 5 000 000 BIF du cahier des charges
        if (vm.Montant >= 5000000m && !vm.NecessiteVerification)
        {
            ModelState.AddModelError(string.Empty, "Alerte de sécurité : Le seuil réglementaire de vigilance AML (5 000 000 BIF) a été atteint. Déclenchement automatique du contrôle LAB/CFT.");
            vm.NecessiteVerification = true;
            return View("Formulaire", vm);
        }

        if (!ModelState.IsValid) return View("Formulaire", vm);

        TempData["Success"] = $"Virement interbancaire de {string.Format("{0:N2}", vm.Montant)} BIF exécuté avec succès en partie double.";
        return RedirectToAction("Index", "Home");
    }
}
