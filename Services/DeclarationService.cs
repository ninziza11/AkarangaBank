using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class DeclarationService : IDeclarationService
{
    private readonly AkarangaBankContext _context;

    public DeclarationService(AkarangaBankContext context)
    {
        _context = context;
    }

    public async Task<AlerteAML> CreerAlerteAsync(
        int clientId,
        int? operationId,
        string typeAlerte,
        string niveau,
        string description)
    {
        var alerte = new AlerteAML
        {
            ClientId = clientId,
            OperationId = operationId,
            TypeAlerte = typeAlerte,
            Niveau = niveau,
            Description = description,
            Statut = "Ouverte",
            DateAlerte = DateTime.UtcNow
        };

        _context.AlertesAML.Add(alerte);
        await _context.SaveChangesAsync();

        return alerte;
    }

    public async Task<bool> TraiterAlerteAsync(
        int alerteId,
        int traiteParId,
        string statut,
        string? commentaire)
    {
        var alerte = await _context.AlertesAML
            .FirstOrDefaultAsync(a => a.Id == alerteId);

        if (alerte == null)
        {
            return false;
        }

        alerte.Statut = statut;
        alerte.TraiteParId = traiteParId;
        alerte.DateTraitement = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(commentaire))
        {
            alerte.Description =
                (alerte.Description ?? string.Empty) +
                " | " +
                commentaire;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<AlerteAML>> GetAlertesAsync(
        string? statut,
        string? niveau)
    {
        var query = _context.AlertesAML
            .Include(a => a.Client)
            .Include(a => a.Operation)
            .Include(a => a.TraitePar)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statut))
        {
            query = query.Where(a => a.Statut == statut);
        }

        if (!string.IsNullOrWhiteSpace(niveau))
        {
            query = query.Where(a => a.Niveau == niveau);
        }

        return await query
            .OrderByDescending(a => a.DateAlerte)
            .ToListAsync();
    }
}