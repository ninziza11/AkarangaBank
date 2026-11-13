using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class CompensationService : ICompensationService
{
    private readonly AkarangaBankContext _context;

    public CompensationService(AkarangaBankContext context)
    {
        _context = context;
    }

    public async Task<CompensationAgence> CalculerCompensationAsync(
        int agenceId,
        DateTime date)
    {
        var debut = date.Date;
        var fin = debut.AddDays(1);

        var virements = await _context.VirementsInterAgences
            .Where(v => v.DateCreation >= debut && v.DateCreation < fin)
            .Where(v =>
                v.AgenceSourceId == agenceId ||
                v.AgenceDestinationId == agenceId)
            .Where(v => v.Statut == "EnCompensation")
            .ToListAsync();

        var totalCredits = virements
            .Where(v => v.AgenceDestinationId == agenceId)
            .Sum(v => v.Montant);

        var totalDebits = virements
            .Where(v => v.AgenceSourceId == agenceId)
            .Sum(v => v.Montant);

        var numeroCompteCompensation = $"9000-{agenceId:D4}";

        var compteCompensation = await _context.Comptes
            .FirstOrDefaultAsync(c =>
                c.NumeroCompte == numeroCompteCompensation)
            ?? throw new InvalidOperationException(
                $"Compte de compensation introuvable pour l'agence {agenceId}.");

        var compensation = new CompensationAgence
        {
            Reference = $"COMP-{date:yyyyMMdd}-{agenceId:D4}",
            AgenceId = agenceId,
            CompteCompensationId = compteCompensation.Id,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            Solde = totalCredits - totalDebits,
            DateCompensation = date,
            Statut = "EnCours"
        };

        _context.CompensationsAgences.Add(compensation);
        await _context.SaveChangesAsync();

        return compensation;
    }

    public async Task<bool> ReglerCompensationAsync(
        int compensationId,
        int valideParId)
    {
        var compensation = await _context.CompensationsAgences
            .Include(c => c.CompteCompensation)
            .FirstOrDefaultAsync(c => c.Id == compensationId);

        if (compensation is null)
        {
            return false;
        }

        if (compensation.Statut == "Regle")
        {
            return true;
        }

        if (compensation.CompteCompensation is null)
        {
            throw new InvalidOperationException(
                "Le compte de compensation associé est introuvable.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            if (compensation.Solde != 0)
            {
                var operationId =
                    await GetOrCreateOperationReglementAsync(
                        compensation,
                        valideParId);

                var ecriture = new Ecriture
                {
                    OperationId = operationId,
                    CompteId = compensation.CompteCompensationId,
                    Sens = compensation.Solde > 0 ? "Credit" : "Debit",
                    Montant = Math.Abs(compensation.Solde),
                    Devise = compensation.CompteCompensation.Devise,
                    DateEcriture = DateTime.UtcNow,
                    Libelle =
                        $"Règlement compensation {compensation.Reference}"
                };

                _context.Ecritures.Add(ecriture);
            }

            compensation.Statut = "Regle";
            compensation.DateCompensation = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<CompensationAgence>> GetCompensationsAsync(
        int? agenceId,
        DateTime? date)
    {
        var query = _context.CompensationsAgences
            .Include(c => c.Agence)
            .Include(c => c.CompteCompensation)
            .AsNoTracking()
            .AsQueryable();

        if (agenceId.HasValue)
        {
            query = query.Where(c => c.AgenceId == agenceId.Value);
        }

        if (date.HasValue)
        {
            var debut = date.Value.Date;
            var fin = debut.AddDays(1);

            query = query.Where(c =>
                c.DateCompensation >= debut &&
                c.DateCompensation < fin);
        }

        return await query
            .OrderByDescending(c => c.DateCompensation)
            .ToListAsync();
    }

    public async Task<bool> VerifierEquilibreCompensationsAsync(
        DateTime date)
    {
        var debut = date.Date;
        var fin = debut.AddDays(1);

        var compensations = await _context.CompensationsAgences
            .AsNoTracking()
            .Where(c =>
                c.DateCompensation >= debut &&
                c.DateCompensation < fin)
            .ToListAsync();

        var totalDebits = compensations.Sum(c => c.TotalDebits);
        var totalCredits = compensations.Sum(c => c.TotalCredits);

        return totalDebits == totalCredits;
    }

    private async Task<int> GetOrCreateOperationReglementAsync(
        CompensationAgence compensation,
        int valideParId)
    {
        var operation = new Operation
        {
            Reference = $"REG-{compensation.Reference}",
            TypeOperation = "ReglementCompensation",
            CompteId = compensation.CompteCompensationId,
            UtilisateurId = valideParId,
            AgenceId = compensation.AgenceId,
            Montant = Math.Abs(compensation.Solde),
            Devise = compensation.CompteCompensation.Devise,
            Statut = "Valide",
            DateOperation = DateTime.UtcNow,
            DateValidation = DateTime.UtcNow,
            Commentaire =
                $"Règlement compensation {compensation.Reference}"
        };

        _context.Operations.Add(operation);
        await _context.SaveChangesAsync();

        return operation.Id;
    }
}