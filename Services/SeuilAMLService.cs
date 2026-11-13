using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class SeuilAMLService : ISeuilAMLService
{
    private readonly AkarangaBankContext _context;

    public SeuilAMLService(AkarangaBankContext context)
    {
        _context = context;
    }

    public async Task<SeuilAML?> GetSeuilAsync(
        string code,
        string typeOperation)
    {
        return await _context.SeuilsAML
            .AsNoTracking()
            .Where(s => s.Code == code && s.Actif)
            .Where(s =>
                s.TypeOperation == typeOperation ||
                s.TypeOperation == "Toutes")
            .OrderByDescending(s =>
                s.TypeOperation == typeOperation)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<SeuilAML>> GetAllAsync()
    {
        return await _context.SeuilsAML
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .ThenBy(s => s.TypeOperation)
            .ToListAsync();
    }

    public async Task<bool> MettreAJourAsync(
        int seuilId,
        decimal nouveauMontant,
        int utilisateurId)
    {
        if (nouveauMontant <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nouveauMontant),
                "Le montant du seuil doit être strictement positif.");
        }

        var seuil = await _context.SeuilsAML.FindAsync(seuilId);

        if (seuil is null)
        {
            return false;
        }

        var ancienMontant = seuil.Montant;
        var dateModification = DateTime.UtcNow;

        seuil.Montant = nouveauMontant;
        seuil.DateModification = dateModification;

        var audit = new AuditLog
        {
            DateEvenement = dateModification,

            // Le modèle AuditLog fourni utilise une propriété string.
            Utilisateur = utilisateurId.ToString(
                CultureInfo.InvariantCulture),

            Action = "ModificationSeuilAML",
            TableConcernee = "SeuilsAML",

            EnregistrementId = seuilId.ToString(
                CultureInfo.InvariantCulture),

            ValeurAvant = ancienMontant.ToString(
                "F2",
                CultureInfo.InvariantCulture),

            ValeurApres = nouveauMontant.ToString(
                "F2",
                CultureInfo.InvariantCulture)
        };

        _context.AuditLogs.Add(audit);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<decimal> GetSeuilVigilanceAsync()
    {
        var seuil = await GetSeuilAsync(
            "VIGILANCE",
            "Toutes");

        return seuil?.Montant ?? 5_000_000m;
    }

    public async Task<decimal> GetSeuilDeclarationAsync()
    {
        var seuil = await GetSeuilAsync(
            "DECLARATION",
            "Toutes");

        return seuil?.Montant ?? 10_000_000m;
    }

    public async Task<decimal> GetSeuilBlocageAsync()
    {
        var seuil = await GetSeuilAsync(
            "BLOCAGE",
            "Toutes");

        return seuil?.Montant ?? 50_000_000m;
    }
}