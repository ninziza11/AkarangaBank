using Microsoft.EntityFrameworkCore;
using AkarangaBank.Web.Data;
using AkarangaBank.Web.Models;

namespace AkarangaBank.Web.Services;

public class ConformiteService : IConformiteService
{
    private readonly AkarangaBankContext _context;

    private const decimal SeuilVigilanceRenforcee = 5_000_000m;
    private const decimal SeuilDeclarationEspece = 10_000_000m;

    public ConformiteService(AkarangaBankContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetSeuilAMLAsync()
    {
        var parametre = await _context.Parametres
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Cle == "SEUIL_AML");

        if (parametre == null)
        {
            return SeuilVigilanceRenforcee;
        }

        return decimal.TryParse(parametre.Valeur, out var seuil)
            ? seuil
            : SeuilVigilanceRenforcee;
    }

    public async Task<ResultatConformite> AnalyserTransactionAsync(
        int compteId,
        decimal montant,
        string typeOperation)
    {
        var compte = await _context.Comptes
            .FirstOrDefaultAsync(c => c.Id == compteId)
            ?? throw new InvalidOperationException("Compte introuvable.");

        var seuilAML = await GetSeuilAMLAsync();

        var resultat = new ResultatConformite
        {
            Montant = montant,
            CompteId = compteId,
            DateAnalyse = DateTime.UtcNow
        };

        if (typeOperation is "Depot" or "Retrait" &&
            montant >= SeuilDeclarationEspece)
        {
            resultat.NecessiteDeclaration = true;
            resultat.Motif =
                $"Transaction en espèces ≥ {SeuilDeclarationEspece:N0} BIF. " +
                "Déclaration obligatoire à la CNRF.";
            resultat.Niveau = NiveauAlerte.Eleve;
        }

        if (montant >= seuilAML)
        {
            resultat.NecessiteVigilanceRenforcee = true;

            resultat.Motif ??=
                $"Montant ≥ {seuilAML:N0} BIF (seuil AML). " +
                "Vérification de l'origine des fonds requise.";

            if (resultat.Niveau != NiveauAlerte.Eleve &&
                resultat.Niveau != NiveauAlerte.Critique)
            {
                resultat.Niveau = NiveauAlerte.Moyen;
            }
        }

        if (typeOperation is "Retrait" or "Virement" &&
            compte.Solde < montant)
        {
            resultat.EstBloquee = true;
            resultat.Motif = "Solde insuffisant.";
            resultat.Niveau = NiveauAlerte.Critique;
        }

        if (compte.Statut != "Actif")
        {
            resultat.EstBloquee = true;
            resultat.Motif =
                $"Compte {compte.Statut}. Opération impossible.";
            resultat.Niveau = NiveauAlerte.Critique;
        }

        return resultat;
    }

    public async Task<bool> DetecterFractionnementAsync(
        int compteId,
        decimal montant,
        DateTime date)
    {
        var seuilAML = await GetSeuilAMLAsync();

        var debut = date.AddHours(-24);

        var totalRecent = await _context.Ecritures
            .Where(e => e.DateEcriture >= debut && e.DateEcriture <= date)
            .Where(e => e.CompteId == compteId)
            .Where(e => e.Montant < seuilAML)
            .SumAsync(e => (decimal?)e.Montant) ?? 0m;

        return totalRecent + montant >= seuilAML &&
               montant < seuilAML;
    }

    public async Task<NiveauRisqueClient> EvaluerRisqueClientAsync(
        int clientId)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (client == null)
        {
            return NiveauRisqueClient.Inconnu;
        }

        var score = 0;

        var nombreComptes = await _context.Comptes
            .CountAsync(c => c.ClientId == clientId);

        if (nombreComptes > 3)
        {
            score += 2;
        }

        var aTransactionsElevees = await _context.Ecritures
            .Join(
                _context.Comptes,
                ecriture => ecriture.CompteId,
                compte => compte.Id,
                (ecriture, compte) => new { ecriture, compte })
            .AnyAsync(x =>
                x.compte.ClientId == clientId &&
                x.ecriture.Montant >= SeuilVigilanceRenforcee);

        if (aTransactionsElevees)
        {
            score += 3;
        }

        var aAlertes = await _context.AlertesAML.AnyAsync(a => a.ClientId == clientId);

        if (aAlertes)
        {
            score += 6;
        }

        return score switch
        {
            >= 6 => NiveauRisqueClient.Eleve,
            >= 3 => NiveauRisqueClient.Moyen,
            _ => NiveauRisqueClient.Faible
        };
    }
}