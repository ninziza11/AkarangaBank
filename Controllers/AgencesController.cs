using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;
using AkarangaBank.Web.ViewModels;

namespace AkarangaBank.Web.Controllers;

// RBAC : L'accès complet au référentiel des agences est exclusivement réservé aux administrateurs [1f2]
[Authorize(Roles = "admin")]
public class AgencesController : Controller
{
    private readonly AkarangaBankContext _context;

    public AgencesController(AkarangaBankContext context)
    {
        _context = context;
    }

    // GET: /Agences
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // 1. Extraction de la liste brute des agences depuis SQL Server
        var agences = await _context.Agences
            .AsNoTracking()
            .OrderBy(a => a.Code)
            .ToListAsync();
            
        // 2. Encapsulation dans le conteneur de liste requis par l'interface graphique [1f2]
        var viewModel = new AgenceListViewModel
        {
            Agences = agences
        };
            
        return View(viewModel);
    }

    // GET: /Agences/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var agence = await _context.Agences
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agence == null)
        {
            return NotFound();
        }

        // 🛠️ RÉALIGNEMENT : Utilisation directe de l'entité brute stable
        return View(agence);
    }

    // GET: /Agences/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Agence());
    }

    // POST: /Agences/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Agence agence)
    {
        if (!ModelState.IsValid)
        {
            return View(agence);
        }

        // Vérifier l'unicité réglementaire du code agence
        bool codeExiste = await _context.Agences.AnyAsync(a => a.Code == agence.Code.Trim());
        if (codeExiste)
        {
            ModelState.AddModelError("Code", "Ce code agence est déjà attribué dans le référentiel.");
            return View(agence);
        }

        agence.DateCreation = DateTime.UtcNow;
        _context.Agences.Add(agence);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"L'agence {agence.Nom} ({agence.Code}) a été créée avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Agences/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var agence = await _context.Agences.FindAsync(id);
        if (agence == null)
        {
            return NotFound();
        }

        return View(agence);
    }

    // POST: /Agences/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Agence agence)
    {
        if (id != agence.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(agence);
        }

        var agenceDb = await _context.Agences.FindAsync(id);
        if (agenceDb == null)
        {
            return NotFound();
        }

        // Mettre à jour uniquement les propriétés modifiables autorisées
        agenceDb.Nom = agence.Nom;
        agenceDb.Ville = agence.Ville;
        agenceDb.EstSiege = agence.EstSiege;
        agenceDb.DateModification = DateTime.UtcNow;

        _context.Agences.Update(agenceDb);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Les modifications de l'agence {agenceDb.Nom} ont été enregistrées.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Agences/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var agence = await _context.Agences
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agence == null)
        {
            return NotFound();
        }

        // Sécurité LAB/CFT : Empêcher la suppression d'une agence si des comptes y sont rattachés
        bool aDesComptes = await _context.Comptes.AnyAsync(c => c.AgenceId == id);
        if (aDesComptes)
        {
            TempData["Error"] = $"Suppression interdite : L'agence {agence.Nom} possède des comptes clients actifs.";
            return RedirectToAction(nameof(Index));
        }

        return View(agence);
    }

    // POST: /Agences/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var agence = await _context.Agences.FindAsync(id);
        if (agence == null)
        {
            return NotFound();
        }

        _context.Agences.Remove(agence);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"L'agence {agence.Nom} a été retirée du référentiel.";
        return RedirectToAction(nameof(Index));
    }
}
